import AppKit
import CoreGraphics

/// Detects whether any app currently has a window that fills an entire screen.
/// This covers presentation mode (Keynote, PowerPoint), fullscreen video (VLC, QuickTime,
/// browser video), and any fullscreen game or app.
///
/// Called on-demand at break-fire time. No background polling needed.
@MainActor
final class FullscreenDetector {
    static let shared = FullscreenDetector()

    var isFullscreenAppActive: Bool {
        guard let windowList = CGWindowListCopyWindowInfo(
            [.optionOnScreenOnly, .excludeDesktopElements],
            kCGNullWindowID
        ) as? [[CFString: Any]] else { return false }

        // Compare by size only (CG and Cocoa coordinate origins differ, but dimensions match)
        let screenSizes = NSScreen.screens.map {
            CGSize(width: $0.frame.width, height: $0.frame.height)
        }

        for info in windowList {
            // Skip our own windows and system UI elements
            guard
                let ownerName = info[kCGWindowOwnerName] as? String,
                ownerName != "Pausa",
                ownerName != "Window Server",
                ownerName != "Dock",
                let alpha = info[kCGWindowAlpha] as? CGFloat, alpha > 0,
                let boundsDict = info[kCGWindowBounds] as? [String: CGFloat],
                let w = boundsDict["Width"],
                let h = boundsDict["Height"],
                w > 100, h > 100   // ignore tiny utility windows
            else { continue }

            let windowSize = CGSize(width: w, height: h)

            if screenSizes.contains(where: {
                abs($0.width  - windowSize.width)  < 5 &&
                abs($0.height - windowSize.height) < 5
            }) {
                return true
            }
        }
        return false
    }
}
