using System;
using System.Collections.Generic;
using RtmpStreamerPlugin.Streaming;
using VideoOS.Platform;
using VideoOS.Platform.Background;
using VideoOS.Platform.Messaging;

namespace RtmpStreamerPlugin.Background
{
    /// <summary>
    /// Background plugin that runs on the Event Server as a Windows service.
    /// Manages RTMP streaming sessions that persist independently of the Management Client.
    /// </summary>
    public class RtmpStreamerBackgroundPlugin : BackgroundPlugin
    {
        private StreamSessionManager _sessionManager;
        private object _configMessageObj;

        public override Guid Id => RtmpStreamerPluginDefinition.BackgroundPluginId;
        public override string Name => "RTMP Streamer Background";

        public override List<EnvironmentType> TargetEnvironments
        {
            get { return new List<EnvironmentType> { EnvironmentType.Service }; }
        }

        public override void Init()
        {
            PluginLog.Info("Background plugin initializing");

            _sessionManager = new StreamSessionManager();

            // Register for configuration change notifications, filtered to only our plugin's Kind
            _configMessageObj = EnvironmentManager.Instance.RegisterReceiver(
                OnConfigurationChanged,
                new MessageIdAndRelatedKindFilter(
                    MessageId.Server.ConfigurationChangedIndication,
                    RtmpStreamerPluginDefinition.PluginKindId));

            // Load and start configured streams
            LoadAndStartStreams();
        }

        public override void Close()
        {
            PluginLog.Info("Background plugin closing, stopping all streams");

            if (_configMessageObj != null)
            {
                EnvironmentManager.Instance.UnRegisterReceiver(_configMessageObj);
                _configMessageObj = null;
            }

            _sessionManager?.Dispose();
            _sessionManager = null;
        }

        private void LoadAndStartStreams()
        {
            try
            {
                // Load configuration stored by the Admin plugin
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
                        _sessionManager.StartFromConfig(configs);
                    }
                }

                PluginLog.Info($"Loaded and started streams. Active: {CountSessions()}");
            }
            catch (Exception ex)
            {
                PluginLog.Error($"Error loading config: {ex.Message}", ex);
            }
        }

        private object OnConfigurationChanged(Message message, FQID dest, FQID sender)
        {
            PluginLog.Info("Configuration changed, reloading streams");

            try
            {
                // Stop all current streams
                _sessionManager?.StopAll();

                // Reload and restart
                LoadAndStartStreams();
            }
            catch (Exception ex)
            {
                PluginLog.Error($"Error reloading config: {ex.Message}", ex);
            }

            return null;
        }

        private int CountSessions()
        {
            int count = 0;
            if (_sessionManager != null)
            {
                foreach (var _ in _sessionManager.GetSessions())
                    count++;
            }
            return count;
        }
    }
}
