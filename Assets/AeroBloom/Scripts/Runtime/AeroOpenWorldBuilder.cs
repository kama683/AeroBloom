using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using TMPro;

namespace AeroBloom
{
    /// <summary>
    /// Builds the TestSandbox open exploration meadow (rolling hills, bubbles, clouds, landmarks).
    /// </summary>
    public static class AeroOpenWorldBuilder
    {
        public const string RootName = "OpenWorld";

        private static AeroOpenWorldBuildContext s_ctx = AeroOpenWorldBuildContext.Runtime;

        public static void Build(bool forceRebuild = true, AeroOpenWorldBuildContext context = null)
        {
            s_ctx = context ?? AeroOpenWorldBuildContext.Runtime;

            if (forceRebuild)
                DestroyRoot(RootName);

            if (GameObject.Find(RootName) != null)
                return;

            AeroPackConfig pack = Resources.Load<AeroPackConfig>("AeroPackConfig");
            WorldPalette p = WorldPalette.Create(pack);

            GameObject root = new GameObject(RootName);
            Transform env      = Child(root.transform, "Environment");
            Transform landmarks = Child(root.transform, "Landmarks");
            Transform decor    = Child(root.transform, "Decor");
            Transform sky      = Child(root.transform, "Sky");

            SetupRenderAndLighting(env);
            SculptRollingHillsTerrain();
            BuildGiantClouds(sky, p);
            BuildSkyPlanet(sky, pack, p);
            BuildNatureScatter(decor, p);
            BuildFieldBubbles(decor, p);
            BuildCrystalLake(landmarks, p);
            BuildWaterfall(landmarks, p);
            BuildAquarium(landmarks, p);
            BuildFloatingIslands(sky, p);
            BuildWorldSigns(landmarks, p);
            BuildAmbientParticles(env);
            if (!s_ctx.editorScene)
            {
                CleanupSceneForExplore();
                float spawnY = SampleTerrainY(0f, 0f) + 1.2f;
                PlacePlayer(new Vector3(0f, spawnY, 0f));
                EnsurePlayerCamera();
            }
        }

        // ── Environment ──────────────────────────────────────────

        private static void SetupRenderAndLighting(Transform env)
        {
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogDensity = 0.00055f;
            RenderSettings.fogColor = new Color(0.72f, 0.90f, 1f);
            RenderSettings.ambientMode = AmbientMode.Skybox;

            Shader skyShader = Shader.Find("Skybox/Procedural");
            if (skyShader != null)
            {
                Material sky = new Material(skyShader);
                sky.SetColor("_SkyTint", new Color(0.35f, 0.72f, 1f));
                sky.SetColor("_GroundColor", new Color(0.55f, 0.95f, 0.65f));
                sky.SetFloat("_AtmosphereThickness", 0.72f);
                sky.SetFloat("_Exposure", 1.55f);
                sky.SetFloat("_SunDisk", 2f);
                sky.SetFloat("_SunSize", 0.045f);
                RenderSettings.skybox = sky;
            }

            GameObject sunGO = new GameObject("Sun");
            sunGO.transform.SetParent(env, false);
            sunGO.transform.rotation = Quaternion.Euler(48f, -35f, 0f);
            Light sun = sunGO.AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.color = new Color(1f, 0.98f, 0.92f);
            sun.intensity = 1.65f;
            sun.shadows = LightShadows.Soft;
            sun.shadowStrength = 0.72f;

            SpawnPointLight(env, "Fill", new Vector3(-40f, 60f, 30f), new Color(0.4f, 0.85f, 1f), 0.6f, 200f);
            SpawnPointLight(env, "Warm", new Vector3(90f, 40f, -50f), new Color(0.5f, 1f, 0.7f), 0.5f, 180f);

            GameObject volGO = new GameObject("Explore Post FX");
            volGO.transform.SetParent(env, false);
            Volume vol = volGO.AddComponent<Volume>();
            vol.isGlobal = true;
            vol.priority = 2f;
            VolumeProfile prof = ScriptableObject.CreateInstance<VolumeProfile>();
            vol.sharedProfile = prof;

            var bloom = prof.Add<Bloom>(true);
            bloom.threshold.Override(0.55f);
            bloom.intensity.Override(1.45f);
            bloom.scatter.Override(0.65f);
            bloom.tint.Override(new Color(0.85f, 0.96f, 1f));

            var ca = prof.Add<ColorAdjustments>(true);
            ca.postExposure.Override(0.15f);
            ca.saturation.Override(38f);
            ca.contrast.Override(8f);

            var vig = prof.Add<Vignette>(true);
            vig.intensity.Override(0.08f);
            vig.smoothness.Override(0.4f);
        }

