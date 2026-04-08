import Foundation
import AppKit

/// Single source of truth for all UserDefaults-backed settings.
@MainActor
final class Preferences {
    static let shared = Preferences()
    private let defaults = UserDefaults.standard

    private enum Key: String {
        case intervalSeconds
        case intensity
        case duration
        case flashMode
        case flashColorName
        case activeScreenOnly
        case respectDND
        case launchAtLogin
    }

    var intervalSeconds: Int {
        get { let v = defaults.integer(forKey: Key.intervalSeconds.rawValue); return v > 0 ? v : 20 * 60 }
        set { defaults.set(newValue, forKey: Key.intervalSeconds.rawValue) }
    }

    var intensity: CGFloat {
        get {
            let v = defaults.double(forKey: Key.intensity.rawValue)
            return v > 0 ? CGFloat(v) : 0.4
        }
        set { defaults.set(Double(newValue), forKey: Key.intensity.rawValue) }
    }

    var duration: TimeInterval {
        get {
            let v = defaults.double(forKey: Key.duration.rawValue)
            return v > 0 ? v : 20.0
        }
        set { defaults.set(newValue, forKey: Key.duration.rawValue) }
    }

    var flashMode: String {
        get { defaults.string(forKey: Key.flashMode.rawValue) ?? "Menu Bar + Glow" }
        set { defaults.set(newValue, forKey: Key.flashMode.rawValue) }
    }

    var flashColorName: String {
        get { defaults.string(forKey: Key.flashColorName.rawValue) ?? "Red" }
        set { defaults.set(newValue, forKey: Key.flashColorName.rawValue) }
    }

    var activeScreenOnly: Bool {
        get { defaults.object(forKey: Key.activeScreenOnly.rawValue) as? Bool ?? false }
        set { defaults.set(newValue, forKey: Key.activeScreenOnly.rawValue) }
    }

    var respectDND: Bool {
        get { defaults.object(forKey: Key.respectDND.rawValue) as? Bool ?? true }
        set { defaults.set(newValue, forKey: Key.respectDND.rawValue) }
    }

    var launchAtLogin: Bool {
        get { defaults.object(forKey: Key.launchAtLogin.rawValue) as? Bool ?? false }
        set { defaults.set(newValue, forKey: Key.launchAtLogin.rawValue) }
    }

    /// Map stored color name → NSColor
    static let colorMap: [(name: String, color: NSColor)] = [
        ("Red",    .red),
        ("Orange", .orange),
        ("Yellow", .yellow),
        ("Green",  .green),
        ("Blue",   .systemBlue),
        ("Purple", .purple),
    ]

    var flashColor: NSColor {
        Self.colorMap.first { $0.name == flashColorName }?.color ?? .red
    }
}
