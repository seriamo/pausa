# Pausa for Windows

The Windows version of Pausa: a tray app that flashes a gentle glow every few minutes so you look away from the screen. It uses the same defaults and step values as the macOS app.

- **Needs** Windows 10 (1809) or later. Windows 11 is recommended. Builds are available for x64 and Arm64.
- **Flash modes:** Top Glow and Screen Border.
- **UI:** left-click the tray icon for the Windows 11-style settings flyout; right-click for a short menu.
- **Pauses** while the screen is locked and, optionally, while a full-screen app is in front.

## Layout

| Path | What |
|---|---|
| `src/Pausa.Core` | Platform-neutral logic (`net10.0`): timer, settings, step values, flash curve, overlay and flyout geometry, update check, telemetry. Builds and tests anywhere. |
| `src/Pausa.Windows` | The WPF app (`net10.0-windows`): tray icon, layered overlay windows, flyout, About window, startup toggle, smoke test. |
| `tests/Pausa.Core.Tests` | xUnit tests for `Pausa.Core`. |
| `installer/Pausa.iss` | Per-user Inno Setup installer. No admin prompt; installs to `%LOCALAPPDATA%\Programs\Pausa`. |
| `tools/make_ico.swift` | Regenerates `Assets/Pausa.ico` from the macOS app icon (run on a Mac). |
| `Directory.Build.props` | The **single source of the Windows version**. |

## Build and test

Requires the .NET 10 SDK. Everything compiles on macOS and Linux too (`EnableWindowsTargeting`), but only Windows can run the app.

```bash
dotnet build windows/Pausa.slnx
dotnet test windows/Pausa.slnx
```

Publish a self-contained, single-file `Pausa.exe`:

```bash
dotnet publish windows/src/Pausa.Windows -c Release -r win-x64 -o windows/artifacts/publish/win-x64
# or -r win-arm64
```

Build the installer on Windows with Inno Setup 6.3+:

```powershell
iscc /DAppVersion=1.0.0 /DArch=x64 windows\installer\Pausa.iss   # → windows\artifacts\dist\Pausa-Setup-x64.exe
```

Installer switches, besides Inno's usual `/VERYSILENT` and `/TASKS=startup`: `/NOLAUNCH` stops it reopening Pausa after a silent install.

Regenerate the icon (macOS):

```bash
swift windows/tools/make_ico.swift windows/src/Pausa.Windows/Assets/Pausa.ico
```

## Smoke test

`Pausa.exe --smoke-test <dir>` runs the real app against throwaway settings. It never touches the network or the registry. It opens the flyout, menu and About window in light and dark themes, fires both flash modes, and writes screenshots plus `report.json` to `<dir>`. Exit code 0 means every step passed.

Set `PAUSA_FAKE_UPDATE=9.9` to show the update banner without a network call.

App data:
- Settings: `%APPDATA%\Seriamo\Pausa\settings.json`
- Log: `%LOCALAPPDATA%\Seriamo\Pausa\pausa.log`

## CI

The [Windows workflow](../.github/workflows/windows.yml) runs on every push that touches `windows/`:

1. Tests.
2. Publishes x64 and Arm64.
3. Smoke-tests x64 on `windows-latest`.
4. Builds both installers, then installs and uninstalls the x64 installer.
5. Smoke-tests the Arm64 exe on `windows-11-arm`.

The `pausa-windows-<version>` artifact holds the four executables and `SHA256SUMS.txt`.

## Releasing

1. Bump `<Version>` in `windows/Directory.Build.props`.
2. Tag and push:

   ```bash
   git tag windows-v1.0.0 && git push origin windows-v1.0.0
   ```

The tag must equal `windows-v` plus that version, or the build fails. The workflow then creates a GitHub release titled "Pausa for Windows X.Y.Z" with these assets:
- `Pausa-Setup-x64.exe`
- `Pausa-Setup-arm64.exe`
- `Pausa-x64.exe`
- `Pausa-arm64.exe`
- `SHA256SUMS.txt`

Windows releases are **never marked latest**, because the macOS app's update check and the README's `Pausa.dmg` link read `/releases/latest`. A second workflow, [`release-latest-guard.yml`](../.github/workflows/release-latest-guard.yml), puts a macOS release back as latest if one is ever changed by hand. The Windows app asks `https://updates.seriamo.com/v1/products/pausa/releases/latest?platform=windows` once a day. That answer comes from the Seriamo release manifest (`seriamo-platform/workers/delivery/src/manifest.ts`), so add each Windows release there, with its per-architecture assets and SHA-256s, after CI publishes it. The website's download button uses the same manifest through `/v1/products/pausa/download?platform=windows&arch=x64|arm64`.

### Code signing

Unsigned builds work, but SmartScreen warns about them. Signing uses [Azure Artifact Signing](https://learn.microsoft.com/azure/artifact-signing/). It runs only for `windows-v*` tags, and only when these repository (or `windows-release` environment) settings exist:

| Kind | Name |
|---|---|
| Variable | `AZURE_SIGNING_ACCOUNT`: the signing account name |
| Variable | `AZURE_SIGNING_ENDPOINT`: e.g. `https://eus.codesigning.azure.net/` |
| Variable | `AZURE_SIGNING_PROFILE`: the certificate profile name |
| Secret | `AZURE_CLIENT_ID`, `AZURE_TENANT_ID`, `AZURE_SUBSCRIPTION_ID`: an Entra app registration with the *Artifact Signing Certificate Profile Signer* role |

Tag builds run in the `windows-release` environment. That lets a single federated credential cover every release; give it the subject `repo:seriamo/pausa:environment:windows-release`.

The workflow signs both `Pausa.exe` builds before packaging, then signs both installers. It doesn't sign the Inno uninstaller.

## Telemetry and feedback

Pausa uses the same Seriamo endpoints on Windows and macOS. Every network address is in [`Endpoints.cs`](src/Pausa.Core/Endpoints.cs).

- **Usage:** on launch, the app sends one `app_launch` event to `https://telemetry.seriamo.com/v1/events`. The event holds the app version, `platform: "windows"` and a random install ID. A failed send is retried once after 30 seconds with the same event ID. **Share anonymous usage** in the flyout turns this off and deletes the install ID.
- **Updates:** a daily check against `updates.seriamo.com`. It hides releases whose `min_os_version` is newer than this Windows build.
- **Feedback:** **Feedback** in the flyout footer and **Send feedback** in the tray menu open `https://feedback.seriamo.com/?product=pausa&platform=windows&version=…` in the browser.
