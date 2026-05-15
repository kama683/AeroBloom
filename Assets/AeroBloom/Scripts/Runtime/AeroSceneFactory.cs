using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using TMPro;

namespace AeroBloom
{
    public static class AeroSceneFactory
    {
        public const string RootName = "AeroBloom_Runtime";

        // ─────────────────────────────────────────────
        // BOOTSTRAP
        // ─────────────────────────────────────────────
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void BootstrapEmptyScene()
        {
            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            if (scene.name == "MainMenu" || scene.buildIndex == 0) return;
            if (Object.FindFirstObjectByType<AeroLevelDirector>() != null) return;
            BuildPrototypeScene(false);
        }

        // ─────────────────────────────────────────────
        // MAIN BUILD ENTRY
        // ─────────────────────────────────────────────
        public static AeroLevelDirector BuildPrototypeScene(bool forceRebuild)
        {
            if (forceRebuild) DestroyNamedRoot(RootName);
            AeroLevelDirector existing = Object.FindFirstObjectByType<AeroLevelDirector>();
            if (existing != null && !forceRebuild) return existing;

            AeroPackConfig pack = Resources.Load<AeroPackConfig>("AeroPackConfig");

            GameObject rootGO  = new GameObject(RootName);
            Transform  root    = rootGO.transform;
            Transform  envRoot  = Child(root, "_ENVIRONMENT");
            Transform  cityRoot = Child(root, "_CITY_BACKGROUND");
            Transform  parkRoot = Child(root, "_PARKOUR_PATH");
            Transform  vfxRoot  = Child(root, "_VFX");

            Palette p = Palette.Create();
            RemoveTemplateSceneObjects(root);
            SetupEnvironment(envRoot);
            CreateGroundPlane(envRoot, p, pack);
            CreateAmbientParticles(vfxRoot);
            CreateCity(cityRoot, p);

            Vector3 startPos = new Vector3(0f, 0.6f, -5f);
            AeroPlayerController player = CreatePlayer(root, startPos, Quaternion.identity, pack);

            AeroLevelDirector director = rootGO.AddComponent<AeroLevelDirector>();
            int seeds = 0, cps = 0;

            CreateSection1_GrasslandStart(parkRoot, director, p, ref seeds, ref cps);
            CreateSection2_TowerDiscHops(parkRoot, director, p, ref seeds, ref cps);
            CreateSection3_BlueBlockCanyon(parkRoot, director, p, ref seeds, ref cps);
            CreateSection4_MistCrossing(parkRoot, director, p, ref seeds, ref cps);
            CreateSection5_DiscTowerAscent(parkRoot, director, p, ref seeds, ref cps);
            CreateSection6_HighPlatformRun(parkRoot, director, p, ref seeds, ref cps);
            CreateSection7_BubbleJumpFinale(parkRoot, vfxRoot, director, p, ref seeds, ref cps);

            director.Configure(player, seeds, cps, startPos, Quaternion.identity);
            director.ShowMessage("AeroBloom — Reach the Summit!", 4f);
            return director;
        }

        // ─────────────────────────────────────────────
        // ENVIRONMENT
        // ─────────────────────────────────────────────
        private static void SetupEnvironment(Transform root)
        {
            RenderSettings.fog = false;

            // Skybox ambient — Unity samples the procedural skybox for ambient automatically
            RenderSettings.ambientMode = AmbientMode.Skybox;

            Shader skyShader = Shader.Find("Skybox/Procedural");
            if (skyShader != null)
            {
                Material sky = new Material(skyShader);
                sky.SetColor("_SkyTint",     new Color(0.07f, 0.44f, 0.92f));
                sky.SetColor("_GroundColor", new Color(0.10f, 0.38f, 0.65f));
                sky.SetFloat("_AtmosphereThickness", 0.78f);
                sky.SetFloat("_Exposure",            1.22f);
                sky.SetFloat("_SunDisk",             2f);
                sky.SetFloat("_SunSize",             0.04f);
                sky.SetFloat("_SunSizeConvergence",  8f);
                RenderSettings.skybox = sky;
                DynamicGI.UpdateEnvironment();
            }

            // Main sun — warm, high-intensity, sharp soft shadows
            GameObject sunGO = new GameObject("Directional Light");
            sunGO.transform.SetParent(root, false);
            sunGO.transform.rotation = Quaternion.Euler(50f, -28f, 0f);
            Light sun = sunGO.AddComponent<Light>();
            sun.type             = LightType.Directional;
            sun.color            = new Color(1f, 0.97f, 0.90f);
            sun.intensity        = 1.55f;
            sun.shadows          = LightShadows.Soft;
            sun.shadowStrength   = 0.78f;
            sun.shadowBias       = 0.015f;
            sun.shadowNormalBias = 0.35f;

            // Sky fill + atmosphere accent lights
            SpawnPointLight(root, "FillSky", new Vector3(  0f, 120f, 380f), new Color(0.28f, 0.62f, 1.00f), 0.55f, 280f);
            SpawnPointLight(root, "AtmosA",  new Vector3(-80f,  40f, 200f), new Color(0.00f, 0.82f, 1.00f), 2.4f,  140f);
            SpawnPointLight(root, "AtmosB",  new Vector3(250f,  60f, 450f), new Color(0.40f, 1.00f, 0.60f), 2.4f,  160f);

            // ── Post-processing ────────────────────────────────────────
            GameObject volGO = new GameObject("Aero Post FX");
            volGO.transform.SetParent(root, false);
            Volume vol = volGO.AddComponent<Volume>();
            vol.isGlobal = true; vol.priority = 2f;
            VolumeProfile prof = ScriptableObject.CreateInstance<VolumeProfile>();
            vol.sharedProfile = prof;

            // ACES tonemapping — cinematic, punchy contrast
            var tone = prof.Add<Tonemapping>(true);
            tone.mode.Override(TonemappingMode.ACES);

            // Bloom — characteristic Frutiger glow on emissive surfaces
            var bloom = prof.Add<Bloom>(true);
            bloom.threshold.Override(0.58f);
            bloom.intensity.Override(1.4f);
            bloom.scatter.Override(0.62f);
            bloom.tint.Override(new Color(0.82f, 0.95f, 1f));
            bloom.highQualityFiltering.Override(true);

            // Color grading — vivid, slightly cool, high clarity
            var ca = prof.Add<ColorAdjustments>(true);
            ca.postExposure.Override(0.22f);
            ca.contrast.Override(14f);
            ca.colorFilter.Override(new Color(0.93f, 0.98f, 1.04f));
            ca.saturation.Override(36f);

            // White balance — cool Frutiger Aero temperature
            var wb = prof.Add<WhiteBalance>(true);
            wb.temperature.Override(-10f);
            wb.tint.Override(3f);

            // Fine color grading — push blues into shadows and highlights
            var lgg = prof.Add<LiftGammaGain>(true);
            lgg.lift.Override(new Vector4(0.98f, 0.99f, 1.02f, 0f));
            lgg.gamma.Override(new Vector4(0.97f, 0.99f, 1.04f, 0f));
            lgg.gain.Override(new Vector4(0.95f, 0.97f, 1.06f, 0f));

            // Shadows / Midtones / Highlights
            var smt = prof.Add<ShadowsMidtonesHighlights>(true);
            smt.shadows.Override(new Vector4(1.00f, 1.01f, 1.05f, 0f));
            smt.midtones.Override(new Vector4(0.98f, 1.00f, 1.03f, 0f));
            smt.highlights.Override(new Vector4(0.95f, 0.97f, 1.06f, 0f));

            // Chromatic aberration — subtle glass/lens refraction feel
            var chrom = prof.Add<ChromaticAberration>(true);
            chrom.intensity.Override(0.10f);

            // Vignette — draws eye to centre of screen
            var vig = prof.Add<Vignette>(true);
            vig.color.Override(new Color(0.02f, 0.05f, 0.14f));
            vig.intensity.Override(0.26f);
            vig.smoothness.Override(0.38f);
            vig.rounded.Override(true);

            // Film grain — very subtle, adds tactile texture
            var grain = prof.Add<FilmGrain>(true);
            grain.type.Override(FilmGrainLookup.Medium1);
            grain.intensity.Override(0.06f);
            grain.response.Override(0.85f);

            // Depth of field — blurs only distant background (>90 m), foreground sharp
            var dof = prof.Add<DepthOfField>(true);
            dof.mode.Override(DepthOfFieldMode.Gaussian);
            dof.gaussianStart.Override(90f);
            dof.gaussianEnd.Override(220f);
            dof.gaussianMaxRadius.Override(1.2f);
            dof.highQualitySampling.Override(true);
        }

        private static void CreateGroundPlane(Transform root, Palette p, AeroPackConfig pack)
        {
            // Bright green grass — no collider so player falls through and respawns
            Material grassMat = null;
            if (pack != null && pack.grassMaterial != null) grassMat = pack.grassMaterial;
            if (grassMat == null) grassMat = LoadFA("FA_Ground");
            if (grassMat == null) grassMat = p.Grass;

            GameObject grass = GameObject.CreatePrimitive(PrimitiveType.Plane);
            grass.name = "GrassPlane";
            grass.transform.SetParent(root, false);
            grass.transform.position   = new Vector3(0f, -3.5f, 380f);
            grass.transform.localScale = new Vector3(82f, 1f, 92f);
            AssignMat(grass, grassMat);
            DestroyCollider(grass);

            Material waterMat = LoadFA("FA_Ground");
            if (waterMat == null) waterMat = p.Water;
            GameObject water = GameObject.CreatePrimitive(PrimitiveType.Plane);
            water.name = "WaterPlane";
            water.transform.SetParent(root, false);
            water.transform.position   = new Vector3(0f, -4f, 380f);
            water.transform.localScale = new Vector3(85f, 1f, 95f);
            AssignMat(water, waterMat);
            DestroyCollider(water);
        }



        private static void CreateAmbientParticles(Transform root)
        {
            GameObject psGO = new GameObject("AmbientDust");
            psGO.transform.SetParent(root, false);
            psGO.transform.position = new Vector3(0f, 60f, 380f);
            ParticleSystem ps = psGO.AddComponent<ParticleSystem>();

            var main = ps.main;
            main.startLifetime   = new ParticleSystem.MinMaxCurve(10f, 16f);
            main.startSpeed      = 0.2f;
            main.startSize       = new ParticleSystem.MinMaxCurve(0.02f, 0.05f);
            main.startColor      = new ParticleSystem.MinMaxGradient(new Color(1f, 1f, 1f, 0.4f), new Color(0.7f, 1f, 0.8f, 0.3f));
            main.maxParticles    = 600;
            main.simulationSpace = ParticleSystemSimulationSpace.World;

            var emit = ps.emission; emit.rateOverTime = 25f;
            var shape = ps.shape; shape.shapeType = ParticleSystemShapeType.Box; shape.scale = new Vector3(700f, 220f, 800f);

            var vel = ps.velocityOverLifetime;
            vel.enabled = true; vel.space = ParticleSystemSimulationSpace.World;
            vel.y = new ParticleSystem.MinMaxCurve(0.12f, 0.28f);
            vel.x = new ParticleSystem.MinMaxCurve(-0.06f, 0.06f);
            vel.z = new ParticleSystem.MinMaxCurve(-0.06f, 0.06f);

            var psr = psGO.GetComponent<ParticleSystemRenderer>();
            psr.shadowCastingMode = ShadowCastingMode.Off;
            psr.material = MakeParticleMat(new Color(1f, 1f, 1f, 0.35f));
        }

