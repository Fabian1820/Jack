import AppKit
import JackCore
import SwiftUI

/// One dictation at a time: a tap on the microphone starts recording and the next one stops it and transcribes.
@MainActor
final class VoiceDictation: ObservableObject {
    enum Phase: Equatable { case idle, recording(UUID), transcribing(UUID) }

    @Published private(set) var phase = Phase.idle
    @Published var errorMessage: String?
    private let recorder = VoiceRecorder()
    private var autoStop: Task<Void, Never>?
    /// Not published: the live waveform reads these every frame without redrawing the window.
    private(set) var startedAt: Date?
    func level() -> Float { recorder.level() }

    func phase(for id: UUID) -> Phase {
        switch phase {
        case .recording(id): .recording(id)
        case .transcribing(id): .transcribing(id)
        default: .idle
        }
    }

    /// Starts when idle; stops and hands the text to `insert` when it was recording this conversation.
    func toggle(for id: UUID, insert: @escaping (String) -> Void) {
        switch phase {
        case .idle: Task { await start(for: id, insert: insert) }
        case .recording(id): finish(id, insert: insert)
        default: break
        }
    }

    func cancel() {
        autoStop?.cancel()
        recorder.cancel()
        phase = .idle
    }

    private func start(for id: UUID, insert: @escaping (String) -> Void) async {
        errorMessage = nil
        guard await VoiceRecorder.requestAccess() else { errorMessage = VoiceError.microphoneDenied.errorDescription; return }
        do { try recorder.start() } catch {
            errorMessage = (error as? VoiceError ?? .recordingFailed).errorDescription
            return
        }
        startedAt = Date()
        phase = .recording(id)
        autoStop = Task { [weak self] in
            try? await Task.sleep(for: .seconds(VoiceConfig.maxSeconds))
            guard !Task.isCancelled else { return }
            self?.finish(id, insert: insert)
        }
    }

    private func finish(_ id: UUID, insert: @escaping (String) -> Void) {
        autoStop?.cancel()
        guard let file = recorder.stop() else { phase = .idle; return }
        phase = .transcribing(id)
        Task {
            defer { phase = .idle }
            do { insert(try await VoiceTranscriber.transcribe(file)) }
            catch {
                JackLog.write("voice: \(error.localizedDescription)")
                errorMessage = (error as? LocalizedError)?.errorDescription ?? error.localizedDescription
            }
        }
    }
}

/// The composer's microphone: tap to dictate, tap again to stop and drop the text into the message.
struct VoiceDictationButton: View {
    @ObservedObject var dictation: VoiceDictation
    let conversationID: UUID
    let insert: (String) -> Void
    let report: (String) -> Void

    var body: some View {
        let phase = dictation.phase(for: conversationID)
        Button { dictation.toggle(for: conversationID, insert: insert) } label: {
            Group {
                switch phase {
                case .idle: Image(systemName: "mic").foregroundStyle(JackPalette.muted)
                case .recording: Image(systemName: "stop.circle.fill").foregroundStyle(.red)
                case .transcribing: ProgressView().controlSize(.small).scaleEffect(0.6)
                }
            }
            .font(.system(size: 12.5, weight: .medium))
            .frame(width: 24, height: 24).contentShape(Rectangle())
        }
        .buttonStyle(.plain)
        // Only the chat that is recording can stop it; while one dictates or transcribes, the others wait.
        .disabled(!isRecording(phase) && dictation.phase != .idle)
        .help(isRecording(phase) ? "Detener y transcribir" : "Dictar con la voz (un toque para empezar, otro para parar)")
        .accessibilityLabel(isRecording(phase) ? "Detener dictado" : "Dictar")
        .onChange(of: dictation.errorMessage) { _, message in
            if let message, dictation.phase(for: conversationID) == .idle { report(message) }
        }
    }

    private func isRecording(_ phase: VoiceDictation.Phase) -> Bool {
        if case .recording = phase { return true }
        return false
    }
}

/// While dictating, the message box gives way to a live view of the voice, in the spirit of Gemini Live:
/// bars that follow the microphone, a timer, and buttons to discard or to stop and transcribe.
struct DictationLiveView: View {
    let dictation: VoiceDictation
    let transcribing: Bool
    let onCancel: () -> Void
    let onStop: () -> Void
    @Environment(\.accessibilityReduceMotion) private var reduceMotion
    @State private var meter = LevelMeter()

