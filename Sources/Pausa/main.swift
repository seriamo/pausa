import AppKit
import ServiceManagement

// MARK: - Custom menu bar icons (eye + star pupil, matching app icon)

/// Draws the Pausa eye icon for the menu bar. Template image so it adapts to light/dark menu bar.
/// Outline eye with filled 4-pointed star. Same geometry as PausaMenuBarIcon.svg, inverted rendering.
func pausaMenuBarIcon(paused: Bool = false) -> NSImage {
    let size: CGFloat = 22
    let img = NSImage(size: NSSize(width: size, height: size), flipped: false) { rect in

        // --- Eye outline (almond shape with rounded tips, stroked) ---
        let eye = NSBezierPath()
        eye.move(to: NSPoint(x: 2.072, y: 11.274))
        eye.curve(to: NSPoint(x: 2.072, y: 10.725),
                    controlPoint1: NSPoint(x: 1.906, y: 11.129),
                    controlPoint2: NSPoint(x: 1.906, y: 10.870))
        eye.curve(to: NSPoint(x: 11.0, y: 6.159),
                    controlPoint1: NSPoint(x: 7.596, y: 5.926),
                    controlPoint2: NSPoint(x: 10.456, y: 6.159))
        eye.curve(to: NSPoint(x: 19.928, y: 10.725),
                    controlPoint1: NSPoint(x: 11.544, y: 6.159),
                    controlPoint2: NSPoint(x: 14.403, y: 5.926))
        eye.curve(to: NSPoint(x: 19.928, y: 11.274),
                    controlPoint1: NSPoint(x: 20.094, y: 10.870),
                    controlPoint2: NSPoint(x: 20.094, y: 11.129))
        eye.curve(to: NSPoint(x: 11.0, y: 15.840),
                    controlPoint1: NSPoint(x: 14.404, y: 16.073),
                    controlPoint2: NSPoint(x: 11.544, y: 15.840))
        eye.curve(to: NSPoint(x: 2.072, y: 11.274),
                    controlPoint1: NSPoint(x: 10.456, y: 15.840),
                    controlPoint2: NSPoint(x: 7.597, y: 16.073))
        eye.close()
        NSColor.black.setStroke()
        eye.lineWidth = 1.2
        eye.stroke()

        // --- Star (4-pointed with rounded caps, filled) ---
        let star = NSBezierPath()
        star.move(to: NSPoint(x: 11.317, y: 14.493))
        star.line(to: NSPoint(x: 11.879, y: 11.879))
        star.line(to: NSPoint(x: 14.493, y: 11.317))
        star.curve(to: NSPoint(x: 14.493, y: 10.684),
                    controlPoint1: NSPoint(x: 14.834, y: 11.243),
                    controlPoint2: NSPoint(x: 14.834, y: 10.757))
        star.line(to: NSPoint(x: 11.879, y: 10.122))
        star.line(to: NSPoint(x: 11.317, y: 7.508))
        star.curve(to: NSPoint(x: 10.684, y: 7.508),
                    controlPoint1: NSPoint(x: 11.243, y: 7.167),
                    controlPoint2: NSPoint(x: 10.757, y: 7.167))
        star.line(to: NSPoint(x: 10.122, y: 10.122))
        star.line(to: NSPoint(x: 7.508, y: 10.684))
        star.curve(to: NSPoint(x: 7.508, y: 11.317),
                    controlPoint1: NSPoint(x: 7.167, y: 10.758),
                    controlPoint2: NSPoint(x: 7.167, y: 11.244))
        star.line(to: NSPoint(x: 10.122, y: 11.879))
        star.line(to: NSPoint(x: 10.684, y: 14.493))
        star.curve(to: NSPoint(x: 11.317, y: 14.493),
                    controlPoint1: NSPoint(x: 10.758, y: 14.834),
                    controlPoint2: NSPoint(x: 11.244, y: 14.834))
        star.close()
        NSColor.black.setFill()
        star.fill()

        // Slash for paused state
        if paused {
            let slash = NSBezierPath()
            let inset: CGFloat = size * 0.1
            slash.move(to: NSPoint(x: inset, y: size - inset))
            slash.line(to: NSPoint(x: size - inset, y: inset))
            NSColor.black.setStroke()
            slash.lineWidth = 1.4
            slash.lineCapStyle = .round
            slash.stroke()
        }

        return true
    }
    img.isTemplate = true
    return img
}