        private static void SculptRollingHillsTerrain()
        {
            Terrain terrain = Object.FindFirstObjectByType<Terrain>();
            if (terrain == null || terrain.terrainData == null)
                return;

            TerrainData data = terrain.terrainData;
            int res = data.heightmapResolution;
            float[,] heights = data.GetHeights(0, 0, res, res);
            Vector3 size = data.size;
            terrain.transform.position = new Vector3(-size.x * 0.5f, 0f, -size.z * 0.5f);

            for (int z = 0; z < res; z++)
            {
                for (int x = 0; x < res; x++)
                {
                    float nx = x / (float)(res - 1);
                    float nz = z / (float)(res - 1);

                    float rolling = Mathf.PerlinNoise(nx * 3.2f, nz * 3.2f) * 0.09f;
                    rolling += Mathf.PerlinNoise(nx * 7.5f + 2f, nz * 7.5f) * 0.035f;
                    float lakeBowl = Mathf.Exp(-((nx - 0.58f) * (nx - 0.58f) + (nz - 0.52f) * (nz - 0.52f)) * 12f) * -0.06f;
                    float southFlat = Mathf.Exp(-((nz - 0.22f) * (nz - 0.22f)) * 10f) * 0.03f;

                    heights[z, x] = Mathf.Clamp01(0.045f + rolling + lakeBowl + southFlat);
                }
            }

            data.SetHeights(0, 0, heights);
            terrain.Flush();
        }

        private static float SampleTerrainY(float worldX, float worldZ)
        {
            Terrain terrain = Terrain.activeTerrain;
            if (terrain == null)
                return 4f;
            return terrain.SampleHeight(new Vector3(worldX, 0f, worldZ)) + terrain.transform.position.y;
        }

        // ── Sky & atmosphere ───────────────────────────────────────

        private static void BuildGiantClouds(Transform sky, WorldPalette p)
        {
            (Vector3 pos, float scale)[] clouds =
            {
                (new Vector3(-60f, 55f, 80f), 28f),
                (new Vector3(40f, 62f, 100f), 35f),
                (new Vector3(90f, 48f, 30f), 22f),
                (new Vector3(-30f, 70f, -50f), 30f),
                (new Vector3(10f, 45f, -90f), 20f),
            };

            for (int i = 0; i < clouds.Length; i++)
            {
                GameObject c = MakeSphere(sky, "Cloud" + i, clouds[i].pos, clouds[i].scale, p.Cloud, false);
                for (int j = 0; j < 3; j++)
                {
                    float a = i * 2.1f + j * 1.35f;
                    Vector3 off = new Vector3(Mathf.Sin(a) * 10f, Mathf.Cos(a * 1.2f) * 5f, Mathf.Cos(a) * 9f);
                    float s = clouds[i].scale * (0.5f + j * 0.12f);
                    MakeSphere(sky, "Cloud" + i + "_" + j, clouds[i].pos + off, s, p.Cloud, false);
                }
            }
        }

        private static void BuildSkyPlanet(Transform sky, AeroPackConfig pack, WorldPalette p)
        {
            Vector3 pos = new Vector3(75f, 95f, -60f);
            GameObject planet;
            if (pack != null && pack.earthPrefab != null)
            {
                planet = Object.Instantiate(pack.earthPrefab);
                planet.name = "SkyPlanet";
            }
            else
            {
                planet = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                AssignMat(planet, p.Globe);
                DestroyCollider(planet);
            }

            planet.transform.SetParent(sky, false);
            planet.transform.position = pos;
            planet.transform.localScale = Vector3.one * 28f;
            if (planet.GetComponent<EarthRotate>() == null)
                planet.AddComponent<EarthRotate>().rotateSpeed = 1.5f;
        }

        private static void BuildNatureScatter(Transform decor, WorldPalette p)
        {
            System.Random rng = new System.Random(12);
            for (int i = 0; i < 32; i++)
            {
                float x = (float)(rng.NextDouble() * 140 - 70);
                float z = (float)(rng.NextDouble() * 140 - 70);
                if (x * x + z * z < 36f) continue;
                float y = SampleTerrainY(x, z);
                BuildTree(decor, "Tree" + i, new Vector3(x, y, z), 2.5f + (float)rng.NextDouble() * 3f, p);
            }

            for (int i = 0; i < 40; i++)
            {
                float x = (float)(rng.NextDouble() * 130 - 65);
                float z = (float)(rng.NextDouble() * 130 - 65);
                float y = SampleTerrainY(x, z);
                float s = 0.6f + (float)rng.NextDouble() * 1.2f;
                MakeSphere(decor, "Bush" + i, new Vector3(x, y + s * 0.4f, z), s, p.Lily, false);
            }
        }

        private static void BuildTree(Transform parent, string name, Vector3 ground, float height, WorldPalette p)
        {
            Transform t = Child(parent, name);
            Cyl(t, "Trunk", ground + new Vector3(0f, height * 0.35f, 0f), new Vector3(0.5f, height * 0.35f, 0.5f), p.Wood);
            MakeSphere(t, "Leaves", ground + new Vector3(0f, height * 0.85f, 0f), height * 0.45f, p.Tree, false);
        }

