using System;
using UnityEngine;

// Original deterministic synthesis. No third-party recordings, network, or per-frame generation.
public static class LegendProceduralAudio
{
    const int Rate = 22050;
    static readonly double[] Frequencies = { 440, 196, 330, 130, 520 };
    public static bool CanCreate(string cue) => cue == "ambient_water_lap" || cue == "ambient_reeds" || cue == "ambient_bridge_creak" ||
        cue == "seed_0" || cue == "seed_1" || cue == "seed_2" || cue == "seed_3" || cue == "seed_4" || cue == "dodge" || cue == "ambient_frogs";

    public static AudioClip Create(string cue)
    {
        if (!CanCreate(cue)) return null;
        bool ambient = cue.StartsWith("ambient_");
        float duration = ambient ? 4 : cue == "dodge" ? .34f : .72f;
        float[] samples = new float[(int)(Rate * duration)];
        int seedIndex = (ambient || cue == "dodge") ? -1 : cue[cue.Length - 1] - '0';
        var random = new System.Random(1979 + Math.Max(0, seedIndex));
        double lowNoise = 0, highNoise = 0;
        for (int i = 0; i < samples.Length; i++) {
            double t = i / (double)Rate;
            double noise = random.NextDouble() * 2 - 1;
            lowNoise += (noise - lowNoise) * .025;
            highNoise += (noise - highNoise) * .18;
            double signal;
            if (cue == "ambient_water_lap") {
                double lap = .30 + .70 * Math.Pow(.5 + .5 * Math.Sin(t * Math.PI), 3);
                signal = (lowNoise * 1.8 + Math.Sin(t * 2 * Math.PI * 63) * .015) * lap;
            }
            else if (cue == "ambient_reeds") signal = (noise - highNoise) * (.03 + .045 * Math.Pow(.5 + .5 * Math.Sin(t * Math.PI / 2), 2));
            else if (cue == "ambient_bridge_creak") {
                double envelope = Math.Pow(.5 + .5 * Math.Cos(t * Math.PI), 10);
                signal = Math.Sin(2 * Math.PI * (110 * t + 4 * Math.Sin(t * Math.PI))) * envelope * .12 + lowNoise * envelope * .2;
            }
            else if (cue == "ambient_frogs") {
                double cycle = (t * 1.6) % 1.0;
                double pulse = cycle < 0.22 ? Math.Sin(cycle / 0.22 * Math.PI) : 0.0;
                double frogHz = 175.0 + 35.0 * Math.Sin(t * 12.0);
                signal = (Math.Sin(2 * Math.PI * frogHz * t) * 0.22 + lowNoise * 0.35) * pulse * 0.16;
            }
            else if (cue == "dodge") {
                double envelope = Math.Sin(t / duration * Math.PI);
                double pitch = 260.0 - (t / duration) * 130.0;
                signal = (highNoise * 0.42 + lowNoise * 0.30 + Math.Sin(2 * Math.PI * pitch * t) * 0.22) * envelope;
            }
            else {
                double envelope = Math.Min(1, t / .035) * Math.Exp(-t * (seedIndex == 3 ? 6 : 4));
                double baseHz = Frequencies[seedIndex];
                double tone = Math.Sin(2 * Math.PI * baseHz * t) * .35 + Math.Sin(2 * Math.PI * baseHz * 1.5 * t) * .15;
                if (seedIndex == 1) tone += lowNoise * 1.2;
                if (seedIndex == 3) tone = highNoise * .8 + Math.Sin(2 * Math.PI * (130 * t - 36 * t * t)) * .2;
                if (seedIndex == 4) tone = (noise - highNoise) * .2 + Math.Sin(2 * Math.PI * (520 * t + 230 * t * t)) * .15;
                signal = tone * envelope;
            }
            // Loop edges and one-shot tails go to zero to avoid abrupt discontinuities.
            double edge = Math.Min(1, Math.Min(t, duration - t) / .08);
            samples[i] = (float)Math.Max(-.65, Math.Min(.65, signal * Math.Max(0, edge)));
        }
        AudioClip clip = AudioClip.Create(cue, samples.Length, 1, Rate, false);
        clip.SetData(samples, 0); return clip;
    }
}