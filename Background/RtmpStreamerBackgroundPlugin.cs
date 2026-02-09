using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Threading;
using VideoOS.Platform;
using VideoOS.Platform.Background;
using VideoOS.Platform.Messaging;

namespace RtmpStreamerPlugin.Background
{
    public class RtmpStreamerBackgroundPlugin : BackgroundPlugin
    {
        private object _configMessageObj;
        private readonly ConcurrentDictionary<Guid, HelperProcess> _helpers = new ConcurrentDictionary<Guid, HelperProcess>();
        private Timer _monitorTimer;
        private volatile bool _closing;
        private string _helperExePath;
        private string _serverUri;
        private string _milestoneDir;
        private string _lastConfigSnapshot;

        public override Guid Id => RtmpStreamerPluginDefinition.BackgroundPluginId;
        public override string Name => "RTMP Streamer Background";

        public override System.Collections.Generic.List<EnvironmentType> TargetEnvironments
        {
            get { return new System.Collections.Generic.List<EnvironmentType> { EnvironmentType.Service }; }
        }

        public override void Init()
        {
            PluginLog.Info("Background plugin initializing");

            var pluginDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            _helperExePath = Path.Combine(pluginDir, "RtmpStreamerHelper.exe");

            if (!File.Exists(_helperExePath))
            {
                PluginLog.Error($"Helper exe not found: {_helperExePath}");
                return;
            }

            PluginLog.Info($"Helper exe: {_helperExePath}");

            _milestoneDir = Path.GetDirectoryName(typeof(EnvironmentManager).Assembly.Location);
            PluginLog.Info($"Milestone dir: {_milestoneDir}");

            try
            {
                var serverId = EnvironmentManager.Instance.MasterSite.ServerId;
                _serverUri = $"{serverId.ServerScheme}://{serverId.ServerHostname}:{serverId.Serverport}";
                PluginLog.Info($"Management server URI: {_serverUri}");
            }
            catch (Exception ex)
            {
                PluginLog.Error($"Failed to determine management server URI: {ex.Message}", ex);
                return;
            }

            _configMessageObj = EnvironmentManager.Instance.RegisterReceiver(
                OnConfigurationChanged,
                new MessageIdAndRelatedKindFilter(
                    MessageId.Server.ConfigurationChangedIndication,
                    RtmpStreamerPluginDefinition.PluginKindId));

            LoadAndStartStreams();

            _monitorTimer = new Timer(MonitorHelpers, null, TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(10));
        }

        public override void Close()
        {
            PluginLog.Info("Background plugin closing, stopping all helpers");
            _closing = true;

            _monitorTimer?.Dispose();
            _monitorTimer = null;

            if (_configMessageObj != null)
            {
                EnvironmentManager.Instance.UnRegisterReceiver(_configMessageObj);
                _configMessageObj = null;
            }

            StopAllHelpers();
            WriteAllHelperStatus();
        }

        private void LoadAndStartStreams()
        {
            try
            {
                var items = Configuration.Instance.GetItemConfigurations(
                    RtmpStreamerPluginDefinition.PluginId, null, RtmpStreamerPluginDefinition.PluginKindId);

                foreach (var item in items)
                {
                    // Each item is one stream with its own properties
                    var enabled = !item.Properties.ContainsKey("Enabled") || item.Properties["Enabled"] != "No";
                    if (!enabled) continue;

                    if (!item.Properties.ContainsKey("CameraId") || !item.Properties.ContainsKey("RtmpUrl"))
                        continue;

                    if (!Guid.TryParse(item.Properties["CameraId"], out var cameraId) || cameraId == Guid.Empty)
                        continue;

                    var rtmpUrl = item.Properties["RtmpUrl"];
                    if (string.IsNullOrEmpty(rtmpUrl)) continue;

                    var cameraName = item.Properties.ContainsKey("CameraName") ? item.Properties["CameraName"] : "";
                    var allowUntrustedCerts = item.Properties.ContainsKey("AllowUntrustedCerts")
                        && item.Properties["AllowUntrustedCerts"] == "Yes";

                    LaunchHelper(item.FQID.ObjectId, cameraId, cameraName, rtmpUrl, allowUntrustedCerts);
                }

                _lastConfigSnapshot = GetConfigSnapshot();
                PluginLog.Info($"Loaded streams. Active helpers: {_helpers.Count}");
                WriteAllHelperStatus();
            }
            catch (Exception ex)
            {
                PluginLog.Error($"Error loading config: {ex.Message}", ex);
            }
        }

