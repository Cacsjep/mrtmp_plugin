using System;
using System.Threading;
using RtmpStreamerPlugin.Rtmp;
using VideoOS.Platform;

namespace RtmpStreamerPlugin.Streaming
{
    /// <summary>
    /// Orchestrates streaming from one camera to one RTMP destination.
    /// Pipeline: CameraFrameSource → FlvMuxer → RtmpPublisher
    /// </summary>
    internal class StreamSession : IDisposable
    {
        private CameraFrameSource _frameSource;
        private FlvMuxer _muxer;
        private RtmpPublisher _publisher;

        private readonly Item _cameraItem;
        private readonly string _rtmpUrl;
        private readonly string _sessionId;

        private Thread _reconnectThread;
        private CancellationTokenSource _cts;
        private volatile bool _running;
        private DateTime _startTime;
        private DateTime _streamEpoch;
        private bool _streamEpochSet;

        // Stats
        private long _framesSent;
        private long _bytesSent;
        private long _keyFramesSent;
        private string _lastError;

        public string SessionId => _sessionId;
        public string CameraName => _cameraItem?.Name ?? "Unknown";
        public Guid CameraId => _cameraItem?.FQID?.ObjectId ?? Guid.Empty;
        public string RtmpUrl => _rtmpUrl;
        public bool IsRunning => _running;
        public long FramesSent => _framesSent;
        public long BytesSent => _bytesSent;
        public long KeyFramesSent => _keyFramesSent;
        public string LastError => _lastError;
        public TimeSpan Uptime => _running ? DateTime.UtcNow - _startTime : TimeSpan.Zero;

        public StreamSession(Item cameraItem, string rtmpUrl)
        {
            _cameraItem = cameraItem ?? throw new ArgumentNullException(nameof(cameraItem));
            _rtmpUrl = rtmpUrl ?? throw new ArgumentNullException(nameof(rtmpUrl));
            _sessionId = Guid.NewGuid().ToString("N").Substring(0, 8);
        }

        /// <summary>
        /// Start the streaming session. Connects to camera and RTMP server.
        /// </summary>
        public void Start()
        {
            if (_running) return;

            _cts = new CancellationTokenSource();
            _running = true;
            _startTime = DateTime.UtcNow;
            _framesSent = 0;
            _bytesSent = 0;
            _keyFramesSent = 0;
            _lastError = null;
            _streamEpochSet = false;

            _reconnectThread = new Thread(RunLoop)
            {
                IsBackground = true,
                Name = $"RTMP-Session-{_sessionId}"
            };
            _reconnectThread.Start();
        }

        /// <summary>
        /// Stop the streaming session.
        /// </summary>
        public void Stop()
        {
            _running = false;
            _cts?.Cancel();

            Cleanup();

            if (_reconnectThread != null && _reconnectThread.IsAlive)
                _reconnectThread.Join(5000);

            _reconnectThread = null;
        }

        public void Dispose()
        {
            Stop();
            _cts?.Dispose();
        }

        private void RunLoop()
        {
            while (_running && !_cts.IsCancellationRequested)
            {
                try
                {
                    Log($"Connecting to RTMP server: {_rtmpUrl}");

                    // Initialize components
                    _muxer = new FlvMuxer();
                    _publisher = new RtmpPublisher();
                    _frameSource = new CameraFrameSource();

                    // Connect to RTMP server
                    _publisher.Connect(_rtmpUrl);
                    Log("RTMP connected, starting camera frame source");

                    // Start camera frame source
                    _frameSource.FrameReceived += OnFrameReceived;
                    _frameSource.Error += OnFrameError;
                    _frameSource.Init(_cameraItem);
                    _frameSource.Start();

                    Log("Streaming started");

                    // Wait until cancelled or error
                    _cts.Token.WaitHandle.WaitOne();
                }
                catch (Exception ex)
                {
                    _lastError = ex.Message;
                    Log($"Error: {ex.Message}");
                }

                Cleanup();

                // If still running, wait before reconnecting
                if (_running && !_cts.IsCancellationRequested)
                {
                    Log("Reconnecting in 5 seconds...");
                    _cts.Token.WaitHandle.WaitOne(5000);
                    _streamEpochSet = false;
                }
            }
        }

        private void OnFrameReceived(byte[] annexBData, bool isKeyFrame, DateTime timestamp)
        {
            try
            {
                if (!_running || _publisher == null || !_publisher.IsPublishing)
                    return;

                // Calculate RTMP timestamp (ms since stream start)
                if (!_streamEpochSet)
                {
                    _streamEpoch = timestamp;
                    _streamEpochSet = true;
                }

                uint rtmpTimestamp = (uint)(timestamp - _streamEpoch).TotalMilliseconds;
                if (rtmpTimestamp < 0) rtmpTimestamp = 0;

                // Mux the frame
                byte[] flvPayload = _muxer.MuxFrame(annexBData, isKeyFrame, out byte[] sequenceHeader);

                // Send sequence header first if we just got SPS/PPS
                if (sequenceHeader != null)
                {
                    Log($"Sending AVC sequence header ({sequenceHeader.Length} bytes)");
                    _publisher.SendVideoData(sequenceHeader, rtmpTimestamp);
                    _bytesSent += sequenceHeader.Length;
                }

                // Send the video frame
                if (flvPayload != null)
                {
                    _publisher.SendVideoData(flvPayload, rtmpTimestamp);
                    _framesSent++;
                    _bytesSent += flvPayload.Length;
                    if (isKeyFrame) _keyFramesSent++;

                    if (_framesSent <= 3 || _framesSent % 500 == 0)
                        Log($"Frame #{_framesSent} sent: {flvPayload.Length} bytes, ts={rtmpTimestamp}ms, keyframe={isKeyFrame}");
                }
            }
            catch (Exception ex)
            {
                _lastError = ex.Message;
                Log($"Send error: {ex.Message}");
                // Trigger reconnect by cancelling
                try { _cts?.Cancel(); } catch { }
            }
        }

        private void OnFrameError(string message)
        {
            _lastError = message;
            Log($"Frame source error: {message}");
        }

        private void Cleanup()
        {
            try
            {
                if (_frameSource != null)
                {
                    _frameSource.FrameReceived -= OnFrameReceived;
                    _frameSource.Error -= OnFrameError;
                    _frameSource.Dispose();
                    _frameSource = null;
                }
            }
            catch { }

            try { _publisher?.Dispose(); _publisher = null; } catch { }
            _muxer = null;
        }

        private void Log(string message)
        {
            PluginLog.Info($"[Session:{_sessionId}] {message}");
        }
    }
}
