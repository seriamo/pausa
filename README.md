<h1 align="center">Glint ✨</h1>                                                           
<p align="center"><b>A reminder to look away.</b> Your eyes will thank you.</p> 


**More than half of adults experience dry eye symptoms, and most never seek help**. Staring at screens reduces your blink rate, making it worse. Glint is a lightweight macOS menu-bar app that flashes a gentle overlay on your screen every few minutes, just enough to break the staring habit and give your eyes a rest. Based on the 20-20-20 rule recommended by [eye care professionals](https://www.aoa.org/AOA/Images/Patients/Eye%20Conditions/20-20-20-rule.pdf).

![Glint app flashing](https://cdn.seriamo.com/glint/glint-flashing-quick.gif)

## Installation

Requirement: macOS 14 (Sonoma) or later

### Download
Grab the latest version of [Glint](https://github.com/seriamo/glint/releases/latest/download/Glint.dmg). Open it, drag Glint to Applications, done.

> ⚠️ **Note:** Glint is not notarized with Apple. On first launch, macOS may warn that it's from an unidentified developer. Right-click the app and select **Open** to bypass this.

### Build from source

```bash
git clone https://github.com/seriamo/glint
cd glint
bash Scripts/bundle.sh
open Glint.app
```

For development:

```bash
swift build
```

## Features
![Glint e2e demo](https://cdn.seriamo.com/glint/glint-e2e-demo-short.gif)
- **Timed pause**: 5 min, 10 min, 30 min, 1 hour, indefinitely, or until restart
- **Skips fullscreen apps**: no interruptions during presentations, video, or games
- **Multi-monitor support**: flash all screens or active screen only
- **Countdown in menu bar**: shows seconds remaining during a flash
- **Runs entirely on your Mac**:  no accounts, no subscriptions, no tracking. One anonymous ping on first launch to measure activation. That's it.
- **Launch automatically at login**: set it once, forget it
-  **Three flash modes**: solid menu bar, menu bar with glow, or full screen border

![Glint flash modes](https://cdn.seriamo.com/glint/glint-flash-mode.gif)

- **Pick your color**: choose a flash color that matches your style

![Glint colors](https://cdn.seriamo.com/glint/glint-colors2.gif)


## What's next?

**💻 Interested in a Windows version?** [Let me know here](https://github.com/seriamo/glint/issues/2)

A reaction on the issue is enough — no sign-up required.


## License

MIT — see [LICENSE](LICENSE)
