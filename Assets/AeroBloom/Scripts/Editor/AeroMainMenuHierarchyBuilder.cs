using System.IO;
using AeroBloom;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering;
using UnityEngine.UI;

public static class AeroMainMenuHierarchyBuilder
{
    private const string MainMenuScenePath = "Assets/Scenes/MainMenu.unity";
    private const string ConfigResourcePath = "AeroPackConfig";
    private const string UiBackgroundSpritePath = "UI/Skin/Background.psd";
    private const string UiStandardSpritePath = "UI/Skin/UISprite.psd";

    [MenuItem("AeroBloom/Main Menu/Rebuild Hierarchy")]
    public static void RebuildHierarchy()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        if (!File.Exists(MainMenuScenePath))
        {
            EditorUtility.DisplayDialog(
                "Main Menu Missing",
                "Main menu scene not found at:\n" + MainMenuScenePath,
                "OK");
            return;
        }

        EditorSceneManager.OpenScene(MainMenuScenePath, OpenSceneMode.Single);
        Debug.Log("[AeroMainMenuHierarchyBuilder] Opened your saved MainMenu scene without regeneration.");
    }

    public static void RebuildGeneratedHierarchy()
    {
        string sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        if (sceneName != "MainMenu")
        {
            bool proceed = EditorUtility.DisplayDialog(
                "Rebuild Main Menu",
                "Active scene is not 'MainMenu'. Rebuild here anyway?",
                "Rebuild",
                "Cancel");
            if (!proceed) return;
        }

        AeroPackConfig pack = Resources.Load<AeroPackConfig>(ConfigResourcePath);

        GameObject root = GameObject.Find("AeroMainMenu");
        if (root == null)
        {
            root = new GameObject("AeroMainMenu");
            Undo.RegisterCreatedObjectUndo(root, "Create AeroMainMenu Root");
        }

        AeroMainMenu menu = root.GetComponent<AeroMainMenu>();
        if (menu == null)
        {
            menu = Undo.AddComponent<AeroMainMenu>(root);
        }

        ClearChildren(root.transform);
        CleanupSceneCameraAndEventSystem();

        BuildWorld(root.transform, pack, out _);

        Button playButton;
        Button quitButton;
        BuildUi(root.transform, pack, out playButton, out quitButton);

        EnsureEventSystem();

        AudioSource musicSource = root.GetComponent<AudioSource>();
        if (musicSource == null)
        {
            musicSource = Undo.AddComponent<AudioSource>(root);
        }

        SerializedObject serializedMenu = new SerializedObject(menu);
        serializedMenu.FindProperty("playButton").objectReferenceValue = playButton;
        serializedMenu.FindProperty("quitButton").objectReferenceValue = quitButton;
        serializedMenu.FindProperty("musicSource").objectReferenceValue = musicSource;
        serializedMenu.ApplyModifiedPropertiesWithoutUndo();

        EditorSceneManager.MarkSceneDirty(root.scene);
        Selection.activeGameObject = root;
        Debug.Log("[AeroMainMenuHierarchyBuilder] Main menu rebuilt in Hierarchy.");
    }

    private static void EnsureMainMenuRuntimeBindings()
    {
        AeroPackConfig pack = Resources.Load<AeroPackConfig>(ConfigResourcePath);
        GameObject root = GameObject.Find("AeroMainMenu");
        if (root == null)
        {
            root = new GameObject("AeroMainMenu");
            Undo.RegisterCreatedObjectUndo(root, "Create AeroMainMenu Root");
        }

        AeroMainMenu menu = root.GetComponent<AeroMainMenu>();
        if (menu == null)
        {
            menu = Undo.AddComponent<AeroMainMenu>(root);
        }

        Button playButton = FindButtonByName("PLAY_Button");
        Button quitButton = FindButtonByName("QUIT_Button");

        AudioSource musicSource = root.GetComponent<AudioSource>();
        if (musicSource == null)
        {
            musicSource = Undo.AddComponent<AudioSource>(root);
        }

        EnsureEventSystem();

        SerializedObject serializedMenu = new SerializedObject(menu);
        serializedMenu.FindProperty("playButton").objectReferenceValue = playButton;
        serializedMenu.FindProperty("quitButton").objectReferenceValue = quitButton;
        serializedMenu.FindProperty("musicSource").objectReferenceValue = musicSource;
        serializedMenu.ApplyModifiedPropertiesWithoutUndo();

        EnsureLegacyMenuLandmarks(root.transform, pack);
        Selection.activeGameObject = root;
    }

    private static void EnsureLegacyMenuLandmarks(Transform root, AeroPackConfig pack)
    {
        Transform world = root.Find("World");
        if (world == null)
        {
            world = CreateChild(root, "World");
        }

        if (world.Find("Hill") == null || world.Find("GroundBlend") == null)
        {
            CreateGround(world, pack);
        }

        if (world.Find("MenuBuddy") == null)
        {
            CreateBuddy(world, pack);
        }
    }

    private static Button FindButtonByName(string objectName)
    {
        Button[] buttons = Object.FindObjectsByType<Button>(FindObjectsSortMode.None);
        for (int i = 0; i < buttons.Length; i++)
        {
            if (buttons[i].name == objectName) return buttons[i];
        }

        return null;
    }

    private static void ClearChildren(Transform parent)
    {
        for (int i = parent.childCount - 1; i >= 0; i--)
        {
            Undo.DestroyObjectImmediate(parent.GetChild(i).gameObject);
        }
    }

    private static void CleanupSceneCameraAndEventSystem()
    {
        Camera[] cameras = Object.FindObjectsByType<Camera>(FindObjectsSortMode.None);
        for (int i = 0; i < cameras.Length; i++)
        {
            Undo.DestroyObjectImmediate(cameras[i].gameObject);
        }

        EventSystem[] eventSystems = Object.FindObjectsByType<EventSystem>(FindObjectsSortMode.None);
        for (int i = 0; i < eventSystems.Length; i++)
        {
            Undo.DestroyObjectImmediate(eventSystems[i].gameObject);
        }
    }

    private static void BuildWorld(Transform root, AeroPackConfig pack, out Camera menuCamera)
    {
        Transform worldRoot = CreateChild(root, "World");

        SetupSky(pack);
        RenderSettings.ambientMode = AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.88f, 0.96f, 1f);
        RenderSettings.fog = false;

        GameObject cameraGo = new GameObject("Menu Camera");
        Undo.RegisterCreatedObjectUndo(cameraGo, "Create Menu Camera");
        cameraGo.transform.SetParent(worldRoot, false);
        cameraGo.tag = "MainCamera";

        menuCamera = cameraGo.AddComponent<Camera>();
        menuCamera.clearFlags = CameraClearFlags.Skybox;
        menuCamera.fieldOfView = 48f;
        menuCamera.nearClipPlane = 0.05f;
        menuCamera.farClipPlane = 500f;
        cameraGo.AddComponent<AudioListener>();
        cameraGo.transform.position = new Vector3(0.6f, 2.8f, -9.2f);
        cameraGo.transform.rotation = Quaternion.Euler(4f, -1.5f, 0f);

        GameObject sunGo = new GameObject("Sun");
        Undo.RegisterCreatedObjectUndo(sunGo, "Create Sun");
        sunGo.transform.SetParent(worldRoot, false);
        sunGo.transform.rotation = Quaternion.Euler(52f, -38f, 0f);
        Light sun = sunGo.AddComponent<Light>();
        sun.type = LightType.Directional;
        sun.intensity = 1.45f;
        sun.color = new Color(1f, 0.97f, 0.90f);
        sun.shadows = LightShadows.Soft;

        GameObject volumeGo = new GameObject("Menu Post FX");
        Undo.RegisterCreatedObjectUndo(volumeGo, "Create Post FX");
        volumeGo.transform.SetParent(worldRoot, false);
        Volume volume = volumeGo.AddComponent<Volume>();
        volume.isGlobal = true;
        volume.priority = 2f;

        if (pack != null && pack.globalVolumeProfile != null)
        {
            volume.sharedProfile = pack.globalVolumeProfile;
        }

        if (pack != null && pack.mainMenuBackgroundTexture != null)
        {
            CreateBackdrop(worldRoot, menuCamera, pack.mainMenuBackgroundTexture);
        }

        CreateGround(worldRoot, pack);
        CreateBuildings(worldRoot, pack);
        CreatePlanet(worldRoot, pack);
        CreateBuddy(worldRoot, pack);
        CreateBubbles(worldRoot);
    }

    private static void SetupSky(AeroPackConfig pack)
    {
        if (pack != null && pack.skyboxMaterial != null)
        {
            RenderSettings.skybox = pack.skyboxMaterial;
            return;
        }

        Shader skyShader = Shader.Find("Skybox/Procedural");
        if (skyShader == null) return;

        Material sky = new Material(skyShader);
        sky.SetColor("_SkyTint", new Color(0.46f, 0.74f, 1f));
        sky.SetColor("_GroundColor", new Color(0.36f, 0.72f, 0.40f));
        sky.SetFloat("_AtmosphereThickness", 0.80f);
        sky.SetFloat("_Exposure", 1.40f);
        sky.SetFloat("_SunDisk", 1f);
        sky.SetFloat("_SunSize", 0.022f);
        RenderSettings.skybox = sky;
    }

    private static void CreateBackdrop(Transform parent, Camera menuCamera, Texture2D texture)
    {
        GameObject backdrop = GameObject.CreatePrimitive(PrimitiveType.Quad);
        Undo.RegisterCreatedObjectUndo(backdrop, "Create Backdrop");
        backdrop.name = "MenuBackdrop";
        backdrop.transform.SetParent(parent, false);
        Object.DestroyImmediate(backdrop.GetComponent<Collider>());

        float distance = 120f;
        backdrop.transform.position = menuCamera.transform.position + menuCamera.transform.forward * distance;
        backdrop.transform.rotation = menuCamera.transform.rotation;

        float viewHeight = 2f * distance * Mathf.Tan(menuCamera.fieldOfView * 0.5f * Mathf.Deg2Rad);
        float viewWidth = viewHeight * (16f / 9f);
        float texAspect = texture.width / (float)texture.height;
        float viewAspect = viewWidth / viewHeight;

        float width;
        float height;
        if (texAspect > viewAspect)
        {
            height = viewHeight;
            width = height * texAspect;
        }
        else
        {
            width = viewWidth;
            height = width / texAspect;
        }

        backdrop.transform.localScale = new Vector3(width, height, 1f);

        Shader shader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Texture");
        if (shader == null) return;

        Material material = new Material(shader);
        if (material.HasProperty("_BaseMap")) material.SetTexture("_BaseMap", texture);
        if (material.HasProperty("_MainTex")) material.SetTexture("_MainTex", texture);
        if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", Color.white);
        if (material.HasProperty("_Color")) material.SetColor("_Color", Color.white);
        backdrop.GetComponent<MeshRenderer>().sharedMaterial = material;
    }

    private static void CreateGround(Transform parent, AeroPackConfig pack)
    {
        Shader litShader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");

        GameObject hill = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        Undo.RegisterCreatedObjectUndo(hill, "Create Hill");
        hill.name = "Hill";
        hill.transform.SetParent(parent, false);
        hill.transform.position = new Vector3(3.1f, -4.6f, 7.2f);
        hill.transform.localScale = new Vector3(14f, 8f, 14f);
        Object.DestroyImmediate(hill.GetComponent<Collider>());

        if (litShader == null) return;

        Material hillMaterial = pack != null && pack.grassMaterial != null
            ? new Material(pack.grassMaterial)
            : new Material(litShader);

        if (pack == null || pack.grassMaterial == null)
        {
            Color grassColor = new Color(0.37f, 0.78f, 0.33f);
            if (hillMaterial.HasProperty("_BaseColor")) hillMaterial.SetColor("_BaseColor", grassColor);
            else if (hillMaterial.HasProperty("_Color")) hillMaterial.SetColor("_Color", grassColor);
            if (hillMaterial.HasProperty("_Smoothness")) hillMaterial.SetFloat("_Smoothness", 0.52f);
        }

        hill.GetComponent<Renderer>().sharedMaterial = hillMaterial;

        GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
        Undo.RegisterCreatedObjectUndo(floor, "Create Ground Blend");
        floor.name = "GroundBlend";
        floor.transform.SetParent(parent, false);
        floor.transform.position = new Vector3(0f, -0.12f, 10f);
        floor.transform.localScale = new Vector3(15f, 1f, 15f);
        Object.DestroyImmediate(floor.GetComponent<Collider>());
        floor.GetComponent<Renderer>().sharedMaterial = hillMaterial;
    }

    private static void CreateBuildings(Transform parent, AeroPackConfig pack)
    {
        if (pack == null) return;

        for (int i = 0; i < 8; i++)
        {
            GameObject prefab = (i % 3 == 0 && pack.towerBuildingPrefab != null)
                ? pack.towerBuildingPrefab
                : pack.basicBuildingPrefab;
            if (prefab == null) continue;

            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            Undo.RegisterCreatedObjectUndo(instance, "Create Building");
            instance.name = "MenuBuilding_" + i;
            instance.transform.SetParent(parent, false);

            float side = i % 2 == 0 ? -1f : 1f;
            instance.transform.position = new Vector3(side * (10f + i * 2.7f), -1f, 19f + i * 4f);
            instance.transform.rotation = Quaternion.Euler(0f, i * 25f, 0f);
            instance.transform.localScale = Vector3.one * 0.95f;

            Collider[] colliders = instance.GetComponentsInChildren<Collider>();
            for (int c = 0; c < colliders.Length; c++)
            {
                Undo.DestroyObjectImmediate(colliders[c]);
            }
        }
    }

    private static void CreatePlanet(Transform parent, AeroPackConfig pack)
    {
        if (pack == null || pack.earthPrefab == null) return;

        GameObject planet = (GameObject)PrefabUtility.InstantiatePrefab(pack.earthPrefab);
        Undo.RegisterCreatedObjectUndo(planet, "Create Planet");
        planet.name = "MenuPlanet";
        planet.transform.SetParent(parent, false);
        planet.transform.position = new Vector3(-1.2f, 6.1f, 16f);
        planet.transform.rotation = Quaternion.Euler(16f, 196f, 4f);
        planet.transform.localScale = Vector3.one * 7.4f;

        Collider[] colliders = planet.GetComponentsInChildren<Collider>();
        for (int i = 0; i < colliders.Length; i++)
        {
            Undo.DestroyObjectImmediate(colliders[i]);
        }

        EarthRotate rotate = planet.GetComponent<EarthRotate>();
        if (rotate == null) rotate = Undo.AddComponent<EarthRotate>(planet);
        rotate.rotateSpeed = 8f;

        AeroOrbitProp orbit = planet.GetComponent<AeroOrbitProp>();
        if (orbit != null)
        {
            Undo.DestroyObjectImmediate(orbit);
        }
    }

    private static void CreateBuddy(Transform parent, AeroPackConfig pack)
    {
        if (pack == null || pack.msnBuddyPrefab == null) return;

        GameObject buddy = (GameObject)PrefabUtility.InstantiatePrefab(pack.msnBuddyPrefab);
        Undo.RegisterCreatedObjectUndo(buddy, "Create Buddy");
        buddy.name = "MenuBuddy";
        buddy.transform.SetParent(parent, false);
        buddy.transform.position = new Vector3(4.25f, 0.1f, 7f);
        buddy.transform.rotation = Quaternion.Euler(0f, -154f, 0f);
        buddy.transform.localScale = Vector3.one * 1.08f;

        if (pack.msnBuddyAnimator != null)
        {
            Animator animator = buddy.GetComponentInChildren<Animator>();
            if (animator != null)
            {
                animator.runtimeAnimatorController = pack.msnBuddyAnimator;
            }
        }
    }

    private static void CreateBubbles(Transform parent)
    {
        Shader litShader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
        if (litShader == null) return;

        for (int i = 0; i < 9; i++)
        {
            GameObject bubble = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            Undo.RegisterCreatedObjectUndo(bubble, "Create Bubble");
            bubble.name = "MenuBubble_" + i;
            bubble.transform.SetParent(parent, false);
            bubble.transform.position = new Vector3(2f + Mathf.Sin(i * 1.6f) * 2.7f, 2.9f + i * 0.9f, 6.5f + i * 2.5f);
            bubble.transform.localScale = Vector3.one * (0.38f + (i % 3) * 0.22f);
            Object.DestroyImmediate(bubble.GetComponent<Collider>());

            Material material = new Material(litShader);
            Color bubbleColor = new Color(0.88f, 1f, 1f, 0.16f);
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", bubbleColor);
            else if (material.HasProperty("_Color")) material.SetColor("_Color", bubbleColor);
            if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", 1f);
            if (material.HasProperty("_Surface")) material.SetFloat("_Surface", 1f);

            material.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
            material.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
            material.SetInt("_ZWrite", 0);
            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.renderQueue = (int)RenderQueue.Transparent;
            bubble.GetComponent<Renderer>().sharedMaterial = material;

            AeroOrbitProp orbit = Undo.AddComponent<AeroOrbitProp>(bubble);
            orbit.axis = new Vector3(0.18f, 1f, 0.08f);
            orbit.degreesPerSecond = 3.2f + i * 0.65f;
            orbit.bobHeight = 0.08f + (i % 3) * 0.05f;
            orbit.bobSpeed = 0.5f + i * 0.09f;
        }
    }

    private static void BuildUi(Transform root, AeroPackConfig pack, out Button playButton, out Button quitButton)
    {
        Transform uiRoot = CreateChild(root, "UI");

        GameObject canvasGo = new GameObject("Menu Canvas");
        Undo.RegisterCreatedObjectUndo(canvasGo, "Create Menu Canvas");
        canvasGo.transform.SetParent(uiRoot, false);

        Canvas canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 150;

        CanvasScaler scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
        scaler.matchWidthOrHeight = 0.5f;

        canvasGo.AddComponent<GraphicRaycaster>();
        Transform ui = canvasGo.transform;

        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (font == null) font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        if (font == null) font = Font.CreateDynamicFontFromOSFont("Arial", 18);
        Sprite roundedSprite = LoadBuiltinUiSprite();

        if (pack != null && pack.mainMenuForegroundTexture != null)
        {
            RawImage foreground = CreateRawImage(ui, "ForegroundHill", pack.mainMenuForegroundTexture, new Color(1f, 1f, 1f, 0.94f));
            RectTransform fgRect = foreground.rectTransform;
            fgRect.anchorMin = new Vector2(0.5f, 0f);
            fgRect.anchorMax = new Vector2(0.5f, 0f);
            fgRect.pivot = new Vector2(0.5f, 0f);
            fgRect.anchoredPosition = Vector2.zero;
            fgRect.sizeDelta = new Vector2(2200f, 600f);
        }

        CreatePanel(ui, "GlobalTint", new Color(0.02f, 0.08f, 0.23f, 0.20f), Vector2.zero, Vector2.one);

        Image card = CreatePanel(ui, "GlassCard", new Color(0.06f, 0.22f, 0.55f, 0.58f), new Vector2(0.055f, 0.14f), new Vector2(0.40f, 0.88f), roundedSprite, Image.Type.Sliced);
        CreatePanel(card.transform, "CardShine", new Color(0.72f, 0.98f, 1f, 0.14f), new Vector2(0f, 0.62f), new Vector2(1f, 1f));
        Image cardBorder = CreatePanel(card.transform, "CardBorder", new Color(0.40f, 0.96f, 1f, 0.50f), Vector2.zero, Vector2.one, roundedSprite, Image.Type.Sliced);
        cardBorder.rectTransform.offsetMin += new Vector2(-3f, -3f);
        cardBorder.rectTransform.offsetMax += new Vector2(3f, 3f);

        CreateText(card.transform, "Title1", "Aero", font, 96, FontStyle.Bold, new Color(1f, 1f, 1f, 0.97f),
            TextAnchor.MiddleLeft, new Vector2(0.10f, 0.72f), new Vector2(0.90f, 0.90f));
        CreateText(card.transform, "Title2", "Bloom", font, 96, FontStyle.Bold, new Color(0.36f, 0.96f, 1f, 0.97f),
            TextAnchor.MiddleLeft, new Vector2(0.10f, 0.56f), new Vector2(0.90f, 0.76f));

        CreateText(card.transform, "Tagline", "Restore the Sky-Garden Network", font, 21, FontStyle.Italic,
            new Color(0.86f, 0.97f, 1f, 0.85f), TextAnchor.MiddleLeft,
            new Vector2(0.10f, 0.46f), new Vector2(0.90f, 0.55f));

        CreatePanel(card.transform, "Divider", new Color(0.34f, 0.94f, 1f, 0.52f), new Vector2(0.10f, 0.41f), new Vector2(0.90f, 0.414f));

        playButton = CreateMenuButton(card.transform, "PLAY", font,
            new Vector2(0.12f, 0.24f), new Vector2(0.88f, 0.36f),
            new Color(0.12f, 0.70f, 1f, 1f), new Color(0.02f, 0.25f, 0.62f, 1f), Color.white, roundedSprite);

        quitButton = CreateMenuButton(card.transform, "QUIT", font,
            new Vector2(0.12f, 0.11f), new Vector2(0.88f, 0.20f),
            new Color(1f, 1f, 1f, 0.11f), new Color(0.34f, 0.94f, 1f, 0.30f), new Color(0.84f, 0.97f, 1f, 1f), roundedSprite);

        CreatePanel(ui, "Footer", new Color(0.02f, 0.10f, 0.28f, 0.85f), Vector2.zero, new Vector2(1f, 0.07f), roundedSprite, Image.Type.Sliced);
        CreateText(ui, "Controls", "WASD  MOVE      SPACE  JUMP/DOUBLE-JUMP      SHIFT  SPRINT      E  DASH      CTRL  SLIDE      R  RESPAWN",
            font, 14, FontStyle.Normal, new Color(0.66f, 0.95f, 1f, 0.85f), TextAnchor.MiddleCenter,
            new Vector2(0f, 0f), new Vector2(1f, 0.07f));
    }

    private static void EnsureEventSystem()
    {
        if (Object.FindFirstObjectByType<EventSystem>() != null) return;

        GameObject eventSystem = new GameObject("EventSystem");
        Undo.RegisterCreatedObjectUndo(eventSystem, "Create EventSystem");
        eventSystem.AddComponent<EventSystem>();
#if ENABLE_INPUT_SYSTEM
        eventSystem.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
#else
        eventSystem.AddComponent<StandaloneInputModule>();
#endif
    }

    private static Transform CreateChild(Transform parent, string name)
    {
        GameObject go = new GameObject(name);
        Undo.RegisterCreatedObjectUndo(go, "Create " + name);
        go.transform.SetParent(parent, false);
        return go.transform;
    }

    private static Image CreatePanel(Transform parent, string name, Color color, Vector2 anchorMin, Vector2 anchorMax,
        Sprite sprite = null, Image.Type type = Image.Type.Simple)
    {
        GameObject go = new GameObject(name);
        Undo.RegisterCreatedObjectUndo(go, "Create " + name);
        go.transform.SetParent(parent, false);

        Image image = go.AddComponent<Image>();
        image.sprite = sprite;
        image.type = sprite != null ? type : Image.Type.Simple;
        image.color = color;
        image.raycastTarget = false;

        RectTransform rect = image.rectTransform;
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        return image;
    }

    private static Sprite LoadBuiltinUiSprite()
    {
        Sprite sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>(UiBackgroundSpritePath);
        if (sprite != null) return sprite;
        return AssetDatabase.GetBuiltinExtraResource<Sprite>(UiStandardSpritePath);
    }

    private static RawImage CreateRawImage(Transform parent, string name, Texture texture, Color color)
    {
        GameObject go = new GameObject(name);
        Undo.RegisterCreatedObjectUndo(go, "Create " + name);
        go.transform.SetParent(parent, false);

        RawImage image = go.AddComponent<RawImage>();
        image.texture = texture;
        image.color = color;
        image.raycastTarget = false;
        return image;
    }

    private static Text CreateText(Transform parent, string name, string value, Font font,
        int size, FontStyle style, Color color, TextAnchor alignment, Vector2 anchorMin, Vector2 anchorMax)
    {
        GameObject go = new GameObject(name);
        Undo.RegisterCreatedObjectUndo(go, "Create " + name);
        go.transform.SetParent(parent, false);

        Text text = go.AddComponent<Text>();
        text.text = value;
        text.font = font;
        text.fontSize = size;
        text.fontStyle = style;
        text.color = color;
        text.alignment = alignment;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        text.raycastTarget = false;

        RectTransform rect = text.rectTransform;
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        return text;
    }

    private static Button CreateMenuButton(Transform parent, string label, Font font,
        Vector2 anchorMin, Vector2 anchorMax, Color background, Color border, Color textColor, Sprite roundedSprite)
    {
        Image shadow = CreatePanel(parent, label + "_Shadow", new Color(0f, 0.04f, 0.14f, 0.35f), anchorMin, anchorMax, roundedSprite, Image.Type.Sliced);
        shadow.rectTransform.offsetMin += new Vector2(6f, -8f);
        shadow.rectTransform.offsetMax += new Vector2(6f, -8f);

        Image borderImage = CreatePanel(parent, label + "_Border", border, anchorMin, anchorMax, roundedSprite, Image.Type.Sliced);
        borderImage.rectTransform.offsetMin += new Vector2(-4f, -4f);
        borderImage.rectTransform.offsetMax += new Vector2(4f, 4f);

        GameObject buttonGo = new GameObject(label + "_Button");
        Undo.RegisterCreatedObjectUndo(buttonGo, "Create " + label + " Button");
        buttonGo.transform.SetParent(parent, false);

        Image buttonImage = buttonGo.AddComponent<Image>();
        buttonImage.sprite = roundedSprite;
        buttonImage.type = roundedSprite != null ? Image.Type.Sliced : Image.Type.Simple;
        buttonImage.color = background;

        RectTransform buttonRect = buttonImage.rectTransform;
        buttonRect.anchorMin = anchorMin;
        buttonRect.anchorMax = anchorMax;
        buttonRect.offsetMin = Vector2.zero;
        buttonRect.offsetMax = Vector2.zero;

        Button button = buttonGo.AddComponent<Button>();
        ColorBlock colors = button.colors;
        colors.normalColor = background;
        colors.highlightedColor = Color.Lerp(background, Color.white, 0.25f);
        colors.pressedColor = Color.Lerp(background, new Color(0.05f, 0.35f, 0.9f), 0.60f);
        colors.selectedColor = background;
        colors.fadeDuration = 0.08f;
        button.colors = colors;

        CreatePanel(buttonGo.transform, "Gloss", new Color(1f, 1f, 1f, 0.20f), new Vector2(0f, 0.55f), new Vector2(1f, 1f));
        CreateText(buttonGo.transform, "Label", label, font, 28, FontStyle.Bold, textColor,
            TextAnchor.MiddleCenter, Vector2.zero, Vector2.one);

        return button;
    }
}
