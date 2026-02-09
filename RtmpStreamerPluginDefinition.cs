using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using VideoOS.Platform;
using VideoOS.Platform.Admin;
using VideoOS.Platform.Background;
using VideoOS.Platform.UI;

namespace RtmpStreamerPlugin
{
    public class RtmpStreamerPluginDefinition : PluginDefinition
    {
        internal static readonly Guid PluginId = new Guid("ABA1B2C3-D4E5-6789-ABCD-EF0123456789");
        internal static readonly Guid PluginKindId = new Guid("ABA1B2C3-D4E5-6789-ABCD-EF0123456780");
        internal static readonly Guid BackgroundPluginId = new Guid("ABA1B2C3-D4E5-6789-ABCD-EF0123456781");

        private static readonly Image _defaultIcon = CreateDefaultIcon();
        private List<BackgroundPlugin> _backgroundPlugins = new List<BackgroundPlugin>();
        private List<ItemNode> _itemNodes;
        private Image _icon;

        internal static Image DefaultIcon => _defaultIcon;

        public override Guid Id => PluginId;
        public override string Name => "RTMP Streamer";
        public override string SharedNodeName => "RTMP Streamer";
        public override string VersionString => "1.0.0.0";

        public override Image Icon => _icon ?? _defaultIcon;

        public override void Init()
        {
            try
            {
                _icon = VideoOS.Platform.UI.Util.ImageList.Images[
                    VideoOS.Platform.UI.Util.PluginIx];
            }
            catch { }

            if (EnvironmentManager.Instance.EnvironmentType == EnvironmentType.Service)
            {
                _backgroundPlugins.Add(new Background.RtmpStreamerBackgroundPlugin());
            }
        }

        public override void Close()
        {
            _itemNodes = null;
            _backgroundPlugins.Clear();
        }

        public override List<ItemNode> ItemNodes
        {
            get
            {
                if (_itemNodes == null)
                {
                    _itemNodes = new List<ItemNode>
                    {
                        new ItemNode(
                            PluginKindId,
                            Guid.Empty,
                            "RTMP Stream",          // singular item name
                            _defaultIcon,           // node image
                            "RTMP Streams",         // plural/group name
                            _defaultIcon,           // group image
                            Category.Text,
                            true,                   // includeInExport
                            ItemsAllowed.Many,
                            new Admin.RtmpStreamerItemManager(PluginKindId),
                            null
                        )
                    };
                }
                return _itemNodes;
            }
        }

        public override UserControl GenerateUserControl()
        {
            return new HelpUserControl(
                _defaultIcon,
                "RTMP Streamer Plugin",
                "This plugin streams live camera video from Milestone XProtect to RTMP servers.\n\n" +
                "Supported platforms:\n" +
                "  - YouTube Live\n" +
                "  - Twitch\n" +
                "  - Facebook Live\n" +
                "  - Any custom RTMP/RTMPS endpoint\n\n" +
                "How it works:\n" +
                "The plugin runs on the Event Server and launches a helper process for each configured stream. " +
                "Each helper connects to the Recording Server, receives the live H.264 video stream, " +
                "packages it in FLV format, and publishes it to the configured RTMP URL.\n\n" +
                "To configure streams, expand the 'RTMP Streams' node in the tree on the left.");
        }

        public override List<BackgroundPlugin> BackgroundPlugins => _backgroundPlugins;

        private static Image CreateDefaultIcon()
        {
            var bmp = new Bitmap(16, 16);
            using (var g = Graphics.FromImage(bmp))
            {
                g.Clear(Color.FromArgb(200, 40, 40));
                g.FillPolygon(Brushes.White, new[]
                {
                    new PointF(5, 3), new PointF(5, 13), new PointF(13, 8)
                });
            }
            return bmp;
        }
    }
}
