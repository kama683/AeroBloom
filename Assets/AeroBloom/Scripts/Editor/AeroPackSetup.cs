using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace AeroBloom.EditorTools
{
    public static class AeroPackSetup
    {
        private const string PackRoot = "Assets/Addons/FrutigerAeroPack";
        private const string ConfigPath = "Assets/Resources/AeroPackConfig.asset";

        [MenuItem("AeroBloom/Setup Pack Assets")]
        public static void SetupPackAssets()
        {
            if (!Directory.Exists("Assets/Resources"))
            {
                Directory.CreateDirectory("Assets/Resources");
                AssetDatabase.Refresh();
            }

            AeroPackConfig config = AssetDatabase.LoadAssetAtPath<AeroPackConfig>(ConfigPath);
            if (config == null)
            {
                config = ScriptableObject.CreateInstance<AeroPackConfig>();
                AssetDatabase.CreateAsset(config, ConfigPath);
            }

            config.skyboxMaterial          = Load<Material>     ("Sky/FrutigerSkybox.mat");
            config.globalVolumeProfile     = Load<VolumeProfile>("Example/GlobalVolumeProfile.asset");
            config.grassMaterial           = Load<Material>     ("Example/Materials/Grass/Grass.mat");
            config.musicMain               = Load<AudioClip>    ("Audio/Music/Aero (DnB) (by FlooferLand).flac");
            config.musicAlt                = Load<AudioClip>    ("Audio/Music/Aero (by FlooferLand).flac");
            config.sfxPop                  = Load<AudioClip>    ("Audio/Sounds/Pop.wav");
            config.sfxSwoosh               = Load<AudioClip>    ("Audio/Sounds/Swoosh.wav");
            config.basicBuildingPrefab     = Load<GameObject>   ("Props/Buildings/Basic Building.prefab");
            config.towerBuildingPrefab     = Load<GameObject>   ("Props/Buildings/Tower Building.prefab");
            config.earthPrefab             = Load<GameObject>   ("Props/Earth/Earth.prefab");
            config.footstepParticlesPrefab = Load<GameObject>            ("Props/FootstepParticles.prefab");
            config.msnBuddyPrefab          = Load<GameObject>            ("Characters/MsnBuddy/MsnBuddy.fbx");
            config.msnBuddyAnimator        = Load<RuntimeAnimatorController>("Characters/MsnBuddy/MsnBuddy Animations.controller");

            EditorUtility.SetDirty(config);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            EditorUtility.DisplayDialog(
                "AeroBloom",
                "Pack assets wired to AeroPackConfig!\n\nNext step: AeroBloom > Build Playable Prototype Scene",
                "OK");
        }

        private static T Load<T>(string relativePath) where T : Object
        {
            string fullPath = $"{PackRoot}/{relativePath}";
            T asset = AssetDatabase.LoadAssetAtPath<T>(fullPath);
            if (asset == null)
                Debug.LogWarning($"[AeroPackSetup] Not found: {fullPath}");
            return asset;
        }
    }
}