    /// Smooths the input so the bars swell fast and fall slowly.
    final class LevelMeter {
        private var value = 0.0
        func update(_ raw: Double) -> Double {
            value = raw > value ? value + (raw - value) * 0.5 : value * 0.92
            return value
        }
    }

    var body: some View {
        HStack(spacing: 14) {
            roundButton("xmark", fill: JackPalette.panelStrong, tint: JackPalette.muted, help: "Descartar el dictado", action: onCancel)
                .keyboardShortcut(.cancelAction)
                .opacity(transcribing ? 0 : 1).disabled(transcribing)
            TimelineView(.animation(minimumInterval: reduceMotion ? 0.12 : 1.0 / 30)) { timeline in
                VStack(spacing: 6) {
                    Canvas { context, size in drawBars(context, size, time: timeline.date.timeIntervalSinceReferenceDate) }
                        .frame(height: 38)
                    Text(status(at: timeline.date))
                        .font(.system(size: 11, weight: .medium).monospacedDigit()).foregroundStyle(.secondary)
                }
            }
            if transcribing {
                ProgressView().controlSize(.small).frame(width: 32, height: 32)
            } else {
                roundButton("checkmark", fill: JackPalette.accent, tint: .white, help: "Parar y transcribir", action: onStop)
            }
        }
        .padding(.vertical, 4)
        .accessibilityElement(children: .ignore)
        .accessibilityLabel(transcribing ? "Transcribiendo" : "Escuchando")
        .onAppear { NSHapticFeedbackManager.defaultPerformer.perform(.levelChange, performanceTime: .now) }
    }

    private func roundButton(_ symbol: String, fill: Color, tint: Color, help: String, action: @escaping () -> Void) -> some View {
        Button {
            NSHapticFeedbackManager.defaultPerformer.perform(.alignment, performanceTime: .now)
            action()
        } label: {
            Image(systemName: symbol).font(.system(size: 12, weight: .bold)).foregroundStyle(tint)
                .frame(width: 32, height: 32).background(fill, in: Circle()).contentShape(Circle())
        }
        .buttonStyle(.plain).help(help).accessibilityLabel(help)
    }

    private func status(at date: Date) -> String {
        guard !transcribing else { return "Transcribiendo…" }
        let seconds = Int(date.timeIntervalSince(dictation.startedAt ?? date))
        return "Escuchando · \(seconds / 60):" + String(format: "%02d", seconds % 60)
    }

    private func drawBars(_ context: GraphicsContext, _ size: CGSize, time: Double) {
        let level = transcribing ? 0 : meter.update(Double(dictation.level()))
        let width: CGFloat = 3, gap: CGFloat = 4.5
        let count = max(2, Int((size.width + gap) / (width + gap)))
        let left = (size.width - (CGFloat(count) * (width + gap) - gap)) / 2
        var path = Path()
        for index in 0..<count {
            let place = Double(index) / Double(count - 1)
            // The middle moves most, so the shape reads as a voice and not as noise.
            let envelope = 0.3 + 0.7 * sin(place * .pi)
            let amplitude: Double
            if transcribing {
                amplitude = 0.12 + 0.4 * envelope * (0.5 + 0.5 * sin(time * 4.5 - place * 10))
            } else {
                let wobble = 0.4 + 0.6 * abs(sin(time * 3.1 + Double(index) * 0.7) * cos(time * 1.7 + Double(index) * 0.31))
                amplitude = 0.07 + 0.03 * sin(time * 1.4 + place * 6) + level * envelope * wobble * 0.93
            }
            let height = max(width, CGFloat(amplitude) * size.height)
            path.addRoundedRect(in: CGRect(x: left + CGFloat(index) * (width + gap), y: (size.height - height) / 2, width: width, height: height),
                                cornerSize: CGSize(width: width / 2, height: width / 2))
        }
        context.fill(path, with: .linearGradient(Gradient(colors: [JackPalette.blue, JackPalette.purple, Color(red: 0.95, green: 0.45, blue: 0.7)]),
                                                 startPoint: CGPoint(x: 0, y: 0), endPoint: CGPoint(x: size.width, y: 0)))
    }
}
