using UnityEngine;
using UnityEngine.Rendering;

namespace AeroBloom
{
    /// <summary>Procedural dreamy cloud visuals + soft particle texture (URP).</summary>
    public static class AeroCloudVisuals
    {
        private static Material s_cloudMat;
        private static Material s_cloudCoreMat;
        private static Material s_particleMat;
        private static Texture2D s_softPuffTex;

        public static Material CloudMaterial
        {
            get
            {
                if (s_cloudMat == null)
                    s_cloudMat = CreateCloudMaterial(0.42f, 0.32f);
                return s_cloudMat;
            }
        }

        public static Material CloudCoreMaterial
        {
            get
            {
                if (s_cloudCoreMat == null)
                    s_cloudCoreMat = CreateCloudMaterial(0.58f, 0.48f);
                return s_cloudCoreMat;
            }
        }

        public static Material ParticleMaterial
        {
            get
            {
                if (s_particleMat == null)
                    s_particleMat = CreateParticleMaterial();
                return s_particleMat;
            }
        }

        public static Texture2D SoftPuffTexture
        {
            get
            {
                if (s_softPuffTex == null)
                    s_softPuffTex = GenerateSoftPuffTexture(128);
                return s_softPuffTex;
            }
        }

        /// <summary>Invisible collider + fluffy cloud mesh stack.</summary>
        public static GameObject CreateDreamyCloudPlatform(Transform parent, string name, Vector3 worldPos,
            Vector3 footprint, bool addMover, Vector3 moveAxis, float moveAmp, float moveSpd, float movePhase)
        {
            GameObject root = new GameObject(name);
            root.transform.SetParent(parent, false);
            root.transform.position = worldPos;

            float w = footprint.x;
            float d = footprint.z;
            float colH = 0.32f;

            GameObject col = GameObject.CreatePrimitive(PrimitiveType.Cube);
            col.name = "CloudCollider";
            col.transform.SetParent(root.transform, false);
            col.transform.localPosition = Vector3.zero;
            col.transform.localScale = new Vector3(w, colH, d);
            Object.Destroy(col.GetComponent<MeshRenderer>());

            SpawnCloudVolume(root.transform, footprint, CloudMaterial, CloudCoreMaterial, Random.Range(0, 9999));

            if (addMover)
            {
                PlatformMover mv = root.AddComponent<PlatformMover>();
                mv.moveAxis = moveAxis;
                mv.amplitude = moveAmp;
                mv.speed = moveSpd;
                mv.phaseOffset = movePhase;
            }

            return root;
        }

        public static void SpawnCloudVolume(Transform parent, Vector3 footprint, Material outer, Material core, int seed)
        {
            var rng = new System.Random(seed);
            GameObject volume = new GameObject("CloudVolume");
            volume.transform.SetParent(parent, false);
            volume.transform.localPosition = new Vector3(0f, -0.05f, 0f);

            float w = footprint.x;
            float d = footprint.z;
            float baseR = Mathf.Max(w, d) * 0.42f;

            (Vector3 pos, Vector3 scale, bool inner)[] blobs =
            {
                (Vector3.zero, new Vector3(baseR * 2.1f, baseR * 0.55f, baseR * 2.1f), true),
                (new Vector3(-w * 0.22f, baseR * 0.08f, -d * 0.18f), new Vector3(baseR * 1.5f, baseR * 0.42f, baseR * 1.35f), false),
                (new Vector3(w * 0.24f, baseR * 0.05f, d * 0.2f), new Vector3(baseR * 1.45f, baseR * 0.4f, baseR * 1.4f), false),
                (new Vector3(0f, baseR * 0.18f, d * 0.22f), new Vector3(baseR * 1.25f, baseR * 0.38f, baseR * 1.2f), false),
                (new Vector3(-w * 0.12f, baseR * 0.12f, d * 0.1f), new Vector3(baseR * 1.1f, baseR * 0.35f, baseR * 1.05f), true),
                (new Vector3(w * 0.08f, baseR * 0.22f, -d * 0.15f), new Vector3(baseR * 0.95f, baseR * 0.32f, baseR * 0.9f), false),
            };

            for (int i = 0; i < blobs.Length; i++)
            {
                var b = blobs[i];
                float jitter = 0.88f + (float)rng.NextDouble() * 0.22f;
                Vector3 sc = b.scale * jitter;
                Material mat = b.inner ? core : outer;
                SpawnCloudBlob(volume.transform, "Puff" + i, b.pos, sc, mat);
            }

            AttachBillboardMist(volume.transform, w, d);
            var drift = volume.AddComponent<AeroCloudDrift>();
            drift.swayAmplitude = 0.18f + (float)rng.NextDouble() * 0.12f;
            drift.rotateSpeed = 1.5f + (float)rng.NextDouble() * 2f;
        }

