# Unofficial/Independent Milestone XProtect RTMP Streamer Plugin

> [!IMPORTANT]
> This is an independent open source project and is **not affiliated with, endorsed by, or supported by Milestone Systems**. XProtect is a trademark of Milestone Systems A/S.

A Milestone XProtect MIP plugin that streams live camera video to RTMP/RTMPS destinations (YouTube Live, Twitch, Facebook Live, custom RTMP servers, etc.). Pure H.264 passthrough from XProtect cameras with silent AAC audio track -- no transcoding, no FFmpeg, no native dependencies.

```
 ┌─────────────────────────────────────────────────────────┐
 │  Milestone XProtect Event Server (Windows Service)      │
 │                                                         │
 │  BackgroundPlugin                                       │
 │    - Reads config from Management Server                │
 │    - Launches one helper process per stream             │
 │    - Monitors health, auto-restarts on crash            │
 │    - Writes to Milestone System Log (LogClient)         │
 │                                                         │
 │  ┌───────────────────────────────────────────────────┐  │
 │  │  RtmpStreamerHelper.exe  (standalone MIP SDK)     │  │
 │  │                                                   │  │
 │  │  RawLiveSource ──► H.264 Annex B                  │  │
 │  │       │                                           │  │
 │  │       ▼                                           │  │
 │  │  GenericByteData Parser ──► FlvMuxer ──► RTMP(S)  │  │
 │  │       │                        │                  │  │
 │  │       │               Silent AAC audio            │  │
 │  └───────────────────────┬───────────────────────────┘  │
 └──────────────────────────│──────────────────────────────┘
                            │
                            │  RTMP/RTMPS publish
                            ▼
                  ┌─────────────────────┐
                  │  YouTube / Twitch / │
                  │  Facebook / Custom  │
                  └─────────────────────┘
```

## Features

- **H.264 passthrough** -- no transcoding, direct from XProtect camera to RTMP server
- **Silent AAC audio track** -- YouTube, Twitch, and Facebook require audio; automatically injected
- **RTMPS (TLS) support** -- required by Twitch and Facebook Live
- **Allow untrusted certificates** -- for self-signed RTMPS servers
- **H.264 codec validation** -- detects H.265/MJPEG cameras and reports clear error
- **Auto-restart** -- crashed helper processes are automatically restarted
- **Management Client integration** -- configure streams, see status icons (green/red/grey) in the tree
- **Milestone System Log** -- stream connect/disconnect/error/crash events visible in MC System Log
- **Multiple streams** -- one helper process per camera, fully independent

## How It Works

The plugin uses a **helper process architecture** to work around the XProtect Event Server limitation where `RawLiveSource` (live video API) is not available in the `EnvironmentType.Service` context.

### Helper Process Communication

```
BackgroundPlugin (Event Server)              RtmpStreamerHelper.exe
┌────────────────────────────┐               ┌──────────────────────────┐
│                            │   args        │                          │
│  Process.Start(helper.exe) │──────────────►│  serverUri, cameraId,    │
│                            │               │  rtmpUrl, milestoneDir,  │
│                            │               │  allowUntrustedCerts     │
│                            │   stderr      │                          │
│  ErrorDataReceived ◄───────│◄──────────────│  STATUS Streaming        │
│   - Parse STATUS lines     │               │  STATUS Error: ...       │
│   - Parse STATS lines      │               │  STATS fps=X frames=N   │
│   - Forward log lines      │               │  (log lines)            │
│   - Write to System Log    │               │                          │
│                            │               │  RawLiveSource ► RTMP(S) │
│                            │               │                          │
│  Process.Kill() ──────────►│───────────────│  (terminates)            │
│  Process.HasExited ────────│───────────────│  (health check)          │
└────────────────────────────┘               └──────────────────────────┘
```

- **Parent to Helper**: Command-line arguments and process lifecycle (`Kill`)
- **Helper to Parent**: stderr stream with `STATUS` lines (state changes), `STATS` lines (telemetry), and log lines
- **Health monitoring**: BackgroundPlugin checks `Process.HasExited` every 10 seconds and auto-restarts dead helpers
- **Authentication**: Helper inherits the Event Server's Windows service account via `CredentialCache.DefaultNetworkCredentials`
- **System Log**: State transitions (connected, error, stopped, crashed) are written to the Milestone System Log via `LogClient` API

