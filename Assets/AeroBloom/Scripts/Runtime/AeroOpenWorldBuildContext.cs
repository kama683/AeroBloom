using UnityEngine;

namespace AeroBloom
{
    public sealed class AeroOpenWorldBuildContext
    {
        public bool editorScene;
        public bool usePrefabAssets;

        public GameObject basicBuildingPrefab;
        public GameObject towerBuildingPrefab;
        public GameObject glassPavilionPrefab;
        public GameObject glassTowerPrefab;

        public static AeroOpenWorldBuildContext Runtime => new AeroOpenWorldBuildContext
        {
            editorScene     = false,
            usePrefabAssets = false
        };
    }
}
