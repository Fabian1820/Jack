import AppKit
import SwiftUI

/// The composer's permission-mode control: a pill with the current mode, opening a list of modes
/// with an icon and a one-line explanation each, like a system settings popover.
struct ModePopupButton: View {
    struct Option: Identifiable, Equatable {
        let id: String?
        let title: String
        let subtitle: String?
        let symbol: String
        let color: Color
    }

    let options: [Option]
    let selected: String?
    let current: Option
    let disabled: Bool
    let footer: String?
    let onSelect: (Option) -> Void
    @State private var shown = false
    @State private var hovering = false
    @State private var hoveredID: String??

    var body: some View {
        Button { shown.toggle() } label: {
            HStack(spacing: 5) {
                Label(current.title, systemImage: current.symbol)
                Image(systemName: "chevron.up.chevron.down").font(.system(size: 8, weight: .semibold)).opacity(0.7)
            }
            .foregroundStyle(current.color)
            .padding(.horizontal, 8).padding(.vertical, 3)
            .background(current.color.opacity(hovering || shown ? 0.2 : 0.12), in: Capsule())
        }
        .buttonStyle(.plain)
        .disabled(disabled)
        .onHover { hovering = $0 }
        .popover(isPresented: $shown, arrowEdge: .top) {
            VStack(alignment: .leading, spacing: 2) {
                Text("Modo de trabajo").font(.headline).padding(.horizontal, 8).padding(.bottom, 6)
                ForEach(options) { option in row(option) }
                if let footer {
                    Text(footer).font(.caption2).foregroundStyle(.tertiary).padding(.horizontal, 8).padding(.top, 8)
                }
            }
            .padding(10).frame(width: 300)
        }
    }

    private func row(_ option: Option) -> some View {
        let isSelected = option.id == selected
        return Button {
            NSHapticFeedbackManager.defaultPerformer.perform(.alignment, performanceTime: .now)
            shown = false
            if !isSelected { onSelect(option) }
        } label: {
            HStack(spacing: 10) {
                Image(systemName: option.symbol)
                    .font(.system(size: 13, weight: .semibold)).foregroundStyle(option.color)
                    .frame(width: 30, height: 30)
                    .background(option.color.opacity(0.15), in: RoundedRectangle(cornerRadius: 8, style: .continuous))
                VStack(alignment: .leading, spacing: 1) {
                    Text(option.title).font(.system(size: 13, weight: .medium))
                    if let subtitle = option.subtitle {
                        Text(subtitle).font(.caption).foregroundStyle(.secondary).lineLimit(2).fixedSize(horizontal: false, vertical: true)
                    }
                }
                Spacer(minLength: 6)
                if isSelected { Image(systemName: "checkmark").font(.system(size: 12, weight: .semibold)).foregroundStyle(JackPalette.accent) }
            }
            .padding(.horizontal, 8).padding(.vertical, 6)
            .background(hoveredID == .some(option.id) ? Color.primary.opacity(0.07) : isSelected ? Color.primary.opacity(0.04) : .clear,
                        in: RoundedRectangle(cornerRadius: 10, style: .continuous))
            .contentShape(RoundedRectangle(cornerRadius: 10, style: .continuous))
        }
        .buttonStyle(.plain)
        .onHover { inside in hoveredID = inside ? .some(option.id) : nil }
        .accessibilityAddTraits(isSelected ? .isSelected : [])
    }
}
