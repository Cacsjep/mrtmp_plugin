using System;
using System.Collections.Generic;
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
        private List<StreamEntry> _streams = new List<StreamEntry>();

        /// <summary>
        /// Fired when the user changes the configuration.
        /// </summary>
        public event EventHandler ConfigurationChanged;

        public StreamConfigUserControl()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Load configuration from an Item's properties into the UI.
        /// </summary>
        public void FillContent(Item item)
        {
            _streams.Clear();

            if (item != null && item.Properties.ContainsKey("StreamConfig"))
            {
                string xml = item.Properties["StreamConfig"];
                var configs = StreamSessionManager.LoadConfigXml(xml);
                foreach (var config in configs)
                {
                    _streams.Add(new StreamEntry
                    {
                        CameraId = config.CameraId,
                        CameraName = config.CameraName,
                        RtmpUrl = config.RtmpUrl,
                        AutoStart = config.AutoStart,
                        Status = "Configured"
                    });
                }
            }

            RefreshGrid();
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
                Status = "Pending"
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
                    MaskStreamKey(entry.RtmpUrl),
                    entry.Status,
                    "-"
                );
            }
        }

        /// <summary>
        /// Mask the stream key portion of the RTMP URL for display security.
        /// </summary>
        private static string MaskStreamKey(string url)
        {
            if (string.IsNullOrEmpty(url)) return url;
            // Find the last slash (after app name) - everything after is the stream key
            int lastSlash = url.LastIndexOf('/');
            if (lastSlash > 10) // after rtmp://host/app/
            {
                string key = url.Substring(lastSlash + 1);
                if (key.Length > 4)
                    return url.Substring(0, lastSlash + 1) + key.Substring(0, 4) + "****";
            }
            return url;
        }

        private class StreamEntry
        {
            public Guid CameraId { get; set; }
            public string CameraName { get; set; }
            public string RtmpUrl { get; set; }
            public bool AutoStart { get; set; }
            public string Status { get; set; }
        }
    }
}
