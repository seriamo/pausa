import AppKit

enum FlashMode: String, CaseIterable {
    case menuBar = "Menu Bar"
    case menuBarGlow = "Menu Bar + Glow"
    case screenBorder = "Screen Border"
}

/// Which edge of the screen a gradient border sits on
private enum BorderEdge {
    case top, bottom, left, right
}

/// View that draws a gradient from the screen edge (full color) fading inward (transparent)
private class GradientBorderView: NSView {
    var edge: BorderEdge = .top
    var flashColor: NSColor = .red

    override func draw(_ dirtyRect: NSRect) {
        guard let context = NSGraphicsContext.current?.cgContext else { return }

        let startColor = flashColor.cgColor
        let endColor = flashColor.withAlphaComponent(0).cgColor

        guard let gradient = CGGradient(
            colorsSpace: CGColorSpaceCreateDeviceRGB(),
            colors: [startColor, endColor] as CFArray,
            locations: [0.0, 1.0]
        ) else { return }

        let (start, end): (CGPoint, CGPoint) = switch edge {
        case .top:    (CGPoint(x: 0, y: bounds.maxY), CGPoint(x: 0, y: bounds.minY))
        case .bottom: (CGPoint(x: 0, y: bounds.minY), CGPoint(x: 0, y: bounds.maxY))
        case .left:   (CGPoint(x: bounds.minX, y: 0), CGPoint(x: bounds.maxX, y: 0))
        case .right:  (CGPoint(x: bounds.maxX, y: 0), CGPoint(x: bounds.minX, y: 0))
        }

        context.drawLinearGradient(gradient, start: start, end: end, options: [])
    }
}

@MainActor
final class FlashOverlay {
    var color: NSColor = .red {
        didSet { updateWindowColors() }
    }

    /// Peak alpha for pulses: Soft=0.15, Medium=0.4, Strong=0.7
    var intensity: CGFloat = 0.4

    /// Total flash duration in seconds
    var duration: TimeInterval = 7.0

    var mode: FlashMode = .menuBarGlow {
        didSet { buildWindows() }
    }

    /// When true, only flash on the screen the cursor is currently on
    var activeScreenOnly: Bool = false

    var onFlashStarted: (() -> Void)?
    var onFlashEnded: (() -> Void)?

    /// Incremented each time a new preview starts; stale pulse callbacks check this and bail.
    private var previewGeneration = 0

    private var windows: [NSWindow] = []
    private var solidWindows: [NSWindow] = []
    private var gradientViews: [GradientBorderView] = []
    private let borderGlowWidth: CGFloat = 100.0

    /// Maps display ID → windows belonging to that screen
    private var screenWindows: [CGDirectDisplayID: [NSWindow]] = [:]

    init() {
        buildWindows()
        NotificationCenter.default.addObserver(
            forName: NSApplication.didChangeScreenParametersNotification,
            object: nil,
            queue: .main
        ) { [weak self] _ in
            MainActor.assumeIsolated {
                self?.buildWindows()
            }
        }
    }

    private func displayID(for screen: NSScreen) -> CGDirectDisplayID {
        screen.deviceDescription[NSDeviceDescriptionKey("NSScreenNumber")] as? CGDirectDisplayID ?? 0
    }

    private func buildWindows() {
        windows.forEach { $0.orderOut(nil) }
        windows.removeAll()
        solidWindows.removeAll()
        gradientViews.removeAll()
        screenWindows.removeAll()

        for screen in NSScreen.screens {
            var screenWins: [NSWindow] = []

            switch mode {
            case .menuBar:
                if let w = makeMenuBarWindow(screen: screen) {
                    windows.append(w)
                    solidWindows.append(w)
                    screenWins.append(w)
                }
            case .menuBarGlow:
                if let w = makeMenuBarWindow(screen: screen) {
                    windows.append(w)
                    solidWindows.append(w)
                    screenWins.append(w)
                }
                let (wins, views) = makeMenuBarGlowWindows(screen: screen)
                windows.append(contentsOf: wins)
                gradientViews.append(contentsOf: views)
                screenWins.append(contentsOf: wins)
            case .screenBorder:
                let (wins, views) = makeGradientBorderWindows(screen: screen)
                windows.append(contentsOf: wins)
                gradientViews.append(contentsOf: views)
                screenWins.append(contentsOf: wins)
            }

            screenWindows[displayID(for: screen)] = screenWins
        }
    }