        // ─────────────────────────────────────────────
        // CITY BACKGROUND — white organic towers on the sides
        // ─────────────────────────────────────────────
        private static void CreateCity(Transform root, Palette p)
        {
            System.Random r = new System.Random(42);
            Material wallMat  = LoadFA("FA_BuildingWall");  if (wallMat  == null) wallMat  = p.AeroBuilding;
            Material glassMat = LoadFA("FA_BuildingGlass"); if (glassMat == null) glassMat = p.AeroBuildingWindow;

            // Left cluster  x = -185 … -75
            for (int i = 0; i < 18; i++)
            {
                float bx  = -185f + (float)r.NextDouble() * 110f;
                float bz  = (float)r.NextDouble() * 760f;
                float h   = 45f + RF(r, 115f);
                float w   = 8f  + RF(r, 16f);
                float yaw = (float)r.NextDouble() * 360f;
                ProceduralBuilding(root, "PL" + i, new Vector3(bx, h * 0.5f - 5f, bz),
                    new Vector3(w, h, w), Quaternion.Euler(0, yaw, 0), i % 5, wallMat, glassMat, p);
            }

            // Right cluster  x = 75 … 185
            for (int i = 0; i < 18; i++)
            {
                float bx  = 75f + (float)r.NextDouble() * 110f;
                float bz  = (float)r.NextDouble() * 760f;
                float h   = 45f + RF(r, 115f);
                float w   = 8f  + RF(r, 16f);
                float yaw = (float)r.NextDouble() * 360f;
                ProceduralBuilding(root, "PR" + i, new Vector3(bx, h * 0.5f - 5f, bz),
                    new Vector3(w, h, w), Quaternion.Euler(0, yaw, 0), (i + 2) % 5, wallMat, glassMat, p);
            }

            // Landmark hero towers — dominate the skyline
            ProceduralBuilding(root, "HeroL", new Vector3(-135f, 110f - 5f, 370f),
                new Vector3(20f, 220f, 20f), Quaternion.identity, 4, wallMat, glassMat, p);
            ProceduralBuilding(root, "HeroR", new Vector3( 135f,  95f - 5f, 370f),
                new Vector3(18f, 190f, 18f), Quaternion.identity, 1, wallMat, glassMat, p);
            // HeroC moved to x=85 so it doesn't overlap the z=764 finish area (was at x=0)
            ProceduralBuilding(root, "HeroC", new Vector3(  85f,  80f - 5f, 760f),
                new Vector3(22f, 160f, 22f), Quaternion.identity, 3, wallMat, glassMat, p);

            // Chrome floating deco spheres
            Material chromeMat = LoadFA("FA_Chrome"); if (chromeMat == null) chromeMat = p.Chrome;
            for (int i = 0; i < 18; i++)
            {
                float sx = (float)(r.NextDouble() - 0.5) * 320f;
                float sz = (float)r.NextDouble() * 700f;
                float sy = 18f + RF(r, 55f);
                float sc = 1.5f + RF(r, 4.5f);
                MakeSphere(root, "DecoSphere" + i, new Vector3(sx, sy, sz), sc, chromeMat, false);
            }

            // ── INNER CLOSE RING (x = ±47-65) — flanking the corridor ─────────────
            for (int i = 0; i < 12; i++)
            {
                float bx = -(47f + RF(r, 18f));
                float bz = 20f + RF(r, 760f);
                float h  = 22f + RF(r, 70f);
                float w  = 5f  + RF(r, 10f);
                ProceduralBuilding(root, "IL" + i, new Vector3(bx, h * 0.5f - 5f, bz),
                    new Vector3(w, h, w), Quaternion.Euler(0, RF(r, 360f), 0), i % 5, wallMat, glassMat, p);
            }
            for (int i = 0; i < 12; i++)
            {
                float bx = 47f + RF(r, 18f);
                float bz = 20f + RF(r, 760f);
                float h  = 22f + RF(r, 70f);
                float w  = 5f  + RF(r, 10f);
                ProceduralBuilding(root, "IR" + i, new Vector3(bx, h * 0.5f - 5f, bz),
                    new Vector3(w, h, w), Quaternion.Euler(0, RF(r, 360f), 0), (i + 3) % 5, wallMat, glassMat, p);
            }

            // ── FAR OUTER RING (x = ±200-380) — tall background skyline ──────────
            for (int i = 0; i < 16; i++)
            {
                float bx = -(200f + RF(r, 180f));
                float bz = RF(r, 800f);
                float h  = 100f + RF(r, 260f);
                float w  = 16f  + RF(r, 28f);
                ProceduralBuilding(root, "OL" + i, new Vector3(bx, h * 0.5f - 5f, bz),
                    new Vector3(w, h, w), Quaternion.Euler(0, RF(r, 360f), 0), i % 5, wallMat, glassMat, p);
            }
            for (int i = 0; i < 16; i++)
            {
                float bx = 200f + RF(r, 180f);
                float bz = RF(r, 800f);
                float h  = 100f + RF(r, 260f);
                float w  = 16f  + RF(r, 28f);
                ProceduralBuilding(root, "OR" + i, new Vector3(bx, h * 0.5f - 5f, bz),
                    new Vector3(w, h, w), Quaternion.Euler(0, RF(r, 360f), 0), (i + 1) % 5, wallMat, glassMat, p);
            }

            // ── MEGA LANDMARK TOWERS — dominate the far horizon ───────────────────
            (Vector3 mp, Vector3 ms, int mt)[] megas = {
                (new Vector3(-280f, 195f,  180f), new Vector3(32f, 390f, 32f), 4),
                (new Vector3( 265f, 180f,  590f), new Vector3(28f, 360f, 28f), 2),
                (new Vector3(-315f, 165f,  560f), new Vector3(26f, 330f, 26f), 0),
                (new Vector3( 295f, 155f,  130f), new Vector3(27f, 310f, 27f), 3),
                (new Vector3(-225f, 220f,  760f), new Vector3(38f, 440f, 38f), 4),
                (new Vector3( 235f, 210f,   28f), new Vector3(36f, 420f, 36f), 1),
            };
            foreach (var m in megas)
                ProceduralBuilding(root, "Mega" + m.mp.x, m.mp, m.ms, Quaternion.identity, m.mt, wallMat, glassMat, p);

            // ── FLOATING AERO HALOS — glowing horizontal rings in the sky ─────────
            Material haloMat = LoadFA("FA_EmissiveCyan"); if (haloMat == null) haloMat = p.EmissiveCyan;
            (float hx, float hy, float hz, float hr)[] halos = {
                (-115f,  88f, 220f, 28f),
                ( 125f,  98f, 430f, 22f),
                ( -95f, 118f, 565f, 32f),
                (  95f,  78f, 140f, 20f),
                (-135f, 108f, 690f, 26f),
            };
            foreach (var halo in halos)
            {
                var hgo = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                hgo.name = "AeroHalo";
                hgo.transform.SetParent(root, false);
                hgo.transform.position   = new Vector3(halo.hx, halo.hy, halo.hz);
                hgo.transform.localScale = new Vector3(halo.hr * 2f, 0.38f, halo.hr * 2f);
                AssignMat(hgo, haloMat);
                DestroyCollider(hgo);
                SpawnPointLight(hgo.transform, "HaloGlow", Vector3.zero,
                    new Color(0f, 0.88f, 1f), 1.3f, halo.hr * 1.6f);
            }

            // ── GROUND-LEVEL LAMP POSTS (section 1, z=0-75) ───────────────────────
            Material lampHead = LoadFA("FA_EmissiveCyan"); if (lampHead == null) lampHead = p.EmissiveCyan;
            for (int i = 0; i < 10; i++)
            {
                float lz = 4f + i * 7.5f;
                BoxNoColl(root, "LShaftL" + i, new Vector3(-25f,  7.0f, lz), new Vector3(0.4f, 14.0f, 0.4f), chromeMat);
                BoxNoColl(root, "LHeadL"  + i, new Vector3(-25f, 14.3f, lz), new Vector3(1.4f,  0.4f, 1.4f), lampHead);
                SpawnPointLight(root, "LLitL" + i, new Vector3(-25f, 14.7f, lz), new Color(0.45f, 1f, 1f), 1.1f, 20f);
                BoxNoColl(root, "LShaftR" + i, new Vector3( 25f,  7.0f, lz), new Vector3(0.4f, 14.0f, 0.4f), chromeMat);
                BoxNoColl(root, "LHeadR"  + i, new Vector3( 25f, 14.3f, lz), new Vector3(1.4f,  0.4f, 1.4f), lampHead);
                SpawnPointLight(root, "LLitR" + i, new Vector3( 25f, 14.7f, lz), new Color(0.45f, 1f, 1f), 1.1f, 20f);
            }


            // ── HIGH-ALTITUDE DECO SPHERES — fill the upper sky ──────────────────
            for (int i = 18; i < 36; i++)
            {
                float sx = (float)(r.NextDouble() - 0.5) * 500f;
                float sz = (float)r.NextDouble() * 800f;
                float sy = 70f + RF(r, 130f);
                float sc = 3f  + RF(r, 9f);
                MakeSphere(root, "HiSphere" + i, new Vector3(sx, sy, sz), sc, chromeMat, false);
            }
        }

