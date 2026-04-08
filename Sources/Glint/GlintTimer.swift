import Foundation

@MainActor
final class GlintTimer {
    var intervalSeconds: Int = 20 {
        didSet { reset() }
    }
    var isPaused: Bool = false
    var isScreenLocked: Bool = false

    /// If set, auto-resume when wall clock passes this date
    var pauseUntil: Date? = nil
    /// Paused indefinitely until next app restart (no auto-resume)
    var pauseUntilRestart: Bool = false

    var secondsRemaining: Int = 0
    var onTick: (() -> Void)?
    var onFire: (() -> Void)?
    var onPauseStateChanged: (() -> Void)?

    private var timer: Timer?

    init() {
        secondsRemaining = intervalSeconds
    }

    func start() {
        timer?.invalidate()
        timer = Timer.scheduledTimer(withTimeInterval: 1.0, repeats: true) { [weak self] _ in
            MainActor.assumeIsolated {
                self?.tick()
            }
        }
        RunLoop.main.add(timer!, forMode: .common)
    }

    func stop() {
        timer?.invalidate()
        timer = nil
    }

    func reset() {
        secondsRemaining = intervalSeconds
        onTick?()
    }

    func handleScreenLocked() {
        isScreenLocked = true
    }

    func handleScreenUnlocked() {
        isScreenLocked = false
        reset()
    }

    // MARK: - Pause controls

    func pause(for seconds: TimeInterval) {
        isPaused = true
        pauseUntil = Date().addingTimeInterval(seconds)
        pauseUntilRestart = false
        onPauseStateChanged?()
        onTick?()
    }

    func pauseIndefinitely() {
        isPaused = true
        pauseUntil = nil
        pauseUntilRestart = false
        onPauseStateChanged?()
        onTick?()
    }

    func pauseUntilNextRestart() {
        isPaused = true
        pauseUntil = nil
        pauseUntilRestart = true
        onPauseStateChanged?()
        onTick?()
    }

    func resume() {
        isPaused = false
        pauseUntil = nil
        pauseUntilRestart = false
        reset()
        onPauseStateChanged?()
    }

    // MARK: - Tick

    private func tick() {
        // Auto-resume timed pause when time has elapsed
        if let until = pauseUntil, Date() >= until {
            pauseUntil = nil
            isPaused = false
            onPauseStateChanged?()
        }

        guard !isPaused, !isScreenLocked else { return }

        secondsRemaining -= 1
        onTick?()

        if secondsRemaining <= 0 {
            onFire?()
            reset()
        }
    }

    // MARK: - Formatting

    var formattedTimeRemaining: String {
        let m = secondsRemaining / 60
        let s = secondsRemaining % 60
        return String(format: "%d:%02d", m, s)
    }

    static func formatInterval(_ seconds: Int) -> String {
        if seconds < 60 {
            return "\(seconds)s"
        } else {
            let m = seconds / 60
            return "\(m) min"
        }
    }
}
