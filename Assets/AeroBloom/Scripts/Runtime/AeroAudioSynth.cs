using System.Collections.Generic;
using UnityEngine;

namespace AeroBloom
{
    public static class AeroAudioSynth
    {
        private const int SampleRate = 44100;

        private static readonly Dictionary<string, AudioClip> CachedClips = new Dictionary<string, AudioClip>();

        public static AudioClip GetTone(string key, float frequency, float duration, float volume)
        {
            string cacheKey = key + "_" + frequency + "_" + duration + "_" + volume;
            AudioClip clip;
            if (CachedClips.TryGetValue(cacheKey, out clip))
            {
                return clip;
            }

            int sampleCount = Mathf.CeilToInt(SampleRate * duration);
            float[] data = new float[sampleCount];

            for (int i = 0; i < sampleCount; i++)
            {
                float t = i / (float)SampleRate;
                float fadeIn = Mathf.Clamp01(t / 0.015f);
                float fadeOut = Mathf.Clamp01((duration - t) / 0.08f);
                float envelope = fadeIn * fadeOut;
                float wave = Mathf.Sin(2f * Mathf.PI * frequency * t);
                wave += 0.35f * Mathf.Sin(2f * Mathf.PI * frequency * 2f * t);
                data[i] = wave * envelope * volume;
            }

            clip = AudioClip.Create(key, sampleCount, 1, SampleRate, false);
            clip.SetData(data, 0);
            CachedClips.Add(cacheKey, clip);
            return clip;
        }

        public static AudioClip GetAmbientPad()
        {
            const string key = "aerobloom_ambient_pad";
            AudioClip clip;
            if (CachedClips.TryGetValue(key, out clip))
            {
                return clip;
            }

            float duration = 18f;
            int sampleCount = Mathf.CeilToInt(SampleRate * duration);
            float[] data = new float[sampleCount];
            float[] notes = { 196f, 246.94f, 329.63f, 392f, 493.88f };

            for (int i = 0; i < sampleCount; i++)
            {
                float t = i / (float)SampleRate;
                float wave = 0f;

                for (int n = 0; n < notes.Length; n++)
                {
                    float drift = 1f + Mathf.Sin((t * 0.07f) + n) * 0.006f;
                    wave += Mathf.Sin(2f * Mathf.PI * notes[n] * drift * t) * (0.11f / notes.Length);
                }

                float shimmer = Mathf.Sin(2f * Mathf.PI * 880f * t) * 0.015f * (0.5f + 0.5f * Mathf.Sin(t * 0.6f));
                float loopFade = Mathf.Min(Mathf.Clamp01(t / 2f), Mathf.Clamp01((duration - t) / 2f));
                data[i] = (wave + shimmer) * loopFade;
            }

            clip = AudioClip.Create(key, sampleCount, 1, SampleRate, false);
            clip.SetData(data, 0);
            CachedClips.Add(key, clip);
            return clip;
        }
    }
}