        private static void ProceduralBuilding(Transform parent, string name,
            Vector3 pos, Vector3 size, Quaternion rot, int type, Material wall, Material glass, Palette p)
        {
            Material glassMat = LoadFA("FA_BuildingGlass"); if (glassMat == null) glassMat = p.AeroBuildingWindow;
            Material eMat     = LoadFA("FA_EmissiveCyan");  if (eMat     == null) eMat     = p.EmissiveCyan;
            Material chromMat = LoadFA("FA_Chrome");        if (chromMat == null) chromMat = p.Chrome;

            Material bodyMat;
            switch (type % 5)
            {
                case 1:  bodyMat = p.Chrome;       break; // silver-white
                case 2:  bodyMat = p.BlueSolid;    break; // deep navy
                case 3:  bodyMat = p.CyanSolid;    break; // teal
                case 4:  bodyMat = p.GlobeBlue;    break; // translucent dark blue
                default: bodyMat = p.AeroBuilding; break; // medium blue
            }
            Material trimMat = (type % 2 == 0) ? eMat : p.EmissiveLime;

            float hh = size.y * 0.5f;

            // Container at building centre
            GameObject tower = new GameObject(name);
            tower.transform.SetParent(parent, false);
            tower.transform.position = pos;
            tower.transform.rotation = rot;

            // ── Main body ─────────────────────────────────────────────
            GameObject body = GameObject.CreatePrimitive(PrimitiveType.Cube);
            body.name = "Body";
            body.transform.SetParent(tower.transform, false);
            body.transform.localPosition = Vector3.zero;
            body.transform.localScale    = size;
            AssignMat(body, bodyMat);
            DestroyCollider(body);

            // ── Horizontal window bands ───────────────────────────────
            float bStep = Mathf.Clamp(size.y / Mathf.Max(1f, Mathf.Round(size.y / 4f)), 3f, 6f);
            float bH    = bStep * 0.52f;
            for (float ly = -hh + bStep; ly < hh; ly += bStep)
            {
                GameObject win = GameObject.CreatePrimitive(PrimitiveType.Cube);
                win.name = "Win";
                win.transform.SetParent(body.transform, false);
                win.transform.localPosition = new Vector3(0f, ly / size.y, 0f);
                win.transform.localScale    = new Vector3(1.003f, bH / size.y, 1.003f);
                AssignMat(win, glassMat);
                DestroyCollider(win);
            }

            // ── Emissive cyan trim every ~8 m ─────────────────────────
            float tStep = Mathf.Clamp(size.y / Mathf.Max(1f, Mathf.Floor(size.y / 8f)), 6f, 11f);
            for (float ly = -hh + tStep * 0.5f; ly < hh; ly += tStep)
            {
                GameObject et = GameObject.CreatePrimitive(PrimitiveType.Cube);
                et.name = "ETrim";
                et.transform.SetParent(body.transform, false);
                et.transform.localPosition = new Vector3(0f, ly / size.y, 0f);
                et.transform.localScale    = new Vector3(1.012f, 0.38f / size.y, 1.012f);
                AssignMat(et, trimMat);
                DestroyCollider(et);
            }

            // Top edge glow
            GameObject topEdge = GameObject.CreatePrimitive(PrimitiveType.Cube);
            topEdge.name = "TopEdge";
            topEdge.transform.SetParent(body.transform, false);
            topEdge.transform.localPosition = new Vector3(0f, 0.502f, 0f);
            topEdge.transform.localScale    = new Vector3(1.016f, 0.42f / size.y, 1.016f);
            AssignMat(topEdge, trimMat);
            DestroyCollider(topEdge);

            // Base ground glow
            GameObject baseEdge = GameObject.CreatePrimitive(PrimitiveType.Cube);
            baseEdge.name = "BaseEdge";
            baseEdge.transform.SetParent(body.transform, false);
            baseEdge.transform.localPosition = new Vector3(0f, -0.502f, 0f);
            baseEdge.transform.localScale    = new Vector3(1.016f, 0.42f / size.y, 1.016f);
            AssignMat(baseEdge, trimMat);
            DestroyCollider(baseEdge);

            // Cyan point light at building top for ambient glow
            SpawnPointLight(tower.transform, "BldgTopGlow",
                new Vector3(0f, hh + 1.5f, 0f),
                new Color(0f, 0.78f, 1f), 2.2f, size.x * 4.0f);

            // ── Type-specific tops ────────────────────────────────────
            switch (type % 5)
            {
                case 1: // Setback block + antenna
                {
                    float sbW = size.x * 0.58f;
                    float sbH = size.y * 0.26f;
                    GameObject sb = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    sb.name = "Setback";
                    sb.transform.SetParent(tower.transform, false);
                    sb.transform.localPosition = new Vector3(0f, hh + sbH * 0.5f, 0f);
                    sb.transform.localScale    = new Vector3(sbW, sbH, sbW);
                    AssignMat(sb, bodyMat);
                    DestroyCollider(sb);

                    // Window bands on setback
                    for (float ly = -sbH * 0.38f; ly < sbH * 0.4f; ly += sbH * 0.32f)
                    {
                        GameObject sw = GameObject.CreatePrimitive(PrimitiveType.Cube);
                        sw.name = "SbWin";
                        sw.transform.SetParent(sb.transform, false);
                        sw.transform.localPosition = new Vector3(0f, ly / sbH, 0f);
                        sw.transform.localScale    = new Vector3(1.003f, 0.38f, 1.003f);
                        AssignMat(sw, glassMat);
                        DestroyCollider(sw);
                    }

                    // Emissive collar between body and setback
                    GameObject collar = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    collar.name = "Collar";
                    collar.transform.SetParent(tower.transform, false);
                    collar.transform.localPosition = new Vector3(0f, hh + 0.18f, 0f);
                    collar.transform.localScale    = new Vector3(size.x + 0.55f, 0.36f, size.x + 0.55f);
                    AssignMat(collar, eMat);
                    DestroyCollider(collar);

                    // Antenna
                    GameObject ant = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                    ant.name = "Antenna";
                    ant.transform.SetParent(tower.transform, false);
                    ant.transform.localPosition = new Vector3(0f, hh + sbH + size.y * 0.055f, 0f);
                    ant.transform.localScale    = new Vector3(0.22f, size.y * 0.055f, 0.22f);
                    AssignMat(ant, chromMat);
                    DestroyCollider(ant);
                    break;
                }

                case 2: // Stepped pyramid — 2 shrinking blocks
                {
                    for (int s = 1; s <= 2; s++)
                    {
                        float f  = 1f - s * 0.28f;
                        float sH = size.y * 0.17f;
                        GameObject step = GameObject.CreatePrimitive(PrimitiveType.Cube);
                        step.name = "Step" + s;
                        step.transform.SetParent(tower.transform, false);
                        step.transform.localPosition = new Vector3(0f, hh + (s - 0.5f) * sH, 0f);
                        step.transform.localScale    = new Vector3(size.x * f, sH, size.x * f);
                        AssignMat(step, bodyMat);
                        DestroyCollider(step);

                        GameObject strim = GameObject.CreatePrimitive(PrimitiveType.Cube);
                        strim.name = "STrim";
                        strim.transform.SetParent(tower.transform, false);
                        strim.transform.localPosition = new Vector3(0f, hh + (s - 1f) * sH, 0f);
                        strim.transform.localScale    = new Vector3(size.x * f + 0.42f, 0.30f, size.x * f + 0.42f);
                        AssignMat(strim, eMat);
                        DestroyCollider(strim);
                    }
                    break;
                }

                case 3: // Glass sphere dome
                {
                    float dR = size.x * 0.62f;
                    GameObject dome = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    dome.name = "Dome";
                    dome.transform.SetParent(tower.transform, false);
                    dome.transform.localPosition = new Vector3(0f, hh + dR * 0.52f, 0f);
                    dome.transform.localScale    = Vector3.one * dR;
                    AssignMat(dome, glassMat);
                    DestroyCollider(dome);

                    // Emissive ring at dome base
                    GameObject ring = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                    ring.name = "DomeRing";
                    ring.transform.SetParent(tower.transform, false);
                    ring.transform.localPosition = new Vector3(0f, hh + 0.18f, 0f);
                    ring.transform.localScale    = new Vector3(dR * 2.3f, 0.12f, dR * 2.3f);
                    AssignMat(ring, eMat);
                    DestroyCollider(ring);
                    break;
                }

                case 4: // Chrome spire with glowing tip
                {
                    float spH = size.y * 0.20f;
                    GameObject spire = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                    spire.name = "Spire";
                    spire.transform.SetParent(tower.transform, false);
                    spire.transform.localPosition = new Vector3(0f, hh + spH, 0f);
                    spire.transform.localScale    = new Vector3(0.35f, spH, 0.35f);
                    AssignMat(spire, chromMat);
                    DestroyCollider(spire);

                    GameObject tip = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    tip.name = "SpireTip";
                    tip.transform.SetParent(tower.transform, false);
                    tip.transform.localPosition = new Vector3(0f, hh + spH * 2f + 0.6f, 0f);
                    tip.transform.localScale    = Vector3.one * 1.3f;
                    AssignMat(tip, eMat);
                    DestroyCollider(tip);

                    SpawnPointLight(tower.transform, "SpireGlow",
                        new Vector3(0f, hh + spH * 2f + 1f, 0f),
                        new Color(0f, 0.85f, 1f), 1.5f, 22f);
                    break;
                }
                // case 0: plain top — only the top edge trim above
            }

            // Top glow point light (all building types)
            SpawnPointLight(tower.transform, "TopGlow",
                new Vector3(0f, hh + 4f, 0f),
                new Color(0.15f, 0.62f, 1f), 0.40f, 28f);
        }

        // ─────────────────────────────────────────────
        // SECTION 1 — GRASSLAND START (z 0 → 80, y 0 → 3)
        // ─────────────────────────────────────────────
        private static void CreateSection1_GrasslandStart(Transform root, AeroLevelDirector dir,
            Palette p, ref int seeds, ref int cps)
        {
            Transform s = Child(root, "Section1_GrasslandStart");

            Plat(s, "Spawn Island", new Vector3(0f, 0f, 0f), new Vector3(22f, 1f, 22f), p.BlueSolid);

            Sign(s, "Welcome",  "AEROBLOOM\nAscend the Summit",        new Vector3( 0f, 3.5f,  6f), Quaternion.identity,           p);
            Sign(s, "Controls", "WASD Move  SPACE Jump\nSHIFT Sprint", new Vector3(-5f, 3.5f, 11f), Quaternion.Euler(0f, -30f, 0f), p);
            Sign(s, "Goal",     "Collect AeroSeeds\nReach the top!",   new Vector3( 5f, 3.5f, 11f), Quaternion.Euler(0f,  30f, 0f), p);

            // 8 stepping platforms gently rising north
            float[] sz = { 14f, 22f, 31f, 40f, 49f, 58f, 67f, 76f };
            float[] sy = {  0f,  0.5f, 0.8f, 1.2f, 1f,  1.8f, 1.5f, 2.5f };
            float[] sx = {  0f,  2f,  -3f,  4f,  -2f,  3f,  -2f,  0f };
            float[] sw = {  8f,  7f,   7f,  6.5f, 7f,  6f,   6f,  10f };

            for (int i = 0; i < 8; i++)
            {
                Material m = (i % 3 == 0) ? p.BlueSolid : (i % 3 == 1 ? p.CyanSolid : p.WhiteGlass);
                Plat(s, "GrassStep " + (i + 1), new Vector3(sx[i], sy[i], sz[i]), new Vector3(sw[i], 0.5f, 6f), m);
                if (i == 2) AddSeed(s, new Vector3(sx[i], sy[i] + 1.5f, sz[i]), dir, p, ref seeds);
                if (i == 5) AddSeed(s, new Vector3(sx[i], sy[i] + 1.5f, sz[i]), dir, p, ref seeds);
            }

            // Blue neon pillars flanking the start corridor — ground at y=-3.5, bottom at y=-4
            Material eMat2 = LoadFA("FA_EmissiveCyan"); if (eMat2 == null) eMat2 = p.EmissiveCyan;
            // center = groundY + height/2  →  -4 + 12 = 8  and  -4 + 20 = 16
            BoxNoColl(s, "Pillar L1", new Vector3(-18f,  8f, 15f), new Vector3(4f, 24f, 4f), p.AeroBuilding);
            BoxNoColl(s, "Pillar R1", new Vector3( 18f,  8f, 15f), new Vector3(4f, 24f, 4f), p.AeroBuilding);
            BoxNoColl(s, "Pillar L2", new Vector3(-20f, 16f, 45f), new Vector3(3f, 40f, 3f), p.AeroBuilding);
            BoxNoColl(s, "Pillar R2", new Vector3( 20f, 16f, 45f), new Vector3(3f, 40f, 3f), p.AeroBuilding);
            // Cyan glow rings at pillar tops  (top = center + height/2)
            BoxNoColl(s, "PillarTopL1", new Vector3(-18f, 20f, 15f), new Vector3(4.4f, 0.5f, 4.4f), eMat2);
            BoxNoColl(s, "PillarTopR1", new Vector3( 18f, 20f, 15f), new Vector3(4.4f, 0.5f, 4.4f), eMat2);
            BoxNoColl(s, "PillarTopL2", new Vector3(-20f, 36f, 45f), new Vector3(3.4f, 0.5f, 3.4f), eMat2);
            BoxNoColl(s, "PillarTopR2", new Vector3( 20f, 36f, 45f), new Vector3(3.4f, 0.5f, 3.4f), eMat2);

            // Ground-level lime accent patches (visual only)
            Material limeE = LoadFA("FA_EmissiveLime"); if (limeE == null) limeE = p.LimeSolid;
            BoxNoColl(s, "GrassPatchL", new Vector3(-12f, -0.05f, 30f), new Vector3(8f, 0.1f, 20f), limeE);
            BoxNoColl(s, "GrassPatchR", new Vector3( 12f, -0.05f, 30f), new Vector3(8f, 0.1f, 20f), limeE);

            Sign(s, "Warn01", "DISC TOWERS AHEAD!\nPlatforms move and spin.\nTime your jumps carefully!", new Vector3(0f, 4.3f, 68f), Quaternion.identity, p, noPole: true);
            Checkpoint(s, "Relay 01 Grassland", new Vector3(0f, 2.8f, 78f), Quaternion.identity, p, ref cps);
        }