        public static void SpawnCloudBank(Transform section, Vector3 center, float spanX, float spanZ, float height, int seed)
        {
            var rng = new System.Random(seed);
            GameObject root = new GameObject("MistCloudBank");
            root.transform.SetParent(section, false);
            root.transform.position = center;

            for (int i = 0; i < 11; i++)
            {
                float ox = ((i % 4) - 1.5f) * spanX * 0.22f + ((float)rng.NextDouble() - 0.5f) * spanX * 0.25f;
                float oz = ((i / 4) - 1f) * spanZ * 0.28f + ((float)rng.NextDouble() - 0.5f) * spanZ * 0.25f;
                float oy = ((float)rng.NextDouble() - 0.2f) * height * 0.4f;
                float r = Mathf.Lerp(spanX, spanZ, 0.5f) * (0.2f + (float)rng.NextDouble() * 0.16f);
                Vector3 sc = new Vector3(r * 2f, r * 0.5f, r * 1.85f);
                Material mat = i % 3 == 0 ? CloudCoreMaterial : CloudMaterial;
                SpawnCloudBlob(root.transform, "Bank" + i, new Vector3(ox, oy, oz), sc, mat);
            }

            AttachBillboardMist(root.transform, spanX, spanZ);
            var drift = root.AddComponent<AeroCloudDrift>();
            drift.swayAmplitude = 0.5f;
            drift.rotateSpeed = 1.2f;
        }

        public static void SpawnPathBeacon(Transform platform, Vector3 footprint)
        {
            GameObject glow = new GameObject("PathGlow");
            glow.transform.SetParent(platform, false);
            glow.transform.localPosition = new Vector3(0f, 0.15f, 0f);

            float hx = footprint.x * 0.5f - 0.2f;
            float hz = footprint.z * 0.5f - 0.2f;
            Material beacon = CreateBeaconMaterial();
            Vector3[] corners =
            {
                new Vector3(-hx, 0f, -hz), new Vector3(hx, 0f, -hz),
                new Vector3(hx, 0f, hz), new Vector3(-hx, 0f, hz),
            };

            for (int c = 0; c < corners.Length; c++)
                SpawnCloudBlob(glow.transform, "Beacon" + c, corners[c], Vector3.one * 0.35f, beacon);

            GameObject lightGO = new GameObject("BeaconLight");
            lightGO.transform.SetParent(glow.transform, false);
            lightGO.transform.localPosition = new Vector3(0f, 0.25f, 0f);
            Light l = lightGO.AddComponent<Light>();
            l.type = LightType.Point;
            l.color = new Color(0.55f, 0.92f, 1f);
            l.intensity = 0.85f;
            l.range = Mathf.Max(footprint.x, footprint.z) + 4f;
            l.shadows = LightShadows.None;
        }

        public static void BuildMistParticleVolume(Transform parent, Vector3 pos, Vector3 boxScale)
        {
            GameObject mistGO = new GameObject("MistParticles");
            mistGO.transform.SetParent(parent, false);
            mistGO.transform.position = pos;

            ParticleSystem mps = mistGO.AddComponent<ParticleSystem>();
            var main = mps.main;
            main.startLifetime = 14f;
            main.startSpeed = 0.06f;
            main.startSize = new ParticleSystem.MinMaxCurve(3f, 9f);
            main.startColor = new Color(0.92f, 0.97f, 1f, 0.35f);
            main.maxParticles = 280;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.gravityModifier = -0.02f;

            var em = mps.emission;
            em.rateOverTime = 32f;

            var shape = mps.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = boxScale;

            var vel = mps.velocityOverLifetime;
            vel.enabled = true;
            vel.x = new ParticleSystem.MinMaxCurve(-0.15f, 0.15f);
            vel.y = new ParticleSystem.MinMaxCurve(-0.02f, 0.06f);
            vel.z = new ParticleSystem.MinMaxCurve(-0.15f, 0.15f);

            var col = mps.colorOverLifetime;
            col.enabled = true;
            Gradient g = new Gradient();
            g.SetKeys(
                new[] { new GradientColorKey(new Color(0.9f, 0.96f, 1f), 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(0.4f, 0.2f),
                    new GradientAlphaKey(0.28f, 0.7f), new GradientAlphaKey(0f, 1f) });
            col.color = g;

            var sizeOL = mps.sizeOverLifetime;
            sizeOL.enabled = true;
            sizeOL.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(
                new Keyframe(0f, 0.6f), new Keyframe(0.5f, 1f), new Keyframe(1f, 0.7f)));

            var psr = mistGO.GetComponent<ParticleSystemRenderer>();
            psr.renderMode = ParticleSystemRenderMode.Billboard;
            psr.shadowCastingMode = ShadowCastingMode.Off;
            psr.receiveShadows = false;
            psr.material = ParticleMaterial;
        }

