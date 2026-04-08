import ServiceManagement
import AppKit

@MainActor
final class StartupManager {
    static let shared = StartupManager()

    var isEnabled: Bool {
        SMAppService.mainApp.status == .enabled
    }

    func setEnabled(_ enabled: Bool) {
        do {
            if enabled {
                try SMAppService.mainApp.register()
            } else {
                try SMAppService.mainApp.unregister()
            }
            Preferences.shared.launchAtLogin = enabled
        } catch {
            let alert = NSAlert()
            alert.messageText = "Could not \(enabled ? "enable" : "disable") launch at login"
            alert.informativeText = error.localizedDescription
            alert.runModal()
        }
    }
}