        // ─────────────────────────────────────────────
        // SECTION 2 — TOWER DISC HOPS (z 80 → 220, y 3 → 28)
        // ─────────────────────────────────────────────
        private static void CreateSection2_TowerDiscHops(Transform root, AeroLevelDirector dir,
            Palette p, ref int seeds, ref int cps)
        {
            Transform s = Child(root, "Section2_TowerDiscHops");

            Plat(s, "ApproachA", new Vector3(0f, 2.5f, 82f), new Vector3(8f, 0.5f, 5f), p.CyanSolid);
            Sign(s, "DiscHint", "Hop the Disc Towers!\nTime the moving ones!", new Vector3(5f, 4.3f, 88f), Quaternion.Euler(0f, 30f, 0f), p, noPole: true);

            Material shaftMat = LoadFA("FA_Chrome");       if (shaftMat == null) shaftMat = p.Chrome;
            Material eMat     = LoadFA("FA_EmissiveCyan"); if (eMat     == null) eMat     = p.EmissiveCyan;

            // 1.5 m gain per step → comfortable single jump (max theoretical = 2.0 m).
            // Approach top ≈2.75  T1 top=4.5  …  T8 top=15.5
            (Vector3 pos, float shaftH, float discR, bool moves, Vector3 mAxis, float mAmp, float mSpd)[] towers =
            {
                (new Vector3( 0f, 0f,  93f),  4.15f, 5.0f, false, Vector3.zero,    0f,  0f),   // top=4.5  +1.75
                (new Vector3( 6f, 0f, 109f),  5.65f, 4.5f, false, Vector3.zero,    0f,  0f),   // top=6.0  +1.5
                (new Vector3(-5f, 0f, 125f),  7.15f, 4.5f, true,  Vector3.right,   3.5f, 0.85f),// top=7.5  +1.5
                (new Vector3( 5f, 0f, 141f),  8.65f, 5.0f, false, Vector3.zero,    0f,  0f),   // top=9.0  +1.5
                (new Vector3(-4f, 0f, 157f), 10.15f, 4.0f, true,  Vector3.forward, 3.0f, 0.95f),// top=10.5 +1.5
                (new Vector3( 4f, 0f, 173f), 11.65f, 4.5f, false, Vector3.zero,    0f,  0f),   // top=12.0 +1.5
                (new Vector3(-3f, 0f, 189f), 13.15f, 4.0f, true,  Vector3.right,   2.5f, 1.0f), // top=13.5 +1.5
                (new Vector3( 0f, 0f, 206f), 15.15f, 6.0f, false, Vector3.zero,    0f,  0f),   // top=15.5 +2.0 double jump
            };

            for (int i = 0; i < towers.Length; i++)
            {
                var t = towers[i];
                Material dm = (i % 2 == 0) ? p.CyanSolid : p.BlueSolid;
                GameObject disc = CreateDiscTower(s, "DiscTower " + (i + 1), t.pos, t.shaftH, t.discR, 0.7f, shaftMat, dm);

                // Emissive edge ring on disc top
                GameObject edgeCyl = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                edgeCyl.name = "DiscEdge";
                edgeCyl.transform.SetParent(disc.transform, false);
                edgeCyl.transform.localPosition = new Vector3(0f, 1f, 0f);   // top face in cylinder local space
                edgeCyl.transform.localScale    = new Vector3((t.discR * 2f + 0.6f) / (t.discR * 2f), 0.1f / 0.7f, (t.discR * 2f + 0.6f) / (t.discR * 2f));
                AssignMat(edgeCyl, eMat);
                DestroyCollider(edgeCyl);

                if (t.moves)
                {
                    PlatformMover pm = disc.AddComponent<PlatformMover>();
                    pm.moveAxis    = t.mAxis;
                    pm.amplitude   = t.mAmp;
                    pm.speed       = t.mSpd;
                    pm.phaseOffset = i * 0.55f;
                }

                SpawnPointLight(disc.transform, "DiscGlow", new Vector3(0f, 2f, 0f), new Color(0f, 0.8f, 1f), 1.0f, 16f);
                if (i % 2 == 0) AddSeed(s, t.pos + new Vector3(0f, t.shaftH + 2.5f, 0f), dir, p, ref seeds);

                // All but the last bridge disc fade away after 3 s
                if (i < towers.Length - 1)
                    disc.AddComponent<AeroDiscFade>();
            }

            AddSeed(s, new Vector3(0f, 17.5f, 206f), dir, p, ref seeds);

            // Bridge platform: Tower8 top 15.5 m → bridge 17 m (single jump) → Canyon plat1 19 m (single jump)
            Plat(s, "CanyonBridge", new Vector3(0f, 17f, 213f), new Vector3(8f, 0.5f, 5f), p.BlueSolid);
            // Relay gate sits ON the bridge platform (bridge top = 17.25 m)
            Sign(s, "Warn02", "CANYON AHEAD!\nPlatforms rise 2-3m each step.\nDouble jump is your best friend!", new Vector3(0f, 18.8f, 203f), Quaternion.identity, p, noPole: true);
            Checkpoint(s, "Relay 02 Disc Hops", new Vector3(0f, 17.3f, 213f), Quaternion.identity, p, ref cps);
        }

        // ─────────────────────────────────────────────
        // SECTION 3 — BLUE BLOCK CANYON (z 220 → 360, y 25 → 55)
        // ─────────────────────────────────────────────
        private static void CreateSection3_BlueBlockCanyon(Transform root, AeroLevelDirector dir,
            Palette p, ref int seeds, ref int cps)
        {
            Transform s = Child(root, "Section3_BlueBlockCanyon");

            Material canyonWall = LoadFA("FA_GlassBlue");   if (canyonWall == null) canyonWall = p.Glass;
            Material eMat       = LoadFA("FA_EmissiveCyan"); if (eMat       == null) eMat       = p.EmissiveCyan;
            Material limeMat    = LoadFA("FA_EmissiveLime"); if (limeMat    == null) limeMat    = p.EmissiveLime;

            // Canyon wall columns flanking the path
            for (int row = 0; row < 9; row++)
            {
                float wallZ = 222f + row * 16f;
                float wallH = 25f + row * 4f;
                BoxNoColl(s, "WallL" + row, new Vector3(-15f, 19f + wallH * 0.5f, wallZ), new Vector3(10f, wallH, 13f), canyonWall);
                BoxNoColl(s, "WallR" + row, new Vector3( 15f, 19f + wallH * 0.5f, wallZ), new Vector3(10f, wallH, 13f), canyonWall);
                BoxNoColl(s, "TrimL" + row, new Vector3(-15f, 19f + wallH,         wallZ), new Vector3(10.2f, 0.4f, 13.2f), eMat);
                BoxNoColl(s, "TrimR" + row, new Vector3( 15f, 19f + wallH,         wallZ), new Vector3(10.2f, 0.4f, 13.2f), eMat);
            }

            // 10 jumping platforms — start at y≈19 (S2 end), ~2m gain each
            // Heights: every step ≤ 3 m (double-jump reachable).
            // X offsets reduced to ≤ 3 m so diagonal distance is never extreme.
            // Z positions kept original; wide gaps bridged by stepping-stone platforms below.
            float[] cpz = { 222f, 232f, 243f, 254f, 265f, 278f, 291f, 308f, 326f, 345f };
            float[] cpy = {  19f,  22f,  25f,  27f,  30f,  32f,  35f,  38f,  41f,  44f };
            float[] cpx = {   0f,   2f,  -2f,   3f,  -2f,   2f,  -2f,   2f,  -1f,   0f };
            float[] cpw = {   9f,   7f,   7f,   6f,   7f,   6f,   7f,   6f,   7f,  11f };

            for (int i = 0; i < cpz.Length; i++)
            {
                Material cm = (i % 2 == 0) ? p.BlueSolid : p.CyanSolid;
                Plat(s, "Canyon " + (i + 1), new Vector3(cpx[i], cpy[i], cpz[i]), new Vector3(cpw[i], 0.6f, 5.5f), cm);
                if (i == 2 || i == 6)
                    AddSeed(s, new Vector3(cpx[i], cpy[i] + 1.5f, cpz[i]), dir, p, ref seeds);
            }

            // Stepping stones to bridge the wide Z gaps between platforms 7→8, 8→9, 9→10.
            // Each stone is halfway between the adjacent platforms; gain is 1.5 m (easy single jump).
            Plat(s, "CanyonStep7b", new Vector3( 0f, 36.5f, 300f), new Vector3(5f, 0.6f, 5.5f), p.CyanSolid);
            Plat(s, "CanyonStep8b", new Vector3( 1f, 39.5f, 317f), new Vector3(5f, 0.6f, 5.5f), p.BlueSolid);
            Plat(s, "CanyonStep9b", new Vector3(-1f, 42.5f, 335f), new Vector3(5f, 0.6f, 5.5f), p.CyanSolid);

            // Wind zone — gentle lateral push through canyon
            GameObject wz = new GameObject("CanyonWind");
            wz.transform.SetParent(s, false);
            wz.transform.position = new Vector3(0f, 38f, 285f);
            BoxCollider wzbc = wz.AddComponent<BoxCollider>();
            wzbc.isTrigger = true; wzbc.size = new Vector3(28f, 28f, 130f);
            AeroWindZone wzs = wz.AddComponent<AeroWindZone>();
            wzs.windDirection = new Vector3(0.35f, 0f, 0f);
            wzs.windStrength  = 2.5f;
            wzs.fluctuate     = true;

            // End landing platform
            Plat(s, "CanyonEnd", new Vector3(0f, 46f, 355f), new Vector3(12f, 0.7f, 9f), p.BlueSolid);
            BoxNoColl(s, "CanyonEndGlow", new Vector3(0f, 46.4f, 355f), new Vector3(12.5f, 0.15f, 9.5f), limeMat);

            Sign(s, "Warn03", "MIST ZONE AHEAD!\nVisibility is very low.\nFollow the glowing lights!", new Vector3(0f, 47.9f, 348f), Quaternion.identity, p, noPole: true);
            Checkpoint(s, "Relay 03 Canyon", new Vector3(0f, 46.4f, 358f), Quaternion.identity, p, ref cps);
            AddSeed(s, new Vector3(0f, 49f, 356f), dir, p, ref seeds);
        }

