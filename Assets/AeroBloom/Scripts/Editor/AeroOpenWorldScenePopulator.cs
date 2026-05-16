using System.IO;
using AeroBloom;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace AeroBloom.EditorTools
{
    public static class AeroOpenWorldScenePopulator
    {
        private const string TestSandboxScenePath = "Assets/Scenes/TestSandbox.unity";

        [MenuItem("AeroBloom/9. Populate TestSandbox Into Scene (Editable)", priority = 9)]
        public static void PopulateIntoSceneMenu() => PopulateIntoScene();

        public static void PopulateIntoScene()
        {
            if (!File.Exists(TestSandboxScenePath))
                AeroPrototypeBuilder.CreateTestSandboxScene();

            var scene = EditorSceneManager.OpenScene(TestSandboxScenePath, OpenSceneMode.Single);
            EnsureStandaloneMarker();
            EnsureBakedMarker();

            var ctx = new AeroOpenWorldBuildContext
            {
                editorScene     = true,
                usePrefabAssets = false,
            };

            AeroOpenWorldBuilder.Build(true, ctx);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();

            EditorUtility.DisplayDialog("AeroBloom",
                "Open meadow placed in TestSandbox (no buildings).\n\n" +
                "Hierarchy: OpenWorld → hills, lake, bubbles, decor.\n" +
                "Edit objects in Scene view, then save the scene.",
                "OK");
        }

        private static void EnsureStandaloneMarker()
        {
            if (Object.FindFirstObjectByType<AeroStandaloneScene>() != null)
                return;
            GameObject root = new GameObject("TestSceneRoot");
            root.AddComponent<AeroStandaloneScene>();
        }

        private static void EnsureBakedMarker()
        {
            AeroStandaloneScene standalone = Object.FindFirstObjectByType<AeroStandaloneScene>();
            if (standalone == null)
                return;
            if (standalone.GetComponent<AeroOpenWorldBaked>() == null)
                standalone.gameObject.AddComponent<AeroOpenWorldBaked>();
        }
    }
}
