using System.IO;
using AeroBloom;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace AeroBloom.EditorTools
{
    public static class AeroTestSandboxBuilder
    {
        private const string TestSandboxScenePath = "Assets/Scenes/TestSandbox.unity";

        [MenuItem("AeroBloom/6. Build TestSandbox Open World (Runtime)", priority = 6)]
        public static void BuildOpenWorldMenu()
        {
            if (!File.Exists(TestSandboxScenePath))
                AeroPrototypeBuilder.CreateTestSandboxScene();

            var scene = EditorSceneManager.OpenScene(TestSandboxScenePath, OpenSceneMode.Single);
            BuildOpenWorldInOpenScene();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            EditorUtility.DisplayDialog("AeroBloom",
                "Runtime open world (rebuilt on Play).\n\nFor editable scene objects use:\nAeroBloom → 9. Populate TestSandbox Into Scene",
                "OK");
        }

        /// <summary>Called from Unity batch mode: -executeMethod AeroBloom.EditorTools.AeroTestSandboxBuilder.BuildOpenWorldBatch</summary>
        public static void BuildOpenWorldBatch()
        {
            BuildOpenWorldMenu();
            EditorApplication.Exit(0);
        }

        public static void BuildOpenWorldInOpenScene()
        {
            EnsureStandaloneMarker();
            AeroOpenWorldBuilder.Build(true);
        }

        private static void EnsureStandaloneMarker()
        {
            if (Object.FindFirstObjectByType<AeroStandaloneScene>() != null)
                return;
            GameObject root = new GameObject("TestSceneRoot");
            root.AddComponent<AeroStandaloneScene>();
        }
    }
}
