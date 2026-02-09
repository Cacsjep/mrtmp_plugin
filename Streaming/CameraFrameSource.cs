using System;
using System.Reflection;
using VideoOS.Platform;
using VideoOS.Platform.Live;

namespace RtmpStreamerPlugin.Streaming
{
    /// <summary>
    /// Wraps RawLiveSource to receive raw H.264 Annex B frames from a Milestone camera.
    /// Parses GenericByteData packets and fires an event with the extracted H.264 data.
    /// </summary>
    internal class CameraFrameSource : IDisposable
    {
        private RawLiveSource _rawSource;
        private Item _cameraItem;
        private bool _started;
        private readonly object _lock = new object();
        private long _eventsReceived;
        private long _framesEmitted;

        /// <summary>
        /// Fired when a new H.264 frame is received from the camera.
        /// </summary>
        public event Action<byte[] /* annexBData */, bool /* isKeyFrame */, DateTime /* timestamp */> FrameReceived;

        /// <summary>
        /// Fired when the source encounters an error.
        /// </summary>
        public event Action<string> Error;

        /// <summary>
        /// The camera item this source is connected to.
        /// </summary>
        public Item CameraItem => _cameraItem;

        /// <summary>
        /// Whether the source is currently receiving frames.
        /// </summary>
        public bool IsStarted => _started;

        /// <summary>
        /// Initialize the frame source for the given camera.
        /// </summary>
        public void Init(Item cameraItem)
        {
            _cameraItem = cameraItem ?? throw new ArgumentNullException(nameof(cameraItem));

            PluginLog.Info($"[FrameSource] Creating RawLiveSource for camera: {_cameraItem.Name}, FQID={_cameraItem.FQID}");

            _rawSource = new RawLiveSource(_cameraItem);
            _rawSource.LiveContentEvent += OnLiveContent;
            _rawSource.LiveStatusEvent += OnLiveStatus;
            _rawSource.Init();

            PluginLog.Info($"[FrameSource] RawLiveSource initialized, setting LiveModeStart=true");
        }

        /// <summary>
        /// Start receiving live frames.
        /// </summary>
        public void Start()
        {
            lock (_lock)
            {
                if (_started) return;
                _rawSource.LiveModeStart = true;
                _started = true;
            }
        }

        /// <summary>
        /// Stop receiving live frames.
        /// </summary>
        public void Stop()
        {
            lock (_lock)
            {
                if (!_started) return;
                _rawSource.LiveModeStart = false;
                _started = false;
            }
        }

        public void Dispose()
        {
            Stop();
            if (_rawSource != null)
            {
                _rawSource.LiveContentEvent -= OnLiveContent;
                _rawSource.LiveStatusEvent -= OnLiveStatus;
                _rawSource.Close();
                _rawSource = null;
            }
        }

        private void OnLiveStatus(object sender, EventArgs e)
        {
            try
            {
                PluginLog.Info($"[FrameSource] LiveStatusEvent: {e.GetType().Name}");
                // Dump all properties
                foreach (var prop in e.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance))
                {
                    try
                    {
                        var val = prop.GetValue(e);
                        PluginLog.Info($"[FrameSource]   Status.{prop.Name} = {val}");
                    }
                    catch { }
                }
            }
            catch { }
        }

        private void OnLiveContent(object sender, EventArgs e)
        {
            try
            {
                _eventsReceived++;

                if (_eventsReceived <= 3 || _eventsReceived % 500 == 0)
                    PluginLog.Info($"[FrameSource] Event #{_eventsReceived}, EventArgs type={e.GetType().Name}");

                // On first event, dump all properties and fields
                if (_eventsReceived == 1)
                {
                    foreach (var prop in e.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance))
                    {
                        try
                        {
                            var val = prop.GetValue(e);
                            PluginLog.Info($"[FrameSource]   Prop {prop.Name} ({prop.PropertyType.Name}) = {val}");
                        }
                        catch (Exception ex2) { PluginLog.Info($"[FrameSource]   Prop {prop.Name} -> error: {ex2.Message}"); }
                    }
                    foreach (var field in e.GetType().GetFields(BindingFlags.Public | BindingFlags.Instance))
                    {
                        try
                        {
                            var val = field.GetValue(e);
                            PluginLog.Info($"[FrameSource]   Field {field.Name} ({field.FieldType.Name}) = {val}");
                        }
                        catch (Exception ex2) { PluginLog.Info($"[FrameSource]   Field {field.Name} -> error: {ex2.Message}"); }
                    }
                }

                var args = e as LiveContentRawEventArgs;
                if (args == null)
                {
                    if (_eventsReceived <= 3)
                        PluginLog.Info($"[FrameSource] EventArgs is not LiveContentRawEventArgs, actual: {e.GetType().FullName}");
                    return;
                }

                if (args.LiveContent == null)
                {
                    if (_eventsReceived <= 3)
                        PluginLog.Info("[FrameSource] LiveContent is null");
                    return;
                }

                byte[] content = args.LiveContent.Content;
                if (content == null || content.Length <= GenericByteDataParser.HeaderSize)
                {
                    if (_eventsReceived <= 3)
                        PluginLog.Info($"[FrameSource] Content null or too small: {content?.Length ?? 0} bytes");
                    return;
                }

                if (_eventsReceived <= 3)
                    PluginLog.Info($"[FrameSource] Content received: {content.Length} bytes, first bytes: {content[0]:X2} {content[1]:X2} {content[6]:X2} {content[7]:X2}");

                if (!GenericByteDataParser.TryParse(content, out var frame))
                {
                    if (_eventsReceived <= 5)
                        PluginLog.Info($"[FrameSource] GenericByteDataParser rejected packet (dataType=0x{((content[0] << 8) | content[1]):X4})");
                    return;
                }

                if (frame.CodecType != GenericByteDataParser.CodecH264)
                {
                    if (_eventsReceived <= 5)
                        PluginLog.Info($"[FrameSource] Non-H264 codec: 0x{frame.CodecType:X4}");
                    return;
                }

                _framesEmitted++;
                if (_framesEmitted <= 3 || _framesEmitted % 500 == 0)
                    PluginLog.Info($"[FrameSource] H.264 frame #{_framesEmitted}: {frame.PayloadData.Length} bytes, keyframe={frame.IsKeyFrame}");

                FrameReceived?.Invoke(frame.PayloadData, frame.IsKeyFrame, frame.PictureTimestamp);
            }
            catch (Exception ex)
            {
                Error?.Invoke($"Frame processing error: {ex.Message}");
            }
        }
    }
}
