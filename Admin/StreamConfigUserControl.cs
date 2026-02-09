using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using VideoOS.Platform;
using VideoOS.Platform.UI;

namespace RtmpStreamerPlugin.Admin
{
    public partial class StreamConfigUserControl : UserControl
    {
        private Item _currentItem;
        private Item _selectedCameraItem;
        private Timer _refreshTimer;

        internal event EventHandler ConfigurationChangedByUser;

        public StreamConfigUserControl()
        {
            InitializeComponent();

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

        public string DisplayName => _txtName.Text;

        public void FillContent(Item item)
        {
            _currentItem = item;
            if (item == null)
            {
                ClearContent();
                return;
            }

            _txtName.Text = item.Name;

            // Load camera
            if (item.Properties.ContainsKey("CameraId"))
            {
                var cameraIdStr = item.Properties["CameraId"];
                if (Guid.TryParse(cameraIdStr, out var cameraId) && cameraId != Guid.Empty)
                {
                    var cameraItem = Configuration.Instance.GetItem(cameraId, Kind.Camera);
                    if (cameraItem != null)
                    {
                        _selectedCameraItem = cameraItem;
                        _btnSelectCamera.Text = cameraItem.Name;
                    }
                    else
                    {
                        _btnSelectCamera.Text = item.Properties.ContainsKey("CameraName")
                            ? item.Properties["CameraName"] + " (not found)"
                            : "(Camera not found)";
                    }
                }
                else
                {
                    _btnSelectCamera.Text = "(Select camera...)";
                }
            }
            else
            {
                _btnSelectCamera.Text = "(Select camera...)";
            }

            // Load RTMP URL
            _txtRtmpUrl.Text = item.Properties.ContainsKey("RtmpUrl")
                ? item.Properties["RtmpUrl"] : "";

            // Load enabled state
            _chkEnabled.Checked = !item.Properties.ContainsKey("Enabled")
                || item.Properties["Enabled"] != "No";

            // Load status
            RefreshStatusFromItem(item);
        }

        public string ValidateInput()
        {
            if (string.IsNullOrWhiteSpace(_txtName.Text))
                return "Please enter a name for the stream.";

            if (_selectedCameraItem == null)
                return "Please select a camera.";

            var rtmpUrl = _txtRtmpUrl.Text.Trim();
            if (string.IsNullOrEmpty(rtmpUrl) || rtmpUrl == "rtmp://")
                return "Please enter an RTMP URL.";

            if (!Regex.IsMatch(rtmpUrl, @"^rtmps?://[^\s]+$", RegexOptions.IgnoreCase))
                return "RTMP URL must start with rtmp:// or rtmps:// and contain no spaces.";

            return null;
        }

        public void UpdateItem(Item item)
        {
            if (item == null) return;

            item.Name = _txtName.Text;

            if (_selectedCameraItem != null)
            {
                item.Properties["CameraId"] = _selectedCameraItem.FQID.ObjectId.ToString();
                item.Properties["CameraName"] = _selectedCameraItem.Name;
            }

            item.Properties["RtmpUrl"] = _txtRtmpUrl.Text.Trim();

            item.Properties["Enabled"] = _chkEnabled.Checked ? "Yes" : "No";
        }

        public void ClearContent()
        {
            _currentItem = null;
            _selectedCameraItem = null;
            _txtName.Text = "";
            _btnSelectCamera.Text = "(Select camera...)";
            _txtRtmpUrl.Text = "";
            _chkEnabled.Checked = true;
            _lblStatusValue.Text = "-";
            _lblUptimeValue.Text = "-";
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
                _btnSelectCamera.Text = _selectedCameraItem.Name;
                OnUserChange(sender, e);
            }
        }

        internal void OnUserChange(object sender, EventArgs e)
        {
            if (ConfigurationChangedByUser != null)
                ConfigurationChangedByUser(this, new EventArgs());
        }

        private void RefreshStatus()
        {
            if (_currentItem == null) return;

            try
            {
                var freshItem = Configuration.Instance.GetItemConfiguration(
                    RtmpStreamerPluginDefinition.PluginId,
                    RtmpStreamerPluginDefinition.PluginKindId,
                    _currentItem.FQID.ObjectId);

                if (freshItem != null)
                    RefreshStatusFromItem(freshItem);
            }
            catch { }
        }

        private void RefreshStatusFromItem(Item item)
        {
            var status = "-";
            var uptime = "-";

            if (item.Properties.ContainsKey("Status"))
                status = item.Properties["Status"];

            if (item.Properties.ContainsKey("StartTime"))
            {
                if (DateTime.TryParse(item.Properties["StartTime"], CultureInfo.InvariantCulture,
                    DateTimeStyles.RoundtripKind, out var startTime))
                {
                    var elapsed = DateTime.UtcNow - startTime;
                    if (elapsed.TotalSeconds > 0)
                        uptime = elapsed.ToString(@"hh\:mm\:ss");
                }
            }

            if (item.Properties.ContainsKey("Restarts"))
            {
                if (int.TryParse(item.Properties["Restarts"], out var restarts) && restarts > 0)
                    status += $" (R:{restarts})";
            }

            _lblStatusValue.Text = status;
            _lblUptimeValue.Text = uptime;
        }
    }
}
