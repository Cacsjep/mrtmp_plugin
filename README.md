# Milestone XProtect® RTMP Streamer Plugin (Unofficial/Independent)

> [!IMPORTANT]
> This is an independent open source project and is **not affiliated with, endorsed by, or supported by Milestone Systems**. XProtect® is a registered trademark of Milestone Systems A/S.

A Milestone XProtect® MIP plugin that streams live camera video to RTMP/RTMPS destinations. Pure H.264 passthrough from XProtect® cameras with silent AAC audio track -- no transcoding, no FFmpeg, no native dependencies.

```
 ┌─────────────────────────────────────────────────────────┐
 │  Milestone XProtect® Event Server (Windows Service)     │
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

## Demo

https://github.com/user-attachments/assets/dbdcbe12-9d3f-43e5-a263-813a289ad126

## Features

- **H.264 passthrough** -- no transcoding, direct from XProtect® camera to RTMP server
- **Silent AAC audio track** -- YouTube, Twitch, and Facebook require audio; automatically injected
- **RTMPS (TLS) support** -- required by Twitch and Facebook Live
- **Allow untrusted certificates** -- for self-signed RTMPS servers
- **H.264 codec validation** -- detects H.265/MJPEG cameras and reports clear error
- **Auto-restart** -- crashed helper processes are automatically restarted
- **Management Client integration** -- configure streams, see status icons (green/red/grey) in the tree
- **Live log** -- real-time helper process output visible in the Management Client detail panel
- **Milestone System Log** -- stream connect/disconnect/error/crash events visible in MC System Log
- **Multiple streams** -- one helper process per camera, fully independent

## Requirements

- Milestone XProtect® (Professional+, Expert, Corporate, or Essential+)
- Event Server (for the BackgroundPlugin)
- Management Client (for configuration)
- Cameras configured with **H.264 encoding** (H.265 and MJPEG are not supported)

## Installation

1. Download the latest release from the [Releases](../../releases) page
2. **Unblock the ZIP before extracting** -- Windows marks downloaded files as untrusted (Mark of the Web). Right-click the `.zip` → **Properties** → check **Unblock** → OK. If you skip this, Windows will block the plugin DLLs and they will fail to load.
   Alternatively, in PowerShell: `Unblock-File -Path .\RtmpStreamer-*.zip`
3. Stop the **Milestone XProtect® Event Server** service
4. Create a `MIPPlugins` folder in `C:\Program Files\Milestone\` (if it doesn't already exist)
5. Extract the release into `C:\Program Files\Milestone\MIPPlugins\RtmpStreamer\`
7. Start the **Milestone XProtect® Event Server** service
8. Open the **Management Client** -- the plugin appears under **MIP Plug-ins > RTMP Streamer**

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

The BackgroundPlugin automatically launches a helper process for each enabled stream. After saving, it may take a few seconds for the live log to reflect the updated status.

### Status Icons

The Management Client tree shows operational state icons:

| Icon | State | Meaning |
|------|-------|---------|
| Green | Streaming | Video is being sent to RTMP server |
| Normal | Starting/Connecting | Helper is initializing or connecting |
| Red | Error | Connection failed, codec error, etc. |
| Grey | Disabled | Stream is disabled via checkbox |

## Live Log

When you select an enabled stream, the detail panel shows a live log at the bottom. This log displays the most recent output from the streaming process in real time.

- The log shows the last 40 messages and refreshes automatically about twice per second
- Log lines are color-coded: **INFO** (green), **WARN** (yellow), **ERROR** (red), **DEBUG** (gray)

## Architecture

The plugin consists of three components that run in different processes:

### Management Client (Admin UI)

Provides the configuration interface. Lets you create, edit, and delete stream items. When a stream is selected, it subscribes to live status updates from the Event Server and displays them in the detail panel (status, FPS, bitrate, and a live log).

### Event Server (Background Plugin)

Runs as a background service inside the Milestone XProtect® Event Server. It reads the saved stream configurations, launches one helper process per enabled stream, and monitors their health. If a helper crashes it is restarted automatically. The Event Server also relays live status and log messages from the helpers to the Management Client.

### Helper Process (RtmpStreamerHelper.exe)

A standalone executable launched by the Event Server for each stream. It initializes the Milestone SDK, connects to the Recording Server to receive live camera frames, muxes them into FLV, and publishes to the RTMP endpoint. Each helper runs in isolation so one failing stream does not affect the others.

```
Management Client          Event Server               Helper Process
    (Admin UI)          (Background Plugin)      (RtmpStreamerHelper.exe)
        |                       |                          |
        |--- Save Config ------>|                          |
        |                       |--- Launch Process ------>|
        |                       |                          |-- Connect to RecServer
        |                       |                          |-- Connect to RTMP
        |                       |<--- stderr: STATUS ------|
        |                       |<--- stderr: STATS  ------|
        |                       |<--- stderr: log lines ---|
        |<-- MessageComm -------|                          |
        |   (live status +      |                          |
        |    log updates)       |                          |
