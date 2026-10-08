"""Build gentler horse hooves and a fuller sacred/ascension score from project WAVs.

Requires NumPy and SciPy. Run from the repository root. Source WAVs are kept intact.
"""

from pathlib import Path
import wave

import numpy as np
from scipy.signal import butter, sosfilt


ROOT = Path(__file__).resolve().parents[2]
AUDIO = ROOT / "Assets/Resources/ThanhGiongAudio"
RATE = 22050


def read(name):
    with wave.open(str(AUDIO / name), "rb") as source:
        assert source.getframerate() == RATE and source.getsampwidth() == 2
        channels = source.getnchannels()
        samples = np.frombuffer(source.readframes(source.getnframes()), dtype="<i2")
    return samples.astype(np.float64).reshape(-1, channels) / 32768.0


def write(name, signal):
    signal = np.asarray(signal)
    if signal.ndim == 1:
        signal = signal[:, None]
    assert np.max(np.abs(signal)) < 0.98, name
    pcm = np.round(np.clip(signal, -1, 1) * 32767).astype("<i2")
    with wave.open(str(AUDIO / name), "wb") as target:
        target.setnchannels(signal.shape[1])
        target.setsampwidth(2)
        target.setframerate(RATE)
        target.writeframes(pcm.tobytes())
    print(name, "seconds", round(len(signal) / RATE, 2), "peak", round(np.max(np.abs(signal)), 3))


def lowpass(signal, cutoff):
    return sosfilt(butter(3, cutoff, fs=RATE, output="sos"), signal)


def softened_hoof(surface, cutoff):
    original = read(f"hoof_{surface}.wav")[:, 0]
    duration = 0.25
    n = int(duration * RATE)
    t = np.arange(n) / RATE
    # The originals contain a second hard impact at 110 ms. Keep one rounded contact.
    attack = np.zeros(n)
    take = min(int(0.105 * RATE), len(original))
    attack[:take] = original[:take]
    attack = lowpass(attack, cutoff)
    attack *= np.minimum(1.0, t / 0.006) * np.exp(-t * 9.0)
    body = np.sin(2 * np.pi * (125 if surface == "stone" else 94) * t)
    body *= (1 - np.exp(-t * 700)) * np.exp(-t * 29) * 0.085
    result = attack * 0.95 + body
    result *= np.minimum(1.0, (duration - t) / 0.045)
    result *= 0.43 / max(0.43, np.max(np.abs(result)))
    write(f"hoof_{surface}_soft.wav", result)


def pad_note(frequency, t, phase, warmth=1.0):
    # Three slightly detuned partials make a soft bowed texture instead of a bare sine.
    vibrato = 0.006 * np.sin(2 * np.pi * 0.31 * t + phase)
    fundamental = np.sin(2 * np.pi * frequency * t + vibrato)
    second = np.sin(2 * np.pi * 2.002 * frequency * t + phase) * 0.28
    third = np.sin(2 * np.pi * 2.997 * frequency * t - phase) * 0.10
    return warmth * (fundamental + second + third) / 1.38


def sacred_music():
    source = read("music_sacred.wav")[:, 0]
    n = len(source)
    t = np.arange(n) / RATE
    envelope = np.minimum(1.0, t / 0.28) * np.minimum(1.0, (n / RATE - t) / 0.28)
    left = np.zeros(n)
    right = np.zeros(n)
    for frequency, gain in [(110.0, 0.014), (146.83, 0.013), (185.00, 0.010), (220.0, 0.009)]:
        left += gain * pad_note(frequency, t, 0.2)
        right += gain * pad_note(frequency * 1.001, t, 1.1)
    pulse = 0.82 + 0.18 * np.cos(2 * np.pi * t / (n / RATE))
    music = np.column_stack((source * 0.88 + left * envelope * pulse,
                             source * 0.88 + right * envelope * pulse))
    # A very short seam fade protects the existing pluck loop from clicks.
    edge = min(220, n // 8)
    music[:edge] *= np.linspace(0, 1, edge)[:, None]
    music[-edge:] *= np.linspace(1, 0, edge)[:, None]
    write("music_sacred_full.wav", music)


def ascension_music():
    seconds = 9.5
    n = int(RATE * seconds)
    t = np.arange(n) / RATE
    left = np.zeros(n)
    right = np.zeros(n)
    # Four overlapping harmonic swells follow the flight and resolve at the summit.
    chords = [
        (0.0, 2.8, [146.83, 185.00, 220.00]),
        (2.1, 5.1, [146.83, 196.00, 246.94]),
        (4.5, 7.4, [123.47, 185.00, 246.94]),
        (6.8, 9.5, [146.83, 220.00, 293.66]),
    ]
    for index, (start, end, notes) in enumerate(chords):
        rise = np.clip((t - start) / 0.75, 0, 1)
        fall = np.clip((end - t) / 1.0, 0, 1)
        swell = np.sin(np.minimum(rise, fall) * np.pi / 2) ** 2
        for frequency in notes:
            left += pad_note(frequency, t, index * 0.4) * swell * 0.023
            right += pad_note(frequency * 1.0015, t, 1 + index * 0.4) * swell * 0.023
    # Slow air lift, filtered to avoid the gritty white-noise edge.
    rng = np.random.default_rng(1979)
    air = lowpass(rng.standard_normal(n), 1600)
    air *= (np.sin(np.pi * np.clip(t / seconds, 0, 1)) ** 1.3) * 0.006
    left += air
    right += np.roll(air, 131)
    # Sparse bronze-like chimes mark takeoff, skyward lift, and completion.
    for onset, frequency, strength in [(0.2, 587.33, 0.055), (2.6, 739.99, 0.045),
                                       (5.3, 880.0, 0.055), (7.3, 1174.66, 0.075)]:
        relative = t - onset
        active = relative >= 0
        bell = np.zeros(n)
        x = relative[active]
        bell[active] = strength * np.exp(-x * 1.8) * (
            np.sin(2 * np.pi * frequency * x) +
            0.31 * np.sin(2 * np.pi * frequency * 2.01 * x) +
            0.13 * np.sin(2 * np.pi * frequency * 3.93 * x))
        left += bell
        right += np.roll(bell, 71)
    exit_fade = np.clip((seconds - t) / 0.8, 0, 1)
    entrance = np.clip(t / 0.12, 0, 1)
    write("music_ascension.wav", np.column_stack((left, right)) * (exit_fade * entrance * 2.3)[:, None])


if __name__ == "__main__":
    for surface, cutoff in [("dirt", 1900), ("stone", 2800), ("wet", 2100), ("wood", 2300)]:
        softened_hoof(surface, cutoff)
    sacred_music()
    ascension_music()
