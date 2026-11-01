import Foundation

/// Seriamo's shared endpoints. Every Seriamo app talks to the same three hosts, so the
/// privacy policy has one place to describe what is sent where.
enum SeriamoEndpoints {
    static let telemetry = URL(string: "https://telemetry.seriamo.com/v1/events")!

    static func latestRelease(current: String) -> URL {
        var c = URLComponents(string: "https://updates.seriamo.com/v1/products/pausa/releases/latest")!
        c.queryItems = [URLQueryItem(name: "current", value: current), URLQueryItem(name: "platform", value: "macos")]
        return c.url!
    }

    static func feedbackForm(version: String) -> URL {
        var c = URLComponents(string: "https://feedback.seriamo.com/")!
        c.queryItems = [
            URLQueryItem(name: "product", value: "pausa"),
            URLQueryItem(name: "version", value: version),
            URLQueryItem(name: "platform", value: "macos"),
        ]
        return c.url!
    }

    static let releasesPage = URL(string: "https://github.com/seriamo/pausa/releases/latest")!
}

/// Anonymous usage telemetry: one `app_launch` event per launch, carrying the app version and
/// a random install ID. Nothing else is collected. Turning "Share Anonymous Usage" off stops
/// sending and deletes the install ID.
@MainActor
enum Telemetry {
    // Same key the pre-platform tracker used, so existing installs keep their ID.
    private static let installIdKey = "com.seriamo.pausa.deviceId"

    static func recordLaunch(version: String) {
        guard Preferences.shared.shareUsage else { return }
        let event: [String: String] = [
            "event_id": UUID().uuidString,
            "product": "pausa",
            "event": "app_launch",
            "app_version": version,
            "platform": "macos",
            "occurred_at": ISO8601DateFormatter().string(from: Date()),
            "install_id": installId(),
        ]
        let envelope: [String: Any] = ["schema_version": 1, "events": [event]]
        guard let body = try? JSONSerialization.data(withJSONObject: envelope) else { return }
        send(body, retriesLeft: 1)
    }

    static func resetInstallId() {
        UserDefaults.standard.removeObject(forKey: installIdKey)
    }

    private static func installId() -> String {
        if let existing = UserDefaults.standard.string(forKey: installIdKey) { return existing }
        let id = UUID().uuidString
        UserDefaults.standard.set(id, forKey: installIdKey)
        return id
    }

    /// Fire-and-forget. Retries once after 30 s on network errors, 429 and 5xx; the event ID
    /// stays the same, so the server drops a duplicate if the first attempt did land.
    private nonisolated static func send(_ body: Data, retriesLeft: Int) {
        var request = URLRequest(url: SeriamoEndpoints.telemetry)
        request.httpMethod = "POST"
        request.setValue("application/json", forHTTPHeaderField: "Content-Type")
        request.timeoutInterval = 10
        request.httpBody = body

        URLSession.shared.dataTask(with: request) { _, response, error in
            let status = (response as? HTTPURLResponse)?.statusCode ?? 0
            let retryable = error != nil || status == 429 || status >= 500
            guard retryable, retriesLeft > 0 else { return }
            DispatchQueue.global(qos: .utility).asyncAfter(deadline: .now() + 30) {
                send(body, retriesLeft: retriesLeft - 1)
            }
        }.resume()
    }
}

struct AvailableUpdate: Sendable {
    let version: String
    let page: URL
}

/// Asks updates.seriamo.com for the latest release. Downloads stay on GitHub Releases.
enum UpdateChecker {
    static func check(currentVersion: String, completion: @escaping @Sendable (AvailableUpdate) -> Void) {
        var request = URLRequest(url: SeriamoEndpoints.latestRelease(current: currentVersion))
        request.setValue("application/json", forHTTPHeaderField: "Accept")
        request.timeoutInterval = 10

        URLSession.shared.dataTask(with: request) { data, response, error in
            guard error == nil, (response as? HTTPURLResponse)?.statusCode == 200, let data,
                  let json = try? JSONSerialization.jsonObject(with: data) as? [String: Any],
                  let version = json["version"] as? String else { return }

            let newer = (json["update_available"] as? Bool)
                ?? (version.compare(currentVersion, options: .numeric) == .orderedDescending)
            guard newer, supportsThisMac(minimum: json["min_os_version"] as? String) else { return }

            let page = (json["notes_url"] as? String).flatMap(URL.init(string:)) ?? SeriamoEndpoints.releasesPage
            completion(AvailableUpdate(version: version, page: page))
        }.resume()
    }

    private static func supportsThisMac(minimum: String?) -> Bool {
        guard let minimum else { return true }
        let parts = minimum.split(separator: ".").compactMap { Int($0) }
        guard let major = parts.first else { return true }
        let required = OperatingSystemVersion(majorVersion: major,
                                              minorVersion: parts.count > 1 ? parts[1] : 0,
                                              patchVersion: parts.count > 2 ? parts[2] : 0)
        return ProcessInfo.processInfo.isOperatingSystemAtLeast(required)
    }
}
