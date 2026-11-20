import AppKit
import JackCore
import WebKit

/// Executes the agents' `browser_*` and `terminal_*` tools in the same tabs the user sees.
extension WorkspaceSessions {
    func agentCall(_ name: String, _ arguments: [String: Any], conversation: ChatConversation) async -> (String, Bool) {
        let id = conversation.id
        switch name {
        case "browser_open", "browser_read", "browser_eval", "browser_screenshot":
            let tab = tabs(for: id).last { $0.kind == .browser } ?? open(.browser, for: id)
            select(tab.id, for: id)
            let page = browser(tab.id)
            switch name {
            case "browser_open":
                guard let url = (arguments["url"] as? String)?.trimmingCharacters(in: .whitespacesAndNewlines), !url.isEmpty else { return ("url is required.", true) }
                page.open(url)
                for _ in 0..<150 { try? await Task.sleep(for: .milliseconds(100)); if !page.loading { break } }
                return (page.error.map { "Could not load \(url): \($0)" } ?? "Loaded \(page.address) (\(page.title))", page.error != nil)
            case "browser_read":
                let text = (try? await page.webView.evaluateJavaScript("document.body ? document.body.innerText : ''")) as? String ?? ""
                return (IntegratedTools.bounded("\(page.title)\n\(page.address)\n\n\(text)"), false)
            case "browser_eval":
                guard let script = arguments["script"] as? String, !script.isEmpty else { return ("script is required.", true) }
                do {
                    let value = try await page.webView.evaluateJavaScript(script)
                    guard let value, !(value is NSNull) else { return ("null", false) }
                    if JSONSerialization.isValidJSONObject(value), let data = try? JSONSerialization.data(withJSONObject: value) {
                        return (IntegratedTools.bounded(String(decoding: data, as: UTF8.self)), false)
                    }
                    return (IntegratedTools.bounded(String(describing: value)), false)
                } catch { return ("JavaScript error: \(error.localizedDescription)", true) }
            default:
                guard let image = try? await page.webView.takeSnapshot(configuration: nil),
                      let tiff = image.tiffRepresentation, let png = NSBitmapImageRep(data: tiff)?.representation(using: .png, properties: [:]) else {
                    return ("Could not capture the page.", true)
                }
                let folder = FileManager.default.temporaryDirectory.appendingPathComponent("jack-browser", isDirectory: true)
                try? FileManager.default.createDirectory(at: folder, withIntermediateDirectories: true)
                let file = folder.appendingPathComponent(UUID().uuidString + ".png")
                do { try png.write(to: file) } catch { return ("Could not save the screenshot: \(error.localizedDescription)", true) }
                return (file.path, false)
            }
        case "terminal_run":
            guard let command = (arguments["command"] as? String)?.trimmingCharacters(in: .whitespacesAndNewlines), !command.isEmpty else { return ("command is required.", true) }
            let tab = open(.terminal, for: id, title: "Agente")
            let session = terminal(tab.id, conversation: id, directory: conversation.projectPath, directLaunch: TerminalProcessLaunch(executable: "/bin/zsh", arguments: ["-lc", command]))
            let timeout = min(max(arguments["timeout_seconds"] as? Int ?? 60, 1), 600)
            for _ in 0..<(timeout * 10) { if session.hasExited { break }; try? await Task.sleep(for: .milliseconds(100)) }
            let output = IntegratedTools.bounded(session.output)
            return (session.hasExited ? output : "terminal_id=\(tab.id.uuidString) (still running)\n\(output)", false)
        case "terminal_read", "terminal_send":
            let tab = (arguments["terminal_id"] as? String).flatMap(UUID.init(uuidString:)).flatMap { wanted in tabs(for: id).first { $0.id == wanted && $0.kind == .terminal } }
                ?? tabs(for: id).last { $0.kind == .terminal }
            guard let tab, let session = existingTerminal(tab.id) else { return ("No terminal. Start one with terminal_run.", true) }
            if name == "terminal_send" {
                guard let text = arguments["text"] as? String else { return ("text is required.", true) }
                session.type(text)
                try? await Task.sleep(for: .milliseconds(500))
            }
            return (IntegratedTools.bounded(session.output), false)
        default:
            return ("Unknown tool \(name).", true)
        }
    }
}

extension TerminalSession {
    /// Everything on the terminal's screen and scrollback, as text.
    var output: String {
        String(decoding: view.getTerminal().getBufferAsData(), as: UTF8.self).trimmingCharacters(in: .whitespacesAndNewlines)
    }
}
