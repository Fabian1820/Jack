import Foundation

/// Jack's own browser and terminal, offered to Normal-mode agents so they use them instead of opening their own.
public enum IntegratedTools {
    public static let instructions = """
    Jack has its own browser and terminal; the user sees them beside the chat. In Normal mode, use them through the jack MCP tools \
    instead of opening a separate browser or shell.
    - browser_open loads a URL in the browser tab; browser_read returns its title, address and visible text; browser_eval runs \
    JavaScript in the page and returns the result; browser_screenshot saves a PNG you can read as an image.
    - terminal_run runs a shell command in the integrated terminal in your folder and returns its output once it ends (or the \
    output so far after timeout_seconds, with a terminal_id). terminal_read shows the latest output of a terminal and terminal_send \
    types into one that is still running, for example to stop a dev server with Ctrl-C (\\u{3}).
    """

    static let limit = 20_000
    public static func bounded(_ text: String) -> String {
        text.count > limit ? "[…]\n" + String(text.suffix(limit)) : text
    }
}

extension AgentBridge {
    static let integratedTools: [[String: Any]] = [
        tool("browser_open", "Open a URL in Jack's integrated browser tab (the user sees it) and wait for it to load.", [
            "url": ["type": "string", "description": "Address or search text."]], required: ["url"]),
        tool("browser_read", "Title, address and visible text of the integrated browser's current page.", [:], required: []),
        tool("browser_eval", "Run JavaScript in the integrated browser's page and return its result as JSON (for example document.title, or a click: document.querySelector('button').click()).", [
            "script": ["type": "string"]], required: ["script"]),
        tool("browser_screenshot", "Save a PNG of the integrated browser's page and return its path.", [:], required: []),
        tool("terminal_run", "Run a shell command in Jack's integrated terminal, in your folder, and return its output when it ends. Long-running commands return the output so far after timeout_seconds with a terminal_id.", [
            "command": ["type": "string"], "timeout_seconds": ["type": "integer", "description": "Defaults to 60, at most 600."]], required: ["command"]),
        tool("terminal_read", "Latest output of a terminal started with terminal_run. Defaults to the most recent one.", [
            "terminal_id": ["type": "string"]], required: []),
        tool("terminal_send", "Type text into a terminal that is still running (no newline is added). Use \\u{3} for Ctrl-C.", [
            "terminal_id": ["type": "string"], "text": ["type": "string"]], required: ["text"]),
    ]
}
