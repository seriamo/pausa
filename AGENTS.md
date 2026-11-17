# Pausa: instructions for AI coding agents

Pausa is a small eye-break reminder (the 20-20-20 rule) published by Seriamo (Masieri Ventures LLC).
It flashes a click-through overlay on the screen every few minutes. This public repo holds:

| Path | What |
|---|---|
| `Sources/Pausa/` | macOS menu-bar app: Swift 6 + AppKit, no third-party dependencies, macOS 14+. About 1,600 lines in 7 files. |
| `windows/` | Windows port: C# .NET 10 + WPF. See `windows/README.md`. |
| `Scripts/` | `bundle.sh` (Developer ID DMG for GitHub Releases), `bundle-appstore.sh` (Mac App Store `.pkg`), entitlements, icon. |
| `appstore/` | Mac App Store listing sources, review history and store-build notes. See `appstore/README.md`. |
| `.github/workflows/` | `windows.yml` (CI and Windows releases), `release-latest-guard.yml`. |

**Start with [`HANDOFF.md`](HANDOFF.md).** It has the current status, pitfalls and next steps.
Detailed docs: [`appstore/README.md`](appstore/README.md) (App Store Connect state, store build, App
Review), [`windows/README.md`](windows/README.md) (Windows build, CI, releasing, signing), and the
root [`README.md`](README.md) (user-facing).

## Commands

```sh
swift build                              # Mac app, debug
bash Scripts/bundle.sh --no-notarize     # Mac DMG without notarization (full run needs the notary key)
BUILD=<n> Scripts/bundle-appstore.sh --local   # sandboxed test .app, bundle ID com.seriamo.pausa.localtest
dotnet test windows/Pausa.slnx           # Windows core tests (runs on macOS and Linux too)
dotnet build windows/Pausa.slnx
```

The Swift package has no tests. Windows UI is verified by `Pausa.exe --smoke-test <dir>` in CI.

## Rules that matter

- **Never launch a sandboxed Mac build as `com.seriamo.pausa` on a Mac that has the GitHub build
  installed.** macOS moves that app's preferences into the sandbox container and they can't be
  deleted from the shell. Test with `--local` (bundle ID `.localtest`).
- **Mac App Store build** is the same sources compiled with `-DAPP_STORE`, which removes the
  `updates.seriamo.com` self-update (guideline 2.4.5). Keep telemetry and feedback. Wrap any new
  self-update code in `#if !APP_STORE`.
- **Versions:** the Mac version is `VERSION` in `Scripts/bundle.sh` and `currentVersion` in
  `Sources/Pausa/main.swift` (the store script fails if they differ). The Windows version is
  `<Version>` in `windows/Directory.Build.props`; release tags are `windows-v<Version>`.
- **App Store build numbers are the git commit count and must keep rising.** Build 18 is already
  uploaded and `origin/main` has fewer commits than that. Set `BUILD=<n>` explicitly (see
  `HANDOFF.md`).
- **Windows releases are never "latest"** (`--latest=false`). The Mac update check and the
  README's `Pausa.dmg` link read `/releases/latest`.
- Each Windows release must also be added to the Seriamo release manifest (private repo
  `seriamo-platform`) so the app's update check and the website's download button see it.
- Nothing reaches App Store Connect without the maintainer's (Luis Masieri's) explicit approval:
  every write, submit, reply, cancel and release. Scripts dry-run unless given `--apply`; always
  pass `--platform MAC_OS`.
- **Secrets stay out of git:** the App Store Connect API key and profile live in
  `~/.appstoreconnect/`, signing keys in the login keychain, the notarization key under `docs/certs/`
  (git-ignored). Never print, copy or commit them.
- `.gitignore` excludes `*.png`, `*.mp4`, `docs/`, `documents/`, `build/`, `CLAUDE.md` and `.agents/`
  (`appstore/.gitignore` adds `out/`). `documents/` is the local, Synology-backed folder of non-git artifacts
  (see `HANDOFF.md`). Put new durable docs elsewhere, and add a rule or `git add -f` for images that must be tracked.
- Commit trailer: `Co-authored-by: Copilot App <223556219+Copilot@users.noreply.github.com>`.
  Don't force-push, and open a PR only when asked.
- The code makes three network calls, all to Seriamo hosts: `telemetry.seriamo.com` (one anonymous
  `app_launch` event, opt-out), `updates.seriamo.com` (not in the store build) and
  `feedback.seriamo.com` (opens in the browser). Changing what is sent also changes the App Privacy
  answers in App Store Connect and `https://seriamo.com/privacy`.
