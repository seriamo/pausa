#!/usr/bin/env swift
import AppKit

let sizes: [(CGFloat, String)] = [
    (16, "icon_16x16"),
    (32, "icon_16x16@2x"),
    (32, "icon_32x32"),
    (64, "icon_32x32@2x"),
    (128, "icon_128x128"),
    (256, "icon_128x128@2x"),
    (256, "icon_256x256"),
    (512, "icon_256x256@2x"),
    (512, "icon_512x512"),
    (1024, "icon_512x512@2x"),
]

let iconsetPath = "/tmp/Pausa.iconset"
let fm = FileManager.default
try? fm.removeItem(atPath: iconsetPath)
try fm.createDirectory(atPath: iconsetPath, withIntermediateDirectories: true)

for (size, name) in sizes {
    let image = NSImage(size: NSSize(width: size, height: size), flipped: false) { rect in
        // Background: rounded rect with Seriamo Adriatico gradient
        let bgPath = NSBezierPath(roundedRect: rect.insetBy(dx: size * 0.05, dy: size * 0.05),
                                   xRadius: size * 0.2, yRadius: size * 0.2)

        // Gradient: Adriatico (#2e5c7c) to Adriatico Deep (#1e3f55)
        let gradient = NSGradient(starting: NSColor(red: 0.180, green: 0.361, blue: 0.486, alpha: 1.0),
                                  ending: NSColor(red: 0.118, green: 0.247, blue: 0.333, alpha: 1.0))!
        gradient.draw(in: bgPath, angle: -135)

        let cx = size / 2
        let cy = size / 2

        // Eye outline: almond/lens bezier path, white stroke, no fill
        let eyeHalfW = size * 0.30
        let eyeHalfH = size * 0.155
        let eyePath = NSBezierPath()
        eyePath.move(to: NSPoint(x: cx - eyeHalfW, y: cy))
        eyePath.curve(to: NSPoint(x: cx, y: cy + eyeHalfH),
                      controlPoint1: NSPoint(x: cx - eyeHalfW * 0.38, y: cy + eyeHalfH * 1.05),
                      controlPoint2: NSPoint(x: cx - eyeHalfW * 0.06, y: cy + eyeHalfH))
        eyePath.curve(to: NSPoint(x: cx + eyeHalfW, y: cy),
                      controlPoint1: NSPoint(x: cx + eyeHalfW * 0.06, y: cy + eyeHalfH),
                      controlPoint2: NSPoint(x: cx + eyeHalfW * 0.38, y: cy + eyeHalfH * 1.05))
        eyePath.curve(to: NSPoint(x: cx, y: cy - eyeHalfH),
                      controlPoint1: NSPoint(x: cx + eyeHalfW * 0.38, y: cy - eyeHalfH * 1.05),
                      controlPoint2: NSPoint(x: cx + eyeHalfW * 0.06, y: cy - eyeHalfH))
        eyePath.curve(to: NSPoint(x: cx - eyeHalfW, y: cy),
                      controlPoint1: NSPoint(x: cx - eyeHalfW * 0.06, y: cy - eyeHalfH),
                      controlPoint2: NSPoint(x: cx - eyeHalfW * 0.38, y: cy - eyeHalfH * 1.05))
        eyePath.close()
        NSColor.white.withAlphaComponent(0.92).setStroke()
        eyePath.lineWidth = max(1.5, size * 0.038)
        eyePath.stroke()

        // 4-pointed star at pupil in Seriamo Terracotta (#c56549)
        let outerR = size * 0.115
        let innerR = size * 0.028
        let starPath = NSBezierPath()
        for i in 0..<8 {
            let angle = CGFloat(i) * .pi / 4.0 - .pi / 2.0
            let r: CGFloat = i % 2 == 0 ? outerR : innerR
            let px = cx + r * cos(angle)
            let py = cy + r * sin(angle)
            if i == 0 { starPath.move(to: NSPoint(x: px, y: py)) }
            else { starPath.line(to: NSPoint(x: px, y: py)) }
        }
        starPath.close()
        // Terracotta accent (#c56549)
        NSColor(red: 0.773, green: 0.396, blue: 0.286, alpha: 1.0).setFill()
        starPath.fill()

        return true
    }

    guard let tiff = image.tiffRepresentation,
          let bitmap = NSBitmapImageRep(data: tiff),
          let png = bitmap.representation(using: .png, properties: [:]) else {
        print("Failed to render \(name)")
        continue
    }
    let filePath = "\(iconsetPath)/\(name).png"
    try png.write(to: URL(fileURLWithPath: filePath))
}

print("Iconset created at \(iconsetPath)")
print("Run: iconutil -c icns \(iconsetPath) -o Scripts/AppIcon.icns")
