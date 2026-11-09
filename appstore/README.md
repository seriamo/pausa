# Pausa on the Mac App Store

Listing, screenshots and product page header for Pausa's Mac App Store page, and how the store
build is made. **Version 1.1 was submitted for App Review on November 8, 2026** (submission
`d08eb1a8-ef85-40ce-bf93-b036e1bfd62e`). Release is manual, so it goes live only when Luis releases it.

## In App Store Connect

| | |
|---|---|
| App | **Pausa Eye Break** (the name "Pausa" was taken), app ID `6832411313`, SKU `pausa`, bundle ID `com.seriamo.pausa` |
| Version | 1.1, manual release, with build 18 attached (uploaded November 8, 2026) |
| Subtitle | A reminder to look away |
| Categories | Health & Fitness, then Productivity |
| Listing | `listing.json`, `description.txt` and `review-notes.txt`, applied as they are in git |
| Images | The five screenshots and the product page header from `out/`, as rendered from `screenshots.json` and `header.json` |
| Signing | The Mac Installer Distribution certificate `6H2X8FV6UQ` (expires November 2027) and the profile "Pausa Mac App Store" `P3J5V8B6JS`, which goes with the Mac App Distribution certificate `J5DZQ56995` (both expire April 30, 2027) |

Luis set the App Review contact, the App Privacy answers (below) and Pricing and Availability
(Free) on the website.

## What's here

| File | What it is |
|---|---|
| `listing.json` | Name, subtitle, categories, keywords, URLs, age rating and content rights, in the shape `patch-listing.mjs` reads |
| `description.txt` | Store description |
| `review-notes.txt` | Notes for App Review (no account, how to trigger a reminder, what the network request is) |
| `screenshots.json` | Config for the five 2880×1800 Mac screenshots |
| `header.json` | Config for the 3840×1646 product page header (shown on macOS 27 and later) |
| `captures/` | Source captures (not in git; PNGs are ignored) |
| `out/` | Rendered screenshots, header and `preview.html` (not in git) |

The tooling lives in the `seriamo-app-store` skill (`~/.copilot/skills/seriamo-app-store`). To re-render:

```sh
cd ~/.copilot/skills/seriamo-app-store
node scripts/render-screenshots.mjs <repo>/appstore/screenshots.json
node scripts/render-header.mjs <repo>/appstore/header.json --guides   # header-guides.png is for review only
```

### How the captures were made

Each capture is the real app's menus and flash windows, from a capture build of the current
`Sources/Pausa` with the launch ping and update check turned off. It ran on a 1440×900 virtual
Retina display in front of an illustrated desk (a notes window and a 9:41 menu bar), and each frame
was saved with `screencapture -D`. The menus, submenus, highlights, the countdown in the menu bar
and the flash tints are all drawn by AppKit and Pausa, not mocked up. The macOS
screen-recording dot was painted out of the menu bar where it appeared. The rig itself was
throwaway and isn't in the repo.

### Promo video

A 33.5s promo was made with the `seriamo-product-video` skill, outside the repo:

- `~/videos/pausa-brief/video.json` is the brief, with `screens/` holding the captures it uses.
- `~/videos/pausa-brief/patch_spotlight.py` zooms the spotlight scene in on the menu bar. The
  laptop template's default framing leaves the menu too small to read. Re-run it after every
  `new_video.py --force`.
- `~/videos/pausa-brief/patch_flash.py` makes every flashed screen pulse the way
  `FlashOverlay.pulse()` does: 0.3s up, 0.35s down, with the menu-bar countdown ticking. It
  crossfades each flashed capture over its untinted base, using layers in `flash/` built by
  `make_flash_assets.py`. Re-run it after `patch_spotlight.py`, then re-run `cues.py`.
- `~/videos/pausa-promo/renders/` has `pausa-promo.mp4`, the hero still and the social image.

The music bed is "A Baby Story" by Mixkit (track 530, `https://assets.mixkit.co/music/530/530.mp3`),
under the [Mixkit Stock Music Free License](https://mixkit.co/license/modal/musicFree/). That licence
allows commercial use on the web, social media, ads and YouTube with no attribution. It doesn't
allow registering the music with Content ID, claiming it as our own, or using it on TV, radio, CDs
or in games. If a platform flags the video, point the claim to team@mixkit.co. The bed is the first
36s of the track, converted to 48 kHz WAV and mixed under the SFX by `cues.py`.

The video isn't an App Store preview. Previews must be 15 to 30 seconds of footage captured from
the app, and this film runs 33.5s and is illustrated.

The film plays on seriamo.com/pausa as a 1280×720 web encode (`pausa-promo-web.mp4`), with
`pausa-poster.jpg` and English captions in `pausa-captions.vtt`. It follows the Pronto pattern:
poster with a play button, sound only after a press, and no autoplay. The flash in the video pulses
at about 1.5 Hz and fades smoothly, well under the 3 flashes per second photosensitivity limit.

## The store build

`Scripts/bundle-appstore.sh` builds the Mac App Store package. `Scripts/bundle.sh` still builds the
Developer ID DMG for GitHub Releases. The two builds differ in these ways:

- **Sandboxed.** `Scripts/entitlements-appstore.plist` turns on the App Sandbox and keeps
  `network.client`. It drops `cs.allow-unsigned-executable-memory`, which the app doesn't need. It
  also carries the team and application identifiers that the provisioning profile requires.
- **No self-updates.** It's built with `-DAPP_STORE`, which compiles out the updates.seriamo.com
  check and the "Pausa vX available" menu item. The App Store delivers updates.
- **Universal** (arm64 and x86_64), with a minimum of macOS 14.
- **Info.plist** adds `LSApplicationCategoryType` (`public.app-category.healthcare-fitness`),
  `ITSAppUsesNonExemptEncryption` = NO (it uses HTTPS only), `LSMinimumSystemVersion` and the
  Xcode `DT*` keys. The build number is the git commit count, so it goes up with every upload.
- **SDK stamp.** SwiftPM's link step records the deployment target (14.0) as the SDK version.
  The script rewrites that to the SDK the app was actually built with, because App Store Connect
  rejects builds from old SDKs.
- **Signing.** It's signed with "3rd Party Mac Developer Application" (the team's Mac App
  Distribution certificate) and the Mac App Store profile, which is embedded as
  `Contents/embedded.provisionprofile`. The script expects the profile in
  `~/.appstoreconnect/profiles/`. The `.pkg` is made with `productbuild` and signed with
  "3rd Party Mac Developer Installer". The script validates the package with `altool`, and
  `--upload` sends it.

