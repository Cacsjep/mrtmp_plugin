# Unofficial/Independent Milestone XProtect RTMP Streamer Plugin

> [!IMPORTANT]
> This is an independent open source project and is **not affiliated with, endorsed by, or supported by Milestone Systems**. XProtect is a trademark of Milestone Systems A/S.

A Milestone XProtect MIP plugin that streams live camera video to RTMP destinations (YouTube Live, Twitch, custom RTMP servers, etc.). Pure H.264 passthrough from XProtect cameras -- no transcoding, no FFmpeg, no native dependencies.

```
 ┌─────────────────────────────────────────────────────────┐
 │  Milestone XProtect Event Server (Windows Service)      │
 │                                                         │
 │  BackgroundPlugin                                       │
 │    - Reads config from Management Server                │
 │    - Launches one helper process per stream             │
 │    - Monitors health, auto-restarts on crash            │
 │                                                         │
 │  ┌───────────────────────────────────────────────────┐  │
 │  │  RtmpStreamerHelper.exe  (standalone MIP SDK)     │  │
 │  │                                                   │  │
 │  │  RawLiveSource ──► H.264 Annex B                  │  │
 │  │       │                                           │  │
 │  │       ▼                                           │  │
 │  │  GenericByteData Parser ──► FlvMuxer ──► RTMP     │  │
 │  └───────────────────────┬───────────────────────────┘  │
 └──────────────────────────│──────────────────────────────┘
                            │
                            │  RTMP publish
                            ▼
                  ┌─────────────────────┐
                  │  YouTube / Twitch / │
                  │  Custom RTMP Server │
                  └─────────────────────┘
```

## How It Works

The plugin uses a **helper process architecture** to work around the XProtect Event Server limitation where `RawLiveSource` (live video API) is not available in the `EnvironmentType.Service` context.

### Helper Process Communication

```
BackgroundPlugin (Event Server)              RtmpStreamerHelper.exe
┌────────────────────────────┐               ┌──────────────────────────┐
│                            │   stdin       │                          │
│  Process.Start(helper.exe) │──────────────►│  args: serverUri,        │
│                            │   args        │        cameraId,         │
│                            │               │        rtmpUrl           │
│                            │               │                          │
│                            │   stderr      │  MIP SDK standalone mode │
│  ErrorDataReceived ◄───────│◄──────────────│  - Log lines (INFO/ERR)  │
│   - Parse STATS lines      │               │  - STATS fps=X frames=N  │
│   - Forward logs            │               │                          │
│                            │               │  RawLiveSource ► RTMP    │
│                            │               │                          │
│  Process.Kill() ──────────►│───────────────│  (terminates)            │
│  Process.HasExited ────────│───────────────│  (health check)          │
└────────────────────────────┘               └──────────────────────────┘
```

- **Parent to Helper**: Command-line arguments (`serverUri`, `cameraId`, `rtmpUrl`, `milestoneDir`) and process lifecycle (`Kill`)
- **Helper to Parent**: stderr stream containing log lines and periodic `STATS` lines with frame count, FPS, and byte counters
- **Health monitoring**: The BackgroundPlugin checks `Process.HasExited` every 10 seconds and auto-restarts dead helpers
- **Authentication**: The helper inherits the Event Server's Windows service account via `CredentialCache.DefaultNetworkCredentials`

## Requirements

- Milestone XProtect (Professional+, Expert, Corporate, or Essential+)
- Event Server (for the BackgroundPlugin)
- Management Client (for configuration)
- Cameras configured with H.264 encoding

## Installation

