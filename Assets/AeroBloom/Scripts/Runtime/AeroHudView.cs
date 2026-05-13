using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace AeroBloom
{
    public sealed class AeroHudView : MonoBehaviour
    {
        // ── HUD ──────────────────────────────────────────────────
        private Text  timerText;
        private Text  seedText;
        private Image seedFill;
        private Text  checkpointText;
        private Text  messageText;
        private Image messagePanel;

        // ── Finish screen ─────────────────────────────────────────
        private Image finishPanel;
        private Text  finishTitle;
        private Text  finishGrade;
        private Text  finishDetails;
        private bool  finishShown;
        private float finishAnimT;

        // ── Respawn fade ──────────────────────────────────────────
        private Image         fadeOverlay;
        private int           fadePhase;   // 0 idle | 1 in | 2 hold | 3 out
        private float         fadeTimer;
        private System.Action onFadedIn;
        private const float   FadeIn  = 0.40f;
        private const float   Hold    = 0.75f;
        private const float   FadeOut = 0.70f;

        // ─────────────────────────────────────────────────────────
        public static AeroHudView Create(AeroLevelDirector director)
        {
            var go = new GameObject("AeroBloom HUD");
            var cv = go.AddComponent<Canvas>();
            cv.renderMode   = RenderMode.ScreenSpaceOverlay;
            cv.sortingOrder = 20;
            cv.pixelPerfect = true;
            var cs = go.AddComponent<CanvasScaler>();
            cs.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            cs.referenceResolution = new Vector2(1280f, 720f);
            cs.matchWidthOrHeight  = 0.5f;
            go.AddComponent<GraphicRaycaster>();
            var hud = go.AddComponent<AeroHudView>();
            hud.Build(cv.transform, director);
            return hud;
        }

        // ─────────────────────────────────────────────────────────
        // Called by AeroLevelDirector.RespawnPlayer
        // The callback fires exactly when the screen is fully covered.
        public void TriggerRespawnFade(System.Action callback)
        {
            onFadedIn = callback;
            fadePhase = 1;
            fadeTimer = 0f;
        }

        // ─────────────────────────────────────────────────────────
        public void SetHud(string time, int seeds, int seedTotal,
                           int checkpoints, int checkpointTotal,
                           float speed, string message, bool finished)
        {
            timerText.text      = ShortTime(time);
            seedText.text       = "SEEDS  " + seeds + " / " + seedTotal;
            checkpointText.text = RelayDots(checkpoints, checkpointTotal);

            if (seedFill != null && seedTotal > 0)
                seedFill.rectTransform.sizeDelta =
                    new Vector2(258f * Mathf.Clamp01((float)seeds / seedTotal), 0f);

            bool showMsg       = !string.IsNullOrEmpty(message) && !finishShown;
            messagePanel.enabled = showMsg;
            messageText.enabled  = showMsg;
            messageText.text     = message;

            if (finished && !finishShown)
                ShowFinish(time, seeds, seedTotal, checkpoints, checkpointTotal);
        }

        // ─────────────────────────────────────────────────────────
        private void Update()
        {
            TickFade();
            TickFinishAnim();
        }

        private void TickFade()
        {
            if (fadePhase == 0 || fadeOverlay == null) return;
            fadeTimer += Time.deltaTime;
            float a;
            if (fadePhase == 1)
            {
                a = Mathf.SmoothStep(0f, 1f, fadeTimer / FadeIn);
                if (fadeTimer >= FadeIn)
                {
                    onFadedIn?.Invoke();
                    onFadedIn = null;
                    fadePhase = 2; fadeTimer = 0f;
                }
            }
            else if (fadePhase == 2)
            {
                a = 1f;
                if (fadeTimer >= Hold) { fadePhase = 3; fadeTimer = 0f; }
            }
            else
            {
                a = Mathf.SmoothStep(1f, 0f, fadeTimer / FadeOut);
                if (fadeTimer >= FadeOut) { fadePhase = 0; a = 0f; }
            }
            fadeOverlay.color = new Color(0.04f, 0.12f, 0.26f, a * 0.92f);
        }

        private void TickFinishAnim()
        {
            if (!finishShown || finishPanel == null) return;
            finishAnimT = Mathf.Min(finishAnimT + Time.deltaTime, 0.52f);
            float t = Mathf.SmoothStep(0f, 1f, finishAnimT / 0.48f);
            finishPanel.rectTransform.localScale = Vector3.Lerp(Vector3.one * 0.80f, Vector3.one, t);
            var c = finishPanel.color;
            c.a = Mathf.Lerp(0f, 0.95f, t);
            finishPanel.color = c;
        }

        // ─────────────────────────────────────────────────────────
        private void ShowFinish(string time, int seeds, int seedTotal,
                                int cps, int cpTotal)
        {
            finishShown = true;
            finishAnimT = 0f;
            finishPanel.color = new Color(0.88f, 0.99f, 0.97f, 0f);
            finishPanel.gameObject.SetActive(true);

            float elapsed = AeroLevelDirector.Instance != null
                ? AeroLevelDirector.Instance.ElapsedTime : 0f;
            bool full   = seeds >= seedTotal;
            string grade = CalcGrade(seeds, seedTotal, elapsed);

            finishTitle.text  = full ? "FULL  BLOOM" : "RUN  COMPLETE";
            finishTitle.color = full
                ? new Color(0.10f, 0.90f, 0.48f)
                : new Color(0.06f, 0.52f, 1.00f);
            finishGrade.text  = grade;
            finishGrade.color = GradeColor(grade);
            finishDetails.text =
                "TIME    " + time
                + "\n\nSEEDS   " + seeds + " / " + seedTotal
                + "    RELAY   " + cps + " / " + cpTotal;

            Cursor.lockState = CursorLockMode.None;
            Cursor.visible   = true;
        }

        // ─────────────────────────────────────────────────────────
        // Helpers
        private static string CalcGrade(int seeds, int total, float time)
        {
            if (seeds >= total && time < 600f)  return "S";
            if (seeds >= total && time < 1200f) return "A";
            if (seeds >= total * 0.75f)         return "B";
            return "C";
        }

        private static Color GradeColor(string g)
        {
            switch (g)
            {
                case "S": return new Color(1.00f, 0.86f, 0.12f);
                case "A": return new Color(0.28f, 1.00f, 0.52f);
                case "B": return new Color(0.20f, 0.70f, 1.00f);
                default:  return new Color(0.62f, 0.62f, 0.62f);
            }
        }

        private static string RelayDots(int n, int total)
        {
            var sb = new System.Text.StringBuilder("RELAY  ");
            for (int i = 0; i < total; i++) sb.Append(i < n ? '◆' : '◇');
            return sb.ToString();
        }

        private static string ShortTime(string full)
            => (full != null && full.Length >= 5) ? full.Substring(0, 5) : full;

        // ─────────────────────────────────────────────────────────
        // Build
        // ─────────────────────────────────────────────────────────
        private void Build(Transform root, AeroLevelDirector director)
        {
            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (font == null) font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            if (font == null) font = Font.CreateDynamicFontFromOSFont("Arial", 16);

            // ── Stats panel — top left ────────────────────────────
            var stats = Pnl(root, "Stats",
                new Vector2(0f, 1f), new Vector2(22f, -22f), new Vector2(294f, 156f),
                new Color(0.04f, 0.16f, 0.40f, 0.60f));

            // Cyan accent bar on left edge
            Bar(stats.transform, new Vector2(0f, 0f), new Vector2(0f, 1f),
                new Vector2(4f, 0f), Vector2.zero, new Color(0f, 0.92f, 1f, 0.92f));

            Transform statsT = stats.transform;

            // Timer — large, bright
            timerText = TxtTL(statsT, "Timer", font, 32, FontStyle.Bold,
                new Vector2(16f, -14f), new Vector2(260f, 42f),
                new Color(0.88f, 1f, 1f, 1f), TextAnchor.MiddleLeft);

            // Seed label
            seedText = TxtTL(statsT, "Seeds", font, 16, FontStyle.Normal,
                new Vector2(16f, -62f), new Vector2(260f, 22f),
                new Color(0.58f, 1f, 0.80f, 1f), TextAnchor.MiddleLeft);

            // Seed progress bar
            {
                var bgGO = new GameObject("SeedBarBg");
                bgGO.transform.SetParent(statsT, false);
                var bg = bgGO.AddComponent<Image>();
                bg.color = new Color(0f, 0.20f, 0.38f, 0.70f);
                bg.raycastTarget = false;
                var bgRt = bg.rectTransform;
                bgRt.anchorMin = new Vector2(0f, 1f);
                bgRt.anchorMax = new Vector2(0f, 1f);
                bgRt.pivot     = new Vector2(0f, 1f);
                bgRt.anchoredPosition = new Vector2(16f, -88f);
                bgRt.sizeDelta        = new Vector2(258f, 10f);

                var fillGO = new GameObject("SeedFill");
                fillGO.transform.SetParent(bgGO.transform, false);
                seedFill = fillGO.AddComponent<Image>();
                seedFill.color = new Color(0f, 0.92f, 0.72f, 1f);
                seedFill.raycastTarget = false;
                var fillRt = seedFill.rectTransform;
                // anchor: left-stretch (fills height, width is set in SetHud)
                fillRt.anchorMin = new Vector2(0f, 0f);
                fillRt.anchorMax = new Vector2(0f, 1f);
                fillRt.pivot     = new Vector2(0f, 0.5f);
                fillRt.anchoredPosition = Vector2.zero;
                fillRt.sizeDelta        = Vector2.zero;
            }

            // Checkpoint dots
            checkpointText = TxtTL(statsT, "Relay", font, 16, FontStyle.Normal,
                new Vector2(16f, -108f), new Vector2(260f, 22f),
                new Color(0.58f, 0.88f, 1f, 1f), TextAnchor.MiddleLeft);

            // ── Message bar — top centre ──────────────────────────
            messagePanel = Pnl(root, "MsgPanel",
                new Vector2(0.5f, 1f), new Vector2(0f, -22f), new Vector2(700f, 54f),
                new Color(0.66f, 0.94f, 1f, 0.66f));
            messagePanel.enabled = false;

            // Bottom cyan glow line
            Bar(messagePanel.transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(700f, 3f), new Vector2(0f, -1f), new Color(0f, 0.92f, 1f, 0.85f));

            messageText = TxtStretch(messagePanel.transform, "Msg", font, 22, FontStyle.Bold,
                "", new Color(0.02f, 0.20f, 0.38f, 1f));
            messageText.enabled = false;

            // ── Crosshair ─────────────────────────────────────────
            TxtCentre(root, "Cross", font, 18, "+", new Color(1f, 1f, 1f, 0.55f));

            // ── Finish panel — centre (starts inactive) ───────────
            finishPanel = Pnl(root, "FinishPanel",
                new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(640f, 420f),
                new Color(0.88f, 0.99f, 0.97f, 0f));
            finishPanel.rectTransform.localScale = Vector3.one * 0.80f;
            finishPanel.gameObject.SetActive(false);

            // Gold top band
            Bar(finishPanel.transform, new Vector2(0f, 1f), new Vector2(1f, 1f),
                Vector2.zero, Vector2.zero, new Color(1f, 0.84f, 0.14f, 1f));
            // — override height of that bar
            {
                var bars = finishPanel.transform.Find("Bar");
                if (bars != null) bars.GetComponent<RectTransform>().sizeDelta = new Vector2(0f, 6f);
            }

            // Cyan separator line at 1/3 from bottom
            Bar(finishPanel.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(560f, 2f), new Vector2(0f, -32f), new Color(0f, 0.80f, 1f, 0.45f));

            finishTitle = TxtCentre2(finishPanel.transform, "Title", font, 44, FontStyle.Bold,
                "RUN COMPLETE", new Color(0.06f, 0.28f, 0.54f, 1f),
                new Vector2(0f, 112f), new Vector2(600f, 60f));

            finishGrade = TxtCentre2(finishPanel.transform, "Grade", font, 88, FontStyle.Bold,
                "A", new Color(0.28f, 1f, 0.52f, 1f),
                new Vector2(0f, 20f), new Vector2(150f, 108f));

            finishDetails = TxtCentre2(finishPanel.transform, "Details", font, 22, FontStyle.Normal,
                "", new Color(0.06f, 0.22f, 0.42f, 0.90f),
                new Vector2(0f, -82f), new Vector2(560f, 90f));
            finishDetails.lineSpacing = 1.4f;

            // Buttons
            var restart = Btn(finishPanel.transform, "RESTART",
                font, new Vector2(-114f, -176f), new Color(0.14f, 0.72f, 1f, 0.92f));
            restart.onClick.AddListener(() =>
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible   = false;
                SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
            });

            var menu = Btn(finishPanel.transform, "MAIN MENU",
                font, new Vector2(114f, -176f), new Color(0.54f, 0.86f, 1f, 0.72f));
            menu.onClick.AddListener(() =>
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible   = true;
                if (SceneManager.sceneCountInBuildSettings > 1)
                    SceneManager.LoadScene(0);
                else
                    Application.Quit();
            });

            // ── Fade overlay — drawn on top of everything ─────────
            {
                var go = new GameObject("FadeOverlay");
                go.transform.SetParent(root, false);
                fadeOverlay = go.AddComponent<Image>();
                fadeOverlay.color = new Color(0.04f, 0.12f, 0.26f, 0f);
                fadeOverlay.raycastTarget = false;
                var rt = fadeOverlay.rectTransform;
                rt.anchorMin = Vector2.zero;
                rt.anchorMax = Vector2.one;
                rt.offsetMin = Vector2.zero;
                rt.offsetMax = Vector2.zero;
            }
        }

        // ─────────────────────────────────────────────────────────
        // UI primitive helpers
        // ─────────────────────────────────────────────────────────
        private static Image Pnl(Transform p, string n, Vector2 anchor, Vector2 pos, Vector2 sz, Color col)
        {
            var go = new GameObject(n);
            go.transform.SetParent(p, false);
            var img = go.AddComponent<Image>();
            img.color = col;
            var rt = img.rectTransform;
            rt.anchorMin = anchor; rt.anchorMax = anchor; rt.pivot = anchor;
            rt.anchoredPosition = pos; rt.sizeDelta = sz;
            return img;
        }

        private static void Bar(Transform p, Vector2 aMin, Vector2 aMax, Vector2 sz, Vector2 pos, Color col)
        {
            var go = new GameObject("Bar");
            go.transform.SetParent(p, false);
            var img = go.AddComponent<Image>();
            img.color = col; img.raycastTarget = false;
            var rt = img.rectTransform;
            rt.anchorMin = aMin; rt.anchorMax = aMax; rt.pivot = aMin;
            rt.sizeDelta = sz; rt.anchoredPosition = pos;
        }

        private static Text TxtTL(Transform p, string n, Font f, int sz, FontStyle style,
            Vector2 pos, Vector2 rect, Color col, TextAnchor anchor)
        {
            var go = new GameObject(n); go.transform.SetParent(p, false);
            var t = go.AddComponent<Text>();
            t.font = f; t.fontSize = sz; t.fontStyle = style;
            t.color = col; t.alignment = anchor;
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            t.verticalOverflow   = VerticalWrapMode.Overflow;
            var rt = t.rectTransform;
            rt.anchorMin = new Vector2(0f, 1f); rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            rt.anchoredPosition = pos; rt.sizeDelta = rect;
            return t;
        }

        private static Text TxtStretch(Transform p, string n, Font f, int sz, FontStyle style,
            string text, Color col)
        {
            var go = new GameObject(n); go.transform.SetParent(p, false);
            var t = go.AddComponent<Text>();
            t.font = f; t.fontSize = sz; t.fontStyle = style; t.text = text;
            t.color = col; t.alignment = TextAnchor.MiddleCenter;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow   = VerticalWrapMode.Overflow;
            var rt = t.rectTransform;
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(10f, 2f); rt.offsetMax = new Vector2(-10f, -2f);
            return t;
        }

        private static void TxtCentre(Transform p, string n, Font f, int sz, string text, Color col)
        {
            var go = new GameObject(n); go.transform.SetParent(p, false);
            var t = go.AddComponent<Text>();
            t.font = f; t.fontSize = sz; t.fontStyle = FontStyle.Bold; t.text = text;
            t.color = col; t.alignment = TextAnchor.MiddleCenter;
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            t.verticalOverflow   = VerticalWrapMode.Overflow;
            var rt = t.rectTransform;
            rt.anchorMin = new Vector2(0.5f, 0.5f); rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero; rt.sizeDelta = new Vector2(30f, 30f);
        }

        private static Text TxtCentre2(Transform p, string n, Font f, int sz, FontStyle style,
            string text, Color col, Vector2 pos, Vector2 rect)
        {
            var go = new GameObject(n); go.transform.SetParent(p, false);
            var t = go.AddComponent<Text>();
            t.font = f; t.fontSize = sz; t.fontStyle = style; t.text = text;
            t.color = col; t.alignment = TextAnchor.MiddleCenter;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow   = VerticalWrapMode.Overflow;
            var rt = t.rectTransform;
            rt.anchorMin = new Vector2(0.5f, 0.5f); rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = pos; rt.sizeDelta = rect;
            return t;
        }

        private static void TxtBL(Transform p, string n, Font f, int sz, FontStyle style,
            string text, Color col, Vector2 pos, Vector2 rect)
        {
            var go = new GameObject(n); go.transform.SetParent(p, false);
            var t = go.AddComponent<Text>();
            t.font = f; t.fontSize = sz; t.fontStyle = style; t.text = text;
            t.color = col; t.alignment = TextAnchor.MiddleLeft;
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            t.verticalOverflow   = VerticalWrapMode.Overflow;
            var rt = t.rectTransform;
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.zero; rt.pivot = Vector2.zero;
            rt.anchoredPosition = pos; rt.sizeDelta = rect;
        }

        private static Button Btn(Transform p, string label, Font f, Vector2 pos, Color col)
        {
            var go = new GameObject(label + "_Btn"); go.transform.SetParent(p, false);
            var img = go.AddComponent<Image>(); img.color = col;
            var btn = go.AddComponent<Button>();
            var cb = btn.colors;
            cb.normalColor      = col;
            cb.highlightedColor = Color.white;
            cb.pressedColor     = new Color(0.28f, 0.68f, 1f);
            btn.colors = cb; btn.targetGraphic = img;
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f); rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = pos; rt.sizeDelta = new Vector2(222f, 52f);

            var lgo = new GameObject("Label"); lgo.transform.SetParent(go.transform, false);
            var t = lgo.AddComponent<Text>();
            t.font = f; t.fontSize = 22; t.fontStyle = FontStyle.Bold;
            t.text = label; t.color = new Color(0.02f, 0.16f, 0.34f, 1f);
            t.alignment = TextAnchor.MiddleCenter;
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            t.verticalOverflow   = VerticalWrapMode.Overflow;
            var lrt = t.rectTransform;
            lrt.anchorMin = Vector2.zero; lrt.anchorMax = Vector2.one;
            lrt.offsetMin = Vector2.zero; lrt.offsetMax = Vector2.zero;
            return btn;
        }
    }
}
