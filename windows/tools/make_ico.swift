#!/usr/bin/env swift
// Renders the Pausa app icon (same artwork as Scripts/make_icon.swift) into a Windows .ico.
// Usage: swift windows/tools/make_ico.swift windows/src/Pausa.Windows/Assets/Pausa.ico
import AppKit

let output = CommandLine.arguments.count > 1 ? CommandLine.arguments[1] : "Pausa.ico"
let sizes = [16, 20, 24, 32, 40, 48, 64, 256]

func drawIcon(size: CGFloat) {
    let rect = NSRect(x: 0, y: 0, width: size, height: size)
    let bgPath = NSBezierPath(roundedRect: rect.insetBy(dx: size * 0.05, dy: size * 0.05),
                              xRadius: size * 0.2, yRadius: size * 0.2)
    let gradient = NSGradient(starting: NSColor(red: 0.180, green: 0.361, blue: 0.486, alpha: 1.0),
                              ending: NSColor(red: 0.118, green: 0.247, blue: 0.333, alpha: 1.0))!
    gradient.draw(in: bgPath, angle: -135)

    let cx = size / 2
    let cy = size / 2
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

    let outerR = size * 0.115
    let innerR = size * 0.028
    let starPath = NSBezierPath()
    for i in 0..<8 {
        let angle = CGFloat(i) * .pi / 4.0 - .pi / 2.0
        let r: CGFloat = i % 2 == 0 ? outerR : innerR
        let point = NSPoint(x: cx + r * cos(angle), y: cy + r * sin(angle))
        if i == 0 { starPath.move(to: point) } else { starPath.line(to: point) }
    }
    starPath.close()
    NSColor(red: 0.773, green: 0.396, blue: 0.286, alpha: 1.0).setFill()
    starPath.fill()
}

/// Renders at exact pixel size (no Retina doubling) and returns the bitmap.
func render(_ size: Int) -> NSBitmapImageRep {
    let rep = NSBitmapImageRep(bitmapDataPlanes: nil, pixelsWide: size, pixelsHigh: size,
                               bitsPerSample: 8, samplesPerPixel: 4, hasAlpha: true, isPlanar: false,
                               colorSpaceName: .deviceRGB, bytesPerRow: size * 4, bitsPerPixel: 32)!
    NSGraphicsContext.saveGraphicsState()
    let context = NSGraphicsContext(bitmapImageRep: rep)!
    context.imageInterpolation = .high
    NSGraphicsContext.current = context
    drawIcon(size: CGFloat(size))
    context.flushGraphics()
    NSGraphicsContext.restoreGraphicsState()
    return rep
}

extension Data {
    mutating func le16(_ v: Int) { var x = UInt16(v).littleEndian; append(Data(bytes: &x, count: 2)) }
    mutating func le32(_ v: Int) { var x = UInt32(v).littleEndian; append(Data(bytes: &x, count: 4)) }
}

/// Classic 32-bit DIB frame (straight alpha, bottom-up, empty AND mask): readable by every Windows tool.
func dibFrame(_ rep: NSBitmapImageRep) -> Data {
    let w = rep.pixelsWide, h = rep.pixelsHigh
    let maskStride = ((w + 31) / 32) * 4
    var data = Data()
    data.le32(40); data.le32(w); data.le32(h * 2); data.le16(1); data.le16(32)
    data.le32(0); data.le32(w * h * 4 + maskStride * h); data.le32(0); data.le32(0); data.le32(0); data.le32(0)
    let src = rep.bitmapData!
    for y in stride(from: h - 1, through: 0, by: -1) {
        for x in 0..<w {
            let p = src + y * rep.bytesPerRow + x * 4
            let a = Int(p[3])
            func straight(_ c: UInt8) -> UInt8 { a == 0 ? 0 : UInt8(min(255, (Int(c) * 255 + a / 2) / a)) }
            data.append(contentsOf: [straight(p[2]), straight(p[1]), straight(p[0]), UInt8(a)])
        }
    }
    data.append(Data(count: maskStride * h))
    return data
}

let frames: [(Int, Data)] = sizes.map { size in
    let rep = render(size)
    // 256px as PNG keeps the file small; smaller sizes as DIBs for maximum compatibility.
    return (size, size >= 256 ? rep.representation(using: .png, properties: [:])! : dibFrame(rep))
}

var ico = Data()
ico.le16(0); ico.le16(1); ico.le16(frames.count)
var offset = 6 + 16 * frames.count
for (size, frame) in frames {
    ico.append(UInt8(size >= 256 ? 0 : size)); ico.append(UInt8(size >= 256 ? 0 : size))
    ico.append(0); ico.append(0)
    ico.le16(1); ico.le16(32); ico.le32(frame.count); ico.le32(offset)
    offset += frame.count
}
for (_, frame) in frames { ico.append(frame) }

try ico.write(to: URL(fileURLWithPath: output))
print("Wrote \(output) (\(sizes.map(String.init).joined(separator: ", ")) px, \(ico.count) bytes)")
