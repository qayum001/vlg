"""Placeholder phonk drum/bass one-shots for the beat clock (design/heroes/lowkey.md, Open: music).

Pure synthesis, no inputs, no third-party code: python tools/audio/make_beat_samples.py
Writes 44.1 kHz 16-bit mono WAVs to game/assets/audio/beat/. Riser length = 1 bar at BPM.
"""
import math
import os
import random
import struct
import wave

RATE = 44100
BPM = 130
OUT = os.path.join(os.path.dirname(__file__), "..", "..", "game", "assets", "audio", "beat")


def write(name, samples):
    peak = max(1e-9, max(abs(s) for s in samples))
    gain = 0.9 / peak
    os.makedirs(OUT, exist_ok=True)
    with wave.open(os.path.join(OUT, name + ".wav"), "wb") as w:
        w.setnchannels(1)
        w.setsampwidth(2)
        w.setframerate(RATE)
        w.writeframes(b"".join(struct.pack("<h", int(max(-1.0, min(1.0, s * gain)) * 32767)) for s in samples))


def n(sec):
    return int(sec * RATE)


def noise(rng):
    return rng.uniform(-1.0, 1.0)


def kick():
    out, phase = [], 0.0
    for i in range(n(0.45)):
        t = i / RATE
        f = 45 + 110 * math.exp(-t * 30)
        phase += 2 * math.pi * f / RATE
        out.append(math.tanh(1.6 * math.sin(phase) * math.exp(-t * 7)))
    for i in range(n(0.002)):
        out[i] += 0.3 * random.Random(i).uniform(-1, 1)
    return out


def clap():
    rng, out, prev, hp = random.Random(2), [], 0.0, 0.0
    for i in range(n(0.25)):
        t = i / RATE
        env = sum(math.exp(-(t - o) * 180) for o in (0.0, 0.011, 0.022) if t >= o) + 0.6 * math.exp(-t * 14)
        x = noise(rng)
        hp = 0.85 * (hp + x - prev)
        prev = x
        out.append(hp * env + 0.25 * math.sin(2 * math.pi * 185 * t) * math.exp(-t * 30))
    return out


def hat():
    rng, out, prev = random.Random(3), [], 0.0
    for i in range(n(0.06)):
        x = noise(rng)
        out.append((x - prev) * math.exp(-i / RATE * 70))
        prev = x
    return out


def cowbell():
    out, lp = [], 0.0
    for i in range(n(0.35)):
        t = i / RATE
        sq = (1 if math.sin(2 * math.pi * 540 * t) > 0 else -1) + (1 if math.sin(2 * math.pi * 800 * t) > 0 else -1)
        lp += 0.35 * (sq - lp)
        env = math.exp(-t * 14) * 0.8 + 0.2 * math.exp(-t * 4)
        out.append(lp * env)
    return out


def sub808(base=49.0, sec=0.9, drive=2.5):
    out, phase = [], 0.0
    for i in range(n(sec)):
        t = i / RATE
        f = base + 16 * math.exp(-t * 40)
        phase += 2 * math.pi * f / RATE
        out.append(math.tanh(drive * math.sin(phase)) * math.exp(-t * 3) * min(1.0, t * 400))
    return out


def slap():
    rng = random.Random(4)
    period = int(RATE / 98.0)
    buf = [noise(rng) for _ in range(period)]
    out = []
    for i in range(n(0.5)):
        t = i / RATE
        k = i % period
        v = buf[k]
        buf[k] = 0.5 * (v + buf[(k + 1) % period]) * 0.996
        click = 0.6 * noise(rng) * math.exp(-t * 400)
        out.append(math.tanh(2.2 * v + click))
    return out


def thump():
    return [math.sin(2 * math.pi * 80 * i / RATE) * math.exp(-i / RATE * 20) for i in range(n(0.2))]


def riser():
    rng, out, lp, phase = random.Random(5), [], 0.0, 0.0
    sec = 4 * 60.0 / BPM
    for i in range(n(sec)):
        t = i / RATE
        p = t / sec
        lp += (0.02 + 0.5 * p * p) * (noise(rng) - lp)
        phase += 2 * math.pi * (200 + 1000 * p * p) / RATE
        out.append((lp + 0.15 * math.sin(phase)) * (0.1 + 0.9 * p * p))
    return out


def boom():
    rng, out, prev = random.Random(6), [], 0.0
    sub = sub808(base=38.0, sec=1.6, drive=3.0)
    for i in range(len(sub)):
        t = i / RATE
        x = noise(rng)
        crash = (x - prev) * 0.5 * math.exp(-t * 3)
        prev = x
        out.append(sub[i] + crash)
    return out


if __name__ == "__main__":
    for name, fn in [("kick", kick), ("clap", clap), ("hat", hat), ("cowbell", cowbell), ("sub", sub808),
                     ("slap", slap), ("thump", thump), ("riser", riser), ("boom", boom)]:
        write(name, fn())
    print("wrote", os.path.abspath(OUT))
