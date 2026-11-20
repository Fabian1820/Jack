import AppKit
import SwiftUI

/// Reasoning effort as a Control Center style slider: a pill that fills up to the chosen level.
/// The pointer drags it freely and it settles on the nearest level, with a haptic tick on each one it crosses.
struct EffortSlider: View {
    struct Stop: Equatable {
        let id: String?
        let title: String
    }

    let stops: [Stop]
    /// Index of the saved level.
    let selected: Int
    let disabled: Bool
    let onCommit: (Stop) -> Void

    private let width: CGFloat = 264
    private let height: CGFloat = 40
    /// Where the lowest level's fill ends: just past the brain, so even that level reads as filled.
    private let floor: CGFloat = 40
    @Environment(\.accessibilityReduceMotion) private var reduceMotion
    /// Level shown; follows the saved one, and moves first on release so the fill never snaps back.
    @State private var level: Int
    /// Fill edge while dragging, in points.
    @State private var dragX: CGFloat?
    @State private var pressed = false
    @FocusState private var focused: Bool

    init(stops: [Stop], selected: Int, disabled: Bool, onCommit: @escaping (Stop) -> Void) {
        self.stops = stops; self.selected = selected; self.disabled = disabled; self.onCommit = onCommit
        _level = State(initialValue: min(max(selected, 0), max(stops.count - 1, 0)))
    }

    private var last: Int { max(stops.count - 1, 0) }
    private func position(_ index: Int) -> CGFloat { last == 0 ? width : floor + (width - floor) * CGFloat(index) / CGFloat(last) }
    private func nearest(_ x: CGFloat) -> Int { last == 0 ? 0 : min(max(Int(((x - floor) / (width - floor) * CGFloat(last)).rounded()), 0), last) }
    private var fillX: CGFloat { dragX ?? position(level) }
    /// Level nearest the fill edge, so the name and description follow it live while dragging.
    private var current: Int { nearest(fillX) }
    private var currentStop: Stop? { stops.indices.contains(current) ? stops[current] : nil }

    var body: some View {
        VStack(alignment: .leading, spacing: 12) {
            VStack(alignment: .leading, spacing: 3) {
                HStack(alignment: .firstTextBaseline) {
                    Text("Razonamiento").font(.headline)
                    Spacer()
                    Text(currentStop?.title ?? "").font(.headline).foregroundStyle(JackPalette.accent)
                        .contentTransition(.interpolate)
                }
                Text(Self.detail(for: currentStop?.id))
                    .font(.footnote).foregroundStyle(.secondary)
                    .lineLimit(2).fixedSize(horizontal: false, vertical: true)
                    .id(current).transition(.opacity)
            }
            .animation(reduceMotion ? nil : .snappy(duration: 0.18), value: current)
            slider
            HStack {
                Text("Rápido")
                Spacer()
                Text("Profundo")
            }
            .font(.caption2).foregroundStyle(.tertiary)
        }
        .frame(width: width)
        .opacity(disabled ? 0.5 : 1)
        .onChange(of: selected) { _, new in if dragX == nil { level = min(max(new, 0), last) } }
        .accessibilityElement(children: .ignore)
        .accessibilityLabel("Esfuerzo de razonamiento")
        .accessibilityValue(currentStop?.title ?? "")
        .accessibilityAdjustableAction { direction in
            guard !disabled else { return }
            let next = current + (direction == .increment ? 1 : -1)
            if stops.indices.contains(next) { choose(next) }
        }
    }

