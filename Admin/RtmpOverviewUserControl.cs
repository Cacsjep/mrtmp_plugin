using System.Drawing;
using System.Windows.Forms;
using VideoOS.Platform;
using VideoOS.Platform.Admin;

namespace RtmpStreamerPlugin.Admin
{
    public class RtmpOverviewUserControl : ItemNodeUserControl
    {
        public RtmpOverviewUserControl()
        {
            var label = new Label
            {
                AutoSize = false,
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 10F),
                Padding = new Padding(12),
                Text =
                    "RTMP Streams\n\n" +
                    "Stream live camera video to RTMP servers such as YouTube Live, Twitch, " +
                    "or custom RTMP endpoints.\n\n" +
                    "Getting started:\n" +
                    "  1. Right-click 'RTMP Streams' and select 'Create New' to add a stream.\n" +
                    "  2. Select a camera from the VMS.\n" +
                    "  3. Enter the RTMP URL provided by your streaming platform.\n" +
                    "  4. Click Save to start streaming.\n\n" +
                    "Each stream runs as an independent process. If a stream crashes,\n" +
                    "it will automatically restart within seconds.\n\n" +
                    "Use the Enabled checkbox to start or stop individual streams."
            };
            Controls.Add(label);
        }

        public override void Init(Item item) { }
        public override void Close() { }
    }
}