    private func makeMenuBarWindow(screen: NSScreen) -> NSWindow? {
        let fullFrame = screen.frame
        let visibleFrame = screen.visibleFrame
        let menuBarHeight = fullFrame.maxY - visibleFrame.maxY
        guard menuBarHeight > 0 else { return nil }

        let barRect = NSRect(
            x: fullFrame.origin.x,
            y: visibleFrame.maxY,
            width: fullFrame.width,
            height: menuBarHeight
        )
        return makeSolidWindow(frame: barRect)
    }

    private func makeMenuBarGlowWindows(screen: NSScreen) -> ([NSWindow], [GradientBorderView]) {
        let fullFrame = screen.frame
        let visibleFrame = screen.visibleFrame
        let menuBarHeight = fullFrame.maxY - visibleFrame.maxY
        guard menuBarHeight > 0 else { return ([], []) }

        let glowRect = NSRect(
            x: fullFrame.origin.x,
            y: visibleFrame.maxY - borderGlowWidth,
            width: fullFrame.width,
            height: borderGlowWidth
        )
        let (win, view) = makeGradientWindow(frame: glowRect, edge: .top)
        return ([win], [view])
    }

    private func makeGradientBorderWindows(screen: NSScreen) -> ([NSWindow], [GradientBorderView]) {
        let f = screen.frame
        let t = borderGlowWidth
        let edges: [(NSRect, BorderEdge)] = [
            (NSRect(x: f.minX, y: f.maxY - t, width: f.width, height: t), .top),
            (NSRect(x: f.minX, y: f.minY, width: f.width, height: t), .bottom),
            (NSRect(x: f.minX, y: f.minY, width: t, height: f.height), .left),
            (NSRect(x: f.maxX - t, y: f.minY, width: t, height: f.height), .right),
        ]

        var wins: [NSWindow] = []
        var views: [GradientBorderView] = []
        for (rect, edge) in edges {
            let (w, v) = makeGradientWindow(frame: rect, edge: edge)
            wins.append(w)
            views.append(v)
        }
        return (wins, views)
    }

    private func makeGradientWindow(frame: NSRect, edge: BorderEdge) -> (NSWindow, GradientBorderView) {
        let window = NSWindow(
            contentRect: frame,
            styleMask: .borderless,
            backing: .buffered,
            defer: false
        )
        window.level = NSWindow.Level(rawValue: NSWindow.Level.statusBar.rawValue + 1)
        window.backgroundColor = .clear
        window.isOpaque = false
        window.alphaValue = 0
        window.ignoresMouseEvents = true
        window.collectionBehavior = [.canJoinAllSpaces, .stationary]
        window.hasShadow = false

        let gradientView = GradientBorderView(frame: NSRect(origin: .zero, size: frame.size))
        gradientView.edge = edge
        gradientView.flashColor = color
        gradientView.autoresizingMask = [.width, .height]
        window.contentView = gradientView

        return (window, gradientView)
    }

    private func makeSolidWindow(frame: NSRect) -> NSWindow {
        let window = NSWindow(
            contentRect: frame,
            styleMask: .borderless,
            backing: .buffered,
            defer: false
        )
        window.level = NSWindow.Level(rawValue: NSWindow.Level.statusBar.rawValue + 1)
        window.backgroundColor = color
        window.isOpaque = false
        window.alphaValue = 0
        window.ignoresMouseEvents = true
        window.collectionBehavior = [.canJoinAllSpaces, .stationary]
        return window
    }