        private static void BuildFieldBubbles(Transform decor, WorldPalette p)
        {
            System.Random rng = new System.Random(88);
            for (int i = 0; i < 90; i++)
            {
                float x = (float)(rng.NextDouble() * 150 - 75);
                float z = (float)(rng.NextDouble() * 150 - 75);
                float y = SampleTerrainY(x, z) + 1.5f + (float)rng.NextDouble() * 14f;
                float s = 0.6f + (float)rng.NextDouble() * 3.5f;

                GameObject bub = MakeSphere(decor, "FieldBubble" + i, new Vector3(x, y, z), s, p.Bubble, false);
                var f = bub.AddComponent<SoapBubbleFloat>();
                f.riseSpeed = 0.15f + (float)rng.NextDouble() * 0.25f;
                f.swayAmplitude = 0.5f + (float)rng.NextDouble() * 1.2f;
                f.maxHeight = y + 18f + (float)rng.NextDouble() * 12f;
            }
        }

        // ── Landmarks (walk on terrain) ────────────────────────────

        private static void BuildCrystalLake(Transform root, WorldPalette p)
        {
            Transform zone = Child(root, "CrystalLake");
            Vector3 center = new Vector3(38f, 0f, 32f);
            float waterY = SampleTerrainY(center.x, center.z) - 0.8f;

            GameObject water = GameObject.CreatePrimitive(PrimitiveType.Plane);
            water.name = "LakeWater";
            water.transform.SetParent(zone, false);
            water.transform.position = new Vector3(center.x, waterY, center.z);
            water.transform.localScale = new Vector3(18f, 1f, 14f);
            AssignMat(water, p.Water);
            DestroyCollider(water);

            float islandY = SampleTerrainY(center.x, center.z) + 0.3f;
            GameObject island = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            island.name = "LakeIsland";
            island.transform.SetParent(zone, false);
            island.transform.position = new Vector3(center.x, islandY, center.z);
            island.transform.localScale = new Vector3(10f, 0.4f, 7f);
            AssignMat(island, p.Grass);

            for (int i = 0; i < 8; i++)
            {
                float ang = i * Mathf.PI * 2f / 8f;
                float lx = center.x + Mathf.Cos(ang) * 9f;
                float lz = center.z + Mathf.Sin(ang) * 6f;
                MakeSphere(zone, "Lily" + i, new Vector3(lx, waterY + 0.5f, lz), 0.9f, p.Lily, false);
            }

            SpawnPointLight(zone, "LakeGlow", new Vector3(center.x, waterY + 8f, center.z),
                new Color(0.25f, 0.92f, 1f), 3f, 50f);
        }

        private static void BuildWaterfall(Transform root, WorldPalette p)
        {
            Transform zone = Child(root, "Waterfall");
            float bx = 48f, bz = 58f;
            float baseY = SampleTerrainY(bx, bz);
            Vector3 basePos = new Vector3(bx, baseY, bz);

            BoxNoColl(zone, "Cliff", basePos + new Vector3(0f, 8f, 0f), new Vector3(14f, 16f, 6f), p.Rock);
            BoxNoColl(zone, "CliffTop", basePos + new Vector3(0f, 18f, 2f), new Vector3(18f, 4f, 12f), p.Grass);

            GameObject falls = new GameObject("WaterfallFX");
            falls.transform.SetParent(zone, false);
            falls.transform.position = basePos + new Vector3(0f, 16f, 1f);
            ParticleSystem ps = falls.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.startLifetime = 2.2f;
            main.startSpeed = new ParticleSystem.MinMaxCurve(8f, 14f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.15f, 0.45f);
            main.startColor = new Color(0.75f, 0.95f, 1f, 0.85f);
            main.maxParticles = 1200;
            main.gravityModifier = 1.1f;
            var em = ps.emission;
            em.rateOverTime = 180f;
            var sh = ps.shape;
            sh.shapeType = ParticleSystemShapeType.Box;
            sh.scale = new Vector3(5f, 0.3f, 1f);
            var col = ps.colorOverLifetime;
            col.enabled = true;
            Gradient g = new Gradient();
            g.SetKeys(
                new[] { new GradientColorKey(new Color(0.8f, 1f, 1f), 0f), new GradientColorKey(new Color(0.3f, 0.7f, 1f), 1f) },
                new[] { new GradientAlphaKey(0.9f, 0f), new GradientAlphaKey(0.1f, 1f) });
            col.color = g;
            var psr = falls.GetComponent<ParticleSystemRenderer>();
            psr.material = MakeParticleMat(new Color(0.7f, 0.95f, 1f, 0.7f));

            GameObject mist = new GameObject("FallsMist");
            mist.transform.SetParent(zone, false);
            mist.transform.position = basePos + new Vector3(0f, 2f, 2f);
            ParticleSystem mps = mist.AddComponent<ParticleSystem>();
            var mm = mps.main;
            mm.startLifetime = 3f;
            mm.startSpeed = 0.4f;
            mm.startSize = new ParticleSystem.MinMaxCurve(2f, 5f);
            mm.startColor = new Color(1f, 1f, 1f, 0.12f);
            mm.maxParticles = 80;
            var me = mps.emission;
            me.rateOverTime = 20f;
            var ms = mps.shape;
            ms.shapeType = ParticleSystemShapeType.Box;
            ms.scale = new Vector3(8f, 2f, 6f);
            mist.GetComponent<ParticleSystemRenderer>().material =
                MakeParticleMat(new Color(1f, 1f, 1f, 0.15f));

            SpawnPointLight(zone, "FallsLight", basePos + new Vector3(0f, 12f, 4f),
                new Color(0.5f, 0.95f, 1f), 3f, 35f);
        }