        // ─────────────────────────────────────────────
        // SECTION 4 — MIST CROSSING (z 360 → 462, y 54 → 72)
        // ─────────────────────────────────────────────
        private static void CreateSection4_MistCrossing(Transform root, AeroLevelDirector dir,
            Palette p, ref int seeds, ref int cps)
        {
            Transform s = Child(root, "Section4_MistCrossing");
            Material eMat = LoadFA("FA_EmissiveLime"); if (eMat == null) eMat = p.EmissiveLime;

            Sign(s, "MistHint", "Careful...\nThe mist hides the path.", new Vector3(-5f, 47.9f, 368f), Quaternion.Euler(0f, -30f, 0f), p, noPole: true);

            // 12 narrow platforms — start at y≈48 (S3 end), ~1.5m gain each
            (Vector3 pos, Vector3 size, bool moves, Vector3 axis, float amp, float spd)[] plats =
            {
                (new Vector3( 0f, 49f, 366f), new Vector3(5f, 0.5f, 5f),  false, Vector3.zero,    0f, 0f),
                (new Vector3( 4f, 50f, 376f), new Vector3(3f, 0.5f, 5f),  true,  Vector3.right,   3.5f, 0.9f),
                (new Vector3(-3f, 51f, 386f), new Vector3(4f, 0.5f, 4f),  false, Vector3.zero,    0f, 0f),
                (new Vector3( 5f, 53f, 395f), new Vector3(3f, 0.5f, 5f),  true,  Vector3.right,   3.0f, 1.0f),
                (new Vector3(-2f, 54f, 404f), new Vector3(5f, 0.5f, 4f),  false, Vector3.zero,    0f, 0f),
                (new Vector3( 3f, 55f, 413f), new Vector3(3f, 0.5f, 4f),  true,  Vector3.forward, 2.5f, 0.8f),
                (new Vector3(-4f, 56f, 422f), new Vector3(4f, 0.5f, 5f),  false, Vector3.zero,    0f, 0f),
                (new Vector3( 2f, 58f, 431f), new Vector3(3f, 0.5f, 4f),  true,  Vector3.right,   3.0f, 0.95f),
                (new Vector3(-2f, 59f, 440f), new Vector3(4f, 0.5f, 4f),  false, Vector3.zero,    0f, 0f),
                (new Vector3( 1f, 60f, 449f), new Vector3(3f, 0.5f, 5f),  true,  Vector3.right,   2.5f, 1.1f),
                (new Vector3( 0f, 62f, 457f), new Vector3(5f, 0.5f, 4f),  false, Vector3.zero,    0f, 0f),
                (new Vector3( 0f, 64f, 464f), new Vector3(11f, 0.5f, 9f), false, Vector3.zero,    0f, 0f),
            };

            for (int i = 0; i < plats.Length; i++)
            {
                var pl = plats[i];
                Material mat = (i % 3 == 2) ? p.CyanSolid : p.WhiteGlass;
                GameObject plt = Plat(s, "MistPlat " + (i + 1), pl.pos, pl.size, mat);

                if (pl.moves)
                {
                    PlatformMover mv = plt.AddComponent<PlatformMover>();
                    mv.moveAxis    = pl.axis;
                    mv.amplitude   = pl.amp;
                    mv.speed       = pl.spd;
                    mv.phaseOffset = i * 0.7f;
                }

                // Glowing edge so player can see platforms through mist
                BoxChild(plt.transform, "GlowEdge", new Vector3(0f, 0.25f, 0f),
                    new Vector3(pl.size.x + 0.25f, 0.1f, pl.size.z + 0.25f), eMat);

                if (i == 4 || i == 8)
                    AddSeed(s, pl.pos + new Vector3(0f, 1.5f, 0f), dir, p, ref seeds);
            }

            // Mist particle effect
            GameObject mistGO = new GameObject("MistParticles");
            mistGO.transform.SetParent(s, false);
            mistGO.transform.position = new Vector3(0f, 56f, 415f);
            ParticleSystem mps = mistGO.AddComponent<ParticleSystem>();
            var mm = mps.main;
            mm.startLifetime = 9f; mm.startSpeed = 0.15f;
            mm.startSize = new ParticleSystem.MinMaxCurve(3f, 7f);
            mm.startColor = new Color(0.87f, 0.95f, 1f, 0.10f);
            mm.maxParticles = 100;
            mm.simulationSpace = ParticleSystemSimulationSpace.World;
            var me = mps.emission; me.rateOverTime = 12f;
            var ms = mps.shape; ms.shapeType = ParticleSystemShapeType.Box; ms.scale = new Vector3(40f, 16f, 110f);
            var msr = mistGO.GetComponent<ParticleSystemRenderer>();
            msr.shadowCastingMode = ShadowCastingMode.Off;
            msr.material = MakeParticleMat(new Color(0.87f, 0.95f, 1f, 0.10f));

            Sign(s, "Warn04", "TOWER ASCENT BEGINS!\nVertical climb — no falling!\nUse dash and wall-run.", new Vector3(0f, 65.8f, 456f), Quaternion.identity, p, noPole: true);
            Checkpoint(s, "Relay 04 Mist", new Vector3(0f, 64.3f, 466f), Quaternion.identity, p, ref cps);
            AddSeed(s, new Vector3(0f, 66.5f, 465f), dir, p, ref seeds);
        }

        // ─────────────────────────────────────────────
        // SECTION 5 — DISC TOWER ASCENT (z 462 → 572, y 70 → 140)
        // ─────────────────────────────────────────────
        private static void CreateSection5_DiscTowerAscent(Transform root, AeroLevelDirector dir,
            Palette p, ref int seeds, ref int cps)
        {
            Transform s = Child(root, "Section5_DiscTowerAscent");

            Material shaftMat = LoadFA("FA_GlossyWhite");  if (shaftMat == null) shaftMat = p.AeroBuilding;
            Material eMat     = LoadFA("FA_EmissiveCyan");  if (eMat     == null) eMat     = p.EmissiveCyan;

            Plat(s, "S5Approach", new Vector3(0f, 64f, 467f), new Vector3(9f, 0.5f, 6f), p.BlueSolid);
            Sign(s, "AscentHint", "NOW WE GO UP!\nAscend the Towers!", new Vector3(5f, 65.8f, 476f), Quaternion.Euler(0f, 30f, 0f), p, noPole: true);

            // 7 disc towers — ascending from y~64 (S4 end), ~2m gain each
            // disc top = shaftH + 0.4  (discThick=0.8, discThick*0.5=0.4)
            (Vector3 pos, float shaftH, float discR, bool moves, Vector3 mAxis, float mAmp, float mSpd)[] towers =
            {
                (new Vector3( 0f, 0f, 478f), 65.6f, 5.0f, false, Vector3.zero,    0f,   0f),   // top≈66  +2
                (new Vector3( 6f, 0f, 494f), 67.6f, 4.5f, true,  Vector3.right,   3.0f, 0.80f),// top≈68  +2
                (new Vector3(-5f, 0f, 509f), 69.6f, 5.0f, false, Vector3.zero,    0f,   0f),   // top≈70  +2
                (new Vector3( 5f, 0f, 523f), 71.6f, 4.5f, true,  Vector3.forward, 3.5f, 0.75f),// top≈72  +2
                (new Vector3(-4f, 0f, 537f), 73.6f, 5.0f, false, Vector3.zero,    0f,   0f),   // top≈74  +2
                (new Vector3( 3f, 0f, 550f), 75.6f, 4.5f, true,  Vector3.right,   2.5f, 0.90f),// top≈76  +2
                (new Vector3( 0f, 0f, 564f), 78.6f, 6.5f, false, Vector3.zero,    0f,   0f),   // top≈79  +3 (double jump finale)
            };

            for (int i = 0; i < towers.Length; i++)
            {
                var t = towers[i];
                Material dm = (i % 2 == 0) ? p.BlueSolid : p.CyanSolid;
                GameObject disc = CreateDiscTower(s, "AscentTower " + (i + 1), t.pos, t.shaftH, t.discR, 0.8f, shaftMat, dm);

                // Emissive edge cylinder
                GameObject edge = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                edge.name = "Edge";
                edge.transform.SetParent(disc.transform, false);
                edge.transform.localPosition = new Vector3(0f, 1f, 0f);
                edge.transform.localScale    = new Vector3((t.discR * 2f + 0.7f) / (t.discR * 2f), 0.1f / 0.8f, (t.discR * 2f + 0.7f) / (t.discR * 2f));
                AssignMat(edge, eMat);
                DestroyCollider(edge);

                if (t.moves)
                {
                    PlatformMover pm = disc.AddComponent<PlatformMover>();
                    pm.moveAxis    = t.mAxis;
                    pm.amplitude   = t.mAmp;
                    pm.speed       = t.mSpd;
                    pm.phaseOffset = i * 0.6f;
                }

                SpawnPointLight(disc.transform, "Glow", new Vector3(0f, 2.5f, 0f),
                    new Color(0f, 0.72f + i * 0.04f, 1f), 1.5f, 22f);

                if (i % 2 == 0)
                    AddSeed(s, t.pos + new Vector3(0f, t.shaftH + 2.5f, 0f), dir, p, ref seeds);

                // Skip the last disc (has bounce pad) — all others fade after 3 s
                if (i < towers.Length - 1)
                    disc.AddComponent<AeroDiscFade>();
            }

            // Bounce pad on the final disc to launch player into section 6
            BounceAt(s, "AscentBoost", new Vector3(0f, 79.5f, 569f), p, new Vector3(0f, 18f, 6f));

            Sign(s, "Warn05", "HIGH SPRINT ZONE AHEAD!\nSPRINT through speed gates!\nMaximum speed required!", new Vector3(0f, 80.5f, 557f), Quaternion.identity, p, noPole: true);
            Checkpoint(s, "Relay 05 Ascent", new Vector3(0f, 79f, 567f), Quaternion.identity, p, ref cps);
            AddSeed(s, new Vector3(0f, 81.5f, 567f), dir, p, ref seeds);
        }

