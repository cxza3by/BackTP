# BackTP

A [TShock](https://github.com/Pryaxis/TShock) plugin that saves your position before a teleport and lets you return to it with `/back`.

---

##  Features

- Automatically detects teleports via large position jumps (configurable, default: ≥ 120 tiles).
- Saves the last position prior to a teleport.
- Configurable cooldown between `/back` uses (default: 5 seconds).
- Does not save position if the player is dead.

##  Requirements

- TShock 6.0.0 – 6.2.1 (Terraria 1.4.5.5 – 1.4.5.8)
- .NET 9

The plugin is built against TShock 6.0.0 (Terraria 1.4.5.5) and uses only APIs
available in every TShock release for Terraria 1.4.5.x, so the same build works
on TShock 6.0.0, 6.1.0, and 6.2.1.

##  Installation

1. Download the `.dll` from [Releases](../../releases).
2. Copy it to your TShock server's `ServerPlugins` folder.
3. Restart the server.

On first start the plugin creates `BackTP.json` in the TShock save directory
(`tshock/BackTP.json` by default).

##  Configuration

```json
{
  "Settings": {
    "TeleportThresholdTiles": 120,
    "BackCooldownSeconds": 5
  }
}
```

| Setting | Default | Description |
|---------|---------|-------------|
| `TeleportThresholdTiles` | `120` | Minimum position change, in tiles, treated as a teleport. |
| `BackCooldownSeconds` | `5` | Cooldown between `/back` uses, in seconds. Set to `0` to disable it. |

Invalid values are replaced with their defaults and the config is rewritten.
Restart the server after changing the file.

##  Commands

| Command | Description | Permission |
|---------|-------------|------------|
| `/back` | Return to your position before the last teleport. | `backtp.use` |

##  Permissions

To grant access to a group:
```
/group addperm <group> backtp.use
```

##  Building

The default build targets TShock 6.0.0 for maximum compatibility:

```
dotnet build
```

To build against a newer TShock release, override `TShockVersion`:

```
dotnet build -p:TShockVersion=6.2.1
```
