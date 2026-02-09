# MIP SDK Knowledge Base

## Milestone XProtect MIP SDK - Admin Plugin Patterns

### ItemManager Lifecycle

1. `Init()` - Called once when environment initializes
2. `GenerateDetailUserControl()` - Called once when detail panel first shown. Create UserControl, subscribe events here.
3. `FillUserControl(Item item)` - Called each time user selects an item in tree. Set `CurrentItem = item`, call `_userControl.FillContent(item)`.
4. `ClearUserControl()` - Called when item deselected. Set `CurrentItem = null`, clear UI.
5. `ValidateAndSaveUserControl()` - Called by MC when user clicks Save/Apply or navigates away. This is where `UpdateItem()` + `SaveItemConfiguration()` happens. Always return `true`.
6. `ReleaseUserControl()` - Called when control no longer needed. Unsubscribe events, set `_userControl = null`.
7. `Close()` - Called on shutdown.

### ConfigurationChangedByUser Event Pattern

- **Declaration**: `internal event EventHandler ConfigurationChangedByUser;` in UserControl
- **Handler**: Use the INHERITED `ConfigurationChangedByUserHandler` from `ItemManager` base class
- **Purpose**: Marks item as dirty in MC. Does NOT save. Save happens in `ValidateAndSaveUserControl()`.
- **Subscribe**: `_userControl.ConfigurationChangedByUser += new EventHandler(ConfigurationChangedByUserHandler);`
- **Unsubscribe**: `_userControl.ConfigurationChangedByUser -= new EventHandler(ConfigurationChangedByUserHandler);` in `ReleaseUserControl()`

### OnUserChange Pattern

```csharp
// In UserControl - must be internal
internal void OnUserChange(object sender, EventArgs e)
{
    if (ConfigurationChangedByUser != null)
        ConfigurationChangedByUser(this, new EventArgs());
}
```

- Wire to `TextChanged`, `CheckedChanged`, etc. in Designer
- Also call manually from button click handlers (camera select, etc.)
- Fires on every keystroke for text fields - this is expected

### Event Suppression During FillContent

Some samples use a flag to prevent false dirty state during programmatic `FillContent`:
```csharp
private bool _ignoreChanges = false;

internal void FillContent(Item item)
{
    _ignoreChanges = true;
    // ... set UI values ...
    _ignoreChanges = false;
}

internal void OnUserChange(object sender, EventArgs e)
{
    if (ConfigurationChangedByUser != null && _ignoreChanges == false)
        ConfigurationChangedByUser(this, new EventArgs());
}
```
Used in: ServiceTest, AdminTabHardware, AdminTabPlugin.
NOT used in: SensorMonitor, ConfigDump, LicenseRegistration (they allow event during fill).

### UpdateItem Pattern

- Called only from `ValidateAndSaveUserControl()`, NOT on every change
- Guard nullable properties: `if (_selectedItem != null) item.Properties["Key"] = value;`
- Name is always safe to write: `item.Name = _txtName.Text;`
- No validation in UpdateItem - pure data transfer

### CreateItem Pattern

```csharp
public override Item CreateItem(Item parentItem, FQID suggestedFQID)
{
    CurrentItem = new Item(suggestedFQID, "Default Name");
    CurrentItem.Properties["Enabled"] = "Yes";
    if (_userControl != null)
        _userControl.FillContent(CurrentItem);
    Configuration.Instance.SaveItemConfiguration(PluginId, CurrentItem);
    return CurrentItem;
}
```

- FillContent BEFORE SaveItemConfiguration (standard order)
- Item is minimal at creation (just Name and default properties)
- SaveItemConfiguration creates item on server immediately

### Properties Storage

- All custom data stored as `string` key-value pairs in `Item.Properties`
- Camera references: store `FQID.ObjectId.ToString()` as GUID string, resolve with `Configuration.Instance.GetItem(guid, Kind.Camera)`
- Boolean flags: store as `"Yes"` / `"No"` strings
- Complex data: serialize to XML string

### ItemsAllowed

