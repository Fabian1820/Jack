import JackCore
import SwiftUI

/// Sub-agents of every agent, as a card at the top right (Normal mode). Active ones stay; finished ones linger for two minutes.
struct SubagentPopup: View {
    let children: [ChatConversation]
    let statuses: [UUID: ChatStatus]
    let onOpen: (UUID) -> Void

    @State private var collapsed = false
    @State private var hovered: UUID?

    private var shown: [ChatConversation] {
        children.filter { child in
            let status = statuses[child.id] ?? .idle
            return status != .idle || Date().timeIntervalSince(child.updatedAt) < 120
        }
        .sorted { $0.updatedAt > $1.updatedAt }
        .prefix(6).map { $0 }
    }

    private func isActive(_ status: ChatStatus) -> Bool { status == .running || status == .queued }

    var body: some View {
        let rows = shown
        if !rows.isEmpty {
            let active = rows.filter { isActive(statuses[$0.id] ?? .idle) }.count
            VStack(alignment: .leading, spacing: 0) {
                header(total: rows.count, active: active)
                if !collapsed {
                    Divider().opacity(0.5).padding(.horizontal, 10)
                    VStack(spacing: 1) { ForEach(rows) { row($0) } }.padding(4)
                }
            }
            .frame(width: 280)
            .background(Color(nsColor: .windowBackgroundColor), in: shape) // opaque: the transcript scrolls underneath
            .jackGlass(in: shape, basic: JackPalette.panelStrong.opacity(0.6))
            .overlay(shape.stroke(JackPalette.hairline, lineWidth: JackPalette.borderWidth()))
            .shadow(color: .black.opacity(0.14), radius: 14, y: 5)
            .animation(.snappy(duration: 0.2), value: collapsed)
        }
    }

    private var shape: RoundedRectangle { RoundedRectangle(cornerRadius: JackPalette.radius(12), style: .continuous) }

    private func header(total: Int, active: Int) -> some View {
        Button { collapsed.toggle() } label: {
            HStack(spacing: 6) {
                Image(systemName: "point.3.connected.trianglepath.dotted").font(.system(size: 11, weight: .medium)).foregroundStyle(JackPalette.muted)
                Text("Subagentes").font(.system(size: 12, weight: .semibold)).foregroundStyle(JackPalette.primaryText)
                Text(active > 0 ? "\(active) activo\(active == 1 ? "" : "s")" : "\(total)")
                    .font(.system(size: 11)).foregroundStyle(JackPalette.faint)
                Spacer(minLength: 0)
                Image(systemName: "chevron.down").font(.system(size: 9, weight: .bold)).foregroundStyle(JackPalette.faint)
                    .rotationEffect(.degrees(collapsed ? -90 : 0))
            }
            .padding(.horizontal, 12).padding(.vertical, 8).contentShape(Rectangle())
        }
        .buttonStyle(.plain)
    }

    private func row(_ child: ChatConversation) -> some View {
        let status = statuses[child.id] ?? .idle
        return Button { onOpen(child.id) } label: {
            HStack(spacing: 8) {
                statusIcon(status).frame(width: 14, height: 14)
                VStack(alignment: .leading, spacing: 1) {
                    Text(child.title).lineLimit(1).truncationMode(.tail).font(.system(size: 12, weight: .medium)).foregroundStyle(JackPalette.primaryText)
                    Text("\(child.provider.title) · \(label(status))").lineLimit(1).font(.system(size: 10.5)).foregroundStyle(JackPalette.faint)
                }
                Spacer(minLength: 0)
            }
            .padding(.horizontal, 8).padding(.vertical, 5).contentShape(Rectangle())
            .background(hovered == child.id ? JackPalette.hairline.opacity(0.6) : .clear, in: RoundedRectangle(cornerRadius: JackPalette.radius(8), style: .continuous))
        }
        .buttonStyle(.plain)
        .onHover { hovered = $0 ? child.id : (hovered == child.id ? nil : hovered) }
    }

    @ViewBuilder private func statusIcon(_ status: ChatStatus) -> some View {
        switch status {
        case .running, .queued: ProgressView().controlSize(.mini)
        case .failed: Image(systemName: "exclamationmark.circle.fill").foregroundStyle(JackPalette.red)
        case .waiting: Image(systemName: "hourglass.circle.fill").foregroundStyle(JackPalette.amber)
        case .idle: Image(systemName: "checkmark.circle.fill").foregroundStyle(JackPalette.green)
        }
    }

    private func label(_ status: ChatStatus) -> String {
        switch status {
        case .running: return "Trabajando"
        case .queued: return "En cola"
        case .waiting: return "Esperando"
        case .failed: return "Falló"
        case .idle: return "Terminó"
        }
    }
}
