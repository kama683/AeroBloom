using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace AeroBloom
{
    public sealed class AeroMainMenu : MonoBehaviour
    {
        private void Start()
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible   = true;

            AeroPackConfig pack = Resources.Load<AeroPackConfig>("AeroPackConfig");

            // UI must ALWAYS run — wrap 3D setup so exceptions can't block it
            try { SetupWorld(pack); }
            catch (System.Exception e) { Debug.LogWarning("[AeroMainMenu] 3D setup: " + e.Message); }

            BuildUI(pack);

            try { PlayMusic(pack); }
            catch { }
        }

        // ═══════════════════════════════════════════════════════════
        //  3-D BACKGROUND WORLD
        // ═══════════════════════════════════════════════════════════

        private void SetupWorld(AeroPackConfig pack)
        {
            // Sky
            Shader skyShader = Shader.Find("Skybox/Procedural");
            if (skyShader != null)
            {
                var sky = new Material(skyShader);
                sky.SetColor("_SkyTint",     new Color(0.46f, 0.74f, 1f));
                sky.SetColor("_GroundColor", new Color(0.36f, 0.72f, 0.40f));
                sky.SetFloat("_AtmosphereThickness", 0.80f);
                sky.SetFloat("_Exposure", 1.40f);
                sky.SetFloat("_SunDisk",  1f);
                sky.SetFloat("_SunSize",  0.022f);
                RenderSettings.skybox = sky;
            }
            RenderSettings.ambientMode  = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.88f, 0.96f, 1f);
            RenderSettings.fog          = false;

            // Camera
            var camGO = new GameObject("Menu Camera");
            var cam   = camGO.AddComponent<Camera>();
            cam.tag           = "MainCamera";
            cam.clearFlags    = CameraClearFlags.Skybox;
            cam.fieldOfView   = 54f;
            cam.nearClipPlane = 0.05f;
            cam.farClipPlane  = 500f;
            camGO.AddComponent<AudioListener>();
            camGO.transform.position = new Vector3(-0.5f, 2.8f, -9f);
            camGO.transform.rotation = Quaternion.Euler(5f, 4f, 0f);

            // Sun
            var sunGO = new GameObject("Sun");
            sunGO.transform.SetParent(transform, false);
            sunGO.transform.rotation = Quaternion.Euler(52f, -38f, 0f);
            var sun = sunGO.AddComponent<Light>();
            sun.type = LightType.Directional; sun.intensity = 1.42f;
            sun.color = new Color(1f, 0.97f, 0.90f); sun.shadows = LightShadows.Soft;

            // Post-processing
            var volGO = new GameObject("PP");
            volGO.transform.SetParent(transform, false);
            var vol = volGO.AddComponent<Volume>();
            vol.isGlobal = true; vol.priority = 2f;
            var vp = ScriptableObject.CreateInstance<VolumeProfile>();
            vol.sharedProfile = vp;
            var bl = vp.Add<Bloom>(true);
            bl.threshold.Override(0.48f); bl.intensity.Override(0.90f); bl.scatter.Override(0.72f);
            var ca = vp.Add<ColorAdjustments>(true);
            ca.saturation.Override(34f); ca.contrast.Override(11f);
            ca.colorFilter.Override(new Color(0.94f, 0.99f, 1f));

            // Ground plane
            Shader lit = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            var gnd = GameObject.CreatePrimitive(PrimitiveType.Plane);
            gnd.name = "Ground";
            gnd.transform.SetParent(transform, false);
            gnd.transform.position   = new Vector3(0f, -0.02f, 10f);
            gnd.transform.localScale = new Vector3(14f, 1f, 14f);
            Object.Destroy(gnd.GetComponent<Collider>());
            if (lit != null)
            {
                var gm = new Material(lit);
                var gc = new Color(0.34f, 0.78f, 0.30f);   // natural Frutiger green
                if (gm.HasProperty("_BaseColor")) gm.SetColor("_BaseColor", gc); else gm.color = gc;
                if (gm.HasProperty("_Smoothness")) gm.SetFloat("_Smoothness", 0.55f);
                gnd.GetComponent<Renderer>().sharedMaterial = gm;
            }

            // MsnBuddy — just stand there; don't touch Animator (causes exception on FBX root)
            if (pack != null && pack.msnBuddyPrefab != null)
            {
                var buddy = Object.Instantiate(pack.msnBuddyPrefab);
                buddy.name = "Buddy";
                buddy.transform.SetParent(transform, false);
                buddy.transform.position = new Vector3(2.8f, 0f, 3.2f);
                buddy.transform.rotation = Quaternion.Euler(0f, -145f, 0f);
                // Only wire animator if the FBX already has one — never AddComponent on FBX root
                if (pack.msnBuddyAnimator != null)
                {
                    var anim = buddy.GetComponentInChildren<Animator>();
                    if (anim != null) anim.runtimeAnimatorController = pack.msnBuddyAnimator;
                }
            }

            // Earth globe
            if (pack != null && pack.earthPrefab != null)
            {
                var globe = Object.Instantiate(pack.earthPrefab);
                globe.name = "Globe";
                globe.transform.SetParent(transform, false);
                globe.transform.position   = new Vector3(-5.5f, 4.5f, 13f);
                globe.transform.localScale = Vector3.one * 5f;
                foreach (var c in globe.GetComponentsInChildren<Collider>()) Object.Destroy(c);
                var op = globe.AddComponent<AeroOrbitProp>();
                op.axis = new Vector3(0.12f, 1f, 0.06f); op.degreesPerSecond = 9f; op.bobHeight = 0.26f;
            }

            // Background buildings
            if (pack != null)
            {
                for (int i = 0; i < 6; i++)
                {
                    float side   = i % 2 == 0 ? -1f : 1f;
                    var   prefab = (i % 3 == 0 && pack.towerBuildingPrefab != null)
                                   ? pack.towerBuildingPrefab : pack.basicBuildingPrefab;
                    if (prefab == null) continue;
                    var b = Object.Instantiate(prefab);
                    b.name = "Bldg" + i;
                    b.transform.SetParent(transform, false);
                    b.transform.position   = new Vector3(side * (12f + i * 3f), -1f, 20f + i * 5f);
                    b.transform.rotation   = Quaternion.Euler(0f, i * 20f, 0f);
                    b.transform.localScale = Vector3.one;
                    foreach (var c in b.GetComponentsInChildren<Collider>()) Object.Destroy(c);
                }
            }

            // Floating bubbles
            for (int i = 0; i < 12; i++)
            {
                var bub = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                bub.name = "Bub" + i;
                bub.transform.SetParent(transform, false);
                bub.transform.position   = new Vector3(Mathf.Sin(i * 2.3f) * 10f,
                                                        1.5f + i * 0.55f + Mathf.Sin(i * 1.7f),
                                                        4f + i * 1.9f);
                bub.transform.localScale = Vector3.one * (0.42f + (i % 4) * 0.24f);
                Object.Destroy(bub.GetComponent<Collider>());
                if (lit != null)
                {
                    var bm = new Material(lit);
                    var bc = new Color(0.88f, 1f, 1f, 0.16f);
                    if (bm.HasProperty("_BaseColor")) bm.SetColor("_BaseColor", bc); else bm.color = bc;
                    if (bm.HasProperty("_Smoothness")) bm.SetFloat("_Smoothness", 1f);
                    if (bm.HasProperty("_Surface"))    bm.SetFloat("_Surface", 1f);
                    bm.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
                    bm.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
                    bm.SetInt("_ZWrite", 0);
                    bm.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                    bm.renderQueue = (int)RenderQueue.Transparent;
                    bub.GetComponent<Renderer>().sharedMaterial = bm;
                }
                var aop = bub.AddComponent<AeroOrbitProp>();
                aop.degreesPerSecond = 5f + i * 0.85f;
                aop.bobHeight        = 0.13f + (i % 3) * 0.06f;
            }
        }

        // ═══════════════════════════════════════════════════════════
        //  UI
        // ═══════════════════════════════════════════════════════════

        private void BuildUI(AeroPackConfig pack)
        {
            // EventSystem is REQUIRED for button clicks.
            // New Input System projects need InputSystemUIInputModule, not StandaloneInputModule.
            if (FindFirstObjectByType<EventSystem>() == null)
            {
                var es = new GameObject("EventSystem");
                es.AddComponent<EventSystem>();
#if ENABLE_INPUT_SYSTEM
                es.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
#else
                es.AddComponent<StandaloneInputModule>();
#endif
            }

            // Font — Unity 6 renamed built-in from Arial.ttf to LegacyRuntime.ttf
            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (font == null) font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            if (font == null) font = Font.CreateDynamicFontFromOSFont("Arial", 16);

            // Canvas
            var canvasGO = new GameObject("UI Canvas");
            var canvas   = canvasGO.AddComponent<Canvas>();
            canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;
            var sc = canvasGO.AddComponent<CanvasScaler>();
            sc.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            sc.referenceResolution = new Vector2(1920f, 1080f);
            sc.matchWidthOrHeight  = 0.5f;
            canvasGO.AddComponent<GraphicRaycaster>();
            Transform ui = canvas.transform;

            // ── Background layers ──────────────────────────────────

            // Subtle full-screen tint so 3-D world doesn't look washed out
            Panel(ui, "ScreenTint", new Color(0.02f, 0.06f, 0.18f, 0.22f),
                  Vector2.zero, Vector2.one);

            // Left frosted panel (46 % of width)
            Panel(ui, "FrostPanel", new Color(0.06f, 0.24f, 0.68f, 0.58f),
                  Vector2.zero, new Vector2(0.46f, 1f));

            // Faint inner highlight (top-right of the panel)
            Panel(ui, "PanelHighlight", new Color(0.55f, 0.92f, 1f, 0.07f),
                  new Vector2(0f, 0.55f), new Vector2(0.46f, 1f));

            // Cyan left edge bar
            Panel(ui, "BorderL", new Color(0.32f, 0.95f, 1f, 0.92f),
                  Vector2.zero, new Vector2(0.005f, 1f));

            // Cyan top bar (left panel width only)
            Panel(ui, "BorderT", new Color(0.32f, 0.95f, 1f, 0.92f),
                  new Vector2(0f, 0.984f), new Vector2(0.46f, 1f));

            // Full-width footer bar
            Panel(ui, "Footer", new Color(0.02f, 0.10f, 0.28f, 0.85f),
                  Vector2.zero, new Vector2(1f, 0.066f));

            // ── Logo ──────────────────────────────────────────────

            // Soft glow behind title
            CImg(ui, "Glow1", new Color(0.32f, 0.95f, 1f, 0.09f),
                 new Vector2(-490f, 270f), new Vector2(640f, 240f));
            CImg(ui, "Glow2", new Color(1f, 1f, 1f, 0.04f),
                 new Vector2(-490f, 260f), new Vector2(720f, 310f));

            // "Aero" white
            CTxt(ui, "A1", "Aero", font, 120, FontStyle.Bold,
                 new Color(1f, 1f, 1f, 0.97f), new Vector2(-490f, 292f), new Vector2(640f, 148f));

            // "Bloom" cyan
            CTxt(ui, "A2", "Bloom", font, 120, FontStyle.Bold,
                 new Color(0.36f, 0.96f, 1f, 0.97f), new Vector2(-490f, 170f), new Vector2(640f, 148f));

            // Tagline
            CTxt(ui, "Tag", "Restore the Sky-Garden Network", font, 21, FontStyle.Italic,
                 new Color(0.82f, 0.97f, 1f, 0.80f), new Vector2(-490f, 94f), new Vector2(600f, 34f));

            // Cyan divider
            CImg(ui, "Div", new Color(0.32f, 0.95f, 1f, 0.52f),
                 new Vector2(-490f, 55f), new Vector2(520f, 2f));

            // ── Buttons ───────────────────────────────────────────

            // PLAY
            MkBtn(ui, "PLAY", font,
                  new Vector2(-490f, -25f), new Vector2(340f, 72f),
                  new Color(0.12f, 0.70f, 1f, 1f),
                  new Color(0.04f, 0.26f, 0.60f, 1f),
                  Color.white,
                  () => SceneManager.LoadScene(1));

            // QUIT
            MkBtn(ui, "QUIT", font,
                  new Vector2(-490f, -116f), new Vector2(340f, 72f),
                  new Color(1f, 1f, 1f, 0.12f),
                  new Color(0.32f, 0.95f, 1f, 0.30f),
                  new Color(0.75f, 0.97f, 1f, 1f),
                  Application.Quit);

            // Version (bottom-right of left panel)
            ATxt(ui, "Ver", "AeroBloom v1.0",
                 font, 13, FontStyle.Normal, new Color(0.45f, 0.85f, 1f, 0.50f),
                 new Vector2(-20f, 82f), new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(220f, 24f));

            // Footer controls
            ATxt(ui, "Ctrl",
                 "WASD · MOVE     SPACE · JUMP/DOUBLE-JUMP     SHIFT · SPRINT     E · DASH     CTRL · SLIDE     R · RESPAWN",
                 font, 15, FontStyle.Normal, new Color(0.65f, 0.95f, 1f, 0.85f),
                 new Vector2(0f, 21f), new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, 34f));
        }

        // ═══════════════════════════════════════════════════════════
        //  AUDIO
        // ═══════════════════════════════════════════════════════════

        private void PlayMusic(AeroPackConfig pack)
        {
            var src = gameObject.AddComponent<AudioSource>();
            src.loop = true; src.volume = 0.50f; src.spatialBlend = 0f;
            if (pack != null && pack.musicAlt  != null) src.clip = pack.musicAlt;
            else if (pack != null && pack.musicMain != null) src.clip = pack.musicMain;
            if (src.clip != null) src.Play();
        }

        // ═══════════════════════════════════════════════════════════
        //  HELPERS
        // ═══════════════════════════════════════════════════════════

        static void Panel(Transform p, string n, Color c, Vector2 aMin, Vector2 aMax)
        {
            var go = new GameObject(n); go.transform.SetParent(p, false);
            var img = go.AddComponent<Image>(); img.color = c;
            var r = img.rectTransform;
            r.anchorMin = aMin; r.anchorMax = aMax;
            r.offsetMin = r.offsetMax = Vector2.zero;
        }

        static void CImg(Transform p, string n, Color c, Vector2 pos, Vector2 sz)
        {
            var go = new GameObject(n); go.transform.SetParent(p, false);
            var img = go.AddComponent<Image>(); img.color = c;
            var r = img.rectTransform;
            r.anchorMin = r.anchorMax = r.pivot = new Vector2(0.5f, 0.5f);
            r.anchoredPosition = pos; r.sizeDelta = sz;
        }

        static void CTxt(Transform p, string n, string text, Font font,
                         int size, FontStyle style, Color c, Vector2 pos, Vector2 sz)
        {
            var go = new GameObject(n); go.transform.SetParent(p, false);
            var t = go.AddComponent<Text>();
            if (font != null) t.font = font;
            t.fontSize = size; t.fontStyle = style; t.text = text; t.color = c;
            t.alignment = TextAnchor.MiddleLeft;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            var r = t.rectTransform;
            r.anchorMin = r.anchorMax = r.pivot = new Vector2(0.5f, 0.5f);
            r.anchoredPosition = pos; r.sizeDelta = sz;
        }

        static void ATxt(Transform p, string n, string text, Font font,
                         int size, FontStyle style, Color c,
                         Vector2 pos, Vector2 aMin, Vector2 aMax, Vector2 sz)
        {
            var go = new GameObject(n); go.transform.SetParent(p, false);
            var t = go.AddComponent<Text>();
            if (font != null) t.font = font;
            t.fontSize = size; t.fontStyle = style; t.text = text; t.color = c;
            t.alignment = TextAnchor.MiddleCenter;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            var r = t.rectTransform;
            r.anchorMin = aMin; r.anchorMax = aMax;
            r.pivot = new Vector2(0.5f, 0f);
            r.anchoredPosition = pos; r.sizeDelta = sz;
        }

        static void MkBtn(Transform p, string label, Font font,
                          Vector2 pos, Vector2 sz,
                          Color bg, Color border, Color txt,
                          UnityEngine.Events.UnityAction onClick)
        {
            // Drop shadow
            var sh = new GameObject(label + "_Sh"); sh.transform.SetParent(p, false);
            var si = sh.AddComponent<Image>(); si.color = new Color(0f, 0.04f, 0.16f, 0.50f);
            var sr = si.rectTransform;
            sr.anchorMin = sr.anchorMax = sr.pivot = new Vector2(0.5f, 0.5f);
            sr.anchoredPosition = pos + new Vector2(3f, -5f); sr.sizeDelta = sz + new Vector2(6f, 2f);

            // Glow border
            var bd = new GameObject(label + "_Bd"); bd.transform.SetParent(p, false);
            var bi = bd.AddComponent<Image>(); bi.color = border;
            var br = bi.rectTransform;
            br.anchorMin = br.anchorMax = br.pivot = new Vector2(0.5f, 0.5f);
            br.anchoredPosition = pos; br.sizeDelta = sz + new Vector2(4f, 4f);

            // Button body
            var go = new GameObject(label + "_Btn"); go.transform.SetParent(p, false);
            var img = go.AddComponent<Image>(); img.color = bg;
            var btn = go.AddComponent<Button>();
            var cb  = btn.colors;
            cb.normalColor      = bg;
            cb.highlightedColor = Color.Lerp(bg, Color.white, 0.30f);
            cb.pressedColor     = Color.Lerp(bg, new Color(0.05f, 0.35f, 0.9f), 0.60f);
            cb.selectedColor    = bg;
            cb.fadeDuration     = 0.08f;
            btn.colors = cb;
            btn.onClick.AddListener(onClick);
            var r = go.GetComponent<RectTransform>();
            r.anchorMin = r.anchorMax = r.pivot = new Vector2(0.5f, 0.5f);
            r.anchoredPosition = pos; r.sizeDelta = sz;

            // Label text
            var lg = new GameObject("L"); lg.transform.SetParent(go.transform, false);
            var t  = lg.AddComponent<Text>();
            if (font != null) t.font = font;
            t.fontSize = 28; t.fontStyle = FontStyle.Bold;
            t.text = label; t.color = txt;
            t.alignment = TextAnchor.MiddleCenter;
            var lr = t.rectTransform;
            lr.anchorMin = Vector2.zero; lr.anchorMax = Vector2.one;
            lr.offsetMin = lr.offsetMax = Vector2.zero;
        }
    }
}
