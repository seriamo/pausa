<h1 align="center">Pausa</h1>                                                           
<p align="center"><b>A reminder to look away.</b> Your eyes will thank you.</p> 


**More than half of adults experience dry eye symptoms, and most never seek help**. Staring at screens reduces your blink rate, making it worse. Pausa is a lightweight macOS menu-bar app that flashes a gentle overlay on your screen every few minutes, just enough to break the staring habit and give your eyes a rest. Based on the 20-20-20 rule recommended by [eye care professionals](https://www.aoa.org/AOA/Images/Patients/Eye%20Conditions/20-20-20-rule.pdf).

![pausa-hero](https://github.com/user-attachments/assets/20b50dfc-9002-4e75-bed6-badd4cff0d32)

## Installation

Requirements: macOS 14 (Sonoma) or later, or Windows 10 (version 1809) or later on x64 or Arm64.

### Path A: Download (Easiest)
**Mac:** grab the latest version of [Pausa](https://github.com/seriamo/pausa/releases/latest/download/Pausa.dmg). Open it, drag Pausa to Applications, done.

**Windows:** download the installer for [x64](https://updates.seriamo.com/v1/products/pausa/download?platform=windows&arch=x64) or [Arm64](https://updates.seriamo.com/v1/products/pausa/download?platform=windows&arch=arm64) and run it. Pausa lives in the notification area. Portable builds are on the [releases page](https://github.com/seriamo/pausa/releases). Until the Windows build is code-signed, SmartScreen may say it doesn't recognize the app; choose **More info**, then **Run anyway**.

### Path B: Build from source

```bash
git clone https://github.com/seriamo/pausa
cd pausa
bash Scripts/bundle.sh
open Pausa.app
```

For development:

```bash
swift build
```

## Features
![pausa-customization](https://github.com/user-attachments/assets/dfb403d9-d356-4602-b344-36c540a1a27e)
- **Timed pause**: 5 min, 10 min, 30 min, 1 hour, indefinitely, or until restart
- **Skips fullscreen apps**: no interruptions during presentations, video, or games
- **Multi-monitor support**: flash all screens or active screen only
- **Countdown in menu bar**: shows seconds remaining during a flash
- **Runs entirely on your Mac**: no accounts, no subscriptions, no tracking. An anonymous launch count (app version and a random install ID) helps us see which versions are in use; turn it off in **Settings → Share Anonymous Usage**.
- **Launch automatically at login**: set it once, forget it
- **Three flash modes**: solid menu bar, menu bar with glow, or full screen border

![pausa-flash-mode](https://github.com/user-attachments/assets/1cf96a83-3e1b-470f-b672-28b5faf5b7a2)

- **Pick your color**: choose a flash color that matches your style

![pausa-colors](https://github.com/user-attachments/assets/7a71db36-824a-4551-8b11-b3d8e59f8dee)

## About

Pausa is built by [Seriamo](https://seriamo.com). One problem, one product, built until it's right.

## Privacy Policy

We don't capture or store any personal data. Pausa talks to three Seriamo endpoints:

- `telemetry.seriamo.com`: one `app_launch` event per launch with the app version and a random install ID. Turning off **Share Anonymous Usage** (Mac) or **Share anonymous usage** (Windows) stops it and deletes the ID.
- `updates.seriamo.com`: checks for a newer release on launch. Downloads come from GitHub Releases.
- `feedback.seriamo.com`: opens in your browser only when you choose **Send Feedback…** (Mac) or **Feedback** (Windows).

None of them store your IP address. See [Privacy Policy](https://seriamo.com/privacy).

## License

MIT. See [LICENSE](LICENSE).
