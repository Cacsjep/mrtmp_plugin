using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows.Forms;
using System.Xml.Linq;
using RtmpStreamerPlugin.Streaming;
using VideoOS.Platform;
using VideoOS.Platform.UI;

namespace RtmpStreamerPlugin.Admin
{
    /// <summary>
    /// WinForms user control for configuring RTMP streams in the Management Client.
    /// Allows adding/removing camera-to-RTMP-URL mappings.
    /// </summary>
    public partial class StreamConfigUserControl : UserControl
    {
        private Item _selectedCameraItem;
        private Item _currentItem;
        private List<StreamEntry> _streams = new List<StreamEntry>();
        private Timer _refreshTimer;

        /// <summary>
        /// Fired when the user changes the configuration.
        /// </summary>
        public event EventHandler ConfigurationChanged;

        public StreamConfigUserControl()
        {
            InitializeComponent();

            // Auto-refresh status every 1 second
            _refreshTimer = new Timer { Interval = 1000 };
            _refreshTimer.Tick += (s, e) => RefreshStatus();
            _refreshTimer.Start();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _refreshTimer?.Stop();
                _refreshTimer?.Dispose();
                components?.Dispose();
            }
            base.Dispose(disposing);
        }

        /// <summary>
        /// Load configuration from an Item's properties into the UI.
        /// </summary>
        public void FillContent(Item item)
        {
            _currentItem = item;
            _streams.Clear();

            if (item == null)
            {
                RefreshGrid();
                return;
            }

            LoadStreamsFromItem(item);
            RefreshGrid();
        }

        private void LoadStreamsFromItem(Item item)
        {
            // Read helper status written by BackgroundPlugin
            var helperStatus = ParseHelperStatus(item);

            if (item.Properties.ContainsKey("StreamConfig"))
            {
                string xml = item.Properties["StreamConfig"];
                var configs = StreamSessionManager.LoadConfigXml(xml);
                foreach (var config in configs)
                {
                    var entry = new StreamEntry
                    {
                        CameraId = config.CameraId,
                        CameraName = config.CameraName,
                        RtmpUrl = config.RtmpUrl,
                        AutoStart = config.AutoStart,
                        Status = "Configured",
                        Fps = "-",
                        Uptime = "-"
                    };

                    if (helperStatus.TryGetValue(config.CameraId, out var hs))
                    {
                        entry.Status = hs.Status;
                        if (hs.Restarts > 0)
                            entry.Status += $" (R:{hs.Restarts})";
                        if (hs.Fps > 0)
                            entry.Fps = hs.Fps.ToString("F1");
                        if (hs.Uptime.TotalSeconds > 0)
                            entry.Uptime = hs.Uptime.ToString(@"hh\:mm\:ss");
                    }

                    _streams.Add(entry);
                }
            }
        }

        private static Dictionary<Guid, HelperStatusEntry> ParseHelperStatus(Item item)
        {
            var result = new Dictionary<Guid, HelperStatusEntry>();

            if (!item.Properties.ContainsKey("StreamStatus"))
                return result;

            try
            {
                var statusRoot = XElement.Parse(item.Properties["StreamStatus"]);
                foreach (var elem in statusRoot.Elements("Helper"))
                {
                    var cameraId = Guid.Parse(elem.Attribute("CameraId")?.Value ?? Guid.Empty.ToString());
                    var entry = new HelperStatusEntry
                    {
                        Status = elem.Attribute("Status")?.Value ?? "Unknown",
                        Restarts = int.Parse(elem.Attribute("Restarts")?.Value ?? "0")
                    };

                    long.TryParse(elem.Attribute("Frames")?.Value, out entry.Frames);
                    double.TryParse(elem.Attribute("Fps")?.Value, NumberStyles.Float,
                        CultureInfo.InvariantCulture, out entry.Fps);

                    if (DateTime.TryParse(elem.Attribute("StartTime")?.Value, CultureInfo.InvariantCulture,
                        DateTimeStyles.RoundtripKind, out var startTime))
                    {
                        entry.Uptime = DateTime.UtcNow - startTime;
                    }

                    result[cameraId] = entry;
                }
            }
            catch { }

            return result;
        }

