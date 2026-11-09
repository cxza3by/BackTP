# BackTP

A [TShock](https://github.com/Pryaxis/TShock) plugin that saves your position before a teleport and lets you return to it with `/back`.

---

##  Features

- Automatically detects teleports via large position jumps (≥ 120 tiles).
- Saves the last position prior to a teleport.
- 5-second cooldown between `/back` uses.
- Does not save position if the player is dead.

##  Requirements

- TShock v6.1.0+
- Terraria 1.4.5.6
- .NET 9

##  Installation

1. Download the `.dll` from [Releases](../../releases).
2. Copy it to your TShock server's `ServerPlugins` folder.
3. Restart the server.

##  Commands

| Command | Description | Permission |
|---------|-------------|------------|
| `/back` | Return to your position before the last teleport. | `backtp.use` |

##  Permissions

To grant access to a group:
```
/group addperm <group> backtp.use
```