        // ─────────────────────────────────────────────
        // SECTION 6 — HIGH PLATFORM RUN (z 572 → 667, y 138 → 162)
        // ─────────────────────────────────────────────
        private static void CreateSection6_HighPlatformRun(Transform root, AeroLevelDirector dir,
            Palette p, ref int seeds, ref int cps)
        {
            Transform s = Child(root, "Section6_HighPlatformRun");
            Material platMat = LoadFA("FA_Platform");      if (platMat == null) platMat = p.WhiteGlass;
            Material eMat    = LoadFA("FA_EmissiveCyan");  if (eMat    == null) eMat    = p.EmissiveCyan;

            Sign(s, "RunHint", "SPRINT AHEAD!\nUse the Speed Gates!", new Vector3(-5f, 80.5f, 577f), Quaternion.Euler(0f, -30f, 0f), p, noPole: true);

            // Platforms start at y≈79 (S5 end), 3 m gain each — every step double-jumpable.
            // Old values had 6–7 m gaps (impossible). Fixed to uniform +3 m progression.
            float[] rz  = { 578f, 592f, 607f, 622f, 636f, 649f, 661f };
            float[] ry  = {  79f,  82f,  85f,  88f,  91f,  94f,  97f };
            float[] rx  = {   0f,   4f,  -3f,   5f,  -4f,   2f,   0f };
            float[] rw  = {  14f,  12f,  12f,  11f,  12f,  11f,  16f };
            bool[]  rmv = { false, false, true, false, true, false, false };

            for (int i = 0; i < rz.Length; i++)
            {
                Material mat = (i % 3 == 0) ? p.BlueSolid : (i % 3 == 1 ? p.CyanSolid : platMat);
                GameObject plt = Plat(s, "HighRun " + (i + 1), new Vector3(rx[i], ry[i], rz[i]),
                    new Vector3(rw[i], 0.6f, 12f), mat);

                if (rmv[i])
                {
                    PlatformMover mv = plt.AddComponent<PlatformMover>();
                    mv.moveAxis    = Vector3.right;
                    mv.amplitude   = 4.5f;
                    mv.speed       = 0.75f;
                    mv.phaseOffset = i * 0.5f;
                }

                // Emissive edge trim
                BoxChild(plt.transform, "RunTrim", new Vector3(0f, 0.3f, 0f),
                    new Vector3(rw[i] + 0.35f, 0.12f, 12.35f), eMat);

                if (i == 1)
                    SpeedGate(s, "SpeedGate1", new Vector3(rx[i], ry[i] + 0.3f, rz[i] + 2f),
                        Quaternion.identity, p, new Vector3(0f, 0f, 14f));
                if (i == 4)
                    SpeedGate(s, "SpeedGate2", new Vector3(rx[i], ry[i] + 0.3f, rz[i] + 2f),
                        Quaternion.identity, p, new Vector3(0f, 0f, 14f));

                if (i == 2 || i == 5)
                    AddSeed(s, new Vector3(rx[i], ry[i] + 1.8f, rz[i]), dir, p, ref seeds);
            }

            // Crosswind on the exposed high section
            GameObject wz = new GameObject("HighWind");
            wz.transform.SetParent(s, false);
            wz.transform.position = new Vector3(0f, 89f, 636f);
            BoxCollider wzbc = wz.AddComponent<BoxCollider>();
            wzbc.isTrigger = true; wzbc.size = new Vector3(30f, 20f, 60f);
            AeroWindZone wzs = wz.AddComponent<AeroWindZone>();
            wzs.windDirection = new Vector3(0.5f, 0f, 0f);
            wzs.windStrength  = 3.0f;
            wzs.fluctuate     = true;

            // Decorative chrome spheres visible at altitude
            Material chromeMat = LoadFA("FA_Chrome"); if (chromeMat == null) chromeMat = p.Chrome;
            for (int i = 0; i < 5; i++)
                MakeSphere(s, "HighSphere" + i, new Vector3(-18f + i * 9f, 86f + i * 2f, 612f + i * 6f),
                    1.5f + i * 0.4f, chromeMat, false);

            Sign(s, "Warn06", "BUBBLE FINALE AHEAD!\nJump on floating bubbles.\nOne hop at a time — don't rush!", new Vector3(0f, 99.1f, 653f), Quaternion.identity, p, noPole: true);
            Checkpoint(s, "Relay 06 HighRun", new Vector3(0f, 97.6f, 663f), Quaternion.identity, p, ref cps);
            AddSeed(s, new Vector3(0f, 100f, 663f), dir, p, ref seeds);
        }

        // ─────────────────────────────────────────────
        // SECTION 7 — BUBBLE JUMP FINALE (z 667 → 765, y 159 → 272)
        // ─────────────────────────────────────────────
        private static void CreateSection7_BubbleJumpFinale(Transform root, Transform vfxRoot,
            AeroLevelDirector dir, Palette p, ref int seeds, ref int cps)
        {
            Transform s = Child(root, "Section7_BubbleJumpFinale");

            Material goldMat  = LoadFA("FA_GoldTrim");    if (goldMat  == null) goldMat  = p.Finish;
            Material eMat     = LoadFA("FA_EmissiveCyan"); if (eMat     == null) eMat     = p.EmissiveCyan;
            Material whiteMat = LoadFA("FA_GlossyWhite"); if (whiteMat == null) whiteMat = p.AeroBuilding;

            Sign(s, "FinaleHint", "THE SUMMIT IS NEAR!\nJump through the Bubbles!", new Vector3(5f, 99.1f, 673f), Quaternion.Euler(0f, 30f, 0f), p, noPole: true);

            // 11 bubble disc platforms — start at y=99 (2.3 m above S6 end, single-jumpable).
            // Shifted down 2 m from original so S6→S7 transition is not impossible.
            float[] bz = { 674f, 684f, 694f, 704f, 714f, 723f, 732f, 740f, 748f, 755f, 761f };
            float[] by = {  99f, 102f, 105f, 108f, 111f, 114f, 117f, 120f, 123f, 126f, 129f };
            float[] bx = {   0f,   4f,  -4f,   5f,  -5f,   3f,  -3f,   4f,  -2f,   1f,   0f };
            float[] br = { 4.5f, 4.0f, 4.5f, 4.0f, 5.0f, 4.0f, 4.5f, 4.0f, 4.5f, 4.0f, 5.5f };

            for (int i = 0; i < bz.Length; i++)
            {
                // Flat cylinder = bubble disc platform
                GameObject bub = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                bub.name = "BubblePlat " + (i + 1);
                bub.transform.SetParent(s, false);
                bub.transform.position   = new Vector3(bx[i], by[i], bz[i]);
                bub.transform.localScale = new Vector3(br[i] * 2f, 0.6f, br[i] * 2f);

                Material bm = (i % 3 == 0) ? p.BlueSolid : (i % 3 == 1 ? p.CyanSolid : p.Bubble);
                AssignMat(bub, bm);

                // Same CapsuleCollider sphere-degeneration fix as disc towers
                Object.DestroyImmediate(bub.GetComponent<Collider>());
                BoxCollider bubBox = bub.AddComponent<BoxCollider>();
                bubBox.center = Vector3.zero;
                bubBox.size   = new Vector3(1f, 2f, 1f);

                // Decorative translucent bubble sphere on top (visual only, parented to section for correct world pos)
                MakeSphere(s, "BubVis_" + i, new Vector3(bx[i], by[i] + 1.5f, bz[i]), br[i] * 1.1f, p.Bubble, false);

                // Emissive glow cylinder slightly larger than platform
                GameObject glowCyl = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                glowCyl.name = "BubGlow";
                glowCyl.transform.SetParent(bub.transform, false);
                glowCyl.transform.localPosition = new Vector3(0f, 0f, 0f);
                glowCyl.transform.localScale    = new Vector3((br[i] * 2f + 0.6f) / (br[i] * 2f), 0.12f / 0.6f, (br[i] * 2f + 0.6f) / (br[i] * 2f));
                AssignMat(glowCyl, eMat);
                DestroyCollider(glowCyl);

                SpawnPointLight(bub.transform, "BubLight", new Vector3(0f, 2f, 0f),
                    new Color(0.15f + i * 0.08f, 0.8f, 1f), 1.3f, 20f);

                if (i % 3 == 0)
                    AddSeed(s, new Vector3(bx[i], by[i] + 1.5f, bz[i]), dir, p, ref seeds);
            }

            // ── FINISH PLATFORM ──
            // Steps approach from the side so the player never needs to jump through a ceiling.
            // Bubble10 top=129.3 → Step1 top=130.8 (+1.5m) → Step2 top=132.8 (+2m) → Finish top=134.3 (+1.5m)
            // All gains ≤ 2m (single-jump max). Side approach = no ceiling collision.
            const float finishY = 134f;
            const float finishZ = 782f;

            // Steps are south of the platform footprint (finish south edge = 782-7 = 775)
            Plat(s, "SummitStep1", new Vector3(0f, 130.5f, 760f), new Vector3(6f, 0.6f, 6f), p.CyanSolid);
            Plat(s, "SummitStep2", new Vector3(0f, 132.5f, 768f), new Vector3(6f, 0.6f, 6f), p.BlueSolid);

            Plat(s, "FinishPlatform", new Vector3(0f, finishY, finishZ), new Vector3(14f, 0.6f, 14f), whiteMat);

            // Gold edge trim — flush with platform top surface (platform is 14x14)
            BoxChild(s, "GoldN", new Vector3(  0f,  finishY + 0.32f, finishZ + 7.3f), new Vector3(14.6f, 0.25f, 0.4f), goldMat);
            BoxChild(s, "GoldS", new Vector3(  0f,  finishY + 0.32f, finishZ - 7.3f), new Vector3(14.6f, 0.25f, 0.4f), goldMat);
            BoxChild(s, "GoldE", new Vector3( 7.3f, finishY + 0.32f, finishZ),         new Vector3(0.4f,  0.25f, 14.6f), goldMat);
            BoxChild(s, "GoldW", new Vector3(-7.3f, finishY + 0.32f, finishZ),         new Vector3(0.4f,  0.25f, 14.6f), goldMat);

            SpawnPointLight(s, "FinishGold", new Vector3(  0f, finishY + 12f, finishZ), new Color(1f,   0.9f, 0.3f), 6f, 100f);
            SpawnPointLight(s, "FinishCyan", new Vector3(  0f, finishY +  7f, finishZ), new Color(0f,   1f,   0.9f), 4f,  80f);
            SpawnPointLight(s, "FinishLime", new Vector3(  0f, finishY +  4f, finishZ), new Color(0.5f, 1f,   0.3f), 3f,  60f);

            // Firework particle bursts
            float[] fwx = {-6f, 6f, -4f, 4f};
            float[] fwzOff = {-6f, -5f, 6f, 5f};
            for (int i = 0; i < 4; i++)
            {
                GameObject fw = new GameObject("Firework" + i);
                fw.transform.SetParent(vfxRoot, false);
                fw.transform.position = new Vector3(fwx[i], finishY + 5f, finishZ + fwzOff[i]);
                ParticleSystem fps = fw.AddComponent<ParticleSystem>();
                var fm = fps.main;
                fm.startLifetime = 3f;
                fm.startSpeed    = new ParticleSystem.MinMaxCurve(10f, 22f);
                fm.startSize     = new ParticleSystem.MinMaxCurve(0.1f, 0.5f);
                fm.startColor    = new ParticleSystem.MinMaxGradient(new Color(0f, 1f, 1f), new Color(0.8f, 1f, 0.2f));
                fm.maxParticles  = 600;
                var fpsEmit = fps.emission;
                fpsEmit.rateOverTime = 0f;
                fpsEmit.SetBursts(new[] { new ParticleSystem.Burst(0f, 600) });
                var fsh = fps.shape; fsh.shapeType = ParticleSystemShapeType.Sphere; fsh.radius = 0.5f;
                var fwr = fw.GetComponent<ParticleSystemRenderer>();
                fwr.shadowCastingMode = ShadowCastingMode.Off;
                fwr.material = MakeParticleMat(new Color(0f, 1f, 1f, 0.9f));
                fps.Stop();
            }

            // Finish trigger
            GameObject finTrig = new GameObject("FinishTrigger");
            finTrig.transform.SetParent(s, false);
            finTrig.transform.position = new Vector3(0f, finishY + 3f, finishZ);
            BoxCollider ftc = finTrig.AddComponent<BoxCollider>();
            ftc.isTrigger = true; ftc.size = new Vector3(14f, 6f, 14f);
            finTrig.AddComponent<AeroFinishGate>();

            Sign(s, "Victory", "YOU REACHED\nTHE SUMMIT!\nAeroBloom Complete!", new Vector3(0f, finishY + 5f, finishZ - 11f), Quaternion.identity, p);

        }

