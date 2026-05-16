using System.IO;
using AeroBloom;
using UnityEditor;
using UnityEngine;

namespace AeroBloom.EditorTools
{
    public static class AeroCutsceneSetup
    {
        private const string CutsceneFolder = "Assets/cutscenes";
        private const string ConfigPath = "Assets/AeroBloom/Resources/AeroCutsceneConfig.asset";

        [MenuItem("AeroBloom/7. Setup Slide Cutscene", priority = 7)]
        public static void SetupCutsceneConfig()
        {
            var config = AssetDatabase.LoadAssetAtPath<AeroCutsceneConfig>(ConfigPath);
            if (config == null)
            {
                if (!Directory.Exists("Assets/AeroBloom/Resources"))
                    AssetDatabase.CreateFolder("Assets/AeroBloom", "Resources");

                config = ScriptableObject.CreateInstance<AeroCutsceneConfig>();
                AssetDatabase.CreateAsset(config, ConfigPath);
            }

            var slides = new System.Collections.Generic.List<Texture2D>();
            for (int i = 1; i <= 7; i++)
            {
                string path = $"{CutsceneFolder}/{i}.png";
                var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                if (tex != null)
                    slides.Add(tex);
                else
                    Debug.LogWarning("[AeroBloom] Missing cutscene image: " + path);
            }

            config.slides = slides.ToArray();
            config.subtitles = DefaultSubtitles();
            config.fadeFromBlack = 1.35f;
            config.slideHold     = 5.5f;
            config.crossfade     = 1.15f;
            config.fadeToGame    = 0.85f;
            config.kenBurnsScale = 1.06f;

            EditorUtility.SetDirty(config);
            AssetDatabase.SaveAssets();
            EditorUtility.DisplayDialog("AeroBloom",
                "Cutscene config saved with " + config.slides.Length + " slides.\n" + ConfigPath,
                "OK");
        }

        private static string[] DefaultSubtitles()
        {
            return new[]
            {
                "A computer is a window into another world — games, messages, and dreams all live behind the screen.",
                "Every tap feels instant… but behind it, millions of data packets race through hidden pathways.",
                "Deep inside the machine lies a luminous realm — the world of AeroBloom.",
                "Tiny AeroBloom workers carry traffic: your chats, your videos, the sites you love. They never rest.",
                "While the routes stay clear, the digital sky glows in perfect harmony.",
                "Sometimes a dangerous file slips in — a virus that scatters traffic and breaks every road.",
                "You are an AeroBloom courier. Gather the lost packets, reach the core, and awaken the antivirus."
            };
        }
    }
}