```

## Logging

### Event Server Logs

Plugin and helper process logs are written to the standard Milestone XProtect® Event Server log:

```
C:\ProgramData\Milestone\XProtect Event Server\Logs\
```

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

## Building from Source

1. Open `RtmpStreamerPlugin.sln` in **Visual Studio** (2019 or later)
2. Restore NuGet packages (the Milestone VideoOS SDK packages require Visual Studio -- they do not resolve via CLI)
3. Build the solution (both `RtmpStreamerPlugin` and `RtmpStreamerHelper` projects)
4. Output is automatically copied to `C:\Program Files\Milestone\MIPPlugins\RtmpStreamer\`

> [!NOTE]
> The build events stop and start the Event Server service and Management Client automatically. Make sure Visual Studio is running as **Administrator**.

## Releasing a New Version

Releases are built automatically by GitHub Actions when you push a version tag.

**Steps:**

1. Update the version in `Properties/AssemblyInfo.cs` (`AssemblyVersion` and `AssemblyFileVersion`)
2. Update `VersionString` in `RtmpStreamerPluginDefinition.cs`
3. Commit the version bump
4. Tag the commit with the matching version and push:
   ```bash
   git tag v1.1.0
   git push origin main --tags
   ```
5. GitHub Actions will automatically build the Release configuration and publish a `RtmpStreamer-v1.1.0.zip` to the [Releases](../../releases) page

The CI build uses `/p:PreBuildEvent= /p:PostBuildEvent=` to skip the Milestone service stop/start that normally happens during local development builds (the service doesn't exist in CI).

## Troubleshooting

| Problem | Solution |
|---|---|
| No video on RTMP server | Check Event Server logs for helper messages. Ensure the camera is configured for H.264 encoding. |
| Helper keeps restarting | Check System Log for crash entries. Common causes: wrong management server URI, camera not found, RTMP server unreachable. |
| Status icon stays normal (not green) | Wait 10-20 seconds for status to propagate. Check logs for errors. |
| "Helper exe not found" | Ensure `RtmpStreamerHelper.exe` is in the same directory as `RtmpStreamerPlugin.dll` in MIPPlugins. |
| YouTube/Twitch rejects stream | Ensure you're using the correct URL format. Twitch and Facebook require `rtmps://`. |
| "Camera is using H.265 codec" error | Change the camera to H.264 in the Recording Server configuration. |
| RTMP connection refused | Verify the RTMP server is running and reachable. Check firewall rules for port 1935 (RTMP) or 443 (RTMPS). |
| Certificate error with RTMPS | For self-signed servers, check **Allow untrusted certificates** in the stream config. |
| DLLs blocked / plugin not loading | Right-click the original `.zip` → Properties → Unblock, then re-extract. See Installation step 2. |
| Live log shows "No response from Event Server" | The Event Server may still be starting up. Wait a few seconds and re-select the stream item. |

## Known Limitations

- **H.264 only** -- cameras must be configured with H.264 encoding (not H.265 or MJPEG)
- **Silent audio only** -- the AAC audio track is silent (camera audio is not captured)

## License

This project is licensed under the [MIT License](LICENSE).

## Disclaimer

THIS SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

This is an independent open source project and is **not affiliated with, endorsed by, or supported by Milestone Systems**. XProtect® is a registered trademark of Milestone Systems A/S.

> [!CAUTION]
> Use at your own risk. This plugin interacts directly with your Milestone installation. Always test in a non-production environment first.

## Contributing

Contributions are welcome! Feel free to open issues or submit pull requests.
