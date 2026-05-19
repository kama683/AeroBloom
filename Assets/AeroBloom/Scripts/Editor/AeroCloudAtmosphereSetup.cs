using UnityEditor;
using UnityEngine;

namespace AeroBloom.EditorTools
{
    public static class AeroCloudAtmosphereSetup
    {
        [MenuItem("AeroBloom/10. Cloud & Fog URP Guide", priority = 10)]
        public static void ShowGuide()
        {
            EditorUtility.DisplayDialog("AeroBloom — Dreamy Clouds (URP)",
                "REBUILD SCENE FIRST:\nAeroBloom → 3. Build Playable Prototype Scene\n\n" +
                "── SHADER ──\n" +
                "Assets/AeroBloom/Shaders/AeroSoftCloud.shader\n" +
                "Material: white-blue, transparent, no ZWrite, Cull Off\n\n" +
                "── URP ASSET (PC_RPAsset) ──\n" +
                "Depth Texture: ON (for soft edges)\n" +
                "Shadow Distance: 40–60\n\n" +
                "── VOLUME (Aero Post FX) ──\n" +
                "Bloom: Threshold 0.55, Intensity 1.3, Tint cool white\n" +
                "Fog: Lighting → Environment (RenderSettings)\n" +
                "DOF Gaussian: Start 55, End 140\n\n" +
                "── RENDER SETTINGS ──\n" +
                "Fog: Exponential², same color, Density ~0.0018\n\n" +
                "── SECTION 4 ──\n" +
                "MistPlat = CloudVolume only (no green cube)\n" +
                "Collider = invisible CloudCollider child\n\n" +
                "Tweak AeroMistAtmosphere on MistAtmosphere object.",
                "OK");
        }
    }
}
