using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace AeroBloom
{
    public static class AeroSceneFactory
    {
        public const string RootName = "AeroBloom_Runtime";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void BootstrapEmptyScene()
        {
            // Never bootstrap inside the MainMenu scene
            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            if (scene.name == "MainMenu" || scene.buildIndex == 0)
                return;

            if (Object.FindFirstObjectByType<AeroLevelDirector>() != null)
                return;

            BuildPrototypeScene(false);
        }

        public static AeroLevelDirector BuildPrototypeScene(bool forceRebuild)
        {
            if (forceRebuild)
            {
                DestroyNamedRoot(RootName);
            }

            AeroLevelDirector existing = Object.FindFirstObjectByType<AeroLevelDirector>();
            if (existing != null && !forceRebuild)
            {
                return existing;
            }

            AeroPackConfig pack = Resources.Load<AeroPackConfig>("AeroPackConfig");

            GameObject root = new GameObject(RootName);
            Palette palette = Palette.Create(pack);
            RemoveTemplateSceneObjects(root.transform);
            SetupEnvironment(root.transform, palette, pack);

            Vector3 startPosition = new Vector3(0f, 0.62f, -3f);
            Quaternion startRotation = Quaternion.identity;

            AeroLevelDirector director = root.AddComponent<AeroLevelDirector>();
            AeroPlayerController player = CreatePlayer(root.transform, startPosition, startRotation, pack);

            int seedCount;
            int checkpointCount;
            BuildCourse(root.transform, director, palette, pack, out seedCount, out checkpointCount);

            director.Configure(player, seedCount, checkpointCount, startPosition, startRotation);
            director.ShowMessage("AeroBloom online. Restore the relays and collect Aero seeds.", 3f);
            return director;
        }

        private static AeroPlayerController CreatePlayer(Transform parent, Vector3 position, Quaternion rotation, AeroPackConfig pack = null)
        {
            GameObject playerObject = new GameObject("Player_AeroRunner");
            playerObject.transform.SetParent(parent, false);
            playerObject.transform.SetPositionAndRotation(position, rotation);
            playerObject.tag = "Player";

            CharacterController characterController = playerObject.AddComponent<CharacterController>();
            characterController.height = 1.8f;
            characterController.radius = 0.35f;
            characterController.center = new Vector3(0f, 0.9f, 0f);
            characterController.stepOffset = 0.45f;
            characterController.slopeLimit = 58f;

            // AddComponent triggers Awake → EnsureCamera() creates a free-floating 3P pivot + camera
            AeroPlayerController player = playerObject.AddComponent<AeroPlayerController>();

            if (pack != null && pack.msnBuddyPrefab != null)
            {
                GameObject body = Object.Instantiate(pack.msnBuddyPrefab);
                body.name = "MsnBuddy Body";
                body.transform.SetParent(playerObject.transform, false);
                body.transform.localPosition = Vector3.zero;
                body.transform.localRotation = Quaternion.identity;
                body.transform.localScale = Vector3.one;
                DestroyCollidersRecursive(body);

                Animator anim = body.GetComponent<Animator>();
                if (anim == null)
                    anim = body.AddComponent<Animator>();
                if (pack.msnBuddyAnimator != null)
                    anim.runtimeAnimatorController = pack.msnBuddyAnimator;

                player.bodyTransform = body.transform;
                player.bodyAnimator = anim;
            }

            if (pack != null && pack.footstepParticlesPrefab != null)
            {
                GameObject particles = Object.Instantiate(pack.footstepParticlesPrefab);
                particles.name = "Footstep Particles";
                particles.transform.SetParent(playerObject.transform, false);
                particles.transform.localPosition = Vector3.zero;
            }

            return player;
        }

        private static void BuildCourse(Transform root, AeroLevelDirector director, Palette palette, AeroPackConfig pack, out int seedCount, out int checkpointCount)
        {
            seedCount = 0;
            checkpointCount = 0;

            CreatePlatform(root, "Start Aqua Island", new Vector3(0f, 0f, 0f), new Vector3(11f, 0.7f, 11f), Quaternion.identity, palette.WhiteGlass);
            CreatePlatform(root, "Login Step 01", new Vector3(0f, 0.45f, 10f), new Vector3(5.5f, 0.5f, 5f), Quaternion.identity, palette.Glass);
            CreatePlatform(root, "Login Step 02", new Vector3(4f, 1.15f, 20f), new Vector3(5.2f, 0.5f, 5f), Quaternion.Euler(0f, 7f, 0f), palette.Glass);
            CreatePlatform(root, "Login Step 03", new Vector3(-2.2f, 1.95f, 31f), new Vector3(5.2f, 0.5f, 5f), Quaternion.Euler(0f, -10f, 0f), palette.Glass);
            CreatePlatform(root, "Spring Dock", new Vector3(3f, 2.85f, 42f), new Vector3(6.3f, 0.55f, 6f), Quaternion.identity, palette.LimeGlass);

            CreateCheckpoint(root, "Relay 01 Login", new Vector3(0f, 0.42f, 2.8f), Quaternion.identity, palette, ref checkpointCount);
            AddSeed(root, new Vector3(0f, 1.6f, 10f), director, palette, ref seedCount);
            AddSeed(root, new Vector3(4f, 2.3f, 20f), director, palette, ref seedCount);
            AddSeed(root, new Vector3(-2.2f, 3.1f, 31f), director, palette, ref seedCount);
            AddSeed(root, new Vector3(3f, 4.1f, 42f), director, palette, ref seedCount);

            CreateBouncePad(root, "Bubble Spring", new Vector3(3f, 3.45f, 43f), Quaternion.identity, palette, new Vector3(0f, 19f, 22f));
            CreatePlatform(root, "Cloud Cache Landing", new Vector3(3f, 7.25f, 60f), new Vector3(8f, 0.55f, 7f), Quaternion.identity, palette.WhiteGlass);
            CreateSpeedGate(root, "Aero Stream 01", new Vector3(3f, 8.5f, 65.3f), Quaternion.identity, palette, new Vector3(0f, 1f, 16f));
            CreatePlatform(root, "Relay Shelf", new Vector3(3f, 7.15f, 72f), new Vector3(6.5f, 0.5f, 5f), Quaternion.identity, palette.Glass);
            CreateCheckpoint(root, "Relay 02 Bubble Cache", new Vector3(3f, 7.55f, 61.5f), Quaternion.identity, palette, ref checkpointCount);
            AddSeed(root, new Vector3(3f, 8.5f, 60f), director, palette, ref seedCount);
            AddSeed(root, new Vector3(3f, 8.35f, 72f), director, palette, ref seedCount);

            GameObject mover = CreatePlatform(root, "Glass Sync Platform", new Vector3(3f, 7.5f, 84f), new Vector3(6.2f, 0.45f, 5.2f), Quaternion.identity, palette.CyanGlass);
            AeroMovingPlatform movingPlatform = mover.AddComponent<AeroMovingPlatform>();
            movingPlatform.localOffset = new Vector3(10f, 0f, 0f);
            movingPlatform.period = 4.6f;

            CreatePlatform(root, "Moving Platform Receiver", new Vector3(13f, 7.7f, 96f), new Vector3(8f, 0.55f, 7f), Quaternion.identity, palette.WhiteGlass);
            CreateCheckpoint(root, "Relay 03 Sync Bridge", new Vector3(13f, 8.08f, 97.3f), Quaternion.identity, palette, ref checkpointCount);
            AddSeed(root, new Vector3(8f, 9.2f, 84f), director, palette, ref seedCount);
            AddSeed(root, new Vector3(13f, 9.1f, 96f), director, palette, ref seedCount);

            CreatePlatform(root, "Wallrun Entry", new Vector3(13f, 8.05f, 104f), new Vector3(7f, 0.5f, 6f), Quaternion.identity, palette.Glass);
            CreateWall(root, "Left Glass Wallrun Wall", new Vector3(10.4f, 11f, 113f), new Vector3(0.45f, 5.8f, 22f), palette.WallGlass);
            CreateWall(root, "Right Glass Wallrun Wall", new Vector3(15.6f, 11f, 113f), new Vector3(0.45f, 5.8f, 22f), palette.WallGlass);
            CreateSpeedGate(root, "Wallrun Aero Stream", new Vector3(13f, 9.55f, 107f), Quaternion.identity, palette, new Vector3(0f, 0.5f, 12f));
            CreatePlatform(root, "Wallrun Exit", new Vector3(13f, 8.45f, 122f), new Vector3(8f, 0.55f, 7f), Quaternion.identity, palette.LimeGlass);
            CreateCheckpoint(root, "Relay 04 Glass Run", new Vector3(13f, 8.82f, 123.5f), Quaternion.identity, palette, ref checkpointCount);
            AddSeed(root, new Vector3(13f, 10.2f, 108f), director, palette, ref seedCount);
            AddSeed(root, new Vector3(13f, 10.3f, 115f), director, palette, ref seedCount);
            AddSeed(root, new Vector3(13f, 9.85f, 122f), director, palette, ref seedCount);

            for (int i = 0; i < 8; i++)
            {
                float angle = i * 0.82f;
                Vector3 position = new Vector3(13f + Mathf.Sin(angle) * 6.5f, 9.1f + i * 0.82f, 132f + i * 7f);
                Quaternion rotation = Quaternion.Euler(0f, Mathf.Sin(angle) * 18f, 0f);
                Material material = i % 2 == 0 ? palette.WhiteGlass : palette.CyanGlass;
                CreatePlatform(root, "Sky Garden Step " + (i + 1), position, new Vector3(5.5f, 0.45f, 4.8f), rotation, material);
                AddSeed(root, position + Vector3.up * 1.25f, director, palette, ref seedCount);

                if (i == 3)
                {
                    CreateSpeedGate(root, "Aero Stream Spiral", position + new Vector3(0f, 1.3f, 2.2f), rotation, palette, new Vector3(0f, 0.8f, 13f));
                }
            }

            CreatePlatform(root, "Final Sky Garden", new Vector3(13f, 16.4f, 190f), new Vector3(13f, 0.8f, 13f), Quaternion.identity, palette.LimeGlass);
            CreateCheckpoint(root, "Relay 05 Sky Garden", new Vector3(13f, 16.9f, 186f), Quaternion.identity, palette, ref checkpointCount);
            AddSeed(root, new Vector3(9f, 18.4f, 190f), director, palette, ref seedCount);
            AddSeed(root, new Vector3(13f, 18.8f, 190f), director, palette, ref seedCount);
            AddSeed(root, new Vector3(17f, 18.4f, 190f), director, palette, ref seedCount);
            CreateFinishGate(root, "Bloom Portal", new Vector3(13f, 17.1f, 196f), Quaternion.identity, palette);

            CreateSign(root, "Opening Pitch Sign", "AEROBLOOM\nrestore the glossy sky-garden network", new Vector3(-4.6f, 1.1f, 3f), Quaternion.Euler(0f, 25f, 0f), palette);
            CreateSign(root, "Wallrun Sign", "hold SHIFT near glass walls\njump to burst away", new Vector3(17.4f, 10.2f, 105f), Quaternion.Euler(0f, -35f, 0f), palette);
            CreateSign(root, "Final Sign", "all relays synced\nenter the bloom portal", new Vector3(7.2f, 18.1f, 191f), Quaternion.Euler(0f, 45f, 0f), palette);

            CreateDecor(root, palette, pack);
        }

        private static void SetupEnvironment(Transform root, Palette palette, AeroPackConfig pack = null)
        {
            RenderSettings.fog = true;
            RenderSettings.fogColor = new Color(0.65f, 0.92f, 1f);
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogDensity = 0.004f;
            // Flat ambient keeps buildings bright regardless of skybox sampling
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.88f, 0.97f, 1f);

            // Always use clean procedural gradient sky — the pack skybox has realistic clouds
            // that break the Frutiger Aero aesthetic. Procedural gives the signature clean blue.
            Shader skyShader = Shader.Find("Skybox/Procedural");
            if (skyShader != null)
            {
                Material sky = new Material(skyShader);
                sky.SetColor("_SkyTint", new Color(0.48f, 0.76f, 1f));
                sky.SetColor("_GroundColor", new Color(0.44f, 0.82f, 0.54f));
                sky.SetFloat("_AtmosphereThickness", 0.82f);
                sky.SetFloat("_Exposure", 1.42f);
                sky.SetFloat("_SunDisk", 1);
                sky.SetFloat("_SunSize", 0.024f);
                RenderSettings.skybox = sky;
            }

            GameObject sunObject = new GameObject("Aero Sun");
            sunObject.transform.SetParent(root, false);
            sunObject.transform.rotation = Quaternion.Euler(52f, -36f, 0f);
            Light sun = sunObject.AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.intensity = 1.42f;
            sun.color = new Color(1f, 0.98f, 0.92f);
            sun.shadows = LightShadows.Soft;

            // Own post-processing profile — pack profile is often tuned for a different mood.
            // Frutiger Aero needs: strong bloom glow, vivid saturation, slight blue-white cast.
            GameObject volumeObject = new GameObject("Aero Post Process");
            volumeObject.transform.SetParent(root, false);
            Volume volume = volumeObject.AddComponent<Volume>();
            volume.isGlobal = true;
            volume.priority = 2f;

            VolumeProfile profile = ScriptableObject.CreateInstance<VolumeProfile>();
            volume.sharedProfile = profile;

            Bloom bloom = profile.Add<Bloom>(true);
            bloom.threshold.Override(0.52f);
            bloom.intensity.Override(0.82f);
            bloom.scatter.Override(0.72f);

            ColorAdjustments colorAdj = profile.Add<ColorAdjustments>(true);
            colorAdj.saturation.Override(34f);
            colorAdj.contrast.Override(12f);
            colorAdj.colorFilter.Override(new Color(0.94f, 0.99f, 1f));

            Vignette vignette = profile.Add<Vignette>(true);
            vignette.intensity.Override(0.08f);
            vignette.smoothness.Override(0.45f);

            // Reflective water plane at the base
            GameObject water = GameObject.CreatePrimitive(PrimitiveType.Plane);
            water.name = "Reflective Water Layer";
            water.transform.SetParent(root, false);
            water.transform.position = new Vector3(6f, -2.6f, 95f);
            water.transform.localScale = new Vector3(50f, 1f, 50f);
            AssignMaterial(water, palette.Water);
            DestroyCollider(water);

            // Soft green ground layer below the water
            GameObject gardenBase = GameObject.CreatePrimitive(PrimitiveType.Plane);
            gardenBase.name = "Soft Green Reflection Field";
            gardenBase.transform.SetParent(root, false);
            gardenBase.transform.position = new Vector3(6f, -2.75f, 95f);
            gardenBase.transform.localScale = new Vector3(40f, 1f, 40f);
            AssignMaterial(gardenBase, palette.Grass);
            DestroyCollider(gardenBase);
        }

        private static void CreateDecor(Transform root, Palette palette, AeroPackConfig pack = null)
        {
            for (int i = 0; i < 22; i++)
            {
                float t = i / 21f;
                float x = Mathf.Sin(i * 2.17f) * 32f + (i % 3) * 9f - 8f;
                float z = Mathf.Lerp(6f, 190f, t) + Mathf.Sin(i) * 6f;
                float y = 3f + Mathf.Sin(i * 1.7f) * 2.3f + (i % 5) * 0.4f;
                float scale = 0.8f + (i % 4) * 0.35f;
                GameObject bubble = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                bubble.name = "Floating Aero Bubble " + (i + 1);
                bubble.transform.SetParent(root, false);
                bubble.transform.position = new Vector3(x, y, z);
                bubble.transform.localScale = Vector3.one * scale;
                AssignMaterial(bubble, palette.Bubble);
                DestroyCollider(bubble);
                AeroOrbitProp orbit = bubble.AddComponent<AeroOrbitProp>();
                orbit.degreesPerSecond = 8f + i;
                orbit.bobHeight = 0.18f + (i % 3) * 0.08f;
            }

            bool hasBuildings = pack != null && (pack.basicBuildingPrefab != null || pack.towerBuildingPrefab != null);
            for (int i = 0; i < 12; i++)
            {
                float side = i % 2 == 0 ? -1f : 1f;
                float z = 18f + i * 14f;
                float height = 8f + (i % 4) * 4f;
                Vector3 pos = new Vector3(side * (23f + i % 3 * 5f), height * 0.5f - 1.5f, z);
                Quaternion rot = Quaternion.Euler(0f, i * 11f, 0f);

                if (hasBuildings)
                {
                    GameObject prefab = (i % 3 == 0 && pack.towerBuildingPrefab != null)
                        ? pack.towerBuildingPrefab
                        : pack.basicBuildingPrefab;
                    if (prefab != null)
                    {
                        GameObject building = Object.Instantiate(prefab);
                        building.name = "Distant Building " + (i + 1);
                        building.transform.SetParent(root, false);
                        float bx = side * (42f + i % 3 * 10f);
                        float bz = 15f + i * 14f;
                        building.transform.position = new Vector3(bx, -2f, bz);
                        building.transform.rotation = Quaternion.Euler(0f, i * 30f, 0f);
                        building.transform.localScale = Vector3.one;
                        DestroyCollidersRecursive(building);
                        ApplyBuildingMaterials(building, palette);
                    }
                }
                else
                {
                    GameObject tower = CreatePlatform(root, "Distant Glass Tower " + (i + 1), pos, new Vector3(4f + (i % 3), height, 4f + (i % 2)), rot, palette.DistantGlass);
                    DestroyCollider(tower);
                }
            }

            if (pack != null && pack.earthPrefab != null)
            {
                GameObject globe = Object.Instantiate(pack.earthPrefab);
                globe.name = "Aero Earth Globe";
                globe.transform.SetParent(root, false);
                globe.transform.position = new Vector3(-18f, 10f, 73f);
                globe.transform.localScale = Vector3.one * 5.2f;
                DestroyCollidersRecursive(globe);
                AeroOrbitProp globeOrbit = globe.AddComponent<AeroOrbitProp>();
                globeOrbit.axis = new Vector3(0.2f, 1f, 0.1f);
                globeOrbit.degreesPerSecond = 9f;
                globeOrbit.bobHeight = 0.35f;
            }
            else
            {
                GameObject globe = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                globe.name = "Aero Earth Globe";
                globe.transform.SetParent(root, false);
                globe.transform.position = new Vector3(-18f, 10f, 73f);
                globe.transform.localScale = Vector3.one * 5.2f;
                AssignMaterial(globe, palette.GlobeBlue);
                DestroyCollider(globe);
                AeroOrbitProp globeOrbit = globe.AddComponent<AeroOrbitProp>();
                globeOrbit.axis = new Vector3(0.2f, 1f, 0.1f);
                globeOrbit.degreesPerSecond = 9f;
                globeOrbit.bobHeight = 0.35f;

                CreatePlatform(root, "Globe Landmass A", new Vector3(-18.8f, 11.2f, 70.2f), new Vector3(3.8f, 0.12f, 1.4f), Quaternion.Euler(18f, 28f, -12f), palette.LimeSolid);
                CreatePlatform(root, "Globe Landmass B", new Vector3(-15.8f, 9.4f, 75.2f), new Vector3(2.6f, 0.12f, 1.6f), Quaternion.Euler(-18f, -35f, 18f), palette.LimeSolid);
            }
        }

        private static GameObject CreatePlatform(Transform parent, string name, Vector3 position, Vector3 scale, Quaternion rotation, Material material)
        {
            GameObject platform = GameObject.CreatePrimitive(PrimitiveType.Cube);
            platform.name = name;
            platform.transform.SetParent(parent, false);
            platform.transform.SetPositionAndRotation(position, rotation);
            platform.transform.localScale = scale;
            AssignMaterial(platform, material);
            return platform;
        }

        private static void CreateWall(Transform parent, string name, Vector3 position, Vector3 scale, Material material)
        {
            GameObject wall = CreatePlatform(parent, name, position, scale, Quaternion.identity, material);
            wall.AddComponent<AeroOrbitProp>().bobHeight = 0f;
        }

        private static void AddSeed(Transform root, Vector3 position, AeroLevelDirector director, Palette palette, ref int seedCount)
        {
            seedCount++;
            GameObject seed = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            seed.name = "Aero Seed " + seedCount;
            seed.transform.SetParent(root, false);
            seed.transform.position = position;
            seed.transform.localScale = Vector3.one * 0.72f;
            AssignMaterial(seed, palette.Seed);

            AeroCollectible collectible = seed.AddComponent<AeroCollectible>();
            collectible.director = director;

            Light light = seed.AddComponent<Light>();
            light.type = LightType.Point;
            light.range = 4f;
            light.intensity = 0.8f;
            light.color = new Color(0.68f, 1f, 0.9f);
        }

        private static void CreateBouncePad(Transform root, string name, Vector3 position, Quaternion rotation, Palette palette, Vector3 localLaunchVelocity)
        {
            GameObject pad = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            pad.name = name;
            pad.transform.SetParent(root, false);
            pad.transform.SetPositionAndRotation(position, rotation);
            pad.transform.localScale = new Vector3(1.65f, 0.08f, 1.65f);
            AssignMaterial(pad, palette.Pad);
            AeroBouncer bouncer = pad.AddComponent<AeroBouncer>();
            bouncer.localLaunchVelocity = localLaunchVelocity;
        }

        private static void CreateSpeedGate(Transform root, string name, Vector3 position, Quaternion rotation, Palette palette, Vector3 localImpulse)
        {
            GameObject gate = new GameObject(name);
            gate.transform.SetParent(root, false);
            gate.transform.SetPositionAndRotation(position, rotation);

            BoxCollider collider = gate.AddComponent<BoxCollider>();
            collider.isTrigger = true;
            collider.center = new Vector3(0f, 1.2f, 0f);
            collider.size = new Vector3(5.4f, 2.4f, 0.8f);

            AeroSpeedGate speedGate = gate.AddComponent<AeroSpeedGate>();
            speedGate.localImpulse = localImpulse;

            CreateChildCube(gate.transform, "Left Rail", new Vector3(-2.5f, 1.2f, 0f), new Vector3(0.18f, 2.4f, 0.18f), palette.CyanSolid);
            CreateChildCube(gate.transform, "Right Rail", new Vector3(2.5f, 1.2f, 0f), new Vector3(0.18f, 2.4f, 0.18f), palette.CyanSolid);
            CreateChildCube(gate.transform, "Top Rail", new Vector3(0f, 2.35f, 0f), new Vector3(5.2f, 0.18f, 0.18f), palette.CyanSolid);
            CreateChildCube(gate.transform, "Bottom Rail", new Vector3(0f, 0.08f, 0f), new Vector3(5.2f, 0.16f, 0.16f), palette.CyanSolid);
        }

        private static void CreateCheckpoint(Transform root, string name, Vector3 position, Quaternion rotation, Palette palette, ref int checkpointCount)
        {
            checkpointCount++;
            GameObject checkpoint = new GameObject(name);
            checkpoint.transform.SetParent(root, false);
            checkpoint.transform.SetPositionAndRotation(position, rotation);

            BoxCollider collider = checkpoint.AddComponent<BoxCollider>();
            collider.isTrigger = true;
            collider.center = new Vector3(0f, 1.45f, 0f);
            collider.size = new Vector3(6.4f, 2.9f, 0.9f);

            Transform respawn = new GameObject("Respawn Point").transform;
            respawn.SetParent(checkpoint.transform, false);
            respawn.localPosition = new Vector3(0f, 0.22f, -2.7f);
            respawn.localRotation = Quaternion.identity;

            CreateChildCube(checkpoint.transform, "Left Pillar", new Vector3(-3f, 1.35f, 0f), new Vector3(0.28f, 2.7f, 0.28f), palette.RelayInactive);
            CreateChildCube(checkpoint.transform, "Right Pillar", new Vector3(3f, 1.35f, 0f), new Vector3(0.28f, 2.7f, 0.28f), palette.RelayInactive);
            CreateChildCube(checkpoint.transform, "Top Bridge", new Vector3(0f, 2.7f, 0f), new Vector3(6.3f, 0.28f, 0.28f), palette.RelayInactive);

            AeroCheckpoint checkpointComponent = checkpoint.AddComponent<AeroCheckpoint>();
            checkpointComponent.checkpointName = name;
            checkpointComponent.respawnPoint = respawn;
            checkpointComponent.inactiveMaterial = palette.RelayInactive;
            checkpointComponent.activeMaterial = palette.RelayActive;
        }

        private static void CreateFinishGate(Transform root, string name, Vector3 position, Quaternion rotation, Palette palette)
        {
            GameObject gate = new GameObject(name);
            gate.transform.SetParent(root, false);
            gate.transform.SetPositionAndRotation(position, rotation);

            BoxCollider collider = gate.AddComponent<BoxCollider>();
            collider.isTrigger = true;
            collider.center = new Vector3(0f, 1.8f, 0f);
            collider.size = new Vector3(7.2f, 3.6f, 1f);

            gate.AddComponent<AeroFinishGate>();
            CreateChildCube(gate.transform, "Bloom Left", new Vector3(-3.3f, 1.6f, 0f), new Vector3(0.34f, 3.2f, 0.34f), palette.Finish);
            CreateChildCube(gate.transform, "Bloom Right", new Vector3(3.3f, 1.6f, 0f), new Vector3(0.34f, 3.2f, 0.34f), palette.Finish);
            CreateChildCube(gate.transform, "Bloom Top", new Vector3(0f, 3.2f, 0f), new Vector3(6.9f, 0.34f, 0.34f), palette.Finish);

            Light light = gate.AddComponent<Light>();
            light.type = LightType.Point;
            light.range = 12f;
            light.intensity = 2.2f;
            light.color = new Color(0.65f, 1f, 0.82f);
        }

        private static void CreateSign(Transform root, string name, string text, Vector3 position, Quaternion rotation, Palette palette)
        {
            GameObject signBack = CreatePlatform(root, name + " Back", position, new Vector3(5.8f, 2.4f, 0.12f), rotation, palette.SignBack);
            DestroyCollider(signBack);

            GameObject textObject = new GameObject(name);
            textObject.transform.SetParent(root, false);
            // TextMesh local +Z is the visible face; Euler(0,180,0) makes it face world -Z toward player
            textObject.transform.SetPositionAndRotation(
                position + rotation * new Vector3(0f, 0f, -0.09f),
                Quaternion.Euler(0f, 180f, 0f));
            TextMesh textMesh = textObject.AddComponent<TextMesh>();
            textMesh.text = text;
            textMesh.anchor = TextAnchor.MiddleCenter;
            textMesh.alignment = TextAlignment.Center;
            textMesh.characterSize = 0.22f;
            textMesh.fontSize = 72;
            textMesh.color = new Color(0.03f, 0.24f, 0.34f, 0.96f);
        }

        private static GameObject CreateChildCube(Transform parent, string name, Vector3 localPosition, Vector3 localScale, Material material)
        {
            GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.name = name;
            cube.transform.SetParent(parent, false);
            cube.transform.localPosition = localPosition;
            cube.transform.localRotation = Quaternion.identity;
            cube.transform.localScale = localScale;
            AssignMaterial(cube, material);
            DestroyCollider(cube);
            return cube;
        }

        private static void AssignMaterial(GameObject target, Material material)
        {
            Renderer renderer = target.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.sharedMaterial = material;
            }
        }

        private static void DestroyCollider(GameObject target)
        {
            Collider collider = target.GetComponent<Collider>();
            if (collider != null)
            {
                DestroySmart(collider);
            }
        }

        private static void ApplyBuildingMaterials(GameObject building, Palette palette)
        {
            foreach (Renderer r in building.GetComponentsInChildren<Renderer>())
            {
                int count = r.sharedMaterials.Length;
                Material[] mats = new Material[count];
                for (int i = 0; i < count; i++)
                {
                    mats[i] = (i == 0) ? palette.AeroBuilding : palette.AeroBuildingWindow;
                }
                r.sharedMaterials = mats;
            }
        }

        private static void DestroyCollidersRecursive(GameObject target)
        {
            foreach (Collider col in target.GetComponentsInChildren<Collider>())
            {
                DestroySmart(col);
            }
        }

        private static void RemoveTemplateSceneObjects(Transform newRoot)
        {
            RemoveDefaultCameras(newRoot);
            DestroySceneObjectByName("Directional Light", newRoot);
            DestroySceneObjectByName("Global Volume", newRoot);
            DestroySceneObjectByName("Terrain", newRoot);
        }

        private static void RemoveDefaultCameras(Transform newRoot)
        {
            Camera[] cameras = Object.FindObjectsByType<Camera>(FindObjectsSortMode.None);
            for (int i = 0; i < cameras.Length; i++)
            {
                if (cameras[i] != null && !cameras[i].transform.IsChildOf(newRoot))
                {
                    DestroySmart(cameras[i].gameObject);
                }
            }
        }

        private static void DestroySceneObjectByName(string objectName, Transform newRoot)
        {
            GameObject target = GameObject.Find(objectName);
            if (target != null && !target.transform.IsChildOf(newRoot))
            {
                DestroySmart(target);
            }
        }

        private static void DestroyNamedRoot(string rootName)
        {
            GameObject existing = GameObject.Find(rootName);
            if (existing != null)
            {
                DestroySmart(existing);
            }
        }

        private static void DestroySmart(Object target)
        {
            if (target == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                Object.Destroy(target);
            }
            else
            {
                Object.DestroyImmediate(target);
            }
        }

        private sealed class Palette
        {
            public Material Glass;
            public Material WhiteGlass;
            public Material CyanGlass;
            public Material LimeGlass;
            public Material WallGlass;
            public Material DistantGlass;
            public Material Water;
            public Material Grass;
            public Material Seed;
            public Material Pad;
            public Material RelayInactive;
            public Material RelayActive;
            public Material Finish;
            public Material CyanSolid;
            public Material LimeSolid;
            public Material GlobeBlue;
            public Material Bubble;
            public Material SignBack;
            public Material AeroBuilding;
            public Material AeroBuildingWindow;

            public static Palette Create(AeroPackConfig pack = null)
            {
                Palette palette = new Palette();
                // Vivid Frutiger Aero palette — high saturation, higher alpha, strong emission
                palette.Glass = CreateMaterial("Aero Glass", new Color(0.35f, 0.82f, 1f, 0.90f), 0.04f, 0.97f, true, new Color(0f, 0.42f, 0.82f));
                palette.WhiteGlass = CreateMaterial("Pearl Glass", new Color(0.96f, 1f, 1f, 0.94f), 0.02f, 0.98f, true, new Color(0.08f, 0.24f, 0.32f));
                palette.CyanGlass = CreateMaterial("Cyan Glass", new Color(0.08f, 0.80f, 1f, 0.92f), 0.04f, 0.97f, true, new Color(0f, 0.62f, 1.1f));
                palette.LimeGlass = CreateMaterial("Lime Glass", new Color(0.48f, 1f, 0.52f, 0.92f), 0.02f, 0.95f, true, new Color(0.14f, 0.75f, 0.24f));
                palette.WallGlass = CreateMaterial("Wallrun Glass", new Color(0.35f, 0.90f, 1f, 0.78f), 0.02f, 0.99f, true, new Color(0f, 0.38f, 0.68f));
                palette.DistantGlass = CreateMaterial("Distant Tower Glass", new Color(0.72f, 0.95f, 1f, 0.42f), 0.06f, 0.88f, true, new Color(0f, 0.12f, 0.22f));
                palette.Water = CreateMaterial("Water Sheet", new Color(0.18f, 0.62f, 0.88f, 0.30f), 0.02f, 1f, true, new Color(0f, 0.12f, 0.24f));
                // Frutiger Aero ground: muted soft green, not neon
                palette.Grass = CreateMaterial("Aero Grass", new Color(0.38f, 0.78f, 0.52f, 0.55f), 0.02f, 0.72f, true, new Color(0.01f, 0.12f, 0.08f));
                palette.Seed = CreateMaterial("Aero Seed", new Color(0.84f, 1f, 0.92f, 0.96f), 0f, 0.98f, true, new Color(0.42f, 2.4f, 1.4f));
                palette.Pad = CreateMaterial("Bubble Spring Pad", new Color(0.82f, 1f, 1f, 0.92f), 0.02f, 0.97f, true, new Color(0.18f, 1.0f, 1.5f));
                palette.RelayInactive = CreateMaterial("Relay Inactive", new Color(0.78f, 0.94f, 1f, 0.62f), 0.08f, 0.88f, true, new Color(0f, 0.14f, 0.26f));
                palette.RelayActive = CreateMaterial("Relay Active", new Color(0.68f, 1f, 0.80f, 0.90f), 0.02f, 0.98f, true, new Color(0.32f, 1.8f, 0.72f));
                palette.Finish = CreateMaterial("Bloom Finish", new Color(0.72f, 1f, 0.74f, 0.94f), 0.02f, 0.99f, true, new Color(0.62f, 2.6f, 0.84f));
                palette.CyanSolid = CreateMaterial("Cyan Solid", new Color(0.02f, 0.72f, 1f, 1f), 0.18f, 0.82f, false, new Color(0f, 0.58f, 1.1f));
                palette.LimeSolid = CreateMaterial("Lime Solid", new Color(0.32f, 0.95f, 0.36f, 1f), 0.06f, 0.72f, false, new Color(0.06f, 0.52f, 0.08f));
                palette.GlobeBlue = CreateMaterial("Globe Blue", new Color(0.10f, 0.48f, 1f, 0.88f), 0.06f, 0.92f, true, new Color(0f, 0.22f, 0.65f));
                palette.Bubble = CreateMaterial("Bubble", new Color(0.88f, 1f, 1f, 0.22f), 0.02f, 1f, true, new Color(0f, 0.10f, 0.18f));
                palette.SignBack = CreateMaterial("Glossy Sign Back", new Color(0.95f, 1f, 1f, 0.78f), 0.04f, 0.94f, true, new Color(0.08f, 0.28f, 0.38f));
                // Pure white gloss body + vivid blue glass windows — classic Frutiger Aero building look
                palette.AeroBuilding = CreateMaterial("Aero Building", new Color(0.96f, 0.99f, 1f, 1f), 0.05f, 0.95f, false, new Color(0.06f, 0.18f, 0.38f));
                palette.AeroBuildingWindow = CreateMaterial("Aero Building Window", new Color(0.22f, 0.68f, 1f, 0.72f), 0.02f, 0.99f, true, new Color(0.04f, 0.42f, 1.0f));
                return palette;
            }

            private static Material CreateMaterial(string name, Color color, float metallic, float smoothness, bool transparent, Color emission)
            {
                Shader shader = Shader.Find("Universal Render Pipeline/Lit");
                if (shader == null)
                {
                    shader = Shader.Find("Standard");
                }

                Material material = new Material(shader);
                material.name = name;

                if (material.HasProperty("_BaseColor"))
                {
                    material.SetColor("_BaseColor", color);
                }
                else
                {
                    material.color = color;
                }

                if (material.HasProperty("_Metallic"))
                {
                    material.SetFloat("_Metallic", metallic);
                }

                if (material.HasProperty("_Smoothness"))
                {
                    material.SetFloat("_Smoothness", smoothness);
                }

                if (emission.maxColorComponent > 0f && material.HasProperty("_EmissionColor"))
                {
                    material.EnableKeyword("_EMISSION");
                    material.SetColor("_EmissionColor", emission);
                }

                if (transparent)
                {
                    if (material.HasProperty("_Surface"))
                    {
                        material.SetFloat("_Surface", 1f);
                    }

                    if (material.HasProperty("_Blend"))
                    {
                        material.SetFloat("_Blend", 0f);
                    }

                    material.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
                    material.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
                    material.SetInt("_ZWrite", 0);
                    material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                    material.DisableKeyword("_ALPHATEST_ON");
                    material.renderQueue = (int)RenderQueue.Transparent;
                }

                return material;
            }
        }
    }
}