    private var slider: some View {
        ZStack(alignment: .leading) {
            Rectangle().fill(Color.primary.opacity(0.09))
            Rectangle().fill(JackPalette.accent).frame(width: fillX)
                // A soft light from above gives the fill depth without a second colour.
                .overlay(alignment: .leading) {
                    Rectangle().fill(LinearGradient(colors: [.white.opacity(0.2), .clear], startPoint: .top, endPoint: .center)).frame(width: fillX)
                }
            ForEach(stops.indices.dropFirst().dropLast(), id: \.self) { index in
                let covered = position(index) <= fillX
                // Dots near the handle step aside instead of sitting under it.
                Circle().fill(covered ? Color.white.opacity(0.6) : Color.primary.opacity(0.28))
                    .frame(width: 3, height: 3)
                    .opacity(abs(position(index) - fillX) < 9 ? 0 : 1)
                    .offset(x: position(index) - 1.5)
            }
            Image(systemName: "brain")
                .font(.system(size: 15, weight: .medium)).foregroundStyle(.white)
                .symbolEffect(.bounce, value: reduceMotion ? 0 : current)
                .frame(width: 24, height: height)
                .padding(.leading, 8)
            Capsule().fill(Color.white).frame(width: 4, height: pressed ? 24 : 18)
                .shadow(color: .black.opacity(0.2), radius: 1.5, y: 0.5)
                .offset(x: fillX - 10)
        }
        .frame(width: width, height: height)
        .clipShape(Capsule())
        .overlay(Capsule().strokeBorder(focused ? JackPalette.accent.opacity(0.6) : Color.primary.opacity(0.08), lineWidth: focused ? 2 : 0.5))
        .scaleEffect(pressed ? 1.02 : 1)
        .contentShape(Capsule())
        .pointerStyle(disabled ? .default : pressed ? .grabActive : .grabIdle)
        .focusable(!disabled)
        .focused($focused)
        .focusEffectDisabled()
        .onKeyPress(.leftArrow) { step(-1) }
        .onKeyPress(.rightArrow) { step(1) }
        .gesture(DragGesture(minimumDistance: 0).onChanged { value in
            guard !disabled else { return }
            if !pressed {
                tick(.generic)
                withAnimation(reduceMotion ? nil : .spring(response: 0.25, dampingFraction: 0.7)) { pressed = true }
            }
            let before = current
            dragX = min(max(value.location.x, floor), width)
            if current != before { tick(.levelChange) }
        }.onEnded { _ in
            guard let x = dragX else { return }
            let index = nearest(x)
            withAnimation(reduceMotion ? nil : .spring(response: 0.3, dampingFraction: 0.75)) {
                level = index; dragX = nil; pressed = false
            }
            tick(.alignment)
            if index != selected, stops.indices.contains(index) { onCommit(stops[index]) }
        })
    }

    /// Arrow keys move one level.
    private func step(_ delta: Int) -> KeyPress.Result {
        let next = current + delta
        guard !disabled, stops.indices.contains(next) else { return .ignored }
        choose(next)
        return .handled
    }

    /// Jumps to a level, for VoiceOver and the keyboard.
    private func choose(_ index: Int) {
        guard index != level || dragX != nil else { return }
        tick(.levelChange)
        withAnimation(reduceMotion ? nil : .spring(response: 0.32, dampingFraction: 0.75)) { level = index; dragX = nil }
        if index != selected { onCommit(stops[index]) }
    }

    private func tick(_ pattern: NSHapticFeedbackManager.FeedbackPattern) {
        NSHapticFeedbackManager.defaultPerformer.perform(pattern, performanceTime: .now)
    }

    static func detail(for id: String?) -> String {
        switch id {
        case nil: "El modelo decide cuánto razonar."
        case "minimal": "Casi sin razonar: respuestas inmediatas."
        case "low": "Respuestas rápidas con poco razonamiento."
        case "medium": "Equilibrio entre velocidad y calidad."
        case "high": "Razona a fondo; lo habitual para código."
        case "xhigh": "Muy profundo: tarda más y gasta más cuota."
        case "max": "Máximo razonamiento, lo más lento y costoso."
        default: "Nivel propio del modelo."
        }
    }
}

/// The composer's reasoning control: a brain with the level, opening a popover with the slider.
struct EffortPopupButton: View {
    let stops: [EffortSlider.Stop]
    let selected: Int
    let disabled: Bool
    var footer = "Se aplica al próximo mensaje"
    let onCommit: (EffortSlider.Stop) -> Void
    @State private var shown = false
    @State private var hovering = false

    var body: some View {
        Button { shown.toggle() } label: {
            Label(stops.indices.contains(selected) ? stops[selected].title : "Razonamiento", systemImage: "brain")
                .foregroundStyle(shown ? JackPalette.accent : JackPalette.muted)
                .padding(.horizontal, 8).padding(.vertical, 3)
                .background(Color.primary.opacity(hovering || shown ? 0.09 : 0.05), in: Capsule())
        }
        .buttonStyle(.plain)
        .onHover { hovering = $0 }
        .help("Esfuerzo de razonamiento")
        .popover(isPresented: $shown, arrowEdge: .top) {
            VStack(alignment: .leading, spacing: 12) {
                EffortSlider(stops: stops, selected: selected, disabled: disabled, onCommit: onCommit)
                if disabled {
                    Label("Detén el agente para cambiarlo", systemImage: "pause.circle")
                        .font(.caption).foregroundStyle(JackPalette.amber)
                } else {
                    Text(footer).font(.caption2).foregroundStyle(.tertiary)
                }
            }
            .padding(16)
        }
    }
}
