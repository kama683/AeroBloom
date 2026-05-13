using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace AeroBloom.EditorTools
{
    public static class AeroMaterialSetup
    {
        private const string MatDir = "Assets/AeroBloom/Resources/Materials";

        [MenuItem("AeroBloom/5. Create FA Materials", priority = 5)]
        public static void CreateFAMaterials()
        {
            if (!Directory.Exists(MatDir))
            {
                Directory.CreateDirectory(MatDir);
                AssetDatabase.Refresh();
            }

            // FA_GlassBlue — transparent aqua glass
            MakeMat("FA_GlassBlue", new Color(0f, 0.831f, 1f, 0.39f), 0.9f, 0.95f, true, Color.black);
            // FA_GlassGreen — transparent lime glass
            MakeMat("FA_GlassGreen", new Color(0.498f, 1f, 0f, 0.31f), 0.8f, 0.95f, true, Color.black);
            // FA_GlossyWhite — opaque white gloss body
            MakeMat("FA_GlossyWhite", new Color(1f, 1f, 1f, 1f), 0.2f, 0.92f, false, Color.black);
            // FA_GlossyCyan — opaque cyan
            MakeMat("FA_GlossyCyan", new Color(0f, 0.831f, 1f, 1f), 0.3f, 0.9f, false, Color.black);
            // FA_EmissiveCyan — glowing cyan
            MakeMat("FA_EmissiveCyan", new Color(0f, 1f, 1f, 1f), 0f, 0.8f, false, new Color(0f, 2f, 2f));
            // FA_EmissiveLime — glowing lime
            MakeMat("FA_EmissiveLime", new Color(0.67f, 1f, 0.67f, 1f), 0f, 0.8f, false, new Color(0.5f, 1.5f, 0.5f));
            // FA_Chrome — mirror chrome
            MakeMat("FA_Chrome", new Color(0.753f, 0.784f, 0.816f, 1f), 1.0f, 1.0f, false, Color.black);
            // FA_BuildingGlass — building facade glass
            MakeMat("FA_BuildingGlass", new Color(0.69f, 0.94f, 1f, 0.55f), 0.95f, 0.98f, true, Color.black);
            // FA_Platform — platform surface
            MakeMat("FA_Platform", new Color(0.91f, 0.98f, 1f, 1f), 0.4f, 0.88f, false, Color.black);
            // FA_Ground — ground sea
            MakeMat("FA_Ground", new Color(0.784f, 0.961f, 1f, 1f), 0.1f, 0.7f, false, Color.black);
            // FA_GoldTrim — gold emissive edge
            MakeMat("FA_GoldTrim", new Color(1f, 0.898f, 0.4f, 1f), 0.5f, 0.9f, false, new Color(1f, 0.7f, 0.1f));
            // FA_BuildingWall — white base with baked window-grid texture
            MakeBuildingWall();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            EditorUtility.DisplayDialog("AeroBloom", "All FA_ materials created in\n" + MatDir, "OK");
        }

        private static void MakeMat(string matName, Color color, float metallic, float smoothness, bool transparent, Color emission)
        {
            string path = MatDir + "/" + matName + ".mat";
            if (AssetDatabase.LoadAssetAtPath<Material>(path) != null)
                AssetDatabase.DeleteAsset(path);

            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");

            Material mat = new Material(shader) { name = matName };

            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
            else mat.color = color;
            if (mat.HasProperty("_Metallic"))   mat.SetFloat("_Metallic", metallic);
            if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", smoothness);

            if (emission.maxColorComponent > 0.01f && mat.HasProperty("_EmissionColor"))
            {
                mat.EnableKeyword("_EMISSION");
                mat.SetColor("_EmissionColor", emission);
            }

            if (transparent)
            {
                if (mat.HasProperty("_Surface")) mat.SetFloat("_Surface", 1f);
                if (mat.HasProperty("_Blend"))   mat.SetFloat("_Blend", 0f);
                mat.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
                mat.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
                mat.SetInt("_ZWrite", 0);
                mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                mat.DisableKeyword("_ALPHATEST_ON");
                mat.renderQueue = (int)RenderQueue.Transparent;
            }

            if (mat.HasProperty("_EnableGPUInstancing")) mat.enableInstancing = true;
            AssetDatabase.CreateAsset(mat, path);
        }

        private static void MakeBuildingWall()
        {
            const int size = 128;
            const int cols = 6, rows = 10;
            const int frame = 3;

            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            Color wall   = Color.white;
            Color window = new Color(0f, 0.8f, 1f, 0.88f);
            Color border = new Color(0.86f, 0.9f, 0.94f);

            int cw = size / cols;
            int ch = size / rows;
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    int lx = x % cw, ly = y % ch;
                    bool isFrame = lx < frame || lx >= cw - frame || ly < frame || ly >= ch - frame;
                    tex.SetPixel(x, y, isFrame ? border : window);
                }
            tex.Apply();

            string texPath = MatDir + "/FA_BuildingWall_Tex.png";
            File.WriteAllBytes(texPath, tex.EncodeToPNG());
            AssetDatabase.ImportAsset(texPath);

            var imp = AssetImporter.GetAtPath(texPath) as TextureImporter;
            if (imp != null) { imp.wrapMode = TextureWrapMode.Repeat; imp.filterMode = FilterMode.Bilinear; imp.SaveAndReimport(); }

            Texture2D savedTex = AssetDatabase.LoadAssetAtPath<Texture2D>(texPath);
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");

            string matPath = MatDir + "/FA_BuildingWall.mat";
            if (AssetDatabase.LoadAssetAtPath<Material>(matPath) != null)
                AssetDatabase.DeleteAsset(matPath);

            Material mat = new Material(shader) { name = "FA_BuildingWall" };
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", Color.white);
            if (savedTex != null)
            {
                string prop = mat.HasProperty("_BaseMap") ? "_BaseMap" : "_MainTex";
                mat.SetTexture(prop, savedTex);
                mat.SetTextureScale(prop, new Vector2(3f, 6f));
            }
            if (mat.HasProperty("_Metallic"))   mat.SetFloat("_Metallic", 0.2f);
            if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", 0.88f);
            mat.enableInstancing = true;
            AssetDatabase.CreateAsset(mat, matPath);
        }
    }
}
