using System.Collections;
using UnityEngine;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace AeroBloom
{
    /// <summary>
    /// Full-screen Frutiger Aero victory story screen shown when the run ends.
    /// Appears before the results table.  Call AeroVictoryScreen.Show(...).
    /// </summary>
    public sealed class AeroVictoryScreen : MonoBehaviour
    {
        private CanvasGroup group;
        private Text        promptTxt;
        private Image       sheen;
        private bool        advance;

        // ─────────────────────────────────────────────────────────
        public static void Show(string formattedTime, bool fullBloom, System.Action onContinue)
        {
            var go = new GameObject("AeroVictoryScreen");
            DontDestroyOnLoad(go);
            var vs = go.AddComponent<AeroVictoryScreen>();
            vs.StartCoroutine(vs.Sequence(formattedTime, fullBloom, onContinue));
        }

        private void Update()
        {
#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current != null &&
                (Keyboard.current.spaceKey.wasPressedThisFrame ||
                 Keyboard.current.enterKey.wasPressedThisFrame ||
                 Keyboard.current.escapeKey.wasPressedThisFrame))
                advance = true;
            if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
                advance = true;
#else
            if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return) ||
                Input.GetKeyDown(KeyCode.Escape) || Input.GetMouseButtonDown(0))
                advance = true;
