using System;
using UnityEngine;

namespace SwingPop.AudioSystem
{
    public static class ProceduralAudioLibrary
    {
        private const int SampleRate = 22050;

        public static AudioClip Create(GameplayAudioCue cue)
        {
            float duration = cue switch
            {
                GameplayAudioCue.UiConfirm => 0.08f,
                GameplayAudioCue.Swing or GameplayAudioCue.PuttSwing => 0.22f,
                GameplayAudioCue.NormalImpact => 0.13f,
                GameplayAudioCue.PerfectImpact => 0.34f,
                GameplayAudioCue.WaterHazard => 0.46f,
                GameplayAudioCue.OutOfBounds => 0.24f,
                GameplayAudioCue.HoleIn => 0.42f,
                GameplayAudioCue.Result => 0.65f,
                _ => 0.18f
            };
            int sampleCount = Mathf.CeilToInt(duration * SampleRate);
            float[] samples = new float[sampleCount];
            uint noiseState = 0x9E3779B9u + (uint)cue * 747796405u;
            float filteredNoise = 0f;

            for (int index = 0; index < sampleCount; index++)
            {
                float time = index / (float)SampleRate;
                float normalized = index / (float)Mathf.Max(1, sampleCount - 1);
                noiseState = noiseState * 1664525u + 1013904223u;
                float noise = ((noiseState >> 8) / 8388607.5f) - 1f;
                filteredNoise = Mathf.Lerp(filteredNoise, noise, 0.18f);
                samples[index] = EvaluateCue(cue, time, normalized, noise, filteredNoise);
            }

            AudioClip clip = AudioClip.Create($"M9 Procedural {cue}", sampleCount, 1, SampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        private static float EvaluateCue(
            GameplayAudioCue cue,
            float time,
            float normalized,
            float noise,
            float filteredNoise)
        {
            float decay = Mathf.Pow(1f - normalized, 2f);
            return cue switch
            {
                GameplayAudioCue.UiConfirm => Sine(time, 720f + normalized * 180f) * decay * 0.32f,
                GameplayAudioCue.Swing => (filteredNoise * 0.42f + Sine(time, 115f) * 0.1f)
                                             * Mathf.Sin(normalized * Mathf.PI) * 0.62f,
                GameplayAudioCue.PuttSwing => (filteredNoise * 0.22f + Sine(time, 185f) * 0.08f)
                                                 * Mathf.Sin(normalized * Mathf.PI) * 0.42f,
                GameplayAudioCue.NormalImpact => (Sine(time, Mathf.Lerp(240f, 95f, normalized)) * 0.62f
                                                   + noise * 0.28f) * decay,
                GameplayAudioCue.PerfectImpact => (Sine(time, 880f) + Sine(time, 1320f) * 0.45f)
                                                  * decay * 0.32f,
                GameplayAudioCue.FairwayLanding => (filteredNoise * 0.55f + Sine(time, 90f) * 0.12f) * decay * 0.5f,
                GameplayAudioCue.RoughLanding => filteredNoise * decay * 0.62f,
                GameplayAudioCue.BunkerLanding => (filteredNoise * 0.75f + noise * 0.12f) * decay * 0.55f,
                GameplayAudioCue.GreenLanding => (filteredNoise * 0.34f + Sine(time, 120f) * 0.08f) * decay * 0.42f,
                GameplayAudioCue.WaterHazard => (filteredNoise * 0.64f + Sine(time, Mathf.Lerp(180f, 70f, normalized)) * 0.16f)
                                                * Mathf.Pow(1f - normalized, 1.35f) * 0.6f,
                GameplayAudioCue.OutOfBounds => Sine(time, Mathf.Lerp(260f, 120f, normalized)) * decay * 0.42f,
                GameplayAudioCue.HoleIn => (Sine(time, Mathf.Lerp(980f, 440f, normalized)) * 0.45f
                                            + Sine(time, 1320f) * 0.14f) * decay,
                GameplayAudioCue.Result => ResultChord(time, normalized),
                _ => 0f
            };
        }

        private static float ResultChord(float time, float normalized)
        {
            float attack = Mathf.Clamp01(normalized * 12f);
            float release = Mathf.Pow(1f - normalized, 1.3f);
            float chord = Sine(time, 523.25f) + Sine(time, 659.25f) + Sine(time, 783.99f);
            return chord / 3f * attack * release * 0.36f;
        }

        private static float Sine(float time, float frequency)
        {
            return Mathf.Sin(2f * Mathf.PI * frequency * time);
        }
    }
}
