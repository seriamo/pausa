// swift-tools-version: 6.0

import PackageDescription

let package = Package(
    name: "Pausa",
    platforms: [.macOS(.v14)],
    targets: [
        .executableTarget(
            name: "Pausa",
            path: "Sources/Pausa"
        )
    ]
)