1. Download the latest release from the [Releases](../../releases) page
2. Stop the **Milestone XProtect Event Server** service
3. Extract the release into `C:\Program Files\Milestone\MIPPlugins\RtmpStreamer\`
4. Ensure the folder contains at minimum:
   - `RtmpStreamerPlugin.dll`
   - `RtmpStreamerHelper.exe`
   - `plugin.def`
   - Milestone SDK DLLs (`VideoOS.Platform.dll`, `VideoOS.Platform.SDK.dll`, `VideoOS.Platform.SDK.Media.dll`, etc.)
5. Start the **Milestone XProtect Event Server** service
6. Open the **Management Client** -- the plugin appears under **MIP Plug-ins > RTMP Streamer**

## Configuration

All configuration is done in the **Management Client** under **MIP Plug-ins > RTMP Streamer > RTMP Streams**.

1. Click **RTMP Streams** in the tree
2. Click **Select...** to pick a camera
3. Enter the RTMP destination URL (e.g., `rtmp://a.rtmp.youtube.com/live2/your-stream-key`)
4. Click **Add Stream**
5. Click **Save** in the toolbar

The BackgroundPlugin will automatically launch a helper process for the stream. The status grid updates every 5 seconds showing:

| Column | Description |
|---|---|
| **Camera** | Camera name from XProtect |
| **RTMP URL** | Destination RTMP URL |
| **Status** | `Streaming`, `Stopped`, or `Configured` |
| **FPS** | Current frames per second |
| **Frames** | Total frames sent since start |
| **Uptime** | Time since helper process started |

## Project Structure

```
rtmp_plugin/
├── RtmpStreamerPlugin.sln
├── RtmpStreamerPlugin.csproj          # MIP plugin (DLL)
├── plugin.def                         # MIP plugin descriptor
├── RtmpStreamerPluginDefinition.cs     # Plugin entry point
├── PluginLog.cs                       # Logging via Milestone SDK
├── Admin/
│   ├── RtmpStreamerItemManager.cs     # Management Client tree integration
│   ├── StreamConfigUserControl.cs     # Configuration UI with live status
│   └── StreamConfigUserControl.Designer.cs
├── Background/
│   └── RtmpStreamerBackgroundPlugin.cs # Helper process manager
├── Streaming/
│   ├── CameraFrameSource.cs           # RawLiveSource wrapper (H.264)
│   ├── GenericByteDataParser.cs       # Milestone GenericByteData header parser
│   ├── StreamSession.cs               # Camera → FlvMuxer → RTMP pipeline
│   └── StreamSessionManager.cs        # Config persistence (XML)
├── Rtmp/
│   ├── Amf0.cs                        # AMF0 reader/writer
│   ├── RtmpPublisher.cs               # RTMP client (connect/publish)
│   ├── RtmpChunkWriter.cs             # RTMP chunk serialization
│   └── FlvMuxer.cs                    # H.264 Annex B → FLV conversion
└── RtmpStreamerHelper/
    ├── RtmpStreamerHelper.csproj       # Standalone console exe
    ├── Program.cs                      # SDK init, streaming, stats reporting
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

Plugin logs are written to the standard Milestone Event Server log:

```
C:\ProgramData\Milestone\XProtect Event Server\Logs\
```

Helper process logs are relayed through the BackgroundPlugin with the prefix `[Helper:<CameraName>]`.

## Troubleshooting

| Problem | Solution |
|---|---|
| No video on RTMP server | Check Event Server logs for `[Helper:...]` messages. Ensure the camera is configured for H.264 encoding. |
| Helper keeps restarting | Check for `Fatal error:` in the logs. Common causes: wrong management server URI, camera not found, RTMP server unreachable. |
| Status shows "Configured" but not "Streaming" | The Event Server may not have started the helper yet. Wait 10 seconds and refresh. Check logs for errors. |
| "Helper exe not found" | Ensure `RtmpStreamerHelper.exe` is in the same directory as `RtmpStreamerPlugin.dll` in MIPPlugins. |
| Management Client tree shows truncated name | Rebuild and redeploy the plugin. The `SharedNodeName` should be "RTMP Streamer". |
| FPS shows 0 or "-" | The helper reports stats every 5 seconds. Wait for the first stats update. If FPS stays at 0, the camera may not be delivering frames. |
| RTMP connection refused | Verify the RTMP server is running and reachable from the Event Server machine. Check firewall rules. |

## Known Limitations

- **H.264 only** -- cameras must be configured with H.264 encoding (not HEVC/H.265 or MJPEG)
- **No audio** -- only video is streamed
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
