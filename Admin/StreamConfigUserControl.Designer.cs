namespace RtmpStreamerPlugin.Admin
{
    partial class StreamConfigUserControl
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this._dataGridView = new System.Windows.Forms.DataGridView();
            this._colCamera = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this._colRtmpUrl = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this._colStatus = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this._colFrames = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this._btnAdd = new System.Windows.Forms.Button();
            this._btnRemove = new System.Windows.Forms.Button();
            this._labelTitle = new System.Windows.Forms.Label();
            this._panelButtons = new System.Windows.Forms.Panel();
            this._panelAdd = new System.Windows.Forms.GroupBox();
            this._btnSelectCamera = new System.Windows.Forms.Button();
            this._txtCameraName = new System.Windows.Forms.TextBox();
            this._labelCamera = new System.Windows.Forms.Label();
            this._txtRtmpUrl = new System.Windows.Forms.TextBox();
            this._labelRtmpUrl = new System.Windows.Forms.Label();
            this._btnAddStream = new System.Windows.Forms.Button();
            this._labelInfo = new System.Windows.Forms.Label();
            ((System.ComponentModel.ISupportInitialize)(this._dataGridView)).BeginInit();
            this._panelButtons.SuspendLayout();
            this._panelAdd.SuspendLayout();
            this.SuspendLayout();

            // _labelTitle
            this._labelTitle.AutoSize = true;
            this._labelTitle.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Bold);
            this._labelTitle.Location = new System.Drawing.Point(12, 12);
            this._labelTitle.Name = "_labelTitle";
            this._labelTitle.Size = new System.Drawing.Size(200, 21);
            this._labelTitle.Text = "RTMP Stream Configuration";

            // _labelInfo
            this._labelInfo.AutoSize = true;
            this._labelInfo.Location = new System.Drawing.Point(14, 38);
            this._labelInfo.Name = "_labelInfo";
            this._labelInfo.Size = new System.Drawing.Size(400, 13);
            this._labelInfo.Text = "Configure cameras to stream via RTMP to YouTube, Twitch, or other services.";

            // _panelAdd
            this._panelAdd.Anchor = ((System.Windows.Forms.AnchorStyles)(System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right));
            this._panelAdd.Controls.Add(this._btnSelectCamera);
            this._panelAdd.Controls.Add(this._txtCameraName);
            this._panelAdd.Controls.Add(this._labelCamera);
            this._panelAdd.Controls.Add(this._txtRtmpUrl);
            this._panelAdd.Controls.Add(this._labelRtmpUrl);
            this._panelAdd.Controls.Add(this._btnAddStream);
            this._panelAdd.Location = new System.Drawing.Point(14, 60);
            this._panelAdd.Name = "_panelAdd";
            this._panelAdd.Size = new System.Drawing.Size(660, 100);
            this._panelAdd.TabIndex = 0;
            this._panelAdd.Text = "Add New Stream";

            // _labelCamera
            this._labelCamera.AutoSize = true;
            this._labelCamera.Location = new System.Drawing.Point(10, 24);
            this._labelCamera.Name = "_labelCamera";
            this._labelCamera.Size = new System.Drawing.Size(46, 13);
            this._labelCamera.Text = "Camera:";

            // _txtCameraName
            this._txtCameraName.Location = new System.Drawing.Point(90, 21);
            this._txtCameraName.Name = "_txtCameraName";
            this._txtCameraName.ReadOnly = true;
            this._txtCameraName.Size = new System.Drawing.Size(300, 20);
            this._txtCameraName.TabIndex = 1;
            this._txtCameraName.Text = "(Select a camera)";

            // _btnSelectCamera
            this._btnSelectCamera.Location = new System.Drawing.Point(396, 19);
            this._btnSelectCamera.Name = "_btnSelectCamera";
            this._btnSelectCamera.Size = new System.Drawing.Size(75, 23);
            this._btnSelectCamera.TabIndex = 2;
            this._btnSelectCamera.Text = "Select...";
            this._btnSelectCamera.UseVisualStyleBackColor = true;
            this._btnSelectCamera.Click += new System.EventHandler(this.BtnSelectCamera_Click);

            // _labelRtmpUrl
            this._labelRtmpUrl.AutoSize = true;
            this._labelRtmpUrl.Location = new System.Drawing.Point(10, 54);
            this._labelRtmpUrl.Name = "_labelRtmpUrl";
            this._labelRtmpUrl.Size = new System.Drawing.Size(62, 13);
            this._labelRtmpUrl.Text = "RTMP URL:";

            // _txtRtmpUrl
            this._txtRtmpUrl.Location = new System.Drawing.Point(90, 51);
            this._txtRtmpUrl.Name = "_txtRtmpUrl";
            this._txtRtmpUrl.Size = new System.Drawing.Size(381, 20);
            this._txtRtmpUrl.TabIndex = 3;

            // _btnAddStream
            this._btnAddStream.Location = new System.Drawing.Point(485, 35);
            this._btnAddStream.Name = "_btnAddStream";
            this._btnAddStream.Size = new System.Drawing.Size(100, 30);
            this._btnAddStream.TabIndex = 4;
            this._btnAddStream.Text = "Add Stream";
            this._btnAddStream.UseVisualStyleBackColor = true;
            this._btnAddStream.Click += new System.EventHandler(this.BtnAddStream_Click);

            // _dataGridView
            this._dataGridView.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
            | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this._dataGridView.AllowUserToAddRows = false;
            this._dataGridView.AllowUserToDeleteRows = false;
            this._dataGridView.AllowUserToResizeRows = false;
            this._dataGridView.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this._dataGridView.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
                this._colCamera,
                this._colRtmpUrl,
                this._colStatus,
                this._colFrames
            });
            this._dataGridView.Location = new System.Drawing.Point(14, 170);
            this._dataGridView.MultiSelect = false;
            this._dataGridView.Name = "_dataGridView";
            this._dataGridView.ReadOnly = true;
            this._dataGridView.RowHeadersVisible = false;
            this._dataGridView.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this._dataGridView.Size = new System.Drawing.Size(660, 250);
            this._dataGridView.TabIndex = 5;

            // _colCamera
            this._colCamera.HeaderText = "Camera";
            this._colCamera.Name = "_colCamera";
            this._colCamera.ReadOnly = true;
            this._colCamera.Width = 160;

            // _colRtmpUrl
            this._colRtmpUrl.HeaderText = "RTMP URL";
            this._colRtmpUrl.Name = "_colRtmpUrl";
            this._colRtmpUrl.ReadOnly = true;
            this._colRtmpUrl.Width = 300;

            // _colStatus
            this._colStatus.HeaderText = "Status";
            this._colStatus.Name = "_colStatus";
            this._colStatus.ReadOnly = true;
            this._colStatus.Width = 90;

            // _colFrames
            this._colFrames.HeaderText = "Frames";
            this._colFrames.Name = "_colFrames";
            this._colFrames.ReadOnly = true;
            this._colFrames.Width = 80;

            // _panelButtons
            this._panelButtons.Anchor = ((System.Windows.Forms.AnchorStyles)(System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left));
            this._panelButtons.Controls.Add(this._btnRemove);
            this._panelButtons.Location = new System.Drawing.Point(14, 425);
            this._panelButtons.Name = "_panelButtons";
            this._panelButtons.Size = new System.Drawing.Size(660, 35);

            // _btnRemove
            this._btnRemove.Location = new System.Drawing.Point(0, 5);
            this._btnRemove.Name = "_btnRemove";
            this._btnRemove.Size = new System.Drawing.Size(120, 25);
            this._btnRemove.TabIndex = 6;
            this._btnRemove.Text = "Remove Selected";
            this._btnRemove.UseVisualStyleBackColor = true;
            this._btnRemove.Click += new System.EventHandler(this.BtnRemove_Click);

            // _btnAdd (hidden, not used - we use _panelAdd instead)
            this._btnAdd.Visible = false;

            // StreamConfigUserControl
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this._labelTitle);
            this.Controls.Add(this._labelInfo);
            this.Controls.Add(this._panelAdd);
            this.Controls.Add(this._dataGridView);
            this.Controls.Add(this._panelButtons);
            this.Name = "StreamConfigUserControl";
            this.Size = new System.Drawing.Size(690, 470);
            ((System.ComponentModel.ISupportInitialize)(this._dataGridView)).EndInit();
            this._panelButtons.ResumeLayout(false);
            this._panelAdd.ResumeLayout(false);
            this._panelAdd.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        private System.Windows.Forms.DataGridView _dataGridView;
        private System.Windows.Forms.DataGridViewTextBoxColumn _colCamera;
        private System.Windows.Forms.DataGridViewTextBoxColumn _colRtmpUrl;
        private System.Windows.Forms.DataGridViewTextBoxColumn _colStatus;
        private System.Windows.Forms.DataGridViewTextBoxColumn _colFrames;
        private System.Windows.Forms.Button _btnAdd;
        private System.Windows.Forms.Button _btnRemove;
        private System.Windows.Forms.Label _labelTitle;
        private System.Windows.Forms.Panel _panelButtons;
        private System.Windows.Forms.GroupBox _panelAdd;
        private System.Windows.Forms.Button _btnSelectCamera;
        private System.Windows.Forms.TextBox _txtCameraName;
        private System.Windows.Forms.Label _labelCamera;
        private System.Windows.Forms.TextBox _txtRtmpUrl;
        private System.Windows.Forms.Label _labelRtmpUrl;
        private System.Windows.Forms.Button _btnAddStream;
        private System.Windows.Forms.Label _labelInfo;
    }
}
