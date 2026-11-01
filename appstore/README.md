# Pausa on the Mac App Store

Listing, screenshots and product page header for Pausa's Mac App Store page. **Nothing has been
submitted.** There's no Pausa app record in App Store Connect yet; the bundle ID
`com.seriamo.pausa` is registered. The app needs the code changes below before it can pass review.

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

## Before submitting: app changes

The Mac App Store build needs these. They belong with the planned update and telemetry endpoint
work.

1. **App Sandbox.** Add `com.apple.security.app-sandbox`, keep
   `com.apple.security.network.client`, and drop `com.apple.security.cs.allow-unsigned-executable-memory`
   (`Scripts/entitlements.plist`). Then check that the fullscreen detection
   (`CGWindowListCopyWindowInfo` in `FullscreenDetector.swift`), the screen lock and unlock
   notifications and Launch at Login (`SMAppService`) still work under the sandbox.
2. **No self-updates.** Store builds can't check GitHub Releases or point people at a download
   (`checkForUpdates`, `showUpdateAvailable` and `openUpdatePage` in `main.swift`). The App Store
   handles updates. Remove them from the store build, or compile them out with a flag if the GitHub
   build keeps them.
3. **Telemetry, declared.** Whatever the new endpoint sends must match App Privacy, the privacy
   policy and the last line of `description.txt`. If the new build sends what the current launch
   ping sends (version plus a random installation ID), the App Privacy answers are:
   - Data types: Identifiers › Device ID, and Usage Data › Product Interaction
   - Purpose: Analytics
   - Linked to the user: No. Used for tracking: No.
   - If it sends anything new, update all three places.
4. **Store packaging.** `Scripts/bundle.sh` makes a Developer ID DMG, which the store won't take.
   The store build needs:
   - Signing with an Apple Distribution certificate and a Mac App Store provisioning profile.
   - A `.pkg` built with `productbuild` and a Mac Installer Distribution certificate.
   - Upload with Transporter or `xcrun altool`.
   - In `Info.plist`: `LSApplicationCategoryType` = `public.app-category.healthcare-fitness`,
     `ITSAppUsesNonExemptEncryption` = `NO` (HTTPS only), and a `CFBundleVersion` build number that
     goes up with every upload. Today it equals the marketing version.

   The icon already includes the 1024 px size.
5. **Privacy policy.** seriamo.com/privacy says Pausa "checks GitHub for updates". Update that
   paragraph when the update check and telemetry change.
6. **README and site.** Point the download links at the App Store once it's live.
7. **Worth checking.** On the virtual display, the Menu Bar modes briefly drew nothing on a screen
   whose `visibleFrame` had no menu bar inset. This was seen only on the virtual display, but it's
   worth testing with "Displays have separate Spaces" turned off.

## App Store Connect steps

On the website (the API can't do these):

1. Create the app with platform macOS, name Pausa, bundle ID `com.seriamo.pausa`, SKU `pausa`
   and language English (U.S.).
2. App Privacy: see item 3 above.
3. Pricing and Availability: Free, all territories.

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

`review.mjs --submit` sends the app to review. Run it only when Luis says to.

## Open decisions

- **Name.** "Pausa" if it's free on the store, otherwise "Pausa Eye Breaks".
- **Categories.** Health & Fitness first, then Productivity. Swap them if Pausa should sit with
  focus tools.
- **Age rating.** `healthOrWellnessTopics` is set to true because Pausa is an eye-strain habit
  tool. Check the rating App Store Connect gives it, and set it to false if that seems too strong.
- **Screenshot 4.** It shows purple being previewed. Green and orange captures exist too.
