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
            this._lblRtmpExamples = new System.Windows.Forms.Label();
            this._chkEnabled = new System.Windows.Forms.CheckBox();
            this._chkAllowUntrustedCerts = new System.Windows.Forms.CheckBox();
            this.SuspendLayout();
            //
            // _labelTitle
            //
            this._labelTitle.AutoSize = true;
            this._labelTitle.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Bold);
            this._labelTitle.Location = new System.Drawing.Point(12, 12);
            this._labelTitle.Name = "_labelTitle";
            this._labelTitle.Size = new System.Drawing.Size(223, 21);
            this._labelTitle.TabIndex = 0;
            this._labelTitle.Text = "RTMP Stream Configuration";
            //
            // _labelName
            //
            this._labelName.AutoSize = true;
            this._labelName.Location = new System.Drawing.Point(14, 50);
            this._labelName.Name = "_labelName";
            this._labelName.Size = new System.Drawing.Size(38, 13);
            this._labelName.TabIndex = 1;
            this._labelName.Text = "Name:";
            //
            // _txtName
            //
            this._txtName.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this._txtName.Location = new System.Drawing.Point(110, 47);
            this._txtName.Name = "_txtName";
            this._txtName.Size = new System.Drawing.Size(400, 20);
            this._txtName.TabIndex = 0;
            this._txtName.TextChanged += new System.EventHandler(this.OnUserChange);
            //
            // _labelCamera
            //
            this._labelCamera.AutoSize = true;
            this._labelCamera.Location = new System.Drawing.Point(14, 80);
            this._labelCamera.Name = "_labelCamera";
            this._labelCamera.Size = new System.Drawing.Size(46, 13);
            this._labelCamera.TabIndex = 2;
            this._labelCamera.Text = "Camera:";
            //
            // _btnSelectCamera
            //
            this._btnSelectCamera.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this._btnSelectCamera.Location = new System.Drawing.Point(110, 75);
            this._btnSelectCamera.Name = "_btnSelectCamera";
            this._btnSelectCamera.Size = new System.Drawing.Size(400, 23);
            this._btnSelectCamera.TabIndex = 1;
            this._btnSelectCamera.Text = "Select camera";
            this._btnSelectCamera.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this._btnSelectCamera.UseVisualStyleBackColor = true;
            this._btnSelectCamera.Click += new System.EventHandler(this.BtnSelectCamera_Click);
            //
            // _labelRtmpUrl
            //
            this._labelRtmpUrl.AutoSize = true;
            this._labelRtmpUrl.Location = new System.Drawing.Point(14, 112);
            this._labelRtmpUrl.Name = "_labelRtmpUrl";
            this._labelRtmpUrl.Size = new System.Drawing.Size(66, 13);
            this._labelRtmpUrl.TabIndex = 3;
            this._labelRtmpUrl.Text = "RTMP URL:";
            //
            // _txtRtmpUrl
            //
            this._txtRtmpUrl.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this._txtRtmpUrl.Location = new System.Drawing.Point(110, 109);
            this._txtRtmpUrl.Name = "_txtRtmpUrl";
            this._txtRtmpUrl.Size = new System.Drawing.Size(400, 20);
            this._txtRtmpUrl.TabIndex = 2;
            this._txtRtmpUrl.TextChanged += new System.EventHandler(this.OnUserChange);
            //
            // _lblRtmpExamples
            //
            this._lblRtmpExamples.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this._lblRtmpExamples.ForeColor = System.Drawing.SystemColors.GrayText;
            this._lblRtmpExamples.Location = new System.Drawing.Point(110, 132);
            this._lblRtmpExamples.Name = "_lblRtmpExamples";
            this._lblRtmpExamples.Size = new System.Drawing.Size(400, 30);
            this._lblRtmpExamples.TabIndex = 4;
            this._lblRtmpExamples.Text = "YouTube: rtmp://a.rtmp.youtube.com/live2/STREAM-KEY\rTwitch: rtmps://live.twitch.t" +
    "v/app/STREAM-KEY";
            //
            // _chkEnabled
            //
            this._chkEnabled.AutoSize = true;
            this._chkEnabled.Checked = true;
            this._chkEnabled.CheckState = System.Windows.Forms.CheckState.Checked;
            this._chkEnabled.Location = new System.Drawing.Point(110, 170);
            this._chkEnabled.Name = "_chkEnabled";
            this._chkEnabled.Size = new System.Drawing.Size(65, 17);
            this._chkEnabled.TabIndex = 3;
            this._chkEnabled.Text = "Enabled";
            this._chkEnabled.UseVisualStyleBackColor = true;
            this._chkEnabled.CheckedChanged += new System.EventHandler(this.OnUserChange);
            //
            // _chkAllowUntrustedCerts
            //
            this._chkAllowUntrustedCerts.AutoSize = true;
            this._chkAllowUntrustedCerts.Location = new System.Drawing.Point(110, 193);
            this._chkAllowUntrustedCerts.Name = "_chkAllowUntrustedCerts";
            this._chkAllowUntrustedCerts.Size = new System.Drawing.Size(304, 17);
            this._chkAllowUntrustedCerts.TabIndex = 5;
            this._chkAllowUntrustedCerts.Text = "Allow untrusted certificates (for self-signed RTMPS servers)";
            this._chkAllowUntrustedCerts.UseVisualStyleBackColor = true;
            this._chkAllowUntrustedCerts.CheckedChanged += new System.EventHandler(this.OnUserChange);
            //
            // StreamConfigUserControl
            //
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this._labelTitle);
            this.Controls.Add(this._labelName);
            this.Controls.Add(this._txtName);
            this.Controls.Add(this._labelCamera);
            this.Controls.Add(this._btnSelectCamera);
            this.Controls.Add(this._labelRtmpUrl);
            this.Controls.Add(this._txtRtmpUrl);
            this.Controls.Add(this._lblRtmpExamples);
            this.Controls.Add(this._chkEnabled);
            this.Controls.Add(this._chkAllowUntrustedCerts);
            this.Name = "StreamConfigUserControl";
            this.Size = new System.Drawing.Size(530, 220);
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
        private System.Windows.Forms.Label _lblRtmpExamples;
        private System.Windows.Forms.CheckBox _chkEnabled;
        private System.Windows.Forms.CheckBox _chkAllowUntrustedCerts;
    }
}
