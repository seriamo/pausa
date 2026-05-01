<h1 align="center">Pausa</h1>                                                           
<p align="center"><b>A reminder to look away.</b> Your eyes will thank you.</p> 


**More than half of adults experience dry eye symptoms, and most never seek help**. Staring at screens reduces your blink rate, making it worse. Pausa is a lightweight macOS menu-bar app that flashes a gentle overlay on your screen every few minutes, just enough to break the staring habit and give your eyes a rest. Based on the 20-20-20 rule recommended by [eye care professionals](https://www.aoa.org/AOA/Images/Patients/Eye%20Conditions/20-20-20-rule.pdf).

![pausa-hero](https://github.com/user-attachments/assets/20b50dfc-9002-4e75-bed6-badd4cff0d32)

## Installation

Requirement: macOS 14 (Sonoma) or later

### Download
Grab the latest version of [Pausa](https://github.com/seriamo/pausa/releases/latest/download/Pausa.dmg). Open it, drag Pausa to Applications, done.

### Build from source

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
- **Runs entirely on your Mac**: no accounts, no subscriptions, no tracking. Anonymous ping on app launch to track version and activation.
- **Launch automatically at login**: set it once, forget it
- **Three flash modes**: solid menu bar, menu bar with glow, or full screen border

![pausa-flash-mode](https://github.com/user-attachments/assets/1cf96a83-3e1b-470f-b672-28b5faf5b7a2)

- **Pick your color**: choose a flash color that matches your style

![pausa-colors](https://github.com/user-attachments/assets/7a71db36-824a-4551-8b11-b3d8e59f8dee)

## What's next?

**Interested in a Windows version?** [Let us know here.](https://github.com/seriamo/pausa/issues/2)

A reaction on the issue is enough.


## About

Pausa is built by [Seriamo](https://seriamo.com). One problem, one product, built until it's right.

## License

MIT. See [LICENSE](LICENSE).