        private static void BuildGlassPavilion(Transform root, AeroPackConfig pack, WorldPalette p)
        {
            Transform zone = Child(root, "GlassPavilion");
            float px = -32f, pz = 18f;
            float py = SampleTerrainY(px, pz);
            Vector3 pos = new Vector3(px, py, pz);

            if (TrySpawnPrefab(s_ctx.glassPavilionPrefab, zone, pos, Quaternion.identity, Vector3.one, "ChillPavilion"))
                return;

            BuildGlassPavilionGeometry(zone, pos, p);
        }

        /// <summary>Root object for saving <see cref="AeroOpenWorldPrefabs.GlassPavilion"/> in the editor.</summary>
        public static GameObject CreateGlassPavilionPrefabRoot()
        {
            AeroPackConfig pack = Resources.Load<AeroPackConfig>("AeroPackConfig");
            WorldPalette p = WorldPalette.Create(pack);
            var root = new GameObject("GlassPavilion");
            BuildGlassPavilionGeometry(root.transform, Vector3.zero, p);
            return root;
        }

        /// <summary>Root object for saving <see cref="AeroOpenWorldPrefabs.GlassTower"/> in the editor.</summary>
        public static GameObject CreateGlassTowerPrefabRoot()
        {
            AeroPackConfig pack = Resources.Load<AeroPackConfig>("AeroPackConfig");
            WorldPalette p = WorldPalette.Create(pack);
            var root = new GameObject("GlassTower");
            var rng = new System.Random(7);
            BuildGlassTowerGeometry(root.transform, Vector3.zero, p, rng, 0);
            return root;
        }

        private static void BuildGlassPavilionGeometry(Transform parent, Vector3 groundPos, WorldPalette p)
        {
            float px = groundPos.x, py = groundPos.y, pz = groundPos.z;
            BoxNoColl(parent, "Floor", new Vector3(px, py + 0.1f, pz), new Vector3(12f, 0.2f, 10f), p.Glass);
            BoxNoColl(parent, "WallL", new Vector3(px - 5.5f, py + 3f, pz), new Vector3(0.3f, 6f, 10f), p.Glass);
            BoxNoColl(parent, "WallR", new Vector3(px + 5.5f, py + 3f, pz), new Vector3(0.3f, 6f, 10f), p.Glass);
            BoxNoColl(parent, "WallBack", new Vector3(px, py + 3f, pz + 4.8f), new Vector3(12f, 6f, 0.3f), p.Glass);
            BoxNoColl(parent, "Roof", new Vector3(px, py + 6.2f, pz), new Vector3(13f, 0.4f, 11f), p.Cyan);
            Cyl(parent, "PillarA", new Vector3(px - 4f, py + 3f, pz - 3f), new Vector3(0.4f, 3f, 0.4f), p.Chrome);
            Cyl(parent, "PillarB", new Vector3(px + 4f, py + 3f, pz - 3f), new Vector3(0.4f, 3f, 0.4f), p.Chrome);
        }

        private static void BuildAquarium(Transform root, WorldPalette p)
        {
            Transform zone = Child(root, "Aquarium");
            float ax = 28f, az = -22f;
            float ay = SampleTerrainY(ax, az);

            BoxNoColl(zone, "Tank", new Vector3(ax, ay + 5f, az), new Vector3(10f, 10f, 10f), p.Glass);
            BoxNoColl(zone, "Frame", new Vector3(ax, ay + 5f, az), new Vector3(10.4f, 10.4f, 10.4f), p.Cyan);
            MakeSphere(zone, "CoralA", new Vector3(ax - 2f, ay + 3f, az), 1.2f, p.Lily, false);
            MakeSphere(zone, "CoralB", new Vector3(ax + 2f, ay + 4f, az + 1f), 0.9f, p.Emissive, false);
            Cyl(zone, "Castle", new Vector3(ax, ay + 3f, az - 1f), new Vector3(2f, 3f, 2f), p.Chrome);
        }

        private static void BuildFloatingIslands(Transform sky, WorldPalette p)
        {
            (Vector3 pos, float size)[] islands =
            {
                (new Vector3(-20f, 28f, 40f), 5f),
                (new Vector3(15f, 35f, 55f), 6f),
                (new Vector3(35f, 42f, 20f), 4.5f),
            };

            foreach (var isl in islands)
            {
                GameObject cyl = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                cyl.name = "FloatIsland";
                cyl.transform.SetParent(sky, false);
                cyl.transform.position = isl.pos;
                cyl.transform.localScale = new Vector3(isl.size * 2f, 0.8f, isl.size * 2f);
                AssignMat(cyl, p.Grass);
                DestroyCollider(cyl);
                MakeSphere(sky, "IslandTree", isl.pos + new Vector3(0f, 2.5f, 0f), 1.8f, p.Tree, false);
            }
        }

