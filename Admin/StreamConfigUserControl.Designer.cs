namespace RtmpStreamerPlugin.Admin
{
    partial class StreamConfigUserControl
    {
        private System.ComponentModel.IContainer components = null;

        private void InitializeComponent()
        {
            this._labelTitle = new System.Windows.Forms.Label();
            this._labelName = new System.Windows.Forms.Label();
            this._txtName = new System.Windows.Forms.TextBox();
            this._labelCamera = new System.Windows.Forms.Label();
            this._btnSelectCamera = new System.Windows.Forms.Button();
            this._labelRtmpUrl = new System.Windows.Forms.Label();
            this._txtRtmpUrl = new System.Windows.Forms.TextBox();
            this._chkEnabled = new System.Windows.Forms.CheckBox();
            this._grpStatus = new System.Windows.Forms.GroupBox();
            this._labelStatus = new System.Windows.Forms.Label();
            this._lblStatusValue = new System.Windows.Forms.Label();
            this._labelUptime = new System.Windows.Forms.Label();
            this._lblUptimeValue = new System.Windows.Forms.Label();
            this._grpStatus.SuspendLayout();
            this.SuspendLayout();

            // _labelTitle
            this._labelTitle.AutoSize = true;
            this._labelTitle.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Bold);
            this._labelTitle.Location = new System.Drawing.Point(12, 12);
            this._labelTitle.Name = "_labelTitle";
            this._labelTitle.Size = new System.Drawing.Size(200, 21);
            this._labelTitle.Text = "RTMP Stream Configuration";

            // _labelName
            this._labelName.AutoSize = true;
            this._labelName.Location = new System.Drawing.Point(14, 50);
            this._labelName.Name = "_labelName";
            this._labelName.Size = new System.Drawing.Size(38, 13);
            this._labelName.Text = "Name:";

            // _txtName
            this._txtName.Anchor = ((System.Windows.Forms.AnchorStyles)(System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right));
            this._txtName.Location = new System.Drawing.Point(110, 47);
            this._txtName.Name = "_txtName";
            this._txtName.Size = new System.Drawing.Size(400, 20);
            this._txtName.TabIndex = 0;
            this._txtName.TextChanged += new System.EventHandler(this.OnUserChange);

            // _labelCamera
            this._labelCamera.AutoSize = true;
            this._labelCamera.Location = new System.Drawing.Point(14, 80);
            this._labelCamera.Name = "_labelCamera";
            this._labelCamera.Size = new System.Drawing.Size(46, 13);
            this._labelCamera.Text = "Camera:";

            // _btnSelectCamera
            this._btnSelectCamera.Anchor = ((System.Windows.Forms.AnchorStyles)(System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right));
            this._btnSelectCamera.Location = new System.Drawing.Point(110, 75);
            this._btnSelectCamera.Name = "_btnSelectCamera";
            this._btnSelectCamera.Size = new System.Drawing.Size(400, 23);
            this._btnSelectCamera.TabIndex = 1;
            this._btnSelectCamera.Text = "(Select camera...)";
            this._btnSelectCamera.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this._btnSelectCamera.UseVisualStyleBackColor = true;
            this._btnSelectCamera.Click += new System.EventHandler(this.BtnSelectCamera_Click);

            // _labelRtmpUrl
            this._labelRtmpUrl.AutoSize = true;
            this._labelRtmpUrl.Location = new System.Drawing.Point(14, 112);
            this._labelRtmpUrl.Name = "_labelRtmpUrl";
            this._labelRtmpUrl.Size = new System.Drawing.Size(62, 13);
            this._labelRtmpUrl.Text = "RTMP URL:";

            // _txtRtmpUrl
            this._txtRtmpUrl.Anchor = ((System.Windows.Forms.AnchorStyles)(System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right));
            this._txtRtmpUrl.Location = new System.Drawing.Point(110, 109);
            this._txtRtmpUrl.Name = "_txtRtmpUrl";
            this._txtRtmpUrl.Size = new System.Drawing.Size(400, 20);
            this._txtRtmpUrl.TabIndex = 2;
            this._txtRtmpUrl.TextChanged += new System.EventHandler(this.OnUserChange);

            // _chkEnabled
            this._chkEnabled.AutoSize = true;
            this._chkEnabled.Checked = true;
            this._chkEnabled.CheckState = System.Windows.Forms.CheckState.Checked;
            this._chkEnabled.Location = new System.Drawing.Point(110, 142);
            this._chkEnabled.Name = "_chkEnabled";
            this._chkEnabled.Size = new System.Drawing.Size(65, 17);
            this._chkEnabled.TabIndex = 3;
            this._chkEnabled.Text = "Enabled";
            this._chkEnabled.UseVisualStyleBackColor = true;
            this._chkEnabled.CheckedChanged += new System.EventHandler(this.OnUserChange);

            // _grpStatus
            this._grpStatus.Anchor = ((System.Windows.Forms.AnchorStyles)(System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right));
            this._grpStatus.Controls.Add(this._labelStatus);
            this._grpStatus.Controls.Add(this._lblStatusValue);
            this._grpStatus.Controls.Add(this._labelUptime);
            this._grpStatus.Controls.Add(this._lblUptimeValue);
            this._grpStatus.Location = new System.Drawing.Point(14, 175);
            this._grpStatus.Name = "_grpStatus";
            this._grpStatus.Size = new System.Drawing.Size(496, 70);
            this._grpStatus.TabIndex = 4;
            this._grpStatus.TabStop = false;
            this._grpStatus.Text = "Stream Status";

            // _labelStatus
            this._labelStatus.AutoSize = true;
            this._labelStatus.Location = new System.Drawing.Point(10, 22);
            this._labelStatus.Name = "_labelStatus";
            this._labelStatus.Size = new System.Drawing.Size(40, 13);
            this._labelStatus.Text = "Status:";

            // _lblStatusValue
            this._lblStatusValue.AutoSize = true;
            this._lblStatusValue.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this._lblStatusValue.Location = new System.Drawing.Point(96, 22);
            this._lblStatusValue.Name = "_lblStatusValue";
            this._lblStatusValue.Size = new System.Drawing.Size(14, 15);
            this._lblStatusValue.Text = "-";

            // _labelUptime
            this._labelUptime.AutoSize = true;
            this._labelUptime.Location = new System.Drawing.Point(10, 44);
            this._labelUptime.Name = "_labelUptime";
            this._labelUptime.Size = new System.Drawing.Size(43, 13);
            this._labelUptime.Text = "Uptime:";

            // _lblUptimeValue
            this._lblUptimeValue.AutoSize = true;
            this._lblUptimeValue.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this._lblUptimeValue.Location = new System.Drawing.Point(96, 44);
            this._lblUptimeValue.Name = "_lblUptimeValue";
            this._lblUptimeValue.Size = new System.Drawing.Size(14, 15);
            this._lblUptimeValue.Text = "-";

            // StreamConfigUserControl
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this._labelTitle);
            this.Controls.Add(this._labelName);
            this.Controls.Add(this._txtName);
            this.Controls.Add(this._labelCamera);
            this.Controls.Add(this._btnSelectCamera);
            this.Controls.Add(this._labelRtmpUrl);
            this.Controls.Add(this._txtRtmpUrl);
            this.Controls.Add(this._chkEnabled);
            this.Controls.Add(this._grpStatus);
            this.Name = "StreamConfigUserControl";
            this.Size = new System.Drawing.Size(530, 260);
            this._grpStatus.ResumeLayout(false);
            this._grpStatus.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        private System.Windows.Forms.Label _labelTitle;
        private System.Windows.Forms.Label _labelName;
        private System.Windows.Forms.TextBox _txtName;
        private System.Windows.Forms.Label _labelCamera;
        private System.Windows.Forms.Button _btnSelectCamera;
        private System.Windows.Forms.Label _labelRtmpUrl;
        private System.Windows.Forms.TextBox _txtRtmpUrl;
        private System.Windows.Forms.CheckBox _chkEnabled;
        private System.Windows.Forms.GroupBox _grpStatus;
        private System.Windows.Forms.Label _labelStatus;
        private System.Windows.Forms.Label _lblStatusValue;
        private System.Windows.Forms.Label _labelUptime;
        private System.Windows.Forms.Label _lblUptimeValue;
    }
}