```sh
Scripts/bundle-appstore.sh            # build/appstore/Pausa.pkg, validated
Scripts/bundle-appstore.sh --upload   # ...and uploaded
Scripts/bundle-appstore.sh --local    # ad-hoc signed, sandboxed .app for testing on this Mac
```

`--local` uses the bundle ID `com.seriamo.pausa.localtest` for a reason. The first launch of a
sandboxed app *moves* `~/Library/Preferences/<bundle id>.plist` into
`~/Library/Containers/<bundle id>/`. A test build with the real ID would take the installed
Pausa's settings with it, and macOS won't let you delete that container from the command line. The
same move helps people switching from the GitHub build: their settings and install ID carry over
to the App Store build.

Sandbox checks, run on macOS 27.2:

- The sandboxed build ran with no sandbox denials.
- `CGWindowListCopyWindowInfo` still returns owner name, alpha and bounds for every on-screen
  window, which is all `FullscreenDetector` uses.
- Distributed notifications still arrive, so `com.apple.screenIsLocked` and
  `com.apple.screenIsUnlocked` work.
- `SMAppService.mainApp` is the supported login-item API for sandboxed apps. It's only called when
  someone turns Launch at Login on.

What the store build sends, and its App Privacy answers:

- **Telemetry.** `Telemetry.recordLaunch` posts one `app_launch` event to telemetry.seriamo.com
  with the product, app version, platform, a timestamp, an event ID and a random install ID.
  Settings › Share Anonymous Usage turns it off and deletes the ID.
- **App Privacy:**
  - Data types: Identifiers › Device ID, and Usage Data › Product Interaction.
  - Purpose: Analytics.
  - Linked to the user: No. Used for tracking: No.
- **Feedback.** Send Feedback… opens feedback.seriamo.com in the browser. Anything typed there is
  entered on the web, not in the app, so it isn't app-collected data.
- **Privacy policy.** seriamo.com/privacy already covers telemetry.seriamo.com and
  updates.seriamo.com. Its last update was November 1, 2026.

Still to check by hand: the Menu Bar flash modes with "Displays have separate Spaces" turned off.
On the virtual capture display, they briefly drew nothing on a screen whose `visibleFrame` had no
menu bar inset.

After launch, point the README and site download links at the App Store.

## App Store Connect steps

On the website (the API can't do these):

1. Create the app (done): platform macOS, bundle ID `com.seriamo.pausa`, SKU `pausa`, English (U.S.).
2. App Privacy: see "What the store build sends" above.
3. Pricing and Availability: Free, all territories.
4. App Review contact: name, phone and support@seriamo.com. This stays in App Store Connect, not in git.

Everything else uses the skill's scripts. **Always pass `--platform MAC_OS`; the default is
`IOS`.** Every script below is a dry run unless you add `--apply`.

```sh
cd ~/.copilot/skills/seriamo-app-store/scripts
node status.mjs --app com.seriamo.pausa --platform MAC_OS
node patch-listing.mjs <repo>/appstore/listing.json --platform MAC_OS
node upload-screenshots.mjs <repo>/appstore/out --app com.seriamo.pausa --platform MAC_OS
node upload-header.mjs <repo>/appstore/out/header.png --app com.seriamo.pausa --platform MAC_OS
node attach-build.mjs --app com.seriamo.pausa --build <n> --platform MAC_OS --wait
```

The version string in App Store Connect must match the build's `CFBundleShortVersionString`.
Check it before uploading a new marketing version.

`review.mjs --submit` sends the app to review. Run it only when Luis says to.

## Open decisions

- **Screenshots.** They were captured from 1.0, so the menus don't show Send Feedback… or Share
  Anonymous Usage. Recapture them for the next version.
- **Categories.** Health & Fitness first, then Productivity. Swap them if Pausa should sit with
  focus tools.
- **Age rating.** `healthOrWellnessTopics` is set to true because Pausa is an eye-strain habit
  tool. Check the rating App Store Connect gives it, and set it to false if that seems too strong.
- **Screenshot 4.** It shows purple being previewed. Green and orange captures exist too.
- **Video.** The end card says "Free". Change it if Pausa won't be free. The storyboard wasn't
  reviewed before polish, so check the cut as a whole.