        private static void BuildFrutigerArchitecture(Transform decor, AeroPackConfig pack, WorldPalette p)
        {
            Transform zone = Child(decor, "FrutigerArchitecture");
            System.Random rng = new System.Random(202);

            (string label, Vector3 center)[] districts =
            {
                ("District_Meadow", new Vector3(12f, 0f, 28f)),
                ("District_Lake", new Vector3(48f, 0f, 38f)),
                ("District_West", new Vector3(-52f, 0f, 22f)),
                ("District_South", new Vector3(28f, 0f, -48f)),
                ("District_SouthWest", new Vector3(-35f, 0f, -38f)),
                ("District_North", new Vector3(-18f, 0f, 58f)),
                ("District_East", new Vector3(62f, 0f, -12f)),
                ("District_FarWest", new Vector3(-58f, 0f, -55f)),
            };

            foreach (var d in districts)
                PlaceBuildingCluster(Child(zone, d.label), pack, rng, d.center, 7, 11f, 0.95f, 1.55f);

            Transform ring = Child(zone, "Ring_Meadow");
            for (int i = 0; i < 36; i++)
            {
                float ang = i * Mathf.PI * 2f / 36f;
                float r = 24f + (float)rng.NextDouble() * 20f;
                float x = Mathf.Cos(ang) * r;
                float z = Mathf.Sin(ang) * r;
                if (x * x + z * z < 196f) continue;
                PlacePrefabBuilding(ring, pack, rng, x, z, 0.9f, 1.45f);
            }

            Transform scatter = Child(zone, "Scatter_Hills");
            for (int i = 0; i < 32; i++)
            {
                float x = (float)(rng.NextDouble() * 150 - 75);
                float z = (float)(rng.NextDouble() * 150 - 75);
                if (x * x + z * z < 225f) continue;
                PlacePrefabBuilding(scatter, pack, rng, x, z, 1f, 1.75f);
            }

            BuildHorizonSkyline(Child(zone, "Horizon"), pack, p, rng);
            BuildProceduralGlassTowers(Child(zone, "GlassTowers"), p, rng, 42);
            BuildGlassPavilions(Child(zone, "Pavilions"), p, rng, 10);
        }

        private static void PlaceBuildingCluster(Transform zone, AeroPackConfig pack,
            System.Random rng, Vector3 center, int count, float radius, float scaleMin, float scaleMax)
        {
            for (int i = 0; i < count; i++)
            {
                float ang = (float)rng.NextDouble() * Mathf.PI * 2f;
                float r = (float)rng.NextDouble() * radius;
                float x = center.x + Mathf.Cos(ang) * r;
                float z = center.z + Mathf.Sin(ang) * r;
                PlacePrefabBuilding(zone, pack, rng, x, z, scaleMin, scaleMax);
            }
        }

        private static void PlacePrefabBuilding(Transform zone, AeroPackConfig pack, System.Random rng,
            float x, float z, float scaleMin, float scaleMax)
        {
            bool tower = rng.NextDouble() < 0.42;
            GameObject prefab = tower
                ? (s_ctx.towerBuildingPrefab ?? pack?.towerBuildingPrefab)
                : (s_ctx.basicBuildingPrefab ?? pack?.basicBuildingPrefab);
            if (prefab == null)
                prefab = s_ctx.basicBuildingPrefab ?? pack?.basicBuildingPrefab
                    ?? s_ctx.towerBuildingPrefab ?? pack?.towerBuildingPrefab;
            if (prefab == null) return;

            float y = SampleTerrainY(x, z);
            Vector3 pos = new Vector3(x, y, z);
            Quaternion rot = Quaternion.Euler(0f, (float)rng.NextDouble() * 360f, 0f);
            float s = scaleMin + (float)rng.NextDouble() * (scaleMax - scaleMin);
            string name = (tower ? "Tower" : "Building") + "_" + x.ToString("F0") + "_" + z.ToString("F0");

            if (!TrySpawnPrefab(prefab, zone, pos, rot, Vector3.one * s, name))
                return;

            if (rng.NextDouble() < 0.22f)
            {
                SpawnPointLight(zone, "BldgGlow",
                    new Vector3(x, y + 6f, z),
                    new Color(0.35f, 0.9f, 1f), 1.2f, 18f);
            }
        }

