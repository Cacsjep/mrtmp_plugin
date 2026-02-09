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
                    "Supported protocols: RTMP and RTMPS (TLS).\n" +
                    "A silent audio track is included automatically for YouTube/Twitch compatibility.\n" +
                    "Only H.264 cameras are supported.\n\n" +
                    "Status indicators:\n" +
                    "  Green = Streaming     Yellow = Not yet started     Red = Error     Grey = Disabled"
            };
            Controls.Add(label);
        }

        public override void Init(Item item) { }
        public override void Close() { }
    }
}
