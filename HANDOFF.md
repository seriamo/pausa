# Pausa handoff

**Date:** 2026-11-16. **Written for:** a new AI coding agent (or person) picking this project up on another
computer. It doesn't depend on any chat history. Project rules are in [`AGENTS.md`](AGENTS.md); this
file has the status, the history behind the decisions, the pitfalls and the next steps.

"Verified" below means checked against the repo, the live services or App Store Connect on
2026-11-16 unless a date is given. "Unverified" means it comes from earlier work and wasn't re-checked.

## Start here

1. Read [`AGENTS.md`](AGENTS.md), then the **Where things stand** table below.
2. **The one live item is App Review.** Pausa Eye Break 1.1 (build 18) was rejected on November 13
   under guidelines 4.3(a), 4.3(b) and 4.2.6. Luis replied in Resolution Center on November 14 and did
   **not** resubmit. As of 2026-11-16, `status.mjs` still shows the version `REJECTED` and the
   submission `UNRESOLVED_ISSUES`. Nobody has re-read Resolution Center since the reply, so whether
   Apple answered is **unknown**. Ask Luis, or look in App Store Connect (see "Next steps").
3. Don't change anything in App Store Connect, resubmit, cancel, release or message Apple without
   Luis's explicit approval each time.
4. Before any Mac App Store build, read the **build-number pitfall** below.

## Project

Pausa is a free, MIT-licensed eye-break reminder based on the 20-20-20 rule. Every few minutes
(default 20, range 5-60) it lights the screen edge for a few seconds (default about 5, range 3-20) with a
click-through overlay: no dialog, no sound, no focus steal. It skips reminders while a fullscreen app
is in front and while the screen is locked. Publisher: Seriamo, a brand of Masieri Ventures LLC (Apple
team `FVAPC8DNSW`). Maintainer and approver for everything external: Luis Masieri (GitHub `lmasieri`).

| Path | What |
|---|---|
| `Sources/Pausa/` | Mac app (Swift 6, AppKit, no dependencies, macOS 14+). `main.swift` (app delegate, menus), `FlashOverlay.swift`, `FullscreenDetector.swift`, `PausaTimer.swift`, `Preferences.swift`, `StartupManager.swift` (SMAppService), `SeriamoServices.swift` (endpoints, telemetry, update check) |
| `windows/` | Windows port: `Pausa.Core` (platform-neutral logic, 54 xUnit tests), `Pausa.Windows` (WPF tray app), Inno Setup installer. Details in [`windows/README.md`](windows/README.md) |
| `Scripts/` | `bundle.sh` (Developer ID DMG + notarization), `bundle-appstore.sh` (sandboxed universal `.pkg`), `entitlements.plist`, `entitlements-appstore.plist`, icon tools |
| `appstore/` | Listing sources and the store history. [`appstore/README.md`](appstore/README.md) is the source of truth for App Store Connect |
| `.github/workflows/` | `windows.yml`: tests, publish x64 and arm64, smoke tests, installers, and a release job on `windows-v*` tags. `release-latest-guard.yml`: keeps a macOS release as "latest" |

## Where things stand