## Requirements

- Milestone XProtect (Professional+, Expert, Corporate, or Essential+)
- Event Server (for the BackgroundPlugin)
- Management Client (for configuration)
- Cameras configured with **H.264 encoding** (H.265 and MJPEG are not supported)

## Installation

1. Download the latest release from the [Releases](../../releases) page
2. Stop the **Milestone XProtect Event Server** service
3. Extract the release into `C:\Program Files\Milestone\MIPPlugins\RtmpStreamer\`
4. Ensure the folder contains at minimum:
   - `RtmpStreamerPlugin.dll`
   - `RtmpStreamerHelper.exe`
   - `plugin.def`
5. Start the **Milestone XProtect Event Server** service
6. Open the **Management Client** -- the plugin appears under **MIP Plug-ins > RTMP Streamer**

> [!NOTE]
> Milestone SDK DLLs (`VideoOS.Platform.dll`, etc.) are NOT shipped with the plugin. They are loaded from the Milestone installation directories at runtime.

## Configuration

All configuration is done in the **Management Client** under **MIP Plug-ins > RTMP Streamer > RTMP Streams**.

1. Right-click **RTMP Streams** and select **Create New**
2. Enter a name for the stream
3. Click **Select camera...** to pick a camera
4. Enter the RTMP destination URL, for example:
   - YouTube: `rtmp://a.rtmp.youtube.com/live2/xxxx-xxxx-xxxx-xxxx`
   - Twitch: `rtmps://live.twitch.tv/app/live_xxxxxxxxx`
   - Facebook: `rtmps://live-api-s.facebook.com:443/rtmp/FBxxxxxxxxx`
   - Custom: `rtmp://your-server:1935/live/stream-key`
5. Check **Allow untrusted certificates** if using a self-signed RTMPS server
6. Click **Save** in the toolbar

The BackgroundPlugin automatically launches a helper process for each enabled stream.

### Status Icons

The Management Client tree shows operational state icons:

| Icon | State | Meaning |
|------|-------|---------|
| Green | Streaming | Video is being sent to RTMP server |
| Normal | Starting/Connecting | Helper is initializing or connecting |
| Red | Error | Connection failed, codec error, etc. |
| Grey | Disabled | Stream is disabled via checkbox |

## Project Structure

```
rtmp_plugin/
├── RtmpStreamerPlugin.sln
├── RtmpStreamerPlugin.csproj          # MIP plugin (DLL)
├── plugin.def                         # MIP plugin descriptor
├── RtmpStreamerPluginDefinition.cs     # Plugin entry point, icon, ItemNode
├── PluginLog.cs                       # Logging via EnvironmentManager.Instance.Log
├── SystemLog.cs                       # Milestone System Log via LogClient API
├── Admin/
│   ├── RtmpStreamerItemManager.cs     # MC tree integration, CRUD, operational state
│   ├── RtmpOverviewUserControl.cs     # Info panel for RTMP Streams node
│   ├── StreamConfigUserControl.cs     # Configuration form (name, camera, URL, options)
│   └── StreamConfigUserControl.Designer.cs
├── Background/
│   └── RtmpStreamerBackgroundPlugin.cs # Helper process manager, status, System Log
├── Streaming/
│   ├── CameraFrameSource.cs           # RawLiveSource wrapper, H.264 codec validation
│   ├── GenericByteDataParser.cs       # Milestone GenericByteData header parser
│   ├── StreamSession.cs               # Camera → FlvMuxer → RTMP pipeline, reconnect
│   └── StreamSessionManager.cs        # Session lifecycle
├── Rtmp/
│   ├── Amf0.cs                        # AMF0 reader/writer for RTMP commands
│   ├── RtmpPublisher.cs               # RTMP/RTMPS client (connect/publish/TLS)
│   ├── RtmpChunkWriter.cs             # RTMP chunk serialization
│   ├── FlvMuxer.cs                    # H.264 Annex B → FLV, AAC audio headers
│   └── SilentAacGenerator.cs          # Pre-computed silent AAC-LC frame constants
└── RtmpStreamerHelper/
    ├── RtmpStreamerHelper.csproj       # Standalone console exe
    ├── Program.cs                      # SDK init, assembly resolve, streaming loop
    └── PluginLog.cs                    # Console-based logging (stderr)
```