// Set to true to test the update banner with fake data. Set false before release.
private let DEBUG_FAKE_UPDATE = false
private let currentVersion = "1.0"

@MainActor
final class AppDelegate: NSObject, NSApplicationDelegate {
    private var statusItem: NSStatusItem!
    private var statusLabel: NSMenuItem!
    private var pauseItem: NSMenuItem!
    private var intervalLabel: NSMenuItem!
    private var intensityLabel: NSMenuItem!
    private var durationLabel: NSMenuItem!
    private var launchAtLoginItem: NSMenuItem!
    private var respectDNDItem: NSMenuItem!
    private var activeScreenItem: NSMenuItem!
    private var aboutWindow: NSWindow?
    private var updateItem: NSMenuItem?
    private var updateSeparator: NSMenuItem?
    // Stored so pause item can swap between submenu ↔ direct action
    private var storedPauseSubmenu: NSMenu!
    // Stored for NSMenuDelegate hover preview
    private var modeSubmenu: NSMenu!
    private var colorSubmenu: NSMenu!
    // Committed values — restored if user closes menu without selecting
    private var committedMode: FlashMode = .menuBarGlow
    private var committedColor: NSColor = .red
    // Debounce timer for hover previews
    private var previewDebounceTimer: Timer?

    private let pausaTimer = PausaTimer()
    private let flashOverlay = FlashOverlay()

    private var colorItems: [NSMenuItem] = []
    private var modeItems: [NSMenuItem] = []

    // Discrete snap values — interval back to minutes only
    private let intervalSteps: [Int] = [5*60, 10*60, 15*60, 20*60, 30*60, 45*60, 60*60]
    private let intensitySteps: [(String, CGFloat)] = [("Soft", 0.15), ("Medium", 0.4), ("Strong", 0.7)]
    // Duration extended to include 20s
    private let durationSteps: [(String, TimeInterval)] = [
        ("3s", 3), ("5s", 5), ("7s", 7), ("10s", 10), ("12s", 12), ("15s", 15), ("20s", 20)
    ]

