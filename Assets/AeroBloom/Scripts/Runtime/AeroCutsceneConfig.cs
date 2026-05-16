using UnityEngine;

namespace AeroBloom
{
    [CreateAssetMenu(fileName = "AeroCutsceneConfig", menuName = "AeroBloom/Cutscene Config")]
    public sealed class AeroCutsceneConfig : ScriptableObject
    {
        [Header("Slides (in order)")]
        public Texture2D[] slides;

        [Header("English subtitles — one line per slide")]
        [TextArea(2, 4)]
        public string[] subtitles;

        [Header("Timing (seconds)")]
        public float fadeFromBlack = 1.35f;
        public float slideHold = 5.5f;
        public float crossfade = 1.15f;
        public float fadeToGame = 0.85f;
        public float kenBurnsScale = 1.06f;
    }
}
