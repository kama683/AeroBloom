using System.IO;
using AeroBloom;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace AeroBloom.EditorTools
{
    public static class AeroPrototypeBuilder
    {
        private const string MainMenuScenePath = "Assets/Scenes/MainMenu.unity";
        private const string PrototypeScenePath = "Assets/Scenes/AeroBloomPrototype.unity";
        private const string FieldScenePath = "Assets/Scenes/Field.unity";
        private const string TestSandboxScenePath = "Assets/Scenes/TestSandbox.unity";

        [MenuItem("AeroBloom/1. Setup Pack Assets", priority = 1)]
        public static void SetupPack() => AeroPackSetup.SetupPackAssets();

        [MenuItem("AeroBloom/2. Build Main Menu Scene", priority = 2)]
        public static void BuildMainMenuScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // One root GameObject that holds AeroMainMenu
            GameObject root = new GameObject("AeroMainMenu");
            root.AddComponent<AeroMainMenu>();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, MainMenuScenePath);
            AssetDatabase.SaveAssets();
            EditorUtility.DisplayDialog("AeroBloom", "Main Menu scene created at " + MainMenuScenePath, "OK");
        }

        [MenuItem("AeroBloom/3. Build Playable Prototype Scene", priority = 3)]
        public static void BuildPlayablePrototypeScene()
        {
            // Use Field.unity as the base so its terrain and sky carry over
            var scene = File.Exists(FieldScenePath)
                ? EditorSceneManager.OpenScene(FieldScenePath, OpenSceneMode.Single)
                : EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            AeroSceneFactory.BuildPrototypeScene(true);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, PrototypeScenePath);
            SetBuildScenes();
            AssetDatabase.SaveAssets();
            EditorUtility.DisplayDialog("AeroBloom",
                "Prototype scene created!\nBuild order:\n  0 — MainMenu\n  1 — AeroBloomPrototype",
                "OK");
        }

        [MenuItem("AeroBloom/4. Create Test Sandbox Scene", priority = 4)]
        public static void CreateTestSandboxScene()
        {
            var scene = File.Exists(FieldScenePath)
                ? EditorSceneManager.OpenScene(FieldScenePath, OpenSceneMode.Single)
                : EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

            GameObject runtime = GameObject.Find(AeroSceneFactory.RootName);
            if (runtime != null)
                Object.DestroyImmediate(runtime);

            AeroStandaloneScene marker = Object.FindFirstObjectByType<AeroStandaloneScene>();
            if (marker == null)
            {
                GameObject root = new GameObject("TestSceneRoot");
                root.AddComponent<AeroStandaloneScene>();
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, TestSandboxScenePath);
            AssetDatabase.SaveAssets();
            EditorUtility.DisplayDialog("AeroBloom",
                "Test sandbox saved to " + TestSandboxScenePath +
                "\n\nOpen it and press Play — only this scene runs (no auto level generation).",
                "OK");
        }

        [MenuItem("AeroBloom/5. Build Windows Build", priority = 5)]
        public static void BuildWindowsPrototype()
        {
            if (!File.Exists(MainMenuScenePath))
                BuildMainMenuScene();

            if (!File.Exists(PrototypeScenePath))
                BuildPlayablePrototypeScene();

            string outputDirectory = "Builds/Windows";
            Directory.CreateDirectory(outputDirectory);

            // Apply player settings
            PlayerSettings.companyName = "AeroBloom Team";
            PlayerSettings.productName = "AeroBloom";
            PlayerSettings.bundleVersion = "1.0";

            BuildPlayerOptions options = new BuildPlayerOptions
            {
                scenes = new[] { MainMenuScenePath, PrototypeScenePath },
                locationPathName = Path.Combine(outputDirectory, "AeroBloom.exe"),
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.None
            };

            BuildPipeline.BuildPlayer(options);
            EditorUtility.RevealInFinder(outputDirectory);
        }

        private static void SetBuildScenes()
        {
            var scenes = new System.Collections.Generic.List<EditorBuildSettingsScene>();

            if (File.Exists(MainMenuScenePath))
                scenes.Add(new EditorBuildSettingsScene(MainMenuScenePath, true));

            scenes.Add(new EditorBuildSettingsScene(PrototypeScenePath, true));

            EditorBuildSettings.scenes = scenes.ToArray();
        }
    }
}