    func applicationDidFinishLaunching(_ notification: Notification) {
        trackLaunch()
        // Load saved preferences
        let prefs = Preferences.shared
        pausaTimer.intervalSeconds    = prefs.intervalSeconds
        flashOverlay.intensity        = prefs.intensity
        flashOverlay.duration         = prefs.duration
        flashOverlay.activeScreenOnly = prefs.activeScreenOnly
        if let mode = FlashMode(rawValue: prefs.flashMode) { flashOverlay.mode = mode }
        flashOverlay.color            = prefs.flashColor

        statusItem = NSStatusBar.system.statusItem(withLength: NSStatusItem.variableLength)
        if let button = statusItem.button {
            button.image = pausaMenuBarIcon()
        }

        let menu = NSMenu()

        // --- Status ---
        statusLabel = NSMenuItem(title: "Next Pausa in \(pausaTimer.formattedTimeRemaining)", action: nil, keyEquivalent: "")
        statusLabel.isEnabled = false
        menu.addItem(statusLabel)

        // --- Pause submenu (directly under status) ---
        pauseItem = NSMenuItem(title: "Pause", action: nil, keyEquivalent: "")
        storedPauseSubmenu = NSMenu()

        let pauseOptions: [(String, TimeInterval)] = [
            ("Pause for 5 Minutes",  5 * 60),
            ("Pause for 10 Minutes", 10 * 60),
            ("Pause for 30 Minutes", 30 * 60),
            ("Pause for 1 Hour",     60 * 60),
        ]
        for (title, duration) in pauseOptions {
            let item = NSMenuItem(title: title, action: #selector(pauseForDuration(_:)), keyEquivalent: "")
            item.target = self
            item.representedObject = duration as NSNumber
            storedPauseSubmenu.addItem(item)
        }
        storedPauseSubmenu.addItem(.separator())

        let pauseIndefiniteItem = NSMenuItem(title: "Until I Resume", action: #selector(pauseIndefinitely), keyEquivalent: "")
        pauseIndefiniteItem.target = self
        storedPauseSubmenu.addItem(pauseIndefiniteItem)

        let pauseRestartItem = NSMenuItem(title: "Until Next Restart", action: #selector(pauseUntilRestart), keyEquivalent: "")
        pauseRestartItem.target = self
        storedPauseSubmenu.addItem(pauseRestartItem)

        pauseItem.submenu = storedPauseSubmenu
        menu.addItem(pauseItem)

        menu.addItem(.separator())

        // --- Interval ---
        intervalLabel = NSMenuItem(title: "Interval: \(PausaTimer.formatInterval(pausaTimer.intervalSeconds))", action: nil, keyEquivalent: "")
        intervalLabel.isEnabled = false
        menu.addItem(intervalLabel)

        let intervalSlider = NSSlider(value: Double(intervalStepIndex(for: pausaTimer.intervalSeconds)),
                                      minValue: 0, maxValue: Double(intervalSteps.count - 1),
                                      target: self, action: #selector(intervalSliderChanged(_:)))
        intervalSlider.frame = NSRect(x: 12, y: 0, width: 246, height: 24)
        intervalSlider.numberOfTickMarks = intervalSteps.count
        intervalSlider.allowsTickMarkValuesOnly = true
        intervalSlider.isContinuous = true

        let intervalView = NSView(frame: NSRect(x: 0, y: 0, width: 270, height: 28))
        intervalView.addSubview(intervalSlider)
        let intervalMenuItem = NSMenuItem()
        intervalMenuItem.view = intervalView
        menu.addItem(intervalMenuItem)

        menu.addItem(.separator())

        // --- Settings submenu ---
        let settingsMenuItem = NSMenuItem(title: "Settings", action: nil, keyEquivalent: "")
        let settingsMenu = NSMenu()

        // Capture committed values from loaded prefs
        committedMode = flashOverlay.mode
        committedColor = flashOverlay.color

        // Flash Mode — with subtitle descriptions
        let modeMenuItem = NSMenuItem(title: "Flash Mode", action: nil, keyEquivalent: "")
        modeSubmenu = NSMenu()
        modeSubmenu.delegate = self
        let modes: [(FlashMode, String)] = [
            (.menuBar,     "Solid bar across the top"),
            (.menuBarGlow, "Bar + soft glow below"),
            (.screenBorder,"Gradient glow on all edges"),
        ]
        for (mode, description) in modes {
            let item = NSMenuItem(title: mode.rawValue, action: #selector(modeSelected(_:)), keyEquivalent: "")
            if #available(macOS 14.4, *) {
                item.subtitle = description
            } else {
                item.toolTip = description
            }
            item.target = self
            item.representedObject = mode.rawValue
            if mode == flashOverlay.mode { item.state = .on }
            modeSubmenu.addItem(item)
            modeItems.append(item)
        }
        modeMenuItem.submenu = modeSubmenu
        settingsMenu.addItem(modeMenuItem)

        // Flash Color — with colored circle icons
        let colorMenuItem = NSMenuItem(title: "Flash Color", action: nil, keyEquivalent: "")
        colorSubmenu = NSMenu()
        colorSubmenu.delegate = self
        for (name, color) in Preferences.colorMap {
            let item = NSMenuItem(title: name, action: #selector(colorSelected(_:)), keyEquivalent: "")
            item.target = self
            item.representedObject = color
            item.image = colorCircleImage(color)
            if name == prefs.flashColorName { item.state = .on }
            colorSubmenu.addItem(item)
            colorItems.append(item)
        }
        colorMenuItem.submenu = colorSubmenu
        settingsMenu.addItem(colorMenuItem)

        settingsMenu.addItem(.separator())

        // Intensity
        intensityLabel = NSMenuItem(title: "Intensity: \(currentIntensityName())", action: nil, keyEquivalent: "")
        intensityLabel.isEnabled = false
        settingsMenu.addItem(intensityLabel)

        let intensitySlider = NSSlider(value: Double(intensityStepIndex()),
                                       minValue: 0, maxValue: Double(intensitySteps.count - 1),
                                       target: self, action: #selector(intensitySliderChanged(_:)))
        intensitySlider.frame = NSRect(x: 12, y: 0, width: 246, height: 24)
        intensitySlider.numberOfTickMarks = intensitySteps.count
        intensitySlider.allowsTickMarkValuesOnly = true
        intensitySlider.isContinuous = true

        let intensityView = NSView(frame: NSRect(x: 0, y: 0, width: 270, height: 28))
        intensityView.addSubview(intensitySlider)
        let intensityMenuItem = NSMenuItem()
        intensityMenuItem.view = intensityView
        settingsMenu.addItem(intensityMenuItem)

        // Duration
        durationLabel = NSMenuItem(title: "Duration: \(currentDurationName())", action: nil, keyEquivalent: "")
        durationLabel.isEnabled = false
        settingsMenu.addItem(durationLabel)

        let durationSlider = NSSlider(value: Double(durationStepIndex()),
                                      minValue: 0, maxValue: Double(durationSteps.count - 1),
                                      target: self, action: #selector(durationSliderChanged(_:)))
        durationSlider.frame = NSRect(x: 12, y: 0, width: 246, height: 24)
        durationSlider.numberOfTickMarks = durationSteps.count
        durationSlider.allowsTickMarkValuesOnly = true
        durationSlider.isContinuous = true

        let durationView = NSView(frame: NSRect(x: 0, y: 0, width: 270, height: 28))
        durationView.addSubview(durationSlider)
        let durationMenuItem = NSMenuItem()
        durationMenuItem.view = durationView
        settingsMenu.addItem(durationMenuItem)

        settingsMenu.addItem(.separator())

        // Test Flash — moved into Settings, no keyboard shortcut
        let testItem = NSMenuItem(title: "Test Flash Now", action: #selector(testFlash), keyEquivalent: "")
        testItem.target = self
        settingsMenu.addItem(testItem)

        settingsMenu.addItem(.separator())

        // Active screen only
        activeScreenItem = NSMenuItem(title: "Flash Active Screen Only", action: #selector(toggleActiveScreenOnly), keyEquivalent: "")
        activeScreenItem.target = self
        activeScreenItem.state = prefs.activeScreenOnly ? .on : .off
        settingsMenu.addItem(activeScreenItem)

        // Respect DND
        respectDNDItem = NSMenuItem(title: "Pause During Fullscreen Apps", action: #selector(toggleRespectDND), keyEquivalent: "")
        respectDNDItem.target = self
        respectDNDItem.state = prefs.respectDND ? .on : .off
        settingsMenu.addItem(respectDNDItem)

        // Launch at login
        launchAtLoginItem = NSMenuItem(title: "Launch at Login", action: #selector(toggleLaunchAtLogin), keyEquivalent: "")
        launchAtLoginItem.target = self
        launchAtLoginItem.state = StartupManager.shared.isEnabled ? .on : .off
        settingsMenu.addItem(launchAtLoginItem)

        settingsMenuItem.submenu = settingsMenu
        menu.addItem(settingsMenuItem)

        menu.addItem(.separator())

        // --- About — with eye icon ---
        let aboutItem = NSMenuItem(title: "About Pausa", action: #selector(showAbout), keyEquivalent: "")
        aboutItem.target = self
        let aboutIcon = pausaMenuBarIcon()
        aboutIcon.size = NSSize(width: 16, height: 16)
        aboutItem.image = aboutIcon
        menu.addItem(aboutItem)

        // --- Quit ---
        let quitItem = NSMenuItem(title: "Quit", action: #selector(NSApplication.terminate(_:)), keyEquivalent: "q")
        menu.addItem(quitItem)

        statusItem.menu = menu

        // Wire timer callbacks
        pausaTimer.onTick = { [weak self] in
            self?.updateStatusLabel()
        }
        pausaTimer.onFire = { [weak self] in
            guard let self else { return }
            if Preferences.shared.respectDND && FullscreenDetector.shared.isFullscreenAppActive { return }
            self.flashOverlay.flash()
        }
        pausaTimer.onPauseStateChanged = { [weak self] in
            self?.updatePauseMenuTitle()
            self?.updateMenuBarIcon()
            self?.updateStatusLabel()
        }
        pausaTimer.start()

        // Wire flash icon callbacks
        flashOverlay.onFlashStarted = { [weak self] in
            self?.setFlashActive(true)
        }
        flashOverlay.onFlashEnded = { [weak self] in
            self?.setFlashActive(false)
        }

        // Screen lock/unlock notifications
        DistributedNotificationCenter.default().addObserver(
            self,
            selector: #selector(screenLocked),
            name: NSNotification.Name("com.apple.screenIsLocked"),
            object: nil
        )
        DistributedNotificationCenter.default().addObserver(
            self,
            selector: #selector(screenUnlocked),
            name: NSNotification.Name("com.apple.screenIsUnlocked"),
            object: nil
        )

        // Check for updates (non-blocking)
        checkForUpdates()
    }

    // MARK: - Helpers

    /// Renders a small filled circle in the given color for use as a menu item icon.
    private func colorCircleImage(_ color: NSColor, size: CGFloat = 14) -> NSImage {
        let image = NSImage(size: NSSize(width: size, height: size))
        image.lockFocus()
        let rect = NSRect(x: 1, y: 1, width: size - 2, height: size - 2)
        let path = NSBezierPath(ovalIn: rect)
        color.setFill()
        path.fill()
        // Subtle border so light colors (yellow) are visible on both light/dark menu
        NSColor.black.withAlphaComponent(0.15).setStroke()
        path.lineWidth = 0.5
        path.stroke()
        image.unlockFocus()
        return image
    }

    private func intervalStepIndex(for seconds: Int) -> Int {
        intervalSteps.enumerated().min(by: { abs($0.element - seconds) < abs($1.element - seconds) })?.offset ?? 3
    }

    private func intensityStepIndex() -> Int {
        intensitySteps.enumerated().min(by: { abs($0.element.1 - flashOverlay.intensity) < abs($1.element.1 - flashOverlay.intensity) })?.offset ?? 1
    }

    private func durationStepIndex() -> Int {
        durationSteps.enumerated().min(by: { abs($0.element.1 - flashOverlay.duration) < abs($1.element.1 - flashOverlay.duration) })?.offset ?? 6
    }

    private func currentIntensityName() -> String {
        intensitySteps[intensityStepIndex()].0
    }

    private func currentDurationName() -> String {
        durationSteps[durationStepIndex()].0
    }

    // MARK: - Status & icon updates

    private func updateStatusLabel() {
        if pausaTimer.isPaused {
            if let until = pausaTimer.pauseUntil {
                // Round up so a 10-min pause shows 10:00 at t=0 (not 9:59 due to sub-second drift)
                // and matches the ceiling semantics used by the active countdown.
                let remaining = max(0, Int(until.timeIntervalSinceNow.rounded(.up)))
                let m = remaining / 60
                let s = remaining % 60
                statusLabel.title = "Paused, resumes in \(String(format: "%d:%02d", m, s))"
            } else if pausaTimer.pauseUntilRestart {
                statusLabel.title = "Paused until restart"
            } else {
                statusLabel.title = "Paused"
            }
        } else {
            statusLabel.title = "Next Pausa in \(pausaTimer.formattedTimeRemaining)"
        }
    }

    private func updatePauseMenuTitle() {
        if pausaTimer.isPaused {
            // Become a single direct-action Resume button — no submenu
            pauseItem.submenu = nil
            pauseItem.action = #selector(resumeFromPause)
            pauseItem.target = self
            pauseItem.title = "Resume"
        } else {
            // Restore full pause submenu
            pauseItem.action = nil
            pauseItem.target = nil
            pauseItem.submenu = storedPauseSubmenu
            pauseItem.title = "Pause"
        }
    }

    private func updateMenuBarIcon() {
        guard let button = statusItem.button else { return }
        guard flashEndTime == nil else { return }  // don't interrupt flash countdown
        button.title = ""
        button.image = pausaMenuBarIcon(paused: pausaTimer.isPaused)
    }

    // MARK: - Flash countdown icon

    private var flashCountdownTimer: Timer?
    private var flashEndTime: Date?

    private func setFlashActive(_ active: Bool) {
        flashCountdownTimer?.invalidate()
        flashCountdownTimer = nil
        flashEndTime = nil

        guard let button = statusItem.button else { return }

        if active {
            let duration = flashOverlay.duration
            flashEndTime = Date().addingTimeInterval(duration)

            // Show initial value immediately
            button.image = nil
            button.title = "\(Int(duration.rounded()))s"

            // Update every 0.5s based on actual wall-clock time remaining
            // so it can never drift or cut off early regardless of animation timing
            flashCountdownTimer = Timer.scheduledTimer(withTimeInterval: 0.5, repeats: true) { [weak self] _ in
                MainActor.assumeIsolated {
                    guard let self, let end = self.flashEndTime else { return }
                    let remaining = max(0, Int(end.timeIntervalSinceNow.rounded(.up)))
                    self.statusItem.button?.title = "\(remaining)s"
                }
            }
            RunLoop.main.add(flashCountdownTimer!, forMode: .common)
        } else {
            button.title = ""
            button.image = pausaMenuBarIcon(paused: pausaTimer.isPaused)
        }
    }

    // MARK: - Interval

    @objc private func intervalSliderChanged(_ sender: NSSlider) {
        let idx = Int(sender.doubleValue.rounded())
        let seconds = intervalSteps[idx]
        pausaTimer.intervalSeconds = seconds
        intervalLabel.title = "Interval: \(PausaTimer.formatInterval(seconds))"
        Preferences.shared.intervalSeconds = seconds
    }

    // MARK: - Settings actions

    /// Immediately kills any in-flight preview, then fires one clean preview 400ms after
    /// the last call. This means dragging the slider never produces mid-drag flickers —
    /// only a single flash once the value settles.
    private func schedulePreview(pulses: Int = 2) {
        previewDebounceTimer?.invalidate()
        flashOverlay.cancelPreview()   // instant kill — no jerk during drag
        previewDebounceTimer = Timer.scheduledTimer(withTimeInterval: 0.4, repeats: false) { [weak self] _ in
            MainActor.assumeIsolated { self?.flashOverlay.previewFlash(pulses: pulses) }
        }
        RunLoop.main.add(previewDebounceTimer!, forMode: .common)
    }

    @objc private func intensitySliderChanged(_ sender: NSSlider) {
        let idx = Int(sender.doubleValue.rounded())
        let (name, value) = intensitySteps[idx]
        flashOverlay.intensity = value
        intensityLabel.title = "Intensity: \(name)"
        Preferences.shared.intensity = value
        schedulePreview(pulses: 4)  // 4 pulses ≈ 2s so keyboard nudges are clearly visible
    }

    @objc private func durationSliderChanged(_ sender: NSSlider) {
        let idx = Int(sender.doubleValue.rounded())
        let (name, value) = durationSteps[idx]
        flashOverlay.duration = value
        durationLabel.title = "Duration: \(name)"
        Preferences.shared.duration = value
    }

    @objc private func modeSelected(_ sender: NSMenuItem) {
        guard let rawValue = sender.representedObject as? String,
              let mode = FlashMode(rawValue: rawValue) else { return }
        flashOverlay.mode = mode
        committedMode = mode
        for item in modeItems { item.state = .off }
        sender.state = .on
        Preferences.shared.flashMode = rawValue
    }

    @objc private func colorSelected(_ sender: NSMenuItem) {
        guard let color = sender.representedObject as? NSColor else { return }
        flashOverlay.color = color
        committedColor = color
        for item in colorItems { item.state = .off }
        sender.state = .on
        let name = Preferences.colorMap.first { $0.color == color }?.name ?? "Red"
        Preferences.shared.flashColorName = name
    }

    @objc private func testFlash() {
        flashOverlay.flash()
    }

    // MARK: - Pause actions

    @objc private func pauseForDuration(_ sender: NSMenuItem) {
        guard let duration = (sender.representedObject as? NSNumber)?.doubleValue else { return }
        pausaTimer.pause(for: duration)
    }

    @objc private func pauseIndefinitely() {
        pausaTimer.pauseIndefinitely()
    }

    @objc private func pauseUntilRestart() {
        pausaTimer.pauseUntilNextRestart()
    }

    @objc private func resumeFromPause() {
        pausaTimer.resume()
    }

    // MARK: - Toggle actions

    @objc private func toggleRespectDND() {
        let newValue = !Preferences.shared.respectDND
        Preferences.shared.respectDND = newValue
        respectDNDItem.state = newValue ? .on : .off
    }

    @objc private func toggleActiveScreenOnly() {
        let newValue = !Preferences.shared.activeScreenOnly
        Preferences.shared.activeScreenOnly = newValue
        flashOverlay.activeScreenOnly = newValue
        activeScreenItem.state = newValue ? .on : .off
    }

    @objc private func toggleLaunchAtLogin() {
        let newValue = !StartupManager.shared.isEnabled
        StartupManager.shared.setEnabled(newValue)
        launchAtLoginItem.state = newValue ? .on : .off
    }

    // MARK: - About

    @objc private func showAbout() {
        if let existing = aboutWindow {
            existing.makeKeyAndOrderFront(nil)
            NSApp.activate(ignoringOtherApps: true)
            return
        }

        let windowWidth: CGFloat = 300
        let windowHeight: CGFloat = 280

        let window = NSPanel(
            contentRect: NSRect(x: 0, y: 0, width: windowWidth, height: windowHeight),
            styleMask: [.titled, .closable],
            backing: .buffered,
            defer: false
        )
        window.title = "About Pausa"
        window.center()
        window.isReleasedWhenClosed = false

        let content = NSView(frame: NSRect(x: 0, y: 0, width: windowWidth, height: windowHeight))

        // --- App icon from bundle ---
        let iconView = NSImageView(frame: NSRect(x: 0, y: 195, width: windowWidth, height: 64))
        iconView.image = NSApp.applicationIconImage
        iconView.imageScaling = .scaleProportionallyUpOrDown
        iconView.imageAlignment = .alignCenter
        content.addSubview(iconView)

        // --- App name ---
        let titleField = NSTextField(labelWithString: "Pausa")
        titleField.font = NSFont.boldSystemFont(ofSize: 20)
        titleField.alignment = .center
        titleField.frame = NSRect(x: 0, y: 170, width: windowWidth, height: 26)
        content.addSubview(titleField)

        // --- Version (smaller, secondary) ---
        let versionField = NSTextField(labelWithString: "v\(currentVersion)")
        versionField.font = NSFont.systemFont(ofSize: 12)
        versionField.textColor = .secondaryLabelColor
        versionField.alignment = .center
        versionField.frame = NSRect(x: 0, y: 150, width: windowWidth, height: 18)
        content.addSubview(versionField)

        // --- Description ---
        let descField = NSTextField(wrappingLabelWithString: "A focused eye-care utility for macOS.\nMonitors your screen time and nudges you\ntoward breaks at the right moments.\nQuietly, without friction.")
        descField.font = NSFont.systemFont(ofSize: 12)
        descField.textColor = NSColor.labelColor.withAlphaComponent(0.55)
        descField.alignment = .center
        descField.frame = NSRect(x: 20, y: 78, width: windowWidth - 40, height: 68)
        content.addSubview(descField)

        // --- Credit with hyperlink ---
        let creditField = NSTextField(frame: NSRect(x: 0, y: 40, width: windowWidth, height: 18))
        creditField.isEditable = false
        creditField.isBordered = false
        creditField.isSelectable = true
        creditField.allowsEditingTextAttributes = true
        creditField.drawsBackground = false
        creditField.alignment = .center
        let creditParagraph = NSMutableParagraphStyle()
        creditParagraph.alignment = .center
        let creditStr = NSMutableAttributedString(string: "Built by Seriamo", attributes: [
            .font: NSFont.systemFont(ofSize: 12),
            .foregroundColor: NSColor.secondaryLabelColor,
            .paragraphStyle: creditParagraph
        ])
        creditStr.addAttributes([
            .link: URL(string: "https://seriamo.com")!,
            .foregroundColor: NSColor(red: 0.180, green: 0.361, blue: 0.486, alpha: 1.0),
            .underlineStyle: NSUnderlineStyle.single.rawValue
        ], range: NSRange(location: 9, length: 7))
        creditField.attributedStringValue = creditStr
        content.addSubview(creditField)

        window.contentView = content
        aboutWindow = window

        NotificationCenter.default.addObserver(
            forName: NSWindow.willCloseNotification,
            object: window,
            queue: .main
        ) { [weak self] _ in
            MainActor.assumeIsolated {
                self?.aboutWindow = nil
            }
        }

        window.makeKeyAndOrderFront(nil)
        NSApp.activate(ignoringOtherApps: true)
    }

    @objc private func openSeriamo() {
        NSWorkspace.shared.open(URL(string: "https://seriamo.com")!)
    }

    // MARK: - Update checker

    private func checkForUpdates() {
        if DEBUG_FAKE_UPDATE {
            showUpdateAvailable(version: "2.0")
            return
        }

        let url = URL(string: "https://api.github.com/repos/seriamo/pausa/releases/latest")!
        var request = URLRequest(url: url)
        request.setValue("application/vnd.github+json", forHTTPHeaderField: "Accept")
        request.timeoutInterval = 10

        URLSession.shared.dataTask(with: request) { [weak self] data, _, error in
            guard error == nil, let data,
                  let json = try? JSONSerialization.jsonObject(with: data) as? [String: Any],
                  let tagName = json["tag_name"] as? String else { return }

            let remote = tagName.trimmingCharacters(in: CharacterSet(charactersIn: "vV"))
            if remote.compare(currentVersion, options: .numeric) == .orderedDescending {
                DispatchQueue.main.async {
                    MainActor.assumeIsolated {
                        self?.showUpdateAvailable(version: remote)
                    }
                }
            }
        }.resume()
    }

    private func showUpdateAvailable(version: String) {
        guard let menu = statusItem.menu, updateItem == nil else { return }

        let item = NSMenuItem(title: "Pausa v\(version) available", action: #selector(openUpdatePage), keyEquivalent: "")
        item.target = self
        item.attributedTitle = NSAttributedString(
            string: "Pausa v\(version) available",
            attributes: [
                .foregroundColor: NSColor(red: 0.180, green: 0.361, blue: 0.486, alpha: 1.0),
                .font: NSFont.systemFont(ofSize: 13, weight: .medium)
            ]
        )
        let sep = NSMenuItem.separator()

        menu.insertItem(item, at: 0)
        menu.insertItem(sep, at: 1)
        updateItem = item
        updateSeparator = sep
    }

    @objc private func openUpdatePage() {
        NSWorkspace.shared.open(URL(string: "https://github.com/seriamo/pausa/releases/latest")!)
    }

    // MARK: - Anonymous launch tracking

    private func trackLaunch() {
        guard let url = URL(string: "https://api.seriamo.com/pausa/launch") else { return }
        var request = URLRequest(url: url)
        request.httpMethod = "POST"
        request.setValue("application/json", forHTTPHeaderField: "Content-Type")
        // Velvet rope, not a vault door
        request.setValue("Q7cDfnufc3DKZSW7fkcSsgvb2dIsWtFT-upIM9GbF0s", forHTTPHeaderField: "x-launch-token")
        request.timeoutInterval = 10

        // Persistent anonymous device ID
        let key = "com.seriamo.pausa.deviceId"
        let deviceId: String
        if let existing = UserDefaults.standard.string(forKey: key) {
            deviceId = existing
        } else {
            let newId = UUID().uuidString
            UserDefaults.standard.set(newId, forKey: key)
            deviceId = newId
        }

        let body: [String: String] = ["version": currentVersion, "device_id": deviceId]
        request.httpBody = try? JSONSerialization.data(withJSONObject: body)
        URLSession.shared.dataTask(with: request).resume()
    }

    // MARK: - Screen lock

    @objc private func screenLocked() {
        pausaTimer.handleScreenLocked()
    }

    @objc private func screenUnlocked() {
        pausaTimer.handleScreenUnlocked()
    }
}

// MARK: - NSMenuDelegate (hover preview)

extension AppDelegate: NSMenuDelegate {
    /// Called when the highlighted item changes (user hovers to a new item).
    func menu(_ menu: NSMenu, willHighlight item: NSMenuItem?) {
        previewDebounceTimer?.invalidate()

        guard let item else { return }

        // Capture the values we need before the async hop to avoid Sendable warnings
        let isModeMenu = menu === modeSubmenu
        let isColorMenu = menu === colorSubmenu
        let modeRawValue = item.representedObject as? String
        let hoverColor = item.representedObject as? NSColor

        previewDebounceTimer = Timer.scheduledTimer(withTimeInterval: 0.18, repeats: false) { [weak self] _ in
            MainActor.assumeIsolated {
                guard let self else { return }

                if isModeMenu {
                    guard let rawValue = modeRawValue,
                          let mode = FlashMode(rawValue: rawValue) else { return }
                    self.flashOverlay.mode = mode   // triggers buildWindows()
                    self.flashOverlay.previewFlash()
                } else if isColorMenu {
                    guard let color = hoverColor else { return }
                    self.flashOverlay.color = color
                    self.flashOverlay.previewFlash()
                }
            }
        }
        RunLoop.main.add(previewDebounceTimer!, forMode: .common)
    }

    /// Restore committed values when user closes menu without confirming a choice.
    func menuDidClose(_ menu: NSMenu) {
        if menu === modeSubmenu {
            if flashOverlay.mode != committedMode {
                flashOverlay.mode = committedMode
                for item in modeItems {
                    item.state = (item.representedObject as? String) == committedMode.rawValue ? .on : .off
                }
            }
        } else if menu === colorSubmenu {
            if flashOverlay.color != committedColor {
                flashOverlay.color = committedColor
                for item in colorItems {
                    item.state = (item.representedObject as? NSColor) == committedColor ? .on : .off
                }
            }
        }
    }
}

// — Entry point —

let app = NSApplication.shared
app.setActivationPolicy(.accessory)

let delegate = AppDelegate()
app.delegate = delegate

app.run()
