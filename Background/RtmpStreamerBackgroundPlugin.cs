using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Xml.Linq;
using RtmpStreamerPlugin.Streaming;
using VideoOS.Platform;
using VideoOS.Platform.Background;
using VideoOS.Platform.Messaging;

namespace RtmpStreamerPlugin.Background
{
    /// <summary>
    /// Background plugin that runs on the Event Server as a Windows service.
    /// Launches standalone helper processes to stream cameras to RTMP destinations.
    /// Each helper runs in MIP SDK standalone mode where RawLiveSource is supported.
    /// </summary>
    public class RtmpStreamerBackgroundPlugin : BackgroundPlugin
    {
        private object _configMessageObj;
        private readonly ConcurrentDictionary<string, HelperProcess> _helpers = new ConcurrentDictionary<string, HelperProcess>();
        private Timer _monitorTimer;
        private volatile bool _closing;
        private string _helperExePath;
        private string _serverUri;
        private string _milestoneDir;
        private string _lastStreamConfig; // Track actual config to detect real changes vs status writes

        public override Guid Id => RtmpStreamerPluginDefinition.BackgroundPluginId;
        public override string Name => "RTMP Streamer Background";

        public override System.Collections.Generic.List<EnvironmentType> TargetEnvironments
        {
            get { return new System.Collections.Generic.List<EnvironmentType> { EnvironmentType.Service }; }
        }

        public override void Init()
        {
            PluginLog.Info("Background plugin initializing");

            // Find helper exe (same directory as this DLL)
            var pluginDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            _helperExePath = Path.Combine(pluginDir, "RtmpStreamerHelper.exe");

            if (!File.Exists(_helperExePath))
            {
                PluginLog.Error($"Helper exe not found: {_helperExePath}");
                return;
            }

            PluginLog.Info($"Helper exe: {_helperExePath}");

            // Determine Milestone install directory (for helper assembly resolution)
            _milestoneDir = Path.GetDirectoryName(typeof(EnvironmentManager).Assembly.Location);
            PluginLog.Info($"Milestone dir: {_milestoneDir}");

            // Build management server URI from MasterSite
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

            // Register for config changes, filtered to our plugin's Kind
            _configMessageObj = EnvironmentManager.Instance.RegisterReceiver(
                OnConfigurationChanged,
                new MessageIdAndRelatedKindFilter(
                    MessageId.Server.ConfigurationChangedIndication,
                    RtmpStreamerPluginDefinition.PluginKindId));

            // Load and start configured streams
            LoadAndStartStreams();

            // Monitor helper processes every 10 seconds
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
            WriteHelperStatus(); // Write final "Stopped" status
        }

        private void LoadAndStartStreams()
        {
            try
            {
                var configItems = Configuration.Instance.GetItemConfigurations(
                    RtmpStreamerPluginDefinition.PluginId, null, RtmpStreamerPluginDefinition.PluginKindId);

                foreach (var item in configItems)
                {
                    string configXml = null;
                    if (item.Properties.ContainsKey("StreamConfig"))
                        configXml = item.Properties["StreamConfig"];

                    if (!string.IsNullOrEmpty(configXml))
                    {
                        var configs = StreamSessionManager.LoadConfigXml(configXml);
                        foreach (var config in configs)
                        {
                            if (!config.AutoStart || config.CameraId == Guid.Empty || string.IsNullOrEmpty(config.RtmpUrl))
                                continue;

                            LaunchHelper(config.CameraId, config.CameraName, config.RtmpUrl);
                        }
                    }
                }

                // Snapshot the current stream config for change detection
                _lastStreamConfig = GetStreamConfigSnapshot();

                PluginLog.Info($"Loaded streams. Active helpers: {_helpers.Count}");

                // Write initial status
                WriteHelperStatus();
            }
            catch (Exception ex)
            {
                PluginLog.Error($"Error loading config: {ex.Message}", ex);
            }
        }

