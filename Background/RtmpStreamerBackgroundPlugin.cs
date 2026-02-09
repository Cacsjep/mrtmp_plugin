using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Threading;
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

                PluginLog.Info($"Loaded streams. Active helpers: {_helpers.Count}");
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

                // Read stderr async for logging
                process.ErrorDataReceived += (s, e) =>
                {
                    if (!string.IsNullOrEmpty(e.Data))
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
        }

        private object OnConfigurationChanged(Message message, FQID dest, FQID sender)
        {
            PluginLog.Info("Configuration changed, reloading streams");

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

        private class HelperProcess
        {
            public Process Process;
            public Guid CameraId;
            public string CameraName;
            public string RtmpUrl;
            public DateTime StartTime;
            public int RestartCount;
        }
    }
}
