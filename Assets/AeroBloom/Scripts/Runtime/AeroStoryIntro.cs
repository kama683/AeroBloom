using System.Collections;
using UnityEngine;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace AeroBloom
{
    /// <summary>
    /// Pre-history lore screen — Frutiger Aero style.
    /// Bright sky, glass panel, floating bubbles, gloss highlights.
    /// </summary>
    public sealed class AeroStoryIntro : MonoBehaviour
    {
        // ── Story content ──────────────────────────────────────────
        private const string TitleText    = "AEROBLOOM  CHRONICLES";
        private const string SubtitleText = "Origin Archive  ·  001";

        private const string BodyText =
            "In the digital realm, beauty was maintained\n" +
            "by a living flower of light — the AeroBloom.\n\n" +
            "It held the world in perfect, glowing harmony.\n" +
            "Then it shattered.  Corrupted.  Scattered.\n\n" +
            "Its seeds drift across the digital sky,\n" +
            "waiting for the one who can restore them.";

        private const string CalloutText =
            "YOU  ARE  THE  SPIRIT\n\n" +
            "Climb to the summit.  Gather the seeds.\n" +
            "Restore the AeroBloom.";

        // ── State ──────────────────────────────────────────────────
        private CanvasGroup rootGroup;
        private Text        promptTxt;
        private Image       sheen;
        private bool        advance;
        private bool        skipAll;

        // ─────────────────────────────────────────────────────────
        public static void Play(System.Action onFinished)
        {
            var go = new GameObject("AeroStoryIntro");
            DontDestroyOnLoad(go);
            var intro = go.AddComponent<AeroStoryIntro>();
            intro.StartCoroutine(intro.Sequence(onFinished));
        }

        // ── Input ─────────────────────────────────────────────────
        private void Update()
        {
#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current != null &&
                (Keyboard.current.spaceKey.wasPressedThisFrame ||
                 Keyboard.current.enterKey.wasPressedThisFrame))
                advance = true;
            if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
                advance = true;
            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
                skipAll = true;
#else
            if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return) ||
                Input.GetMouseButtonDown(0))
                advance = true;
            if (Input.GetKeyDown(KeyCode.Escape))
                skipAll = true;
