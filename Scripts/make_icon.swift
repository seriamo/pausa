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

let iconsetPath = "/tmp/Glint.iconset"
let fm = FileManager.default
try? fm.removeItem(atPath: iconsetPath)
try fm.createDirectory(atPath: iconsetPath, withIntermediateDirectories: true)

for (size, name) in sizes {
    let image = NSImage(size: NSSize(width: size, height: size), flipped: false) { rect in
        // Background: rounded rect with gradient
        let bgPath = NSBezierPath(roundedRect: rect.insetBy(dx: size * 0.05, dy: size * 0.05),
                                   xRadius: size * 0.2, yRadius: size * 0.2)

        // Gradient: Azure (#3B82F6) → Azure Deep (#2563EB) — Seriamo B2 brand colors
        let gradient = NSGradient(starting: NSColor(red: 0.231, green: 0.510, blue: 0.965, alpha: 1.0),
                                  ending: NSColor(red: 0.145, green: 0.388, blue: 0.922, alpha: 1.0))!
        gradient.draw(in: bgPath, angle: -135)

        let cx = size / 2
        let cy = size / 2

        // Eye outline — almond/lens bezier path, white stroke, no fill
        let eyeHalfW = size * 0.30
        let eyeHalfH = size * 0.155
        let eyePath = NSBezierPath()
        eyePath.move(to: NSPoint(x: cx - eyeHalfW, y: cy))
        // upper arc: left corner → top → right corner
        eyePath.curve(to: NSPoint(x: cx, y: cy + eyeHalfH),
                      controlPoint1: NSPoint(x: cx - eyeHalfW * 0.38, y: cy + eyeHalfH * 1.05),
                      controlPoint2: NSPoint(x: cx - eyeHalfW * 0.06, y: cy + eyeHalfH))
        eyePath.curve(to: NSPoint(x: cx + eyeHalfW, y: cy),
                      controlPoint1: NSPoint(x: cx + eyeHalfW * 0.06, y: cy + eyeHalfH),
                      controlPoint2: NSPoint(x: cx + eyeHalfW * 0.38, y: cy + eyeHalfH * 1.05))
        // lower arc: right corner → bottom → left corner
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

        // 4-pointed glint star at pupil — classic lens-flare cross shape
        // Long points at N/S/E/W, tight indents at diagonals
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
        NSColor.white.setFill()
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