        private static void SpawnCloudBlob(Transform parent, string name, Vector3 localPos, Vector3 scale, Material mat)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localScale = scale;
            AssignCloudRenderer(go, mat);
            Object.Destroy(go.GetComponent<Collider>());
        }

        private static void AssignCloudRenderer(GameObject go, Material mat)
        {
            MeshRenderer r = go.GetComponent<MeshRenderer>();
            r.sharedMaterial = mat;
            r.shadowCastingMode = ShadowCastingMode.Off;
            r.receiveShadows = false;
            r.motionVectorGenerationMode = MotionVectorGenerationMode.ForceNoMotion;
        }

        private static void AttachBillboardMist(Transform cloudRoot, float spanX, float spanZ)
        {
            GameObject psGO = new GameObject("CloudBillboards");
            psGO.transform.SetParent(cloudRoot, false);
            psGO.transform.localPosition = Vector3.zero;

            ParticleSystem ps = psGO.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.startLifetime = 7f;
            main.startSpeed = 0.04f;
            main.startSize = new ParticleSystem.MinMaxCurve(1.2f, 3.8f);
            main.startColor = new Color(1f, 1f, 1f, 0.32f);
            main.maxParticles = 60;
            main.simulationSpace = ParticleSystemSimulationSpace.Local;

            var em = ps.emission;
            em.rateOverTime = 10f;

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(spanX * 0.95f, 2.2f, spanZ * 0.95f);

            var psr = psGO.GetComponent<ParticleSystemRenderer>();
            psr.renderMode = ParticleSystemRenderMode.Billboard;
            psr.shadowCastingMode = ShadowCastingMode.Off;
            psr.material = ParticleMaterial;
        }

        private static Material CreateCloudMaterial(float centerAlpha, float baseAlpha)
        {
            Shader sh = Shader.Find("AeroBloom/SoftCloud");
            if (sh == null)
                sh = Shader.Find("Universal Render Pipeline/Lit");

            Material m = new Material(sh) { name = "AeroDreamCloud" };
            Color tint = new Color(0.94f, 0.98f, 1f, baseAlpha);
            Color emi = new Color(0.4f, 0.65f, 1f, 0.2f);

            if (sh.name.Contains("SoftCloud"))
            {
                m.SetColor("_BaseColor", tint);
                m.SetColor("_Emission", emi);
                m.SetFloat("_CenterAlpha", centerAlpha);
                m.SetFloat("_EdgeAlpha", 0.9f);
                m.SetFloat("_FresnelPower", 2.2f);
                m.SetFloat("_Softness", 1.4f);
            }
            else
            {
                SetupTransparentLit(m, tint, emi);
            }

            return m;
        }

        private static Material CreateBeaconMaterial()
        {
            return CreateCloudMaterial(0.7f, 0.55f);
        }

        private static Material CreateParticleMaterial()
        {
            Shader sh = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            if (sh == null) sh = Shader.Find("Particles/Standard Unlit");
            Material m = new Material(sh) { name = "AeroCloudParticle" };
            if (m.HasProperty("_BaseColor"))
                m.SetColor("_BaseColor", new Color(0.95f, 0.99f, 1f, 0.35f));
            Texture2D puff = SoftPuffTexture;
            if (m.HasProperty("_BaseMap"))
                m.SetTexture("_BaseMap", puff);
            else if (m.HasProperty("_MainTex"))
                m.SetTexture("_MainTex", puff);
            m.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
            m.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
            m.SetInt("_ZWrite", 0);
            if (m.HasProperty("_Surface")) m.SetFloat("_Surface", 1f);
            m.renderQueue = (int)RenderQueue.Transparent + 50;
            return m;
        }

        private static void SetupTransparentLit(Material m, Color col, Color emi)
        {
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", col);
            if (m.HasProperty("_EmissionColor"))
            {
                m.EnableKeyword("_EMISSION");
                m.SetColor("_EmissionColor", emi);
            }
            if (m.HasProperty("_Surface")) m.SetFloat("_Surface", 1f);
            m.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
            m.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
            m.SetInt("_ZWrite", 0);
            m.renderQueue = (int)RenderQueue.Transparent + 100;
        }

        private static Texture2D GenerateSoftPuffTexture(int size)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                name = "AeroCloudPuff",
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear
            };

            float cx = size * 0.5f;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = (x - cx) / cx;
                    float dy = (y - cx) / cx;
                    float d = Mathf.Sqrt(dx * dx + dy * dy);
                    float a = Mathf.Clamp01(1f - d);
                    a = a * a * (3f - 2f * a); // smoothstep
                    a = Mathf.Pow(a, 1.8f);
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
                }
            }

            tex.Apply();
            return tex;
        }
    }
}