#endif
        }

        // ── Main sequence ──────────────────────────────────────────
        private IEnumerator Sequence(System.Action onFinished)
        {
            BuildUI();
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible   = true;

            AeroLevelDirector.Instance?.PlayUiTone(523f, 0.28f, 0.10f);

            // Fade entire scene in at once — sky + glass panel + bubbles
            yield return FadeGroup(rootGroup, 0f, 1f, 1.1f);

            promptTxt.gameObject.SetActive(true);
            StartCoroutine(PulseAlpha(promptTxt, 0.40f, 1f, 0.65f));

            yield return new WaitForSeconds(0.7f); // grace — prevent accidental skip
            advance = false;
            while (!advance && !skipAll) yield return null;

            yield return FadeGroup(rootGroup, 1f, 0f, 0.55f);

            Destroy(gameObject);
            onFinished?.Invoke();
        }

        // ── Fades ──────────────────────────────────────────────────
        private static IEnumerator FadeGroup(CanvasGroup cg, float from, float to, float dur)
        {
            float t = 0f;
            while (t < dur)
            {
                t += Time.deltaTime;
                cg.alpha = Mathf.SmoothStep(from, to, t / dur);
                yield return null;
            }
            cg.alpha = to;
        }

        private IEnumerator PulseAlpha(Graphic g, float lo, float hi, float speed)
        {
            while (g != null && g.gameObject.activeInHierarchy)
            {
                float a = Mathf.Lerp(lo, hi, 0.5f + 0.5f * Mathf.Sin(Time.time * speed * Mathf.PI));
                var c = g.color; c.a = a; g.color = c;
                yield return null;
            }
        }

        // ─────────────────────────────────────────────────────────
        // BUILD UI  — Frutiger Aero
        // ─────────────────────────────────────────────────────────
        private void BuildUI()
        {
            var cv = gameObject.AddComponent<Canvas>();
            cv.renderMode   = RenderMode.ScreenSpaceOverlay;
            cv.sortingOrder = 120;

            var cs = gameObject.AddComponent<CanvasScaler>();
            cs.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            cs.referenceResolution = new Vector2(1280f, 720f);
            cs.matchWidthOrHeight  = 0.5f;

            gameObject.AddComponent<GraphicRaycaster>();

            rootGroup       = gameObject.AddComponent<CanvasGroup>();
            rootGroup.alpha = 0f;

            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (font == null) font = Resources.GetBuiltinResource<Font>("Arial.ttf");

            Transform r = transform;

            // ── BACKGROUND — clean Frutiger Aero sky ─────────────────
            Img(r, "SkyBase",  V(0f, 0f),    V(1f, 1f),    new Color(0.30f, 0.62f, 0.88f));
            Img(r, "SkyUpper", V(0f, 0.48f), V(1f, 1f),    new Color(0.16f, 0.44f, 0.80f, 0.62f));
            Img(r, "SkyLower", V(0f, 0f),    V(1f, 0.52f), new Color(0.58f, 0.86f, 1.00f, 0.50f));

            // Wide soft bloom at center — NOT narrow rays (those look like UI artifacts)
            Img(r, "Bloom0", V(0.15f, 0.40f), V(0.85f, 1f), new Color(1f, 1f, 1f, 0.048f));
            Img(r, "Bloom1", V(0.28f, 0.55f), V(0.72f, 1f), new Color(1f, 1f, 1f, 0.038f));
            Img(r, "Bloom2", V(0.38f, 0.66f), V(0.62f, 1f), new Color(1f, 1f, 1f, 0.030f));

            // Subtle moving sheen
            sheen = Img(r, "Sheen", V(0f, 0f), V(1f, 0.007f), new Color(1f, 1f, 1f, 0.055f));
            StartCoroutine(AnimateSheen());

            // ── FLOATING BUBBLES — all safely within screen margins ───
            // min x = bx - bs >= 0.10,  max x = bx + bs <= 0.90
            float[] bx = { 0.16f, 0.24f, 0.75f, 0.84f, 0.87f, 0.14f, 0.62f, 0.50f };
            float[] by = { 0.74f, 0.20f, 0.80f, 0.16f, 0.54f, 0.38f, 0.11f, 0.86f };
            float[] bs = { 0.048f, 0.028f, 0.058f, 0.032f, 0.018f, 0.040f, 0.024f, 0.046f };
            for (int i = 0; i < bx.Length; i++)
            {
                var bub = Img(r, "Bub" + i,
                    V(bx[i]-bs[i], by[i]-bs[i]),
                    V(bx[i]+bs[i], by[i]+bs[i]),
                    new Color(0.80f, 0.94f, 1f, 0.18f));
                Img(bub.transform, "Spec", V(0.14f, 0.55f), V(0.52f, 0.86f),
                    new Color(1f, 1f, 1f, 0.58f));
                StartCoroutine(FloatBubble(bub, bx[i], by[i], bs[i], i * 1.37f));
            }

            // ── CARD SHADOW ───────────────────────────────────────────
            Img(r, "Shadow", V(0.048f, 0.104f), V(0.952f, 0.898f),
                new Color(0.10f, 0.36f, 0.70f, 0.26f));

            // ── MAIN GLASS CARD ───────────────────────────────────────
            var card = Img(r, "Card", V(0.05f, 0.11f), V(0.95f, 0.89f),
                new Color(0.91f, 0.96f, 1f, 0.87f));
            Transform cp = card.transform;

            // Top gloss — only top 14%, not half the card
            Img(cp, "Gloss",  V(0f, 0.86f), V(1f, 1f),    new Color(1f, 1f, 1f, 0.52f));
            Img(cp, "Gloss2", V(0f, 0f),    V(1f, 0.055f), new Color(1f, 1f, 1f, 0.18f));

            // Card borders
            HBorder(cp, 1f, new Color(0.22f, 0.68f, 0.96f, 0.82f), 2);
            HBorder(cp, 0f, new Color(0.22f, 0.68f, 0.96f, 0.42f), 2);
            VBorder(cp, 0f, new Color(0.22f, 0.68f, 0.96f, 0.62f), 2);
            VBorder(cp, 1f, new Color(0.22f, 0.68f, 0.96f, 0.32f), 2);

            // Left accent bar
            Img(cp, "Accent", V(0f, 0.02f), V(0.006f, 0.98f),
                new Color(0.06f, 0.60f, 0.96f, 0.90f));

            // ── HEADER ───────────────────────────────────────────────
            Img(cp, "HdrBg", V(0f, 0.78f), V(1f, 1f),
                new Color(0.46f, 0.74f, 0.96f, 0.20f));

            Txt(cp, "Stamp", font, 11, FontStyle.Normal,
                "ARCHIVE  001",
                new Color(0.08f, 0.38f, 0.72f, 0.62f), TextAnchor.MiddleRight,
                V(0.60f, 0.89f), V(0.97f, 0.97f));

            Txt(cp, "Title", font, 28, FontStyle.Bold,
                TitleText,
                new Color(0.04f, 0.13f, 0.40f, 1f), TextAnchor.MiddleLeft,
                V(0.028f, 0.81f), V(0.75f, 0.97f));

            Txt(cp, "Sub", font, 12, FontStyle.Normal,
                SubtitleText,
                new Color(0.06f, 0.38f, 0.74f, 0.82f), TextAnchor.MiddleLeft,
                V(0.028f, 0.75f), V(0.65f, 0.82f));

            Divider(cp, 0.768f, new Color(0.10f, 0.50f, 0.88f, 0.30f));
            Divider(cp, 0.758f, new Color(0.10f, 0.50f, 0.88f, 0.12f));

            // ── BODY ─────────────────────────────────────────────────
            var bodyTxt = Txt(cp, "Body", font, 17, FontStyle.Normal,
                BodyText,
                new Color(0.06f, 0.18f, 0.44f, 0.92f), TextAnchor.UpperLeft,
                V(0.028f, 0.30f), V(0.97f, 0.74f));
            bodyTxt.lineSpacing = 1.62f;

            Divider(cp, 0.290f, new Color(0.10f, 0.50f, 0.88f, 0.26f));

            // ── CALLOUT ──────────────────────────────────────────────
            Img(cp, "CoBg",  V(0f, 0.02f),  V(1f, 0.284f),  new Color(0.55f, 0.80f, 1f, 0.40f));
            Img(cp, "CoTop", V(0f, 0.250f), V(1f, 0.284f),  new Color(1f, 1f, 1f, 0.24f));
            Img(cp, "CoAcc", V(0f, 0.03f),  V(0.004f, 0.272f), new Color(0.06f, 0.60f, 0.96f, 0.78f));

            var calloutTxt = Txt(cp, "Callout", font, 16, FontStyle.Bold,
                CalloutText,
                new Color(0.02f, 0.20f, 0.66f, 1f), TextAnchor.MiddleLeft,
                V(0.028f, 0.03f), V(0.97f, 0.284f));
            calloutTxt.lineSpacing = 1.52f;

            // ── PROMPT ───────────────────────────────────────────────
            promptTxt = Txt(r, "Prompt", font, 19, FontStyle.Bold,
                "PRESS  SPACE  ·  CLICK  TO  BEGIN",
                new Color(0.04f, 0.24f, 0.68f, 1f), TextAnchor.MiddleCenter,
                V(0.20f, 0.02f), V(0.80f, 0.10f));
            promptTxt.gameObject.SetActive(false);

            Txt(r, "Esc", font, 12, FontStyle.Normal,
                "ESC — skip",
                new Color(0.10f, 0.36f, 0.65f, 0.45f), TextAnchor.MiddleLeft,
                V(0.02f, 0.01f), V(0.15f, 0.06f));

            Txt(r, "Mark", font, 12, FontStyle.Bold,
                "A E R O B L O O M",
                new Color(0.08f, 0.36f, 0.70f, 0.22f), TextAnchor.MiddleRight,
                V(0.75f, 0.01f), V(0.98f, 0.06f));
        }

        // ── Moving sheen — bright band sweeping upward ─────────────
        private IEnumerator AnimateSheen()
        {
            while (sheen != null)
            {
                float y = (Time.time * 0.07f) % 1f;
                var rt = sheen.rectTransform;
                rt.anchorMin = new Vector2(0f, y);
                rt.anchorMax = new Vector2(1f, y + 0.006f);
                sheen.color  = new Color(1f, 1f, 1f, 0.042f + 0.028f * Mathf.Sin(Time.time * 0.45f));
                yield return null;
            }
        }

        // ── Bubble float animation ─────────────────────────────────
        private IEnumerator FloatBubble(Image bub, float cx, float cy, float r, float phase)
        {
            while (bub != null)
            {
                float t  = Time.time + phase;
                float nx = cx + Mathf.Sin(t * 0.22f) * 0.010f;
                float ny = cy + Mathf.Cos(t * 0.17f) * 0.016f;
                float a  = 0.14f + 0.08f * Mathf.Sin(t * 0.38f);
                var rt = bub.rectTransform;
                rt.anchorMin = new Vector2(nx - r, ny - r);
                rt.anchorMax = new Vector2(nx + r, ny + r);
                bub.color = new Color(0.76f, 0.92f, 1f, a);
                yield return null;
            }
        }

        // ─────────────────────────────────────────────────────────
        // UI HELPERS
        // ─────────────────────────────────────────────────────────
        private static Vector2 V(float x, float y) => new Vector2(x, y);

        private static Image Img(Transform p, string n, Vector2 aMin, Vector2 aMax, Color c)
        {
            var go  = new GameObject(n); go.transform.SetParent(p, false);
            var img = go.AddComponent<Image>();
            img.color = c; img.raycastTarget = false;
            var rt = img.rectTransform;
            rt.anchorMin = aMin; rt.anchorMax = aMax;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            return img;
        }

        private static Text Txt(Transform p, string n, Font f, int sz, FontStyle sty,
            string text, Color c, TextAnchor anchor, Vector2 aMin, Vector2 aMax)
        {
            var go = new GameObject(n); go.transform.SetParent(p, false);
            var t  = go.AddComponent<Text>();
            t.font = f; t.fontSize = sz; t.fontStyle = sty;
            t.text = text; t.color = c; t.alignment = anchor;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow   = VerticalWrapMode.Overflow;
            t.raycastTarget = false;
            var rt = t.rectTransform;
            rt.anchorMin = aMin; rt.anchorMax = aMax;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            return t;
        }

        private static void Divider(Transform p, float y, Color c)
        {
            var i = Img(p, "Div", V(0.015f, y), V(0.985f, y), c);
            i.rectTransform.sizeDelta = new Vector2(0, 1);
        }

        private static void HBorder(Transform p, float y, Color c, float h)
        {
            var i = Img(p, "Bdr", V(0f, y), V(1f, y), c);
            i.rectTransform.sizeDelta = new Vector2(0, h);
        }

        private static void VBorder(Transform p, float x, Color c, float w)
        {
            var i = Img(p, "Bdr", V(x, 0f), V(x, 1f), c);
            i.rectTransform.sizeDelta = new Vector2(w, 0);
        }
    }
}
