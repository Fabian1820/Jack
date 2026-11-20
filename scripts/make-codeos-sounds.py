#!/usr/bin/env python3
"""Generates the CodeOS interface sounds: original 8-bit console blips (square/triangle waves), no third-party audio.

Usage: Scripts/make-codeos-sounds.py   (needs ffmpeg; rewrites Resources/CodeOS/Sounds/*.flac)
"""
import math, pathlib, struct, subprocess, tempfile, wave

RATE = 48000
OUT = pathlib.Path(__file__).resolve().parent.parent / "Resources/CodeOS/Sounds"
C5, D5, E5, G5, B5, C6, E6, G6, A6 = 523.25, 587.33, 659.25, 783.99, 987.77, 1046.5, 1318.5, 1568.0, 1760.0


def square(phase):
    return 1.0 if (phase % 1.0) < 0.5 else -1.0


def triangle(phase):
    return 4 * abs((phase % 1.0) - 0.5) - 1


def note(freq, dur, wave_fn=square, vol=0.3, end_freq=None, decay=5.0):
    """One blip; end_freq makes it a slide. Short attack and release avoid clicks."""
    out, phase = [], 0.0
    for i in range(int(dur * RATE)):
        t = i / RATE
        phase += (freq if end_freq is None else freq + (end_freq - freq) * t / dur) / RATE
        env = min(1, t / 0.002) * math.exp(-decay * t / dur) * min(1, (dur - t) / 0.008)
        out.append(wave_fn(phase) * env * vol)
    return out


def seq(*parts):
    """Notes one after another; a part is (freq, dur[, kwargs])."""
    out = []
    for freq, dur, *kw in parts:
        out += note(freq, dur, **(kw[0] if kw else {}))
    return out


TRI = {"wave_fn": triangle, "vol": 0.4}
SOUNDS = {
    "move": seq((E6, 0.03, {"vol": 0.22}), (G6, 0.03, {"vol": 0.22})),
    "tab": seq((880, 0.05, TRI), (1174.7, 0.09, TRI)),
    "select": seq(*[(f, 0.06) for f in (C5, E5, G5)], (C6, 0.22, {"decay": 4})),
    "enter": seq((B5, 0.07), (E6, 0.35, {"decay": 4})),
    "back": seq((G5, 0.06), (D5, 0.06), (C5, 0.2, TRI)),
    "launch": seq((200, 0.35, {"end_freq": 1200, "decay": 0.5, "vol": 0.22}), *[(f, 0.07) for f in (C5, E5, G5)], (C6, 0.3, {"decay": 4})),
    "panel": seq((330, 0.16, {"end_freq": 990, "decay": 1, "vol": 0.25}), (G6, 0.08, {"vol": 0.2})),
    "panelClose": seq((990, 0.16, {"end_freq": 330, "decay": 1, "vol": 0.25})),
    "notify": seq((E6, 0.08), (A6, 0.08), (1, 0.08, {"vol": 0}), (E6, 0.08), (A6, 0.25, {"decay": 4})),
    "home": seq(*[(f, 0.09) for f in (C5, E5, G5, C6)], (E6, 0.12), (G6, 0.45, {"decay": 4})),
}

with tempfile.TemporaryDirectory() as tmp:
    for name, samples in SOUNDS.items():
        wav = pathlib.Path(tmp) / f"{name}.wav"
        with wave.open(str(wav), "wb") as w:
            w.setnchannels(1), w.setsampwidth(2), w.setframerate(RATE)
            w.writeframes(b"".join(struct.pack("<h", int(max(-1, min(1, s)) * 32767)) for s in samples + [0.0] * 2400))
        subprocess.run(["ffmpeg", "-y", "-loglevel", "error", "-i", str(wav), str(OUT / f"{name}.flac")], check=True)
        print(name, f"{len(samples) / RATE:.2f}s")