        private static void BuildHorizonSkyline(Transform zone, AeroPackConfig pack, WorldPalette p, System.Random rng)
        {
            for (int ring = 0; ring < 4; ring++)
            {
                float yaw = ring * 90f;
                float dist = 78f + ring * 4f;
                for (int i = 0; i < 12; i++)
                {
                    float t = (i / 11f) * 50f - 25f;
                    Vector3 local = new Vector3(t, 0f, dist);
                    Vector3 pos = Quaternion.Euler(0f, yaw, 0f) * local;
                    float scale = 1.3f + (float)rng.NextDouble() * 0.9f;
                    PlacePrefabBuilding(zone, pack, rng, pos.x, pos.z, scale, scale + 0.4f);
                }
            }
        }

        private static void BuildProceduralGlassTowers(Transform zone, WorldPalette p, System.Random rng, int count)
        {
            for (int i = 0; i < count; i++)
            {
                float x = (float)(rng.NextDouble() * 140 - 70);
                float z = (float)(rng.NextDouble() * 140 - 70);
                if (x * x + z * z < 169f) continue;

                float y = SampleTerrainY(x, z);
                Vector3 pos = new Vector3(x, y, z);
                float scale = 0.85f + (float)rng.NextDouble() * 0.55f;

                if (TrySpawnPrefab(s_ctx.glassTowerPrefab, zone, pos,
                    Quaternion.Euler(0f, (float)rng.NextDouble() * 360f, 0f),
                    Vector3.one * scale, "GlassTower_" + i))
                    continue;

                BuildGlassTowerGeometry(zone, pos, p, rng, i);
            }
        }

        private static void BuildGlassTowerGeometry(Transform parent, Vector3 groundPos, WorldPalette p,
            System.Random rng, int seed)
        {
            float x = groundPos.x, z = groundPos.z;
            float y = groundPos.y;
            float h = 10f + (float)rng.NextDouble() * 22f;
            float w = 3.5f + (float)rng.NextDouble() * 5f;
            Material body = rng.NextDouble() < 0.5 ? p.Glass : p.Building;
            Material trim = rng.NextDouble() < 0.5 ? p.Cyan : p.Emissive;

            BoxNoColl(parent, "Body", new Vector3(x, y + h * 0.5f, z), new Vector3(w, h, w), body);

            int bands = Mathf.Max(2, (int)(h / 5f));
            for (int b = 0; b < bands; b++)
            {
                float ly = y + (b + 0.5f) * (h / bands);
                BoxNoColl(parent, "Trim" + seed + "_" + b,
                    new Vector3(x, ly, z), new Vector3(w + 0.35f, 0.35f, w + 0.35f), trim);
            }

            if (rng.NextDouble() < 0.35f)
            {
                float capR = w * 0.7f;
                MakeSphere(parent, "Cap", new Vector3(x, y + h + capR * 0.4f, z), capR, p.Chrome, false);
            }
        }

        private static void BuildGlassPavilions(Transform zone, WorldPalette p, System.Random rng, int count)
        {
            for (int i = 0; i < count; i++)
            {
                float x = (float)(rng.NextDouble() * 120 - 60);
                float z = (float)(rng.NextDouble() * 120 - 60);
                if (x * x + z * z < 400f) continue;

                float px = x, pz = z;
                float py = SampleTerrainY(px, pz);
                float size = 5f + (float)rng.NextDouble() * 4f;

                BoxNoColl(zone, "PavFloor" + i, new Vector3(px, py + 0.1f, pz),
                    new Vector3(size, 0.2f, size * 0.8f), p.Glass);
                BoxNoColl(zone, "PavRoof" + i, new Vector3(px, py + 3.8f, pz),
                    new Vector3(size + 0.6f, 0.25f, size * 0.8f + 0.6f), p.Cyan);

                for (int c = 0; c < 4; c++)
                {
                    float ang = c * Mathf.PI * 0.5f;
                    float cx = px + Mathf.Cos(ang) * (size * 0.45f);
                    float cz = pz + Mathf.Sin(ang) * (size * 0.38f);
                    Cyl(zone, "PavCol" + i + "_" + c, new Vector3(cx, py + 1.9f, cz),
                        new Vector3(0.35f, 1.9f, 0.35f), p.Chrome);
                }
            }
        }