- `ItemsAllowed.Many` - Multiple items under the tree node, user right-clicks "Create New"
- `ItemsAllowed.One` - Single item, auto-created. Requires FQID construction which is complex. Prefer `Many`.

### FQID Constructors (VideoOS.Platform.FQID)

```
FQID()
FQID(ServerId serverId)
FQID(ServerId serverId, FolderType folderType, Guid kind)
FQID(ServerId serverId, Guid parentId, Guid objectId, FolderType folderType, Guid kind)
FQID(ServerId serverId, Guid parentId, String objectIdString, FolderType folderType, Guid kind)
FQID(String xmlString)
FQID(String xmlString, UserContext context)
FQID(XmlNode xmlNode)
```

### No Re-fetch Before Save

- Items are NOT re-fetched from server before saving
- `CurrentItem` reference is kept and modified directly
- Works because admin UI is single-user per session
- If BackgroundPlugin writes status to same items, avoid writing status during config change handling (use change detection snapshot)

### BackgroundPlugin Config Change Detection

When BackgroundPlugin writes status properties to the same items:
- Use a snapshot of config-only properties (not status) to detect real changes
- Only reload helpers when actual config (CameraId, RtmpUrl, Enabled) changes
- Ignore changes that are just status writes (Status, StartTime, Restarts)

### Environment Types

- `EnvironmentType.Service` - Event Server (Windows service). `RawLiveSource` does NOT work here.
- `EnvironmentType.Administration` - Management Client. Admin UI runs here.
- Standalone mode - Helper processes with `VideoOS.Platform.SDK.Environment.Initialize()`. `RawLiveSource` works here.

### Helper Process Architecture

When live video APIs (`RawLiveSource`) are needed but only work in Standalone mode:
- BackgroundPlugin launches standalone helper .exe processes
- Helper uses `VideoOS.Platform.SDK.Environment.Initialize()` + `AddServer()` + `Login()` + `VideoOS.Platform.SDK.Media.Environment.Initialize()`
- Windows auth via `CredentialCache.DefaultNetworkCredentials` (inherits service account)
- Communication via stderr (STATS lines parsed by BackgroundPlugin)
- Monitor thread checks process health, auto-restarts on crash

### Management Server URI

```csharp
var serverId = EnvironmentManager.Instance.MasterSite.ServerId;
var uri = $"{serverId.ServerScheme}://{serverId.ServerHostname}:{serverId.Serverport}";
```

- `MasterSite` is type `FQID`, has `.ServerId` property
- Do NOT use `MasterSite.FQID.ServerId` (FQID doesn't have FQID property)

### SDK Method Deprecation

- `Environment.AddServer(Uri, NetworkCredential)` deprecated but works. New: `AddServer(bool, Uri, NetworkCredential, bool)`
- `Environment.Login(Uri)` deprecated but works. New: `Login(Uri, Guid, string, string, string, bool)`
- These warnings are harmless.

### Locale Issues

- Always use `CultureInfo.InvariantCulture` for formatting/parsing numbers
- German locale formats `25.0` as `25,0` (comma instead of period)
- Affects: STATS line formatting in helper, any double.ToString() / double.TryParse()

### Build Events (Pre/Post Build)

- `timeout` command does NOT work in MSBuild (stdin redirected). Use `ping -n 4 127.0.0.1 >nul` for delays.
- `xcopy /y /d /e /i` for recursive copy with directory creation
- Add `2>nul` to suppress sharing violation errors during copy
- Stop Event Server service before build, restart after
- Kill admin client and helper processes before build

### Assembly Resolution for Helper Process

```csharp
AppDomain.CurrentDomain.AssemblyResolve += (sender, args) =>
{
    var name = new AssemblyName(args.Name).Name + ".dll";
    foreach (var dir in searchDirs)
    {
        var path = Path.Combine(dir, name);
        if (File.Exists(path))
            return Assembly.LoadFrom(path);
    }
    return null;
};
```

Search dirs: plugin folder, Milestone install dir, Recording Server dir.
