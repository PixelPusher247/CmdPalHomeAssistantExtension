# Home Assistant Extension

Control Home Assistant entities from [PowerToys Command Palette](https://learn.microsoft.com/en-us/windows/powertoys/command-palette/overview)

## Installation

### MSIX (Recommended)

Download `HomeAssistantExtension-<version>.msixbundle` and `HomeAssistantExtension.cer` from [Releases](https://github.com/PixelPusher247/CmdPalHomeAssistantExtension/releases).

**First time on a new PC (requires Administrator — do this once):**

1. Double-click `HomeAssistantExtension.cer` to open the Certificate Import Wizard.
2. Select **Local Machine** and click Next.
3. Choose **Place all certificates in the following store** → Browse → **Trusted People**.
4. Click Next, then Finish.
5. Repeat steps 1–4, but place the certificate in the **Trusted Root Certification Authorities** store.

**Install the extension:**

Open PowerShell and run:

```powershell
Remove-AppxPackage -Name 'PixelPusher247.HomeAssistantExtension' -ErrorAction SilentlyContinue
Add-AppxPackage .\HomeAssistantExtension-<version>.msixbundle
```

Then **restart PowerToys** — the Home Assistant extension will appear in Command Palette.

> **Updating?** Skip the certificate steps. Just run the two PowerShell commands above with the new `.msixbundle`.

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