    private func updateWindowColors() {
        for window in solidWindows {
            window.backgroundColor = color
        }
        for view in gradientViews {
            view.flashColor = color
            view.needsDisplay = true
        }
    }

    // MARK: - Preview flash (no onFlashStarted/Ended callbacks)

    /// Instantly kills any running preview — call this before starting a new one or on slider drag.
    func cancelPreview() {
        previewGeneration += 1
        for w in windows {
            w.alphaValue = 0   // instant, no animation
            w.orderOut(nil)
        }
    }

    /// `pulses`: number of blink cycles. 2 ≈ 1s, 4 ≈ 2s.
    func previewFlash(pulses: Int = 2) {
        // Bump generation so any in-flight pulse callbacks become no-ops
        previewGeneration += 1
        let gen = previewGeneration

        // Instantly kill any windows left visible by a previous preview
        NSAnimationContext.runAnimationGroup { ctx in
            ctx.duration = 0
            for w in windows { w.animator().alphaValue = 0 }
        }
        for w in windows { w.orderOut(nil) }

        let previewWindows = windowsToFlash()
        for window in previewWindows { window.orderFrontRegardless() }
        previewPulse(windows: previewWindows, index: 0, total: pulses, generation: gen)
    }

    private func previewPulse(windows previewWindows: [NSWindow], index: Int, total: Int, generation: Int) {
        // Bail if a newer preview has taken over
        guard index < total, generation == previewGeneration else {
            if generation == previewGeneration {
                for window in previewWindows { window.orderOut(nil) }
            }
            return
        }
        let peakAlpha = intensity
        NSAnimationContext.runAnimationGroup({ ctx in
            ctx.duration = 0.2
            for w in previewWindows { w.animator().alphaValue = peakAlpha }
        }, completionHandler: { [weak self] in
            MainActor.assumeIsolated {
                guard let self, generation == self.previewGeneration else { return }
                NSAnimationContext.runAnimationGroup({ ctx in
                    ctx.duration = 0.3
                    for w in previewWindows { w.animator().alphaValue = 0 }
                }, completionHandler: {
                    MainActor.assumeIsolated {
                        self.previewPulse(windows: previewWindows, index: index + 1, total: total, generation: generation)
                    }
                })
            }
        })
    }

    // MARK: - Flash

    private func windowsToFlash() -> [NSWindow] {
        if activeScreenOnly {
            let mouseLocation = NSEvent.mouseLocation
            let activeScreen = NSScreen.screens.first {
                NSMouseInRect(mouseLocation, $0.frame, false)
            } ?? NSScreen.main ?? NSScreen.screens[0]
            return screenWindows[displayID(for: activeScreen)] ?? windows
        }
        return windows
    }

    func flash() {
        onFlashStarted?()
        let activeWindows = windowsToFlash()

        for window in activeWindows {
            window.orderFrontRegardless()
        }

        let pulseDuration = 0.65
        let pulseCount = max(3, Int(duration / pulseDuration))
        pulse(activeWindows: activeWindows, index: 0, total: pulseCount)
    }

    private func pulse(activeWindows: [NSWindow], index: Int, total: Int) {
        guard index < total else {
            for window in activeWindows {
                window.orderOut(nil)
            }
            onFlashEnded?()
            return
        }

        let progress = CGFloat(index) / CGFloat(total)
        let peakAlpha = intensity * (1.0 - 0.3 * progress)

        NSAnimationContext.runAnimationGroup({ context in
            context.duration = 0.3
            for window in activeWindows {
                window.animator().alphaValue = peakAlpha
            }
        }, completionHandler: { [weak self] in
            MainActor.assumeIsolated {
                NSAnimationContext.runAnimationGroup({ context in
                    context.duration = 0.35
                    for window in activeWindows {
                        window.animator().alphaValue = 0
                    }
                }, completionHandler: {
                    MainActor.assumeIsolated {
                        self?.pulse(activeWindows: activeWindows, index: index + 1, total: total)
                    }
                })
            }
        })
    }
}