        private static bool TrySpawnPrefab(GameObject prefab, Transform parent, Vector3 pos, Quaternion rot,
            Vector3 scale, string objectName)
        {
            if (prefab == null)
                return false;

            GameObject go;
#if UNITY_EDITOR
            if (s_ctx.editorScene && s_ctx.usePrefabAssets && !Application.isPlaying)
            {
                go = (GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(prefab, parent);
                go.name = objectName;
            }
            else
#endif
            {
                go = Object.Instantiate(prefab, parent);
                go.name = objectName;
            }

            go.transform.SetPositionAndRotation(pos, rot);
            go.transform.localScale = scale;
            DestroyCollidersRecursive(go);
            return true;
        }

        private static void DestroyCollidersRecursive(GameObject go)
        {
            foreach (Collider c in go.GetComponentsInChildren<Collider>())
                Object.DestroyImmediate(c);
        }

        private static void BuildWorldSigns(Transform root, WorldPalette p)
        {
            float sy = SampleTerrainY(0f, 8f);
            Sign(root, "Welcome", "AEROBLOOM MEADOW\nexplore freely", new Vector3(0f, sy + 3f, 8f), Quaternion.identity, p);
            Sign(root, "Lake", "CRYSTAL LAKE", new Vector3(38f, SampleTerrainY(38f, 28f) + 3f, 28f), Quaternion.Euler(0f, -30f, 0f), p);
        }

        private static void BuildAmbientParticles(Transform env)
        {
            GameObject dust = new GameObject("AmbientDust");
            dust.transform.SetParent(env, false);
            dust.transform.position = new Vector3(0f, 25f, 0f);
            ParticleSystem ps = dust.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.startLifetime = 12f;
            main.startSpeed = 0.15f;
            main.startSize = new ParticleSystem.MinMaxCurve(0.03f, 0.08f);
            main.startColor = new Color(1f, 1f, 1f, 0.35f);
            main.maxParticles = 400;
            var em = ps.emission;
            em.rateOverTime = 18f;
            var sh = ps.shape;
            sh.shapeType = ParticleSystemShapeType.Box;
            sh.scale = new Vector3(180f, 40f, 180f);
            dust.GetComponent<ParticleSystemRenderer>().material =
                MakeParticleMat(new Color(1f, 1f, 1f, 0.3f));
        }

        // ── Scene cleanup & player ─────────────────────────────────

        private static void CleanupSceneForExplore()
        {
            GameObject cube = GameObject.Find("Cube");
            if (cube != null)
                DestroySmart(cube);

            GameObject runtime = GameObject.Find(AeroSceneFactory.RootName);
            if (runtime != null)
                DestroySmart(runtime);

            GameObject mainCam = GameObject.Find("Main Camera");
            if (mainCam != null)
                mainCam.SetActive(false);
        }

        private static void EnsurePlayerCamera()
        {
            AeroPlayerController player = Object.FindFirstObjectByType<AeroPlayerController>();
            if (player != null)
                player.EnsureExploreCamera();
        }

        private static void PlacePlayer(Vector3 pos)
        {
            AeroPlayerController player = Object.FindFirstObjectByType<AeroPlayerController>();
            if (player == null)
                return;

            CharacterController cc = player.GetComponent<CharacterController>();
            if (cc != null)
                cc.enabled = false;

            player.transform.position = pos;
            player.transform.rotation = Quaternion.Euler(0f, 180f, 0f);
            player.fallRespawnY = -40f;
            player.walkSpeed = 8f;
            player.sprintSpeed = 14f;
            player.extraAirJumps = 2;

            if (cc != null)
                cc.enabled = true;
        }

        // ── Primitives ─────────────────────────────────────────────

        private static GameObject Plat(Transform parent, string name, Vector3 pos, Vector3 scale, Material mat)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.position = pos;
            go.transform.localScale = scale;
            AssignMat(go, mat);
            return go;
        }

        private static GameObject Box(Transform parent, string name, Vector3 pos, Vector3 scale, Material mat)
        {
            return Plat(parent, name, pos, scale, mat);
        }

        private static void BoxNoColl(Transform parent, string name, Vector3 pos, Vector3 scale, Material mat)
        {
            DestroyCollider(Plat(parent, name, pos, scale, mat));
        }

        private static void Cyl(Transform parent, string name, Vector3 pos, Vector3 scale, Material mat)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.position = pos;
            go.transform.localScale = scale;
            AssignMat(go, mat);
        }