        // ─────────────────────────────────────────────
        // DISC TOWER HELPER
        // Creates a thin shaft + wide flat disc on top. Returns the disc (the collideable platform).
        // ─────────────────────────────────────────────
        private static GameObject CreateDiscTower(Transform parent, string name,
            Vector3 basePos, float shaftHeight, float discRadius, float discThick,
            Material shaftMat, Material discMat)
        {
            GameObject tower = new GameObject(name);
            tower.transform.SetParent(parent, false);
            tower.transform.position = basePos;

            // Shaft: Cylinder — localScale.y = halfHeight, localScale.x = diameter
            GameObject shaft = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            shaft.name = "Shaft";
            shaft.transform.SetParent(tower.transform, false);
            shaft.transform.localPosition = new Vector3(0f, shaftHeight * 0.5f, 0f);
            shaft.transform.localScale    = new Vector3(0.8f, shaftHeight * 0.5f, 0.8f);
            AssignMat(shaft, shaftMat);
            DestroyCollider(shaft);

            // Disc: flat wide cylinder — the actual platform
            GameObject disc = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            disc.name = "Disc";
            disc.transform.SetParent(tower.transform, false);
            disc.transform.localPosition = new Vector3(0f, shaftHeight, 0f);
            disc.transform.localScale    = new Vector3(discRadius * 2f, discThick * 0.5f, discRadius * 2f);
            AssignMat(disc, discMat);

            // CapsuleCollider on non-uniformly scaled cylinders degenerates to a sphere in PhysX,
            // making the player stand discRadius metres above the visual surface. Replace with
            // a flat BoxCollider that matches the cylinder's local extents exactly.
            Object.DestroyImmediate(disc.GetComponent<Collider>());
            BoxCollider discBox = disc.AddComponent<BoxCollider>();
            discBox.center = Vector3.zero;
            discBox.size   = new Vector3(1f, 2f, 1f);  // cylinder local: X/Z ±0.5, Y ±1

            return disc;
        }

        // ─────────────────────────────────────────────
        // PLAYER CREATION
        // ─────────────────────────────────────────────
        private static AeroPlayerController CreatePlayer(Transform parent, Vector3 pos, Quaternion rot, AeroPackConfig pack)
        {
            GameObject go = new GameObject("Player_AeroRunner");
            go.transform.SetParent(parent, false);
            go.transform.SetPositionAndRotation(pos, rot);
            go.tag = "Player";

            CharacterController cc = go.AddComponent<CharacterController>();
            cc.height = 1.8f; cc.radius = 0.35f;
            cc.center = new Vector3(0f, 0.9f, 0f);
            cc.skinWidth = 0.02f;
            cc.stepOffset = 0.45f; cc.slopeLimit = 58f;

            AeroPlayerController player = go.AddComponent<AeroPlayerController>();
            player.fallRespawnY = -3f;

            if (pack != null && pack.msnBuddyPrefab != null)
            {
                GameObject body = Object.Instantiate(pack.msnBuddyPrefab);
                body.name = "MsnBuddy Body";
                body.transform.SetParent(go.transform, false);
                body.transform.localPosition = Vector3.zero;
                body.transform.localRotation = Quaternion.identity;
                body.transform.localScale    = Vector3.one;
                DestroyCollidersRecursive(body);

                // Search entire hierarchy — prefab Animator may be on a child (e.g. Armature)
                Animator anim = body.GetComponentInChildren<Animator>(true);
                if (anim == null) anim = body.AddComponent<Animator>();
                if (pack.msnBuddyAnimator != null)
                    anim.runtimeAnimatorController = pack.msnBuddyAnimator;

                // AnimationEvents fire on the same GameObject as the Animator
                if (anim.gameObject.GetComponent<AeroAnimEvents>() == null)
                    anim.gameObject.AddComponent<AeroAnimEvents>();

                player.bodyTransform = body.transform;
                player.bodyAnimator  = anim;
            }

            if (pack != null && pack.footstepParticlesPrefab != null)
            {
                GameObject fp = Object.Instantiate(pack.footstepParticlesPrefab);
                fp.name = "Footstep FX";
                fp.transform.SetParent(go.transform, false);
                fp.transform.localPosition = Vector3.zero;
            }

            // SMAA anti-aliasing + post-processing on the game camera
            if (player.playerCamera != null)
            {
                var camData = player.playerCamera.gameObject.GetComponent<UniversalAdditionalCameraData>();
                if (camData == null)
                    camData = player.playerCamera.gameObject.AddComponent<UniversalAdditionalCameraData>();
                camData.antialiasing        = AntialiasingMode.SubpixelMorphologicalAntiAliasing;
                camData.antialiasingQuality = AntialiasingQuality.High;
                camData.renderPostProcessing = true;
            }

            return player;
        }

        // ─────────────────────────────────────────────
        // PLATFORM / SCENE HELPERS
        // ─────────────────────────────────────────────
        private static GameObject Plat(Transform parent, string name, Vector3 pos, Vector3 scale, Material mat)
            => Plat(parent, name, pos, scale, Quaternion.identity, mat);

        private static GameObject Plat(Transform parent, string name, Vector3 pos, Vector3 scale, Quaternion rot, Material mat)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.SetPositionAndRotation(pos, rot);
            go.transform.localScale = scale;
            AssignMat(go, mat);
            return go;
        }

        private static void BoxNoColl(Transform parent, string name, Vector3 pos, Vector3 scale, Material mat)
        {
            var go = Plat(parent, name, pos, scale, Quaternion.identity, mat);
            DestroyCollider(go);
        }

