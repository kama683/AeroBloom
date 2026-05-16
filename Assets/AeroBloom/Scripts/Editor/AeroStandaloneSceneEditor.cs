using AeroBloom;
using UnityEditor;
using UnityEngine;

namespace AeroBloom.EditorTools
{
    [CustomEditor(typeof(AeroStandaloneScene))]
    public sealed class AeroStandaloneSceneEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            EditorGUILayout.Space(8f);
            if (GUILayout.Button("Build Open World In Scene Now", GUILayout.Height(32f)))
            {
                if (!EditorApplication.isPlaying)
                    AeroOpenWorldScenePopulator.PopulateIntoScene();
                else
                    AeroStandaloneScene.TryBuildOpenWorld();
            }

            EditorGUILayout.HelpBox(
                "If Play shows an empty map: fix red Console errors first, then click the button above " +
                "or use AeroBloom → 9. Delete old OpenWorld first if buildings remain.",
                MessageType.Info);
        }
    }
}
