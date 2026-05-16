using UnityEngine;
using UnityEngine.SceneManagement;

namespace AeroBloom
{
    /// <summary>
    /// TestSandbox: builds the open meadow on Play when OpenWorld is missing.
    /// Use menu AeroBloom → 9 to bake buildings into the scene for manual editing.
    /// </summary>
    [DefaultExecutionOrder(-200)]
    public sealed class AeroStandaloneScene : MonoBehaviour
    {
        public const string DefaultSceneName = "TestSandbox";

        [Tooltip("Generate meadow + buildings when OpenWorld is not in the scene.")]
        [SerializeField] private bool buildOpenWorldOnPlay = true;

        private void Awake()
        {
            TryBuildOpenWorld();
        }

        private void Start()
        {
            AeroPlayerController player = Object.FindFirstObjectByType<AeroPlayerController>();
            if (player != null)
                player.EnsureExploreCamera();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void EnsureOpenWorldOnPlay()
        {
            TryBuildOpenWorld();
        }

        /// <summary>Editor menu / inspector can call this too.</summary>
        public static void TryBuildOpenWorld()
        {
            Scene scene = SceneManager.GetActiveScene();
            if (scene.name != DefaultSceneName)
                return;

            if (GameObject.Find(AeroOpenWorldBuilder.RootName) != null)
                return;

            AeroStandaloneScene marker = Object.FindFirstObjectByType<AeroStandaloneScene>();
            if (marker != null && !marker.buildOpenWorldOnPlay)
                return;

            try
            {
                AeroOpenWorldBuilder.Build(true);
                Debug.Log("[AeroBloom] Open world built for TestSandbox.");
            }
            catch (System.Exception ex)
            {
                Debug.LogError("[AeroBloom] Open world build failed: " + ex);
            }
        }
    }
}