## Building from Source

1. Open `RtmpStreamerPlugin.sln` in **Visual Studio** (2019 or later)
2. Ensure Milestone XProtect is installed (the project references DLLs from the install directories)
3. Build the solution (both `RtmpStreamerPlugin` and `RtmpStreamerHelper` projects)
4. Output is automatically copied to `C:\Program Files\Milestone\MIPPlugins\RtmpStreamer\`

> [!NOTE]
> The build events stop and start the Event Server service and Management Client automatically. Make sure Visual Studio is running as **Administrator**.

### Build Properties

| Property | Default | Description |
|---|---|---|
| `MilestoneInstallDir` | `C:\Program Files\Milestone\XProtect Event Server\` | Path to `VideoOS.Platform.dll` |
| `MilestoneRecServerDir` | `C:\Program Files\Milestone\XProtect Recording Server\` | Path to `VideoOS.Platform.SDK.dll` |
| `MipPluginDir` | `C:\Program Files\Milestone\MIPPlugins\RtmpStreamer\` | Deployment target |

## Logging

### Event Server Logs

Plugin and helper process logs are written to the standard Milestone Event Server log:

```
C:\ProgramData\Milestone\XProtect Event Server\Logs\
```

Helper process log lines are relayed through the BackgroundPlugin with the prefix `[Helper:<CameraName>]`.

### Milestone System Log

Significant events are written to the Milestone System Log (visible in **Management Client > Logs > System Log**) under the **RTMP Streaming** category:

| Event | Severity | When |
|---|---|---|
| Stream connected | Info | Helper successfully publishing to RTMP server |
| Stream error | Error | Connection failed, TLS error, codec mismatch, etc. |
| Stream stopped | Info | Stream stopped normally |
| Helper crashed | Warning | Helper process died unexpectedly, auto-restarting |
| Plugin started | Info | BackgroundPlugin initialized with N active streams |
| Plugin stopped | Info | BackgroundPlugin shutting down |

## Troubleshooting

| Problem | Solution |
|---|---|
| No video on RTMP server | Check Event Server logs for `[Helper:...]` messages. Ensure the camera is configured for H.264 encoding. |
| Helper keeps restarting | Check System Log for crash entries. Common causes: wrong management server URI, camera not found, RTMP server unreachable. |
| Status icon stays normal (not green) | Wait 10-20 seconds for status to propagate. Check logs for errors. |
| "Helper exe not found" | Ensure `RtmpStreamerHelper.exe` is in the same directory as `RtmpStreamerPlugin.dll` in MIPPlugins. |
| YouTube/Twitch rejects stream | Ensure you're using the correct URL format. Twitch and Facebook require `rtmps://`. |
| "Camera is using H.265 codec" error | Change the camera to H.264 in the Recording Server configuration. |
| RTMP connection refused | Verify the RTMP server is running and reachable. Check firewall rules for port 1935 (RTMP) or 443 (RTMPS). |
| Certificate error with RTMPS | For self-signed servers, check **Allow untrusted certificates** in the stream config. |
| Constant config change notifications | This was fixed by only persisting significant status changes. If still occurring, check for other plugins writing to the same items. |

## Known Limitations

- **H.264 only** -- cameras must be configured with H.264 encoding (not H.265 or MJPEG)
- **Silent audio only** -- the AAC audio track is silent (camera audio is not captured)
- **One helper per stream** -- each camera-to-RTMP mapping runs in its own process
- **Windows only** -- requires Milestone XProtect which runs on Windows
- **No transcoding** -- the H.264 stream is passed through as-is; resolution and bitrate are determined by the camera configuration in XProtect

## License

This project is licensed under the [MIT License](LICENSE).

## Disclaimer

THIS SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.

This is an independent open source project and is **not affiliated with, endorsed by, or supported by Milestone Systems**. XProtect is a trademark of Milestone Systems A/S.

> [!CAUTION]
> Use at your own risk. This plugin interacts directly with your Milestone installation. Always test in a non-production environment first.

## Contributing

Contributions are welcome! Feel free to open issues or submit pull requests.