        /// <summary>
        /// Refresh status from server without reloading stream config.
        /// </summary>
        private void RefreshStatus()
        {
            if (_currentItem == null || _streams.Count == 0)
                return;

            try
            {
                // Re-read the item to get fresh status
                var freshItem = Configuration.Instance.GetItemConfiguration(
                    RtmpStreamerPluginDefinition.PluginId,
                    RtmpStreamerPluginDefinition.PluginKindId,
                    _currentItem.FQID.ObjectId);

                if (freshItem == null) return;

                var helperStatus = ParseHelperStatus(freshItem);
                if (helperStatus.Count == 0) return;

                bool changed = false;
                foreach (var entry in _streams)
                {
                    if (helperStatus.TryGetValue(entry.CameraId, out var hs))
                    {
                        var newStatus = hs.Status;
                        if (hs.Restarts > 0) newStatus += $" (R:{hs.Restarts})";
                        var newFps = hs.Fps > 0 ? hs.Fps.ToString("F1") : "-";
                        var newUptime = hs.Uptime.TotalSeconds > 0 ? hs.Uptime.ToString(@"hh\:mm\:ss") : "-";

                        if (entry.Status != newStatus || entry.Fps != newFps || entry.Uptime != newUptime)
                        {
                            entry.Status = newStatus;
                            entry.Fps = newFps;
                            entry.Uptime = newUptime;
                            changed = true;
                        }
                    }
                }

                if (changed)
                    RefreshGrid();
            }
            catch { }
        }

        /// <summary>
        /// Save the current configuration to the Item's properties.
        /// </summary>
        public void StoreProperties(Item item)
        {
            if (item == null) return;

            var root = new XElement("RtmpStreams");
            foreach (var entry in _streams)
            {
                root.Add(new XElement("Stream",
                    new XAttribute("CameraId", entry.CameraId),
                    new XAttribute("CameraName", entry.CameraName),
                    new XAttribute("RtmpUrl", entry.RtmpUrl),
                    new XAttribute("AutoStart", entry.AutoStart)
                ));
            }

            item.Properties["StreamConfig"] = root.ToString();
        }

        /// <summary>
        /// Clear the UI.
        /// </summary>
        public void ClearContent()
        {
            _currentItem = null;
            _streams.Clear();
            _selectedCameraItem = null;
            _txtCameraName.Text = "";
            _txtRtmpUrl.Text = "";
            RefreshGrid();
        }

        private void BtnSelectCamera_Click(object sender, EventArgs e)
        {
            var form = new ItemPickerWpfWindow
            {
                Items = Configuration.Instance.GetItemsByKind(Kind.Camera),
                KindsFilter = new List<Guid> { Kind.Camera },
                SelectionMode = SelectionModeOptions.AutoCloseOnSelect,
            };

            if (form.ShowDialog() == true && form.SelectedItems != null && form.SelectedItems.Any())
            {
                _selectedCameraItem = form.SelectedItems.First();
                _txtCameraName.Text = _selectedCameraItem.Name;
            }
        }

        private void BtnAddStream_Click(object sender, EventArgs e)
        {
            if (_selectedCameraItem == null)
            {
                MessageBox.Show("Please select a camera first.", "RTMP Streamer",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string rtmpUrl = _txtRtmpUrl.Text.Trim();
            if (string.IsNullOrEmpty(rtmpUrl) || !rtmpUrl.StartsWith("rtmp://", StringComparison.OrdinalIgnoreCase))
            {
                MessageBox.Show("Please enter a valid RTMP URL (e.g., rtmp://a.rtmp.youtube.com/live2/stream-key).",
                    "RTMP Streamer", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            _streams.Add(new StreamEntry
            {
                CameraId = _selectedCameraItem.FQID.ObjectId,
                CameraName = _selectedCameraItem.Name,
                RtmpUrl = rtmpUrl,
                AutoStart = true,
                Status = "Pending",
                Fps = "-",
                Uptime = "-"
            });

            // Clear inputs
            _selectedCameraItem = null;
            _txtCameraName.Text = "";
            _txtRtmpUrl.Text = "";

            RefreshGrid();
            ConfigurationChanged?.Invoke(this, EventArgs.Empty);
        }

        private void BtnRemove_Click(object sender, EventArgs e)
        {
            if (_dataGridView.SelectedRows.Count == 0)
                return;

            int idx = _dataGridView.SelectedRows[0].Index;
            if (idx >= 0 && idx < _streams.Count)
            {
                _streams.RemoveAt(idx);
                RefreshGrid();
                ConfigurationChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        private void RefreshGrid()
        {
            _dataGridView.Rows.Clear();
            foreach (var entry in _streams)
            {
                _dataGridView.Rows.Add(
                    entry.CameraName,
                    entry.RtmpUrl,
                    entry.Status,
                    entry.Fps,
                    entry.Uptime
                );
            }
        }

        private class StreamEntry
        {
            public Guid CameraId { get; set; }
            public string CameraName { get; set; }
            public string RtmpUrl { get; set; }
            public bool AutoStart { get; set; }
            public string Status { get; set; }
            public string Fps { get; set; }
            public string Uptime { get; set; }
        }

        private class HelperStatusEntry
        {
            public string Status;
            public int Restarts;
            public long Frames;
            public double Fps;
            public TimeSpan Uptime;
        }
    }
}