        private void LaunchHelper(Guid itemId, Guid cameraId, string cameraName, string rtmpUrl, bool allowUntrustedCerts = false)
        {
            if (_helpers.TryRemove(itemId, out var existing))
                KillHelper(existing);

            try
            {
                PluginLog.Info($"Launching helper: {cameraName} ({cameraId}) -> {rtmpUrl}");

                var psi = new ProcessStartInfo
                {
                    FileName = _helperExePath,
                    Arguments = $"\"{_serverUri}\" \"{cameraId}\" \"{rtmpUrl}\" \"{_milestoneDir}\" \"{(allowUntrustedCerts ? "true" : "false")}\"",
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    RedirectStandardError = true
                };

                var process = Process.Start(psi);
                if (process == null)
                {
                    PluginLog.Error($"Failed to start helper process for {cameraName}");
                    return;
                }

                var helper = new HelperProcess
                {
                    Process = process,
                    ItemId = itemId,
                    CameraId = cameraId,
                    CameraName = cameraName,
                    RtmpUrl = rtmpUrl,
                    AllowUntrustedCerts = allowUntrustedCerts,
                    RestartCount = 0
                };

                process.ErrorDataReceived += (s, e) =>
                {
                    if (string.IsNullOrEmpty(e.Data)) return;
                    if (e.Data.StartsWith("STATS "))
                    {
                        ParseStats(helper, e.Data);
                        return;
                    }
                    if (e.Data.StartsWith("STATUS "))
                    {
                        helper.LastStatus = e.Data.Substring(7);
                        return;
                    }
                    PluginLog.Info($"[Helper:{cameraName}] {e.Data}");
                };
                process.BeginErrorReadLine();

                _helpers[itemId] = helper;
                PluginLog.Info($"Helper launched: PID={process.Id}, camera={cameraName}");
            }
            catch (Exception ex)
            {
                PluginLog.Error($"Failed to launch helper for {cameraName}: {ex.Message}", ex);
            }
        }

        private void StopAllHelpers()
        {
            foreach (var kvp in _helpers)
                KillHelper(kvp.Value);
            _helpers.Clear();
        }

        private void KillHelper(HelperProcess helper)
        {
            try
            {
                if (helper.Process != null && !helper.Process.HasExited)
                {
                    PluginLog.Info($"Killing helper PID={helper.Process.Id} ({helper.CameraName})");
                    helper.Process.Kill();
                    helper.Process.WaitForExit(3000);
                }
            }
            catch (Exception ex)
            {
                PluginLog.Error($"Error killing helper: {ex.Message}");
            }
            finally
            {
                try { helper.Process?.Dispose(); } catch { }
            }
        }

        private void MonitorHelpers(object state)
        {
            if (_closing) return;

            foreach (var kvp in _helpers)
            {
                var itemId = kvp.Key;
                var helper = kvp.Value;

                try
                {
                    if (helper.Process == null || helper.Process.HasExited)
                    {
                        var exitCode = helper.Process?.ExitCode ?? -1;
                        PluginLog.Info($"Helper died (exit={exitCode}): {helper.CameraName}, restart #{helper.RestartCount + 1}");

                        try { helper.Process?.Dispose(); } catch { }

                        var restartCount = helper.RestartCount + 1;
                        LaunchHelper(itemId, helper.CameraId, helper.CameraName, helper.RtmpUrl, helper.AllowUntrustedCerts);

                        if (_helpers.TryGetValue(itemId, out var newHelper))
                            newHelper.RestartCount = restartCount;
                    }
                }
                catch (Exception ex)
                {
                    PluginLog.Error($"Monitor error for {helper.CameraName}: {ex.Message}");
                }
            }

            // Always write status so STATUS line changes propagate to item properties
            WriteAllHelperStatus();
        }

