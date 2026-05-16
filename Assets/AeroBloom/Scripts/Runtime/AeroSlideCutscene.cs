using System.Collections;
using UnityEngine;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace AeroBloom
{
    /// <summary>
    /// Full-screen slide cutscene with crossfades and English subtitles.
    /// </summary>
    public sealed class AeroSlideCutscene : MonoBehaviour
    {
        private CanvasGroup rootGroup;
        private RawImage    layerA;
        private RawImage    layerB;
        private CanvasGroup layerAGroup;
        private CanvasGroup layerBGroup;
        private RectTransform layerARect;
        private RectTransform layerBRect;
        private Image       blackOverlay;
        private Text        subtitleText;
        private Text        hintText;
        private Font        font;

        private bool skipSlide;
        private bool skipAll;

        public static void Play(System.Action onFinished)
        {
            AeroCutsceneConfig config = Resources.Load<AeroCutsceneConfig>("AeroCutsceneConfig");
            if (config == null || config.slides == null || config.slides.Length == 0)
            {
                onFinished?.Invoke();
                return;
            }

            var go = new GameObject("AeroSlideCutscene");
            DontDestroyOnLoad(go);
            var player = go.AddComponent<AeroSlideCutscene>();
            player.StartCoroutine(player.Run(config, onFinished));
        }

        private void Update()
        {
            if (WasAnyPressed())
                skipSlide = true;
#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
                skipAll = true;
#else
            if (Input.GetKeyDown(KeyCode.Escape))
                skipAll = true;
#endif
        }

        private static bool WasAnyPressed()
        {
#if ENABLE_INPUT_SYSTEM
            if (Mouse.current != null &&
                (Mouse.current.leftButton.wasPressedThisFrame ||
                 Mouse.current.rightButton.wasPressedThisFrame))
                return true;
            if (Keyboard.current != null && Keyboard.current.anyKey.wasPressedThisFrame)
                return true;
            return false;
#else
            return Input.anyKeyDown;
#endif
        }

        private IEnumerator Run(AeroCutsceneConfig config, System.Action onFinished)
        {
            BuildUI();
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible   = true;

            blackOverlay.color = Color.black;
            layerAGroup.alpha  = 0f;
            layerBGroup.alpha  = 0f;
            rootGroup.alpha    = 1f;

            yield return FadeOverlay(blackOverlay, 1f, 0f, config.fadeFromBlack);

            for (int i = 0; i < config.slides.Length; i++)
            {
                if (skipAll) break;

                string sub = (config.subtitles != null && i < config.subtitles.Length)
                    ? config.subtitles[i]
                    : "";
                subtitleText.text = sub;

                if (i == 0)
                    yield return ShowFirstSlide(config, config.slides[i]);
                else
                    yield return CrossfadeSlide(config, config.slides[i], i % 2 == 0);

                skipSlide = false;
            }

            subtitleText.text = "";
            yield return FadeOverlay(blackOverlay, 0f, 1f, config.fadeToGame);

            Destroy(gameObject);
            onFinished?.Invoke();
        }

        private IEnumerator ShowFirstSlide(AeroCutsceneConfig config, Texture2D tex)
        {
            SetSlide(layerA, layerARect, tex);
            layerAGroup.alpha = 0f;
            layerBGroup.alpha = 0f;

            const float fadeIn = 0.95f;
            float t = 0f;
            while (t < fadeIn && !skipSlide && !skipAll)
            {
                t += Time.deltaTime;
                layerAGroup.alpha = Mathf.SmoothStep(0f, 1f, t / fadeIn);
                yield return null;
            }

            layerAGroup.alpha = 1f;

            t = 0f;
            float hold = config.slideHold;
            while (t < hold && !skipSlide && !skipAll)
            {
                t += Time.deltaTime;
                ApplyKenBurns(layerARect, t, hold, config.kenBurnsScale);
                yield return null;
            }
        }

        private IEnumerator CrossfadeSlide(AeroCutsceneConfig config, Texture2D tex, bool useAAsFront)
        {
            RawImage frontImg = useAAsFront ? layerA : layerB;
            RawImage backImg  = useAAsFront ? layerB : layerA;
            CanvasGroup frontG = useAAsFront ? layerAGroup : layerBGroup;
            CanvasGroup backG  = useAAsFront ? layerBGroup : layerAGroup;
            RectTransform frontR = useAAsFront ? layerARect : layerBRect;
            RectTransform backR  = useAAsFront ? layerBRect : layerARect;

            SetSlide(backImg, backR, tex);
            backG.alpha  = 1f;
            frontG.alpha = 1f;

            float t = 0f;
            float dur = config.crossfade;
            float hold = config.slideHold;

            while (t < dur + hold && !skipAll)
            {
                t += Time.deltaTime;
                if (t <= dur)
                {
                    float u = Mathf.SmoothStep(0f, 1f, t / dur);
                    frontG.alpha = 1f - u;
                    backG.alpha  = 1f;
                }
                else
                {
                    frontG.alpha = 0f;
                    backG.alpha  = 1f;
                    ApplyKenBurns(backR, t - dur, hold, config.kenBurnsScale);
                }

                if (skipSlide && t > dur * 0.25f)
                    break;

                yield return null;
            }

            frontG.alpha = 0f;
            backG.alpha  = 1f;
        }

        private static void ApplyKenBurns(RectTransform rect, float elapsed, float hold, float targetScale)
        {
            float u = hold > 0.01f ? Mathf.Clamp01(elapsed / hold) : 1f;
            float s = Mathf.Lerp(1f, targetScale, u);
            rect.localScale = new Vector3(s, s, 1f);
        }

        private static void SetSlide(RawImage img, RectTransform rect, Texture2D tex)
        {
            img.texture = tex;
            rect.localScale = Vector3.one;
        }

        private static IEnumerator FadeOverlay(Image img, float from, float to, float dur)
        {
            float t = 0f;
            Color c = img.color;
            while (t < dur)
            {
                t += Time.deltaTime;
                float a = Mathf.SmoothStep(from, to, t / dur);
                c.a = a;
                img.color = c;
                yield return null;
            }

            c.a = to;
            img.color = c;
        }

        private void BuildUI()
        {
            var cv = gameObject.AddComponent<Canvas>();
            cv.renderMode   = RenderMode.ScreenSpaceOverlay;
            cv.sortingOrder = 150;

            var cs = gameObject.AddComponent<CanvasScaler>();
            cs.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            cs.referenceResolution = new Vector2(1920f, 1080f);
            cs.matchWidthOrHeight  = 0.5f;

            gameObject.AddComponent<GraphicRaycaster>();
            rootGroup = gameObject.AddComponent<CanvasGroup>();
            rootGroup.alpha = 1f;

            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (font == null) font = Resources.GetBuiltinResource<Font>("Arial.ttf");

            Transform root = transform;

            layerA = CreateSlideLayer(root, "SlideA", out layerARect, out layerAGroup);
            layerB = CreateSlideLayer(root, "SlideB", out layerBRect, out layerBGroup);

            blackOverlay = CreateFullImage(root, "Black", new Color(0f, 0f, 0f, 1f));
            blackOverlay.transform.SetAsLastSibling();

            Image subBg = CreateFullImage(root, "SubtitleBg",
                new Color(0.02f, 0.08f, 0.18f, 0.72f));
            var subBgRect = subBg.rectTransform;
            subBgRect.anchorMin = new Vector2(0f, 0f);
            subBgRect.anchorMax = new Vector2(1f, 0f);
            subBgRect.pivot     = new Vector2(0.5f, 0f);
            subBgRect.sizeDelta = new Vector2(0f, 140f);

            subtitleText = CreateText(root, "Subtitle", 26, FontStyle.Normal,
                "", TextAnchor.MiddleCenter,
                new Vector2(0.05f, 0.04f), new Vector2(0.95f, 0.20f),
                new Color(0.95f, 0.99f, 1f, 0.98f));

            hintText = CreateText(root, "Hint", 15, FontStyle.Italic,
                "Click or press any key to continue",
                TextAnchor.MiddleCenter,
                new Vector2(0.3f, 0.005f), new Vector2(0.7f, 0.045f),
                new Color(0.65f, 0.92f, 1f, 0.75f));

            StartCoroutine(PulseHint());
        }

        private IEnumerator PulseHint()
        {
            while (hintText != null)
            {
                float a = 0.45f + 0.35f * (0.5f + 0.5f * Mathf.Sin(Time.time * 2.2f));
                Color c = hintText.color;
                c.a = a;
                hintText.color = c;
                yield return null;
            }
        }

        private static RawImage CreateSlideLayer(Transform parent, string name,
            out RectTransform rect, out CanvasGroup group)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            rect = go.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            var raw = go.AddComponent<RawImage>();
            raw.color = Color.white;
            group = go.AddComponent<CanvasGroup>();
            group.alpha = 0f;
            return raw;
        }

        private static Image CreateFullImage(Transform parent, string name, Color color)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var img = go.AddComponent<Image>();
            img.color = color;
            var r = img.rectTransform;
            r.anchorMin = Vector2.zero;
            r.anchorMax = Vector2.one;
            r.offsetMin = Vector2.zero;
            r.offsetMax = Vector2.zero;
            return img;
        }

        private Text CreateText(Transform parent, string name, int size, FontStyle style,
            string content, TextAnchor anchor, Vector2 anchorMin, Vector2 anchorMax, Color color)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var txt = go.AddComponent<Text>();
            txt.font      = font;
            txt.fontSize  = size;
            txt.fontStyle = style;
            txt.text      = content;
            txt.alignment = anchor;
            txt.color     = color;
            txt.horizontalOverflow = HorizontalWrapMode.Wrap;
            txt.verticalOverflow   = VerticalWrapMode.Overflow;
            var r = txt.rectTransform;
            r.anchorMin = anchorMin;
            r.anchorMax = anchorMax;
            r.offsetMin = Vector2.zero;
            r.offsetMax = Vector2.zero;
            return txt;
        }
    }
}