        private void LaunchHelper(Guid cameraId, string cameraName, string rtmpUrl)
        {
            var key = $"{cameraId}|{rtmpUrl}";

            // Kill existing helper for this key if any
            if (_helpers.TryRemove(key, out var existing))
                KillHelper(existing);

            try
            {
                PluginLog.Info($"Launching helper: {cameraName} ({cameraId}) -> {rtmpUrl}");

                var psi = new ProcessStartInfo
                {
                    FileName = _helperExePath,
                    Arguments = $"\"{_serverUri}\" \"{cameraId}\" \"{rtmpUrl}\" \"{_milestoneDir}\"",
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
                    CameraId = cameraId,
                    CameraName = cameraName,
                    RtmpUrl = rtmpUrl,
                    StartTime = DateTime.UtcNow,
                    RestartCount = 0
                };

                // Read stderr async for logging and stats parsing
                process.ErrorDataReceived += (s, e) =>
                {
                    if (string.IsNullOrEmpty(e.Data)) return;

                    // Parse STATS lines: "STATS frames=1234 fps=25.0 bytes=5678900 keyframes=50"
                    if (e.Data.StartsWith("STATS "))
                    {
                        ParseStats(helper, e.Data);
                        return;
                    }

                    PluginLog.Info($"[Helper:{cameraName}] {e.Data}");
                };
                process.BeginErrorReadLine();

                _helpers[key] = helper;
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
                var key = kvp.Key;
                var helper = kvp.Value;

                try
                {
                    if (helper.Process == null || helper.Process.HasExited)
                    {
                        var exitCode = helper.Process?.ExitCode ?? -1;
                        PluginLog.Info($"Helper died (exit={exitCode}): {helper.CameraName}, restart #{helper.RestartCount + 1}");

                        try { helper.Process?.Dispose(); } catch { }

                        // Relaunch
                        var restartCount = helper.RestartCount + 1;
                        LaunchHelper(helper.CameraId, helper.CameraName, helper.RtmpUrl);

                        // Preserve restart count
                        if (_helpers.TryGetValue(key, out var newHelper))
                            newHelper.RestartCount = restartCount;

                    }
                }
                catch (Exception ex)
                {
                    PluginLog.Error($"Monitor error for {helper.CameraName}: {ex.Message}");
                }
            }

            // Update status periodically so admin can see uptime
            WriteHelperStatus();
        }

        /// <summary>
        /// Write current helper status to config items so the Admin plugin can display it.
        /// </summary>
        private void WriteHelperStatus()
        {
            try
            {
                var configItems = Configuration.Instance.GetItemConfigurations(
                    RtmpStreamerPluginDefinition.PluginId, null, RtmpStreamerPluginDefinition.PluginKindId);

                foreach (var item in configItems)
                {
                    var statusRoot = new XElement("HelperStatus");
                    foreach (var helper in _helpers.Values)
                    {
                        bool alive = false;
                        try { alive = helper.Process != null && !helper.Process.HasExited; } catch { }

                        statusRoot.Add(new XElement("Helper",
                            new XAttribute("CameraId", helper.CameraId),
                            new XAttribute("Status", alive ? "Streaming" : "Stopped"),
                            new XAttribute("PID", helper.Process?.Id ?? 0),
                            new XAttribute("StartTime", helper.StartTime.ToString("o")),
                            new XAttribute("Restarts", helper.RestartCount),
                            new XAttribute("Frames", helper.Frames),
                            new XAttribute("Fps", helper.Fps.ToString("F1", System.Globalization.CultureInfo.InvariantCulture)),
                            new XAttribute("Bytes", helper.Bytes),
                            new XAttribute("KeyFrames", helper.KeyFrames)
                        ));
                    }

                    item.Properties["StreamStatus"] = statusRoot.ToString();
                    Configuration.Instance.SaveItemConfiguration(RtmpStreamerPluginDefinition.PluginId, item);
                }
            }
            catch (Exception ex)
            {
                PluginLog.Error($"Error writing helper status: {ex.Message}");
            }
        }

        /// <summary>
        /// Snapshot the current StreamConfig values to detect real config changes.
        /// </summary>
        private string GetStreamConfigSnapshot()
        {
            try
            {
                var sb = new System.Text.StringBuilder();
                var configItems = Configuration.Instance.GetItemConfigurations(
                    RtmpStreamerPluginDefinition.PluginId, null, RtmpStreamerPluginDefinition.PluginKindId);

                foreach (var item in configItems)
                {
                    if (item.Properties.ContainsKey("StreamConfig"))
                        sb.Append(item.Properties["StreamConfig"]);
                }
                return sb.ToString();
            }
            catch
            {
                return "";
            }
        }

        private object OnConfigurationChanged(Message message, FQID dest, FQID sender)
        {
            // Check if the actual stream configuration changed (not just a status update)
            var currentConfig = GetStreamConfigSnapshot();
            if (currentConfig == _lastStreamConfig)
                return null; // Status write or no real change, ignore

            PluginLog.Info("Stream configuration changed, reloading helpers");
            _lastStreamConfig = currentConfig;

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
            // "STATS frames=1234 fps=25.0 bytes=5678900 keyframes=50"
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
            public Guid CameraId;
            public string CameraName;
            public string RtmpUrl;
            public DateTime StartTime;
            public int RestartCount;
            public long Frames;
            public double Fps;
            public long Bytes;
            public long KeyFrames;
        }
    }
}