| Item | State |
|---|---|
| Mac app, GitHub build | **1.1 released** November 1, 2026 as a Developer ID, notarized `Pausa.dmg` (verified: GitHub release `v1.1` is "Latest"; the update manifest returns 1.1). |
| Windows app | **1.0.0 released** November 1, 2026 (`windows-v1.0.0`, not "latest"). **Unsigned**, so SmartScreen warns. The update manifest and the download redirect return it (verified). Code signing is not set up: the repo and the `windows-release` environment have no variables or secrets. |
| Mac App Store | **Pausa Eye Break** (the name "Pausa" was taken), app `6832411313`, SKU `pausa`. Version 1.1 with build 18, manual release, rejected (see below). |
| Source on `main` | Includes the store build, the `appstore/` listing and the review notes up to submission (PR [#3](https://github.com/seriamo/pausa/pull/3), squash-merged as `2e044f0`). |
| Site and download links | **On hold by Luis's instruction until the App Store review resolves.** Don't repoint the README, `seriamo.com/pausa` or the update manifest at the App Store yet. |

### App Review history (facts)

- **2026-11-08/09:** submitted (submission `d08eb1a8-ef85-40ce-bf93-b036e1bfd62e`, version id `299dcbc0-6837-497e-ae50-4d7999ca7112`).
- **2026-11-13:** rejected under 4.3(a) (duplicate functionality), 4.3(b) (an opportunistic variant of a
  crowded category) and 4.2.6 (template or app-generation service). Apple named no defect. It said that
  if the app already complies, **don't resubmit**; reply and answer nine questions: what the app does and
  the problem it solves, the intended user, the market gap, beta testing and feedback applied,
  standalone or suite, whether it could be an in-app purchase of another app, code shared with other
  apps on the account, code or SDKs shared with third-party apps, and whether it was made for a client.
- **Diagnosis:** a category and originality flag (the Mac App Store already has many eye-break and
  20-20-20 apps), not a code, sandbox or metadata defect.
- **2026-11-14 12:50 ET:** Luis chose to reply instead of resubmitting, and approved the text. The reply was
  posted in Resolution Center (the thread showed "Messages (2)"). The text as sent is
  [`appstore/app-review-reply-2026-11-14.md`](appstore/app-review-reply-2026-11-14.md). Every claim in it was
  checked against the code and release history. Q4's beta feedback (don't interrupt presentations or
  fullscreen work; the 0.5.1 pause-countdown fix) was supplied and approved by Luis.
- The account's only other app is **Punti Offers** (iPhone, `com.seriamo.punti`, app `6828660003`). It
  shares no code with Pausa.

## Next steps

1. **Find out whether Apple replied.** In App Store Connect open Pausa Eye Break, then Distribution, then the
   review submission, then Messages. The API can't read or post these (`reviewSubmissionItems` returns 403).
   `node ~/.copilot/skills/seriamo-app-store/scripts/status.mjs --app com.seriamo.pausa --platform MAC_OS`
   shows version and submission state. It still says `UNRESOLVED_ISSUES` until Luis resubmits or Apple acts.
2. **Then choose, with Luis:**
   - *Apple accepts the answers:* the version can be approved without a new build. Luis may still need to
     press **Resubmit to App Review**. Release is manual, so Luis releases it.
   - *Apple asks for changes:* make them, build with a `BUILD` number above 18 (next section), upload, attach
     (`attach-build.mjs --build <n> --wait --apply`), then resubmit.
   - *Apple holds to 4.3:* appeal to the App Review Board, or add substantive differentiating features
     first, or skip the store and keep GitHub and seriamo.com distribution.
3. **After approval and release:** repoint the download links (root `README.md`, `seriamo.com/pausa`, the
   release manifest's Mac entry) at the App Store listing. Use the `seriamo-product-sites` skill: PPE first,
   then production. Decide whether the GitHub DMG stays.
4. **Windows:** set up code signing (table in `windows/README.md`), then cut `windows-v1.0.1` or later.
   Add each Windows release to the manifest in the private repo `seriamo-platform`
   (`workers/delivery/src/manifest.ts`). That is where the update check and the website download button read it.
5. **Next Mac version:** recapture the screenshots (they were taken from 1.0, so the menus lack Send
   Feedback… and Share Anonymous Usage). The capture rig was throwaway and isn't in the repo; see
   `appstore/README.md`.
6. **Housekeeping (all optional):** revoke the orphan Mac Installer certificate `X7MLJ5Z58F` in the developer
   portal (it has no private key anywhere). Renew the Mac App Distribution certificate `J5DZQ56995` and the
   profile `P3J5V8B6JS` before **2027-04-30** and the installer certificate `6H2X8FV6UQ` before
   **2027-11-04**. The pending tracker work is listed under "External services".

## Mac App Store build

Build and upload steps, the sandbox results and the App Privacy answers are in
[`appstore/README.md`](appstore/README.md). Summary of the decisions:

- **One codebase, a compile flag.** `-DAPP_STORE` removes the `updates.seriamo.com` check and the "Pausa vX
  available" menu item (App Store guideline 2.4.5: the store delivers updates). Telemetry and the feedback link
  stay, so the App Privacy answers are Identifiers › Device ID and Usage Data › Product Interaction, for
  analytics, not linked to the user, no tracking.
- **Sandbox.** `Scripts/entitlements-appstore.plist` enables it with `network.client`. Verified on macOS 27.2:
  no sandbox denials, `CGWindowListCopyWindowInfo` works for `FullscreenDetector`, and the lock and unlock
  distributed notifications arrive. **Unverified:** a real `SMAppService` login-item registration in the
  sandboxed build, a real screen lock, and the Menu Bar flash modes with "Displays have separate Spaces"
  turned off.
- **Signing and packaging.** "3rd Party Mac Developer Application" identity plus the embedded profile, then
  `productbuild` signed with "3rd Party Mac Developer Installer", validated with `altool` (see
  [Setup](#setup-and-environment)).

### Pitfalls (each of these cost time)

- **Build-number trap (open, not yet fixed).** `bundle-appstore.sh` defaults `BUILD` to
  `git rev-list --count HEAD`. Build 18 is already in App Store Connect, but `origin/main` has only 13 commits
  (squash merge) and this branch 21, so a build from `main` would be numbered below 18 and be rejected for
  version 1.1. Always run with an explicit number, for example `BUILD=19 Scripts/bundle-appstore.sh --upload`.
  A new marketing version may restart the numbering, but check what App Store Connect already holds first.
- **The sandbox moves preferences.** The first launch of a sandboxed app moves
  `~/Library/Preferences/<bundle id>.plist` into `~/Library/Containers/<bundle id>/`. Doing this with the
  real bundle ID on the maintainer's Mac took his real settings (they were restored by hand), and macOS
  won't let you delete a container root from the shell. That is why `--local` uses
  `com.seriamo.pausa.localtest`. The same move helps real users: people switching from the GitHub build keep
  their settings and install ID. Two undeletable leftover containers (`com.seriamo.pausa` and
  `com.seriamo.sandboxtest`) remain on Luis's Mac; he can trash them in Finder. Unverified whether the
  `com.seriamo.pausa` one interferes with installing the store build there.
- **SDK stamp.** SwiftPM stamps the deployment target (14.0) as the SDK version, which App Store Connect
  rejects. The script repairs it with `vtool -set-build-version`. Use
  `DEVELOPER_DIR=/Applications/Xcode.app/Contents/Developer` (the script defaults to it). `xcode-select`
  points at the Command Line Tools on this Mac.
- **Certificates.** `security import` needs a traditional RSA key
  (`openssl genrsa -traditional`); OpenSSL 4 writes PKCS#8 and the import fails with "Unknown format". An
  orphan installer certificate (`X7MLJ5Z58F`, no private key on any machine) exists from the same stretch of
  certificate work.
- **ASC details.** The App Store Connect version string must equal `CFBundleShortVersionString`;
  `patch-listing` doesn't set it, so it was changed with a raw `PATCH` through `asc.mjs`. `altool` prints a
  harmless `CDUploaderStateInfoResumeFilePath` error. The header image upload's wait loop timed out once; the
  skill's loop is now 120 iterations (a local edit to `upload-header.mjs`).
- **Reply box.** The Resolution Center Reply modal accepts at most 4,000 characters. The full draft was longer, so
  a shortened version was sent. A scripted `setValue` on the textarea reported "unverified" but worked; check
  the character counter.
- **Promo video isn't an App Preview** (33.5 s, over the 30 s limit, and its desk and illustrated shots break
  guideline 2.3.4). It plays on `seriamo.com/pausa`.

## Windows port: decisions

Chosen by Luis at the start: C# .NET 10 and WPF, in `windows/` of the same repo, Windows 10 1809 or later on
x64 and Arm64, a Windows 11-style flyout on tray left-click plus a short right-click menu, and the shared
Seriamo telemetry, update and feedback endpoints.

- `Pausa.Core` is platform-neutral (timer, settings, step values, flash curve, geometry, update check,
  telemetry) so it is unit-tested on any OS. `EnableWindowsTargeting` lets the WPF project compile on macOS
  and Linux; only Windows can run it.
- The tray icon and overlays use raw Win32 (`Shell_NotifyIconW`, layered windows with alpha-only
  `UpdateLayeredWindow`) rather than libraries, so the overlay is click-through and never takes focus. The
  hidden tray host window must not be message-only, or it misses `TaskbarCreated`.
- WPF's `ThemeMode` is still experimental in .NET 10, so `WPF0001` is suppressed. The target is
  `net10.0-windows`; a versioned Windows TFM added about 25 MB of WinRT projections and was reverted.
- Releases are self-contained single-file builds, so they're 50-70 MB (WPF can't be trimmed).
- **Windows releases are created with `--latest=false`** because the Mac update check and the README's DMG
  link read `/releases/latest`. `release-latest-guard.yml` restores a macOS release if one is changed by hand.
- Signing runs only on `windows-v*` tags, inside the `windows-release` environment, with Azure Artifact
  Signing. That environment exists, but its variables and secrets don't (verified).
- Smoke test: `Pausa.exe --smoke-test <dir>` uses throwaway settings and no network or registry. The
  `windows-11-arm` runner sits on the OOBE privacy screen, so its screenshots are meaningless (its JSON report is fine).
  The Inno uninstaller relaunches itself from `%TEMP%`, so the CI round-trip polls rather than relying on `-Wait`.
- The last CI run on this branch (2026-11-09) succeeded (verified). The artifacts are unsigned.

## Setup and environment

**Tools** (versions on the maintainer's Mac): Xcode 27.2 at `/Applications/Xcode.app` (SDK macosx27.2, Swift
6.4.2), .NET SDK 10.0.x, `gh` signed in as `lmasieri`, Node (for the skill scripts), Inno Setup 6.3+ on Windows
only.

**Environment variable names** (no values here): `DEVELOPER_DIR` and `BUILD` and `PROFILE`
(`bundle-appstore.sh`); `PAUSA_FAKE_UPDATE` (shows the Windows update banner without a network call);
`ASC_KEY_ID`, `ASC_ISSUER_ID`, `ASC_KEY_PATH` (the skill's scripts; leave them unset for Xcode-signed uploads, as
the skill describes). GitHub Actions: variables `AZURE_SIGNING_ACCOUNT`, `AZURE_SIGNING_ENDPOINT`,
`AZURE_SIGNING_PROFILE` and secrets `AZURE_CLIENT_ID`, `AZURE_TENANT_ID`, `AZURE_SUBSCRIPTION_ID`.

**Files and credentials that exist only on Luis's Mac (none of these are in git):**

| What | Where | Needed for |
|---|---|---|
| App Store Connect API key config and `.p8` | `~/.appstoreconnect/seriamo.json`, `~/.appstoreconnect/private_keys/` | skill scripts, `altool` validate and upload |
| Mac App Store provisioning profile | `~/.appstoreconnect/profiles/Pausa_Mac_App_Store.provisionprofile` (the script's `PROFILE` default) | store build. It can be re-downloaded from the developer portal |
| Signing identities and their private keys | login keychain: "3rd Party Mac Developer Application", "3rd Party Mac Developer Installer", "Developer ID Application" (all Masieri Ventures LLC, team `FVAPC8DNSW`) | store build, DMG |
| Notarization key | `docs/certs/AuthKey_<id>.p8`, git-ignored (`Scripts/bundle.sh` names the key ID and path) | `bundle.sh` notarization. Not present in this worktree; unverified where the original is |
| Skills | `~/.copilot/skills/seriamo-app-store`, `seriamo-product-sites`, `seriamo-product-video` | ASC scripts, site and video work |

**Needs reconfiguring on a new computer:**
- Copy or re-create the items above (see the backup list below), then reinstall Xcode and run
  `sudo xcode-select -s /Applications/Xcode.app` or keep using `DEVELOPER_DIR`.
- Re-import the signing identities into the login keychain. The installer identity's private key was
  generated on this Mac; if it isn't exported, create a new Mac Installer Distribution certificate and revoke
  the old one.
- The marketing site (`seriamo.com`, with `ppe.seriamo.com` for pre-production) is static files on a NAS
  that the maintainer's Mac mounts at `/Volumes/web_lmasieri/seriamo-com` (production) and
  `/Volumes/web_lmasieri/ppe-seriamo-com` (PPE), and the NAS needs his VPN. Don't run agent tools or `cf`
  with that mount as the working directory: their cache folders get served publicly. See the
  `seriamo-product-sites` skill.
- Computer-use (browser) steps in App Store Connect assume Luis's signed-in Edge. The window IDs used in
  earlier work were specific to that session.

## Commands that were verified

```sh
dotnet test windows/Pausa.slnx                 # Windows core tests; CI runs this
swift build                                    # Mac app
BUILD=19 Scripts/bundle-appstore.sh            # signed, validated build/appstore/Pausa.pkg (needs certs, profile, API key)
BUILD=19 Scripts/bundle-appstore.sh --upload   # ...and upload to App Store Connect
Scripts/bundle-appstore.sh --local             # sandboxed test .app, bundle ID com.seriamo.pausa.localtest
node ~/.copilot/skills/seriamo-app-store/scripts/status.mjs --app com.seriamo.pausa --platform MAC_OS
```

(This handoff session only re-ran `status.mjs` and read the live endpoints; the build commands are verified from
earlier sessions. Build 18 was built, validated, uploaded and processed as `VALID`.)

## External services

- **App Store Connect** (team `FVAPC8DNSW`): app `6832411313`; the API can do almost everything except create
  the app, App Privacy, pricing and Resolution Center messages. Luis did those on the website.
- **Seriamo platform** (a private repo, `seriamo-platform`): `telemetry.seriamo.com/v1/events` (POST-only, one
  `app_launch` event with a random install ID, no IP stored), `updates.seriamo.com` (release manifest and the
  `/v1/products/pausa/download` redirect), `feedback.seriamo.com`. All three were live on 2026-11-16.
  Privacy policy: <https://seriamo.com/privacy> (last updated November 11, 2026, verified 2026-11-16). Its
  Pausa section says the app "checks updates.seriamo.com for a new version", which isn't true of the
  store build (it has no update check), so it over-discloses. Harmless, but consider a store-specific line.
- **Pausa tracker** (private repo `seriamo/pausa-tracker`): the older launch-ping Worker. Branch
  `windows-platform` (a migration that adds a `platform` column) was pushed but **never deployed**. It may be
  superseded by `telemetry.seriamo.com`; decide with Luis before doing anything. Deploying needs
  `wrangler login` and his approval.
- **GitHub:** public repo `seriamo/pausa`. Open issue #2 ("Windows version interest") could be closed now that
  Windows has shipped (unverified whether Luis wants that).

## Artifacts: what is where

In git (this repo): everything under `Sources/`, `windows/`, `Scripts/`, `appstore/` (listing, configs, review
history, the reply text) and the docs.

**Not in git, worth backing up** (sizes from 2026-11-16):

| Artifact | Location | Notes |
|---|---|---|
| Listing screenshots and header (rendered) | `appstore/out/` (14 MB) | Re-renderable from `captures/`; also stored in App Store Connect |
| Screenshot source captures | `appstore/captures/` (13 MB) | The capture rig was throwaway, so these aren't easily recreated. Copies also exist in the sibling worktree `…/pausa/lmasieri-bookish-memory/appstore/` |
| Promo video renders | `~/videos/pausa-promo/renders/` (53 MB): `pausa-promo.mp4`, `pausa-promo-github.mp4`, `pausa-hero.png`, `pausa-social.png` | The film is live on `seriamo.com/pausa` as a web encode. Music is Mixkit "A Baby Story" (free licence, no attribution; licence notes in `appstore/README.md`) |
| Promo video project | `~/videos/pausa-brief/` (271 MB): `video.json`, `patch_spotlight.py`, `patch_flash.py`, `make_flash_assets.py`, `flash/`, `screens/`, `music/` | Built with the `seriamo-product-video` skill |
| Windows CI artifact copy | `~/Downloads/Pausa-Windows/` (218 MB) | Redundant: the same builds are on the GitHub release |
| Session record | `~/.copilot/session-state/e151292e-efd3-402d-b75e-4165d6f46abb/` | Checkpoints, the two reply drafts and screenshots from the session. This handoff replaces the need for it; keep it only as an archive |

**Needs a separate encrypted backup, never git:** everything in the credentials table above, above all the
`~/.appstoreconnect/` folder and exported `.p12` files for the three signing identities.

## Git state when this was written

- Worktree branch `lmasieri-windows-port`, remote `origin` (`https://github.com/seriamo/pausa.git`). PR #3 from this
  branch is merged, and `origin/main` is `2e044f0` (squash). After that merge the branch gained further
  commits (the App Review record and this handoff). They live **only on `origin/lmasieri-windows-port`**; no PR
  has been opened, so they aren't on `main` yet. To land them, open a PR into `main` (only when Luis asks).
- Other local state, not touched by this handoff: the sibling worktree `lmasieri-bookish-memory` (branch
  `lmasieri-pausa-mac-app-store-listing`, tip `11ecad0`, already contained in `origin/lmasieri-windows-port`,
  never pushed on its own) and the main checkout `~/.copilot/repos/pausa` (`main`, 8 commits behind origin).

## Open risks and unknowns

- Apple's response to the November 14 reply is unknown; the store listing may never be approved.
- The build-number trap above will bite the next store upload unless `BUILD` is set.
- Windows builds are unsigned, and signing isn't configured.
- Signing keys and the App Store Connect key exist only on one Mac. Losing it means re-issuing certificates.
- Store-build behaviors listed as unverified above (login item, real lock, separate-Spaces flash modes).
- Screenshots and the promo show 1.0 menus, not 1.1's.
- The licensing of the promo's music is covered by the Mixkit free licence; keep that licence note if you re-cut it.