        private static GameObject MakeSphere(Transform parent, string name, Vector3 pos, float radius, Material mat, bool keepCol)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.position = pos;
            go.transform.localScale = Vector3.one * radius;
            AssignMat(go, mat);
            if (!keepCol) DestroyCollider(go);
            return go;
        }

        private static void MakeRing(Transform parent, string name, Vector3 pos, float radius, Material mat)
        {
            GameObject ring = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            ring.name = name;
            ring.transform.SetParent(parent, false);
            ring.transform.position = pos;
            ring.transform.localScale = new Vector3(radius * 2f, 0.15f, radius * 2f);
            AssignMat(ring, mat);
            DestroyCollider(ring);
        }

        private static void Sign(Transform root, string name, string text, Vector3 pos, Quaternion rot, WorldPalette p)
        {
            Transform s = Child(root, "Signs");
            GameObject back = GameObject.CreatePrimitive(PrimitiveType.Cube);
            back.name = name + "_Sign";
            back.transform.SetParent(s, false);
            back.transform.SetPositionAndRotation(pos, rot);
            back.transform.localScale = new Vector3(5f, 2.2f, 0.12f);
            AssignMat(back, p.Sign);
            DestroyCollider(back);

            GameObject canvasGO = new GameObject(name + "_Label");
            canvasGO.transform.SetParent(s, false);
            canvasGO.transform.SetPositionAndRotation(pos + rot * new Vector3(0f, 0f, -0.08f), rot);
            canvasGO.transform.localScale = Vector3.one * 0.01f;
            Canvas canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            RectTransform crt = canvasGO.GetComponent<RectTransform>();
            crt.sizeDelta = new Vector2(480f, 200f);

            GameObject textGO = new GameObject("TMP");
            textGO.transform.SetParent(canvasGO.transform, false);
            TextMeshProUGUI tmp = textGO.AddComponent<TextMeshProUGUI>();
            RectTransform trt = tmp.rectTransform;
            trt.anchorMin = Vector2.zero;
            trt.anchorMax = Vector2.one;
            trt.sizeDelta = Vector2.zero;
            tmp.text = text;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = new Color(0.02f, 0.2f, 0.38f);
            tmp.fontStyle = FontStyles.Bold;
            tmp.enableAutoSizing = true;
            tmp.fontSizeMin = 12f;
            tmp.fontSizeMax = 72f;
        }

        private static void SpawnPointLight(Transform parent, string name, Vector3 pos, Color color, float intensity, float range)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = pos;
            Light l = go.AddComponent<Light>();
            l.type = LightType.Point;
            l.color = color;
            l.intensity = intensity;
            l.range = range;
            l.shadows = LightShadows.None;
        }

        // ── Helpers ────────────────────────────────────────────────

        private static Transform Child(Transform parent, string name)
        {
            Transform existing = parent.Find(name);
            if (existing != null)
                return existing;
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            return go.transform;
        }

        private static void DestroyRoot(string n)
        {
            GameObject go = GameObject.Find(n);
            if (go != null)
                DestroySmart(go);
        }

        private static void AssignMat(GameObject go, Material mat)
        {
            if (mat == null) return;
            Renderer r = go.GetComponent<Renderer>();
            if (r != null) r.sharedMaterial = mat;
        }

        private static void DestroyCollider(GameObject go)
        {
            Collider c = go.GetComponent<Collider>();
            if (c != null) Object.DestroyImmediate(c);
        }

        private static void DestroySmart(Object o)
        {
            if (o == null) return;
            if (Application.isPlaying) Object.Destroy(o);
            else Object.DestroyImmediate(o);
        }

        private static Material LoadMat(string path)
        {
            return Resources.Load<Material>("Materials/" + path);
        }

        private static Material MakeParticleMat(Color col)
        {
            Shader sh = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            if (sh == null) sh = Shader.Find("Sprites/Default");
            Material m = new Material(sh ?? Shader.Find("Standard"));
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", col);
            else m.color = col;
            return m;
        }

        // ── Palette ────────────────────────────────────────────────

        private sealed class WorldPalette
        {
            public Material Grass, Glass, Cyan, Chrome, Water, Sand, Wood, Rock, Building;
            public Material Bubble, Globe, Emissive, Lily, Sign, Cloud, Tree;

            public static WorldPalette Create(AeroPackConfig pack)
            {
                var p = new WorldPalette();
                p.Grass = pack?.grassMaterial ?? LoadMat("FA_GlassGreen") ?? LoadMat("FA_Ground");
                p.Glass = LoadMat("FA_GlassBlue") ?? LoadMat("FA_GlassClear");
                p.Cyan = LoadMat("FA_GlossyCyan") ?? LoadMat("FA_Platform");
                p.Chrome = LoadMat("FA_Chrome") ?? LoadMat("FA_ChromeMetal");
                p.Water = LoadMat("FA_GlassBlue");
                p.Building = LoadMat("FA_BuildingWall");
                p.Bubble = LoadMat("FA_GlassClear");
                p.Globe = LoadMat("FA_GlassBlue");
                p.Emissive = LoadMat("FA_EmissiveCyan");
                p.Sand = M("Sand", new Color(0.72f, 0.88f, 0.95f, 1f), 0.1f, 0.75f);
                p.Wood = M("Wood", new Color(0.55f, 0.78f, 0.92f, 1f), 0.05f, 0.8f);
                p.Rock = M("Rock", new Color(0.45f, 0.62f, 0.78f, 1f), 0.2f, 0.65f);
                p.Lily = M("Lily", new Color(0.35f, 0.95f, 0.75f, 0.85f), 0f, 0.9f, true);
                p.Sign = M("Sign", new Color(0.95f, 1f, 1f, 0.88f), 0.04f, 0.95f, true);
                p.Cloud = M("Cloud", new Color(1f, 1f, 1f, 0.55f), 0f, 0.92f, true);
                p.Tree = M("Tree", new Color(0.25f, 0.82f, 0.38f, 1f), 0.05f, 0.7f);
                return p;
            }

            private static Material M(string name, Color col, float met, float smooth, bool trans = false)
            {
                Shader sh = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
                Material m = new Material(sh) { name = name };
                if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", col);
                else m.color = col;
                if (m.HasProperty("_Metallic")) m.SetFloat("_Metallic", met);
                if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", smooth);
                if (trans)
                {
                    if (m.HasProperty("_Surface")) m.SetFloat("_Surface", 1f);
                    m.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
                    m.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
                    m.SetInt("_ZWrite", 0);
                    m.renderQueue = (int)RenderQueue.Transparent;
                }
                return m;
            }
        }
    }
}
