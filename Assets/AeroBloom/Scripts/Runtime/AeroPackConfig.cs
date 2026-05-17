using UnityEngine;
using UnityEngine.Rendering;

namespace AeroBloom
{
    [CreateAssetMenu(fileName = "AeroPackConfig", menuName = "AeroBloom/Pack Config")]
    public sealed class AeroPackConfig : ScriptableObject
    {
        [Header("Sky")]
        public Material skyboxMaterial;

        [Header("Post Processing")]
        public VolumeProfile globalVolumeProfile;

        [Header("Materials")]
        public Material grassMaterial;

        [Header("Music")]
        public AudioClip musicMain;
        public AudioClip musicAlt;

        [Header("Sound Effects")]
        public AudioClip sfxPop;
        public AudioClip sfxSwoosh;

        [Header("Props")]
        public GameObject basicBuildingPrefab;
        public GameObject towerBuildingPrefab;
        public GameObject earthPrefab;
        public GameObject footstepParticlesPrefab;

        [Header("Character")]
        public GameObject msnBuddyPrefab;
        public RuntimeAnimatorController msnBuddyAnimator;

        [Header("Main Menu Textures")]
        public Texture2D mainMenuBackgroundTexture;
        public Texture2D mainMenuForegroundTexture;
    }
}
