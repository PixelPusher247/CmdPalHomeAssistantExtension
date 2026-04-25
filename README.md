# Home Assistant Extension

Control Home Assistant entities from [PowerToys Command Palette](https://learn.microsoft.com/en-us/windows/powertoys/command-palette/overview)

## Installation

Download the `.exe` installer from [Releases](https://github.com/PixelPusher247/CmdPalHomeAssistantExtension/releases).

## Setup

1. Open Command Palette, search for **Home Assistant**.
2. Select **Settings** and enter HA URL and a [Long-Lived Access Token](https://www.home-assistant.io/docs/authentication/).
3. Toggle on the entity types (lights, switches, etc.).
4. Entities will appear in the main list, selecting toggles the entity state.

Settings are stored at `%LocalAppData%\HomeAssistantExtension\settings.json`. The access token is saved in Windows Credential Manager.

## Building

Requires .NET 9 SDK and Visual Studio 2022.

```bash
git clone <repo-url>
cd HomeAssistantExtension
dotnet restore
```

1. Open `HomeAssistantExtension.sln`
2. Select the `HomeAssistantExtension.csproj`
3. In the Build menu click Deploy Home Assistant Extension
4. In PowerToys Command Palette type `Reload` to reload all extensions
5. `Home Assistant` should appear in the list

To debug the extension easiest way is to attach to the process in VS (Ctrl+Alt+P) and search for HomeAssistantExtension

CI builds are defined in `.github/workflows/release-extension.yml`.

## License

MIT