#endif
        }

        private IEnumerator Sequence(string time, bool fullBloom, System.Action onContinue)
        {
            BuildUI(time, fullBloom);

            yield return FadeGroup(group, 0f, 1f, 1.0f);

            promptTxt.gameObject.SetActive(true);
            StartCoroutine(PulseAlpha(promptTxt, 0.4f, 1f, 0.65f));

            yield return new WaitForSeconds(0.8f);
            advance = false;
            while (!advance) yield return null;

            yield return FadeGroup(group, 1f, 0f, 0.45f);

            Destroy(gameObject);
            onContinue?.Invoke();
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
        // BUILD
        // ─────────────────────────────────────────────────────────
        private void BuildUI(string time, bool fullBloom)
        {
            var cv = gameObject.AddComponent<Canvas>();
            cv.renderMode   = RenderMode.ScreenSpaceOverlay;
            cv.sortingOrder = 50; // above HUD (20), below intro (120)

            var cs = gameObject.AddComponent<CanvasScaler>();
            cs.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            cs.referenceResolution = new Vector2(1280f, 720f);
            cs.matchWidthOrHeight  = 0.5f;

            gameObject.AddComponent<GraphicRaycaster>();

            group       = gameObject.AddComponent<CanvasGroup>();
            group.alpha = 0f;

            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (font == null) font = Resources.GetBuiltinResource<Font>("Arial.ttf");

            Transform r = transform;

            // ── Sky — warmer for full bloom, standard Aero for partial ─
            Color skyBase = fullBloom
                ? new Color(0.28f, 0.60f, 0.92f)
                : new Color(0.26f, 0.56f, 0.86f);
            Img(r, "Sky",     V(0,0),      V(1,1),      skyBase);
            Img(r, "SkyUp",   V(0,0.46f),  V(1,1f),     new Color(0.14f, 0.40f, 0.82f, 0.65f));
            Img(r, "SkyDwn",  V(0,0f),     V(1,0.54f),  new Color(0.55f, 0.84f, 1.00f, 0.50f));

            // Gold bloom for full-bloom case
            if (fullBloom)
            {
                Img(r, "GoldB1", V(0.22f,0.40f), V(0.78f,1f), new Color(1f, 0.95f, 0.70f, 0.06f));
                Img(r, "GoldB2", V(0.35f,0.55f), V(0.65f,1f), new Color(1f, 0.90f, 0.60f, 0.04f));
            }
            else
            {
                Img(r, "Bloom1", V(0.20f,0.42f), V(0.80f,1f), new Color(1f,1f,1f,0.045f));
                Img(r, "Bloom2", V(0.35f,0.56f), V(0.65f,1f), new Color(1f,1f,1f,0.032f));
            }

            // Floating bubbles (fewer than intro — keeps it clean)
            float[] bx = { 0.13f, 0.82f, 0.88f, 0.15f, 0.60f };
            float[] by = { 0.72f, 0.20f, 0.58f, 0.38f, 0.14f };
            float[] bs = { 0.045f, 0.030f, 0.018f, 0.038f, 0.024f };
            for (int i = 0; i < bx.Length; i++)
            {
                var bub = Img(r, "Bub" + i,
                    V(bx[i]-bs[i], by[i]-bs[i]),
                    V(bx[i]+bs[i], by[i]+bs[i]),
                    new Color(0.80f, 0.94f, 1f, 0.18f));
                Img(bub.transform, "Spec", V(0.14f,0.55f), V(0.52f,0.86f), new Color(1f,1f,1f,0.55f));
                StartCoroutine(FloatBubble(bub, bx[i], by[i], bs[i], i * 1.6f));
            }

            // Moving sheen
            sheen = Img(r, "Sheen", V(0,0), V(1,0.007f), new Color(1f,1f,1f,0.05f));
            StartCoroutine(AnimateSheen());

            // ── Card shadow + card ───────────────────────────────────
            Img(r, "Shadow", V(0.05f,0.10f), V(0.95f,0.92f), new Color(0.10f,0.36f,0.72f,0.24f));

            var card = Img(r, "Card", V(0.06f,0.11f), V(0.94f,0.91f),
                new Color(0.91f, 0.96f, 1f, 0.88f));
            Transform cp = card.transform;

            // Gloss — top 13%
            Img(cp, "Gloss",  V(0f,0.87f), V(1f,1f),    new Color(1f,1f,1f, 0.50f));
            Img(cp, "Gloss2", V(0f,0f),    V(1f,0.05f), new Color(1f,1f,1f, 0.18f));

            // Card borders
            HBorder(cp, 1f, new Color(0.22f,0.70f,0.98f,0.80f), 2);
            HBorder(cp, 0f, new Color(0.22f,0.70f,0.98f,0.40f), 2);
            VBorder(cp, 0f, new Color(0.22f,0.70f,0.98f,0.60f), 2);
            VBorder(cp, 1f, new Color(0.22f,0.70f,0.98f,0.30f), 2);

            // Left accent — gold for full bloom, cyan otherwise
            Color accentCol = fullBloom
                ? new Color(1f, 0.88f, 0.25f, 0.90f)
                : new Color(0.06f, 0.60f, 0.96f, 0.90f);
            Img(cp, "Accent", V(0f,0.02f), V(0.007f,0.98f), accentCol);

            // Header bg
            Img(cp, "HdrBg", V(0f,0.76f), V(1f,1f), new Color(0.46f,0.74f,0.96f,0.18f));

            // ── HEADER ───────────────────────────────────────────────
            // Status tag (top-right)
            Txt(cp, "Tag", font, 11, FontStyle.Normal,
                fullBloom ? "FULL  BLOOM  ◆  S-RANK" : "RUN  COMPLETE",
                fullBloom ? new Color(1f, 0.88f, 0.20f, 0.85f)
                          : new Color(0.08f, 0.40f, 0.76f, 0.65f),
                TextAnchor.MiddleRight, V(0.55f,0.89f), V(0.97f,0.97f));

            // Main title
            string titleStr = fullBloom ? "THE  AEROBLOOM  IS  RESTORED" : "RUN  COMPLETE";
            Color titleCol  = fullBloom
                ? new Color(0.60f, 0.30f, 0.04f, 1f)
                : new Color(0.04f, 0.14f, 0.42f, 1f);
            Txt(cp, "Title", font, fullBloom ? 24 : 27, FontStyle.Bold,
                titleStr, titleCol, TextAnchor.MiddleLeft,
                V(0.028f,0.80f), V(0.88f,0.97f));

            // Subtitle
            string sub = fullBloom
                ? "The digital world glows once more."
                : "The AeroBloom stirs with partial light.";
            Txt(cp, "Sub", font, 13, FontStyle.Normal,
                sub, new Color(0.06f,0.38f,0.74f,0.80f), TextAnchor.MiddleLeft,
                V(0.028f,0.74f), V(0.80f,0.81f));

            Divider(cp, 0.760f, new Color(0.10f,0.50f,0.88f,0.30f));
            Divider(cp, 0.750f, new Color(0.10f,0.50f,0.88f,0.12f));

            // ── STORY BODY ────────────────────────────────────────────
            string body = fullBloom
                ? "You gathered every scattered seed and climbed to the summit.\n" +
                  "The AeroBloom blooms once more — and with it, the digital world\n" +
                  "awakens in radiant, glowing harmony."
                : "You reached the summit, but some seeds remain scattered.\n" +
                  "The AeroBloom stirs — its light incomplete.\n" +
                  "Return and gather what was left behind.";

            var bodyTxt = Txt(cp, "Body", font, 17, FontStyle.Normal,
                body, new Color(0.06f,0.18f,0.44f,0.92f), TextAnchor.UpperLeft,
                V(0.028f,0.36f), V(0.97f,0.72f));
            bodyTxt.lineSpacing = 1.62f;

            Divider(cp, 0.345f, new Color(0.10f,0.50f,0.88f,0.24f));

            // ── TIME / STATS BOX ─────────────────────────────────────
            Img(cp, "StBg",  V(0f,0.02f),  V(1f,0.335f),  new Color(0.55f,0.80f,1f,0.38f));
            Img(cp, "StTop", V(0f,0.300f), V(1f,0.335f),  new Color(1f,1f,1f,0.22f));
            Img(cp, "StAcc", V(0f,0.03f),  V(0.004f,0.325f),
                fullBloom ? new Color(1f,0.88f,0.25f,0.78f) : new Color(0.06f,0.60f,0.96f,0.78f));

            Txt(cp, "TimeLabel", font, 12, FontStyle.Normal,
                "FINAL  TIME",
                new Color(0.06f,0.38f,0.72f,0.75f), TextAnchor.MiddleLeft,
                V(0.028f,0.285f), V(0.35f,0.330f));

            Txt(cp, "TimeVal", font, 26, FontStyle.Bold,
                time,
                fullBloom ? new Color(0.55f,0.28f,0.02f,1f)
                          : new Color(0.02f,0.20f,0.66f,1f),
                TextAnchor.MiddleLeft,
                V(0.028f,0.05f), V(0.55f,0.285f));

            // ── PROMPT ────────────────────────────────────────────────
            promptTxt = Txt(r, "Prompt", font, 19, FontStyle.Bold,
                "PRESS  SPACE  ·  CLICK  FOR  RESULTS",
                new Color(0.04f, 0.24f, 0.68f, 1f), TextAnchor.MiddleCenter,
                V(0.18f,0.02f), V(0.82f,0.09f));
            promptTxt.gameObject.SetActive(false);

            Txt(r, "Mark", font, 12, FontStyle.Bold,
                "A E R O B L O O M",
                new Color(0.08f,0.36f,0.70f,0.22f), TextAnchor.MiddleRight,
                V(0.75f,0.01f), V(0.98f,0.06f));
        }

        // ── Animations ─────────────────────────────────────────────
        private IEnumerator AnimateSheen()
        {
            while (sheen != null)
            {
                float y = (Time.time * 0.07f) % 1f;
                var rt = sheen.rectTransform;
                rt.anchorMin = new Vector2(0f, y);
                rt.anchorMax = new Vector2(1f, y + 0.006f);
                sheen.color  = new Color(1f, 1f, 1f, 0.04f + 0.025f * Mathf.Sin(Time.time * 0.45f));
                yield return null;
            }
        }

        private IEnumerator FloatBubble(Image bub, float cx, float cy, float r, float phase)
        {
            while (bub != null)
            {
                float t  = Time.time + phase;
                float nx = cx + Mathf.Sin(t * 0.22f) * 0.010f;
                float ny = cy + Mathf.Cos(t * 0.17f) * 0.016f;
                float a  = 0.13f + 0.07f * Mathf.Sin(t * 0.38f);
                var rt = bub.rectTransform;
                rt.anchorMin = new Vector2(nx - r, ny - r);
                rt.anchorMax = new Vector2(nx + r, ny + r);
                bub.color = new Color(0.80f, 0.94f, 1f, a);
                yield return null;
            }
        }

        // ── Helpers ────────────────────────────────────────────────
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
