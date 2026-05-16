using System.IO;
using AeroBloom;
using UnityEditor;
using UnityEngine;

namespace AeroBloom.EditorTools
{
    public static class AeroOpenWorldPrefabFactory
    {
        private const string PackBasic = "Assets/Addons/FrutigerAeroPack/Props/Buildings/Basic Building.prefab";
        private const string PackTower = "Assets/Addons/FrutigerAeroPack/Props/Buildings/Tower Building.prefab";

        [MenuItem("AeroBloom/8. Create Open World Prefab Assets", priority = 8)]
        public static void CreatePrefabAssets()
        {
            if (!Directory.Exists(AeroOpenWorldPrefabs.Folder))
                Directory.CreateDirectory(AeroOpenWorldPrefabs.Folder);

            CopyPrefab(PackBasic, AeroOpenWorldPrefabs.BasicBuilding);
            CopyPrefab(PackTower, AeroOpenWorldPrefabs.TowerBuilding);
            CreateGlassPavilionPrefab();
            CreateGlassTowerPrefab();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            EditorUtility.DisplayDialog("AeroBloom",
                "Open-world prefabs saved to:\n" + AeroOpenWorldPrefabs.Folder,
                "OK");
        }

        private static void CopyPrefab(string sourcePath, string destPath)
        {
            GameObject src = AssetDatabase.LoadAssetAtPath<GameObject>(sourcePath);
            if (src == null)
            {
                Debug.LogWarning("[AeroBloom] Missing source prefab: " + sourcePath);
                return;
            }

            GameObject temp = (GameObject)PrefabUtility.InstantiatePrefab(src);
            temp.name = Path.GetFileNameWithoutExtension(destPath);
            PrefabUtility.SaveAsPrefabAsset(temp, destPath);
            Object.DestroyImmediate(temp);
        }

        private static void CreateGlassPavilionPrefab()
        {
            GameObject root = AeroOpenWorldBuilder.CreateGlassPavilionPrefabRoot();
            PrefabUtility.SaveAsPrefabAsset(root, AeroOpenWorldPrefabs.GlassPavilion);
            Object.DestroyImmediate(root);
        }

        private static void CreateGlassTowerPrefab()
        {
            GameObject root = AeroOpenWorldBuilder.CreateGlassTowerPrefabRoot();
            PrefabUtility.SaveAsPrefabAsset(root, AeroOpenWorldPrefabs.GlassTower);
            Object.DestroyImmediate(root);
        }
    }
}