        private static GameObject MakeSphere(Transform parent, string name, Vector3 pos, float radius, Material mat, bool keepCol)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.position   = pos;
            go.transform.localScale = Vector3.one * radius;
            AssignMat(go, mat);
            if (!keepCol) DestroyCollider(go);
            return go;
        }

        private static void BoxChild(Transform parent, string name, Vector3 localPos, Vector3 localScale, Material mat)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localRotation = Quaternion.identity;
            go.transform.localScale    = localScale;
            AssignMat(go, mat);
            DestroyCollider(go);
        }

        private static void Checkpoint(Transform root, string name, Vector3 pos, Quaternion rot, Palette p, ref int count)
        {
            count++;
            GameObject cp = new GameObject(name);
            cp.transform.SetParent(root, false);
            cp.transform.SetPositionAndRotation(pos, rot);

            BoxCollider col = cp.AddComponent<BoxCollider>();
            col.isTrigger = true; col.center = new Vector3(0f, 1.45f, 0f); col.size = new Vector3(6.4f, 2.9f, 0.9f);

            Transform respawn = new GameObject("Respawn").transform;
            respawn.SetParent(cp.transform, false);
            respawn.localPosition = new Vector3(0f, 0.22f, -2.7f);

            BoxChild(cp.transform, "PillarL", new Vector3(-3f, 1.35f, 0f), new Vector3(0.28f, 2.7f, 0.28f), p.RelayInactive);
            BoxChild(cp.transform, "PillarR", new Vector3( 3f, 1.35f, 0f), new Vector3(0.28f, 2.7f, 0.28f), p.RelayInactive);
            BoxChild(cp.transform, "Bridge",  new Vector3( 0f, 2.70f, 0f), new Vector3(6.3f,  0.28f, 0.28f), p.RelayInactive);

            AeroCheckpoint ac = cp.AddComponent<AeroCheckpoint>();
            ac.checkpointName   = name;
            ac.respawnPoint     = respawn;
            ac.inactiveMaterial = p.RelayInactive;
            ac.activeMaterial   = p.RelayActive;
        }

        private static void AddSeed(Transform root, Vector3 worldPos, AeroLevelDirector dir, Palette p, ref int count)
        {
            count++;
            GameObject seed = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            seed.name = "AeroSeed " + count;
            seed.transform.SetParent(root, false);
            seed.transform.position   = worldPos;
            seed.transform.localScale = Vector3.one * 0.72f;
            AssignMat(seed, p.Seed);
            SphereCollider sc = seed.GetComponent<SphereCollider>();
            sc.isTrigger = true;
            sc.radius = 1.2f;

            AeroCollectible col = seed.AddComponent<AeroCollectible>();
            col.director = dir;

            Light lt = seed.AddComponent<Light>();
            lt.type = LightType.Point; lt.range = 4f;
            lt.intensity = 0.8f; lt.color = new Color(0.68f, 1f, 0.9f);
        }

        private static void Sign(Transform root, string name, string text, Vector3 pos, Quaternion rot, Palette p, bool noPole = false)
        {
            var back = Plat(root, name + "_Back", pos, new Vector3(5.6f, 2.4f, 0.14f), rot, p.SignBack);
            DestroyCollider(back);

            if (!noPole)
            {
                var pole = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                pole.name = name + "_Pole";
                pole.transform.SetParent(root, false);
                pole.transform.SetPositionAndRotation(pos + rot * new Vector3(0f, -2.7f, 0f), rot);
                pole.transform.localScale = new Vector3(0.12f, 1.5f, 0.12f);
                AssignMat(pole, p.Chrome);
                DestroyCollider(pole);
            }

            // World-space Canvas: 100 canvas units = 1 world metre (scale 0.01)
            GameObject canvasGO = new GameObject(name + "_Canvas");
            canvasGO.transform.SetParent(root, false);
            canvasGO.transform.SetPositionAndRotation(pos + rot * new Vector3(0f, 0f, -0.09f), rot);
            canvasGO.transform.localScale = Vector3.one * 0.01f;

            Canvas canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;

            RectTransform crt = canvasGO.GetComponent<RectTransform>();
            crt.sizeDelta = new Vector2(520f, 210f);   // 5.20 m × 2.10 m

            GameObject textGO = new GameObject("TMP");
            textGO.transform.SetParent(canvasGO.transform, false);

            TextMeshProUGUI tmp = textGO.AddComponent<TextMeshProUGUI>();
            RectTransform trt   = tmp.rectTransform;
            trt.anchorMin        = Vector2.zero;
            trt.anchorMax        = Vector2.one;
            trt.sizeDelta        = Vector2.zero;
            trt.anchoredPosition = Vector2.zero;

            tmp.text             = text;
            tmp.alignment        = TextAlignmentOptions.Center;
            tmp.color            = new Color(0.02f, 0.16f, 0.30f);
            tmp.fontStyle        = FontStyles.Bold;
            tmp.enableAutoSizing = true;
            tmp.fontSizeMin      = 10f;
            tmp.fontSizeMax      = 90f;
        }

        private static void SpeedGate(Transform root, string name, Vector3 pos, Quaternion rot, Palette p, Vector3 impulse)
        {
            GameObject gate = new GameObject(name);
            gate.transform.SetParent(root, false);
            gate.transform.SetPositionAndRotation(pos, rot);
            BoxCollider bc = gate.AddComponent<BoxCollider>();
            bc.isTrigger = true; bc.center = new Vector3(0f, 1.2f, 0f); bc.size = new Vector3(5f, 2.4f, 0.8f);
            gate.AddComponent<AeroSpeedGate>().localImpulse = impulse;
            BoxChild(gate.transform, "L", new Vector3(-2.3f, 1.2f, 0), new Vector3(0.15f, 2.4f, 0.15f), p.CyanSolid);
            BoxChild(gate.transform, "R", new Vector3( 2.3f, 1.2f, 0), new Vector3(0.15f, 2.4f, 0.15f), p.CyanSolid);
            BoxChild(gate.transform, "T", new Vector3( 0f,   2.3f, 0), new Vector3(4.8f,  0.15f, 0.15f), p.CyanSolid);
        }



        private static void BounceAt(Transform root, string name, Vector3 pos, Palette p, Vector3 launch)
        {
            GameObject pad = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            pad.name = name;
            pad.transform.SetParent(root, false);
            pad.transform.position   = pos;
            pad.transform.localScale = new Vector3(1.6f, 0.08f, 1.6f);
            AssignMat(pad, p.Pad);
            pad.AddComponent<AeroBouncer>().localLaunchVelocity = launch;
        }

        private static void SpawnPointLight(Transform parent, string name, Vector3 pos, Color color, float intensity, float range)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            Light l = go.AddComponent<Light>();
            l.type = LightType.Point; l.color = color;
            l.intensity = intensity; l.range = range;
            l.shadows = LightShadows.None;
        }

        private static Transform Child(Transform parent, string name)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            return go.transform;
        }

        private static Material LoadFA(string name)
        {
            Material m = Resources.Load<Material>("Materials/" + name);
            return m != null ? m : null;
        }

        private static void AssignMat(GameObject go, Material mat)
        {
            if (mat == null) return;
            Renderer r = go.GetComponent<Renderer>();
            if (r != null) r.sharedMaterial = mat;
        }

        private static float RF(System.Random r, float range) => (float)(r.NextDouble() * range);

        private static Material MakeParticleMat(Color col)
        {
            Shader sh = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            if (sh == null) sh = Shader.Find("Universal Render Pipeline/Unlit");
            if (sh == null) sh = Shader.Find("Unlit/Color");
            Material m = new Material(sh != null ? sh : Shader.Find("Standard")) { name = "ParticleMat" };
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", col); else m.color = col;
            if (m.HasProperty("_Surface")) m.SetFloat("_Surface", 1f);
            m.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
            m.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
            m.SetInt("_ZWrite",  0);
            m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            m.renderQueue = (int)RenderQueue.Transparent;
            return m;
        }

        // ─────────────────────────────────────────────
        // SCENE CLEANUP
        // ─────────────────────────────────────────────
        private static void RemoveTemplateSceneObjects(Transform newRoot)
        {
            foreach (Camera cam in Object.FindObjectsByType<Camera>(FindObjectsSortMode.None))
                if (!cam.transform.IsChildOf(newRoot)) DestroySmart(cam.gameObject);
            foreach (string n in new[] { "Directional Light", "Global Volume", "Terrain" })
            {
                GameObject t = GameObject.Find(n);
                if (t != null && !t.transform.IsChildOf(newRoot)) DestroySmart(t);
            }
        }

        private static void DestroyNamedRoot(string n)
        {
            GameObject go = GameObject.Find(n);
            if (go != null) DestroySmart(go);
        }

        private static void DestroyCollider(GameObject go)
        {
            Collider c = go.GetComponent<Collider>();
            if (c != null) Object.DestroyImmediate(c);
        }

        private static void DestroyCollidersRecursive(GameObject go)
        {
            foreach (Collider c in go.GetComponentsInChildren<Collider>())
                Object.DestroyImmediate(c);
        }

        private static void DestroySmart(Object o)
        {
            if (o == null) return;
            if (Application.isPlaying) Object.Destroy(o);
            else Object.DestroyImmediate(o);
        }

        // ─────────────────────────────────────────────
        // PALETTE
        // ─────────────────────────────────────────────
        private sealed class Palette
        {
            public Material Glass, WhiteGlass, CyanGlass, LimeGlass, WallGlass;
            public Material Water, Grass, Seed, Pad, RelayInactive, RelayActive, Finish;
            public Material CyanSolid, BlueSolid, LimeSolid, GlobeBlue, Bubble, SignBack;
            public Material AeroBuilding, AeroBuildingWindow;
            public Material Chrome, EmissiveCyan, EmissiveLime, Ground;

            public static Palette Create()
            {
                var p = new Palette();
                p.Glass         = M("Aero Glass",    new Color(0.35f, 0.82f, 1f,   0.90f), 0.04f, 0.97f, true,  new Color(0f,    0.42f, 0.82f));
                p.WhiteGlass    = M("Pearl Glass",   new Color(0.96f, 1f,   1f,   0.94f), 0.02f, 0.98f, true,  new Color(0.08f, 0.24f, 0.32f));
                p.CyanGlass     = M("Cyan Glass",    new Color(0.08f, 0.80f, 1f,  0.92f), 0.04f, 0.97f, true,  new Color(0f,    0.62f, 1.1f));
                p.LimeGlass     = M("Lime Glass",    new Color(0.48f, 1f,   0.52f,0.92f), 0.02f, 0.95f, true,  new Color(0.14f, 0.75f, 0.24f));
                p.WallGlass     = M("Wall Glass",    new Color(0.35f, 0.90f, 1f,  0.78f), 0.02f, 0.99f, true,  new Color(0f,    0.38f, 0.68f));
                p.Water         = M("Water",         new Color(0.18f, 0.62f, 0.88f,0.30f),0.02f, 1f,   true,  new Color(0f,    0.12f, 0.24f));
                p.Grass         = M("City Floor",     new Color(0.06f, 0.11f, 0.22f, 1f),  0.22f, 0.84f, false, new Color(0f,    0.02f, 0.08f));
                p.Ground        = M("Ground",        new Color(0.78f, 0.96f, 1f,  1f),    0.1f,  0.7f,  false, Color.black);
                p.Seed          = M("Aero Seed",     new Color(0.84f, 1f,   0.92f,0.96f), 0f,    0.98f, true,  new Color(0.55f, 3.5f,  2.0f));
                p.Pad           = M("Bounce Pad",    new Color(0.82f, 1f,   1f,  0.92f),  0.02f, 0.98f, true,  new Color(0.20f, 1.4f,  1.8f));
                p.RelayInactive = M("Relay Off",     new Color(0.78f, 0.94f, 1f,  0.62f), 0.08f, 0.92f, true,  new Color(0f,    0.18f, 0.32f));
                p.RelayActive   = M("Relay On",      new Color(0.68f, 1f,   0.80f,0.90f), 0.02f, 0.98f, true,  new Color(0.42f, 2.8f,  1.0f));
                p.Finish        = M("Bloom Finish",  new Color(1f,    0.9f, 0.4f, 0.94f), 0.5f,  0.92f, false, new Color(1.2f,  0.85f, 0.12f));
                // Glass platform materials — Frutiger Aero aqua glass
                p.CyanSolid     = M("Cyan Glass Plat", new Color(0.10f, 0.82f, 1f,  0.72f), 0.12f, 0.97f, true,  new Color(0f,    0.70f, 1.5f));
                p.BlueSolid     = M("Blue Glass Plat", new Color(0.04f, 0.54f, 1f,  0.68f), 0.10f, 0.96f, true,  new Color(0f,    0.38f, 1.2f));
                p.LimeSolid     = M("Lime Solid",    new Color(0.32f, 0.95f, 0.36f,1f),   0.06f, 0.78f, false, new Color(0.08f, 0.68f, 0.12f));
                p.GlobeBlue     = M("Globe Blue",    new Color(0.10f, 0.48f, 1f,  0.88f), 0.06f, 0.94f, true,  new Color(0f,    0.28f, 0.80f));
                p.Bubble        = M("Bubble",        new Color(0.88f, 1f,   1f,  0.22f),  0.04f, 1f,    true,  new Color(0f,    0.14f, 0.22f));
                p.SignBack      = M("Sign Back",     new Color(0.95f, 1f,   1f,  0.82f),  0.04f, 0.96f, true,  new Color(0.08f, 0.32f, 0.45f));
                p.AeroBuilding  = M("Bldg Body",     new Color(0.58f, 0.78f, 1f,  1f),    0.15f, 0.93f, false, new Color(0.02f, 0.14f, 0.82f));
                p.AeroBuildingWindow = M("Bldg Win", new Color(0.08f, 0.58f, 1f,  0.64f), 0.04f, 1.00f, true,  new Color(0.04f, 0.72f, 2.00f));
                p.Chrome        = M("Chrome",        new Color(0.80f, 0.84f, 0.90f,1f),   1f,    1f,    false, Color.black);
                p.EmissiveCyan  = M("Emissive Cyan", new Color(0f,    1f,   1f,  1f),     0f,    0.92f, false, new Color(0f,    3.2f,  3.5f));
                p.EmissiveLime  = M("Emissive Lime", new Color(0.72f, 1f,   0.72f,1f),    0f,    0.92f, false, new Color(0.65f, 2.5f,  0.65f));
                return p;
            }

            private static Material M(string name, Color col, float met, float smo, bool trans, Color emi)
            {
                Shader sh = Shader.Find("Universal Render Pipeline/Lit");
                if (sh == null) sh = Shader.Find("Standard");
                Material m = new Material(sh) { name = name };
                if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", col); else m.color = col;
                if (m.HasProperty("_Metallic"))   m.SetFloat("_Metallic",   met);
                if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", smo);
                if (emi.maxColorComponent > 0.01f && m.HasProperty("_EmissionColor"))
                { m.EnableKeyword("_EMISSION"); m.SetColor("_EmissionColor", emi); }
                if (trans)
                {
                    if (m.HasProperty("_Surface")) m.SetFloat("_Surface", 1f);
                    if (m.HasProperty("_Blend"))   m.SetFloat("_Blend",   0f);
                    m.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
                    m.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
                    m.SetInt("_ZWrite", 0);
                    m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                    m.DisableKeyword("_ALPHATEST_ON");
                    m.renderQueue = (int)RenderQueue.Transparent;
                }
                return m;
            }
        }
    }
}