        private void WriteAllHelperStatus()
        {
            try
            {
                var items = Configuration.Instance.GetItemConfigurations(
                    RtmpStreamerPluginDefinition.PluginId, null, RtmpStreamerPluginDefinition.PluginKindId);

                foreach (var item in items)
                {
                    string status;
                    string restarts = "0";

                    if (_helpers.TryGetValue(item.FQID.ObjectId, out var helper))
                    {
                        bool alive = false;
                        try { alive = helper.Process != null && !helper.Process.HasExited; } catch { }

                        if (!string.IsNullOrEmpty(helper.LastStatus))
                            status = helper.LastStatus;
                        else
                            status = alive ? "Starting" : "Stopped";

                        restarts = helper.RestartCount.ToString();

                        // Only save when something actually changed
                        if (status == helper.LastWrittenStatus && restarts == helper.LastWrittenRestarts)
                            continue;

                        item.Properties["Status"] = status;
                        item.Properties["Restarts"] = restarts;

                        helper.LastWrittenStatus = status;
                        helper.LastWrittenRestarts = restarts;
                    }
                    else
                    {
                        // No helper running - only write "Stopped" once
                        var existing = item.Properties.ContainsKey("Status") ? item.Properties["Status"] : "";
                        if (existing == "Stopped")
                            continue;

                        item.Properties["Status"] = "Stopped";
                        item.Properties["Restarts"] = "0";
                    }

                    Configuration.Instance.SaveItemConfiguration(RtmpStreamerPluginDefinition.PluginId, item);
                }
            }
            catch (Exception ex)
            {
                PluginLog.Error($"Error writing helper status: {ex.Message}");
            }
        }

        private string GetConfigSnapshot()
        {
            try
            {
                var sb = new System.Text.StringBuilder();
                var items = Configuration.Instance.GetItemConfigurations(
                    RtmpStreamerPluginDefinition.PluginId, null, RtmpStreamerPluginDefinition.PluginKindId);

                foreach (var item in items)
                {
                    sb.Append(item.FQID.ObjectId);
                    if (item.Properties.ContainsKey("CameraId")) sb.Append(item.Properties["CameraId"]);
                    if (item.Properties.ContainsKey("RtmpUrl")) sb.Append(item.Properties["RtmpUrl"]);
                    if (item.Properties.ContainsKey("Enabled")) sb.Append(item.Properties["Enabled"]);
                    if (item.Properties.ContainsKey("AllowUntrustedCerts")) sb.Append(item.Properties["AllowUntrustedCerts"]);
                    sb.Append("|");
                }
                return sb.ToString();
            }
            catch { return ""; }
        }

        private object OnConfigurationChanged(Message message, FQID dest, FQID sender)
        {
            var currentConfig = GetConfigSnapshot();
            if (currentConfig == _lastConfigSnapshot)
                return null;

            PluginLog.Info("Stream configuration changed, reloading helpers");
            _lastConfigSnapshot = currentConfig;

            try
            {
                StopAllHelpers();
                LoadAndStartStreams();
            }
            catch (Exception ex)
            {
                PluginLog.Error($"Error reloading config: {ex.Message}", ex);
            }

            return null;
        }

        private static void ParseStats(HelperProcess helper, string line)
        {
            foreach (var part in line.Split(' '))
            {
                var kv = part.Split('=');
                if (kv.Length != 2) continue;

                switch (kv[0])
                {
                    case "frames":
                        long.TryParse(kv[1], out helper.Frames);
                        break;
                    case "fps":
                        double.TryParse(kv[1], System.Globalization.NumberStyles.Float,
                            System.Globalization.CultureInfo.InvariantCulture, out helper.Fps);
                        break;
                    case "bytes":
                        long.TryParse(kv[1], out helper.Bytes);
                        break;
                    case "keyframes":
                        long.TryParse(kv[1], out helper.KeyFrames);
                        break;
                }
            }
        }

        private class HelperProcess
        {
            public Process Process;
            public Guid ItemId;
            public Guid CameraId;
            public string CameraName;
            public string RtmpUrl;
            public bool AllowUntrustedCerts;
            public int RestartCount;
            public volatile string LastStatus;
            public string LastWrittenStatus;
            public string LastWrittenRestarts;
            public long Frames;
            public double Fps;
            public long Bytes;
            public long KeyFrames;
        }
    }
}
