using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace AeroBloom
{
    public sealed class AeroPauseMenu : MonoBehaviour
    {
        private static AeroPauseMenu instance;

        private CanvasGroup group;
        private bool visible;
        private Button resumeBtn;
        private Button quitBtn;
        private int selectedIdx;

        public static void EnsureExists()
        {
            if (instance != null) return;
            var go = new GameObject("AeroPauseMenu");
            DontDestroyOnLoad(go);
            instance = go.AddComponent<AeroPauseMenu>();
            instance.BuildUI();
        }

        public static void Toggle()
        {
            EnsureExists();
            if (instance.visible) instance.DoHide();
            else instance.DoShow();
        }

        private void DoShow()
        {
            visible = true;
            selectedIdx = 0;
            Time.timeScale = 0f;
            gameObject.SetActive(true);
            StopAllCoroutines();
            StartCoroutine(FadeGroup(group, 0f, 1f, 0.25f));
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            SelectBtn(0);
        }

        private void Update()
        {
            if (!visible) return;

            bool tab = false, esc = false, confirm = false;
            bool clicked = false;
            Vector2 mp = Vector2.zero;

#if ENABLE_INPUT_SYSTEM
            var kb    = UnityEngine.InputSystem.Keyboard.current;
            var mouse = UnityEngine.InputSystem.Mouse.current;
            if (kb != null)
            {
                tab     = kb.tabKey.wasPressedThisFrame;
                esc     = kb.escapeKey.wasPressedThisFrame;
                confirm = kb.enterKey.wasPressedThisFrame || kb.spaceKey.wasPressedThisFrame;
            }
            if (mouse != null && mouse.leftButton.wasPressedThisFrame)
            {
                clicked = true;
                mp = mouse.position.ReadValue();
            }
#else
            tab     = Input.GetKeyDown(KeyCode.Tab);
            esc     = Input.GetKeyDown(KeyCode.Escape);
            confirm = Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Space);
            if (Input.GetMouseButtonDown(0)) { clicked = true; mp = Input.mousePosition; }
#endif
            if (tab)     { selectedIdx = 1 - selectedIdx; SelectBtn(selectedIdx); }
            if (esc)     DoHide();
            if (confirm) { if (selectedIdx == 0) OnResume(); else OnQuit(); }
            if (clicked)
            {
                if (resumeBtn != null && RectTransformUtility.RectangleContainsScreenPoint(
                        resumeBtn.GetComponent<RectTransform>(), mp, null))
                    OnResume();
                else if (quitBtn != null && RectTransformUtility.RectangleContainsScreenPoint(
                        quitBtn.GetComponent<RectTransform>(), mp, null))
                    OnQuit();
            }
        }

        private void SelectBtn(int idx)
        {
            var es = UnityEngine.EventSystems.EventSystem.current;
            if (es == null) return;
            es.SetSelectedGameObject(idx == 0 ? resumeBtn?.gameObject : quitBtn?.gameObject);
        }

        private void DoHide()
        {
            visible = false;
            Time.timeScale = 1f;
            StopAllCoroutines();
            StartCoroutine(HideAfterFade());
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        private IEnumerator HideAfterFade()
        {
            yield return StartCoroutine(FadeGroup(group, 1f, 0f, 0.2f));
            gameObject.SetActive(false);
        }

        private void OnResume() => DoHide();

        private void OnQuit()
        {
            Time.timeScale = 1f;
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        private static IEnumerator FadeGroup(CanvasGroup cg, float from, float to, float dur)
        {
            float t = 0f;
            while (t < dur) { t += Time.unscaledDeltaTime; cg.alpha = Mathf.Lerp(from, to, t / dur); yield return null; }
            cg.alpha = to;
        }

        private void BuildUI()
        {
            var cv = gameObject.AddComponent<Canvas>();
            cv.renderMode = RenderMode.ScreenSpaceOverlay;
            cv.sortingOrder = 90;
            var cs = gameObject.AddComponent<CanvasScaler>();
            cs.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            cs.referenceResolution = new Vector2(1280f, 720f);
            cs.matchWidthOrHeight = 0.5f;
            gameObject.AddComponent<GraphicRaycaster>();
            group = gameObject.AddComponent<CanvasGroup>();
            group.alpha = 0f;

            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (font == null) font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            Transform r = transform;

            var dim = Img(r, "Dim", V(0, 0), V(1, 1), new Color(0.06f, 0.18f, 0.44f, 0.62f));
            dim.raycastTarget = true;

            Img(r, "Shadow", V(0.298f, 0.198f), V(0.702f, 0.802f), new Color(0.10f, 0.30f, 0.60f, 0.22f));
            var cp = Img(r, "Card", V(0.30f, 0.20f), V(0.70f, 0.80f), new Color(0.91f, 0.96f, 1f, 0.93f)).transform;
            Img(cp, "Gloss", V(0f, 0.86f), V(1f, 1f), new Color(1f, 1f, 1f, 0.50f));
            Img(cp, "Accent", V(0f, 0.02f), V(0.006f, 0.98f), new Color(0.06f, 0.60f, 0.96f, 0.90f));

            Txt(cp, "Header", font, 36, FontStyle.Bold, "PAUSED",
                new Color(0.04f, 0.16f, 0.50f, 1f), TextAnchor.MiddleCenter,
                V(0.05f, 0.72f), V(0.95f, 0.92f));

            var dv = Img(cp, "Div", V(0.08f, 0.690f), V(0.92f, 0.692f), new Color(0.10f, 0.55f, 0.90f, 0.30f));
            dv.rectTransform.sizeDelta = new Vector2(0, 1);

            Txt(cp, "Hint", font, 11, FontStyle.Normal, "Press ESC to resume",
                new Color(0.08f, 0.42f, 0.78f, 0.60f), TextAnchor.MiddleCenter,
                V(0.05f, 0.60f), V(0.95f, 0.68f));

            resumeBtn = MakeBtn(cp, "RESUME", font, V(0.12f, 0.38f), V(0.88f, 0.56f),
                new Color(0.88f, 0.96f, 1f, 0.92f), new Color(0.04f, 0.14f, 0.42f, 1f),
                new Color(0.22f, 0.70f, 0.97f, 0.82f), 20);
            resumeBtn.onClick.AddListener(OnResume);

            quitBtn = MakeBtn(cp, "QUIT", font, V(0.20f, 0.18f), V(0.80f, 0.32f),
                new Color(0.78f, 0.90f, 1f, 0.62f), new Color(0.06f, 0.22f, 0.55f, 1f),
                new Color(0.22f, 0.60f, 0.88f, 0.50f), 16);
            quitBtn.onClick.AddListener(OnQuit);

            gameObject.SetActive(false);
        }

        private static Button MakeBtn(Transform p, string label, Font font,
            Vector2 aMin, Vector2 aMax, Color bgCol, Color txtCol, Color borderCol, int fontSize)
        {
            var go = new GameObject(label + "Btn"); go.transform.SetParent(p, false);
            var img = go.AddComponent<Image>(); img.color = bgCol;
            var rt = img.rectTransform; rt.anchorMin = aMin; rt.anchorMax = aMax; rt.offsetMin = rt.offsetMax = Vector2.zero;

            var btn = go.AddComponent<Button>();
            var cols = btn.colors;
            cols.normalColor = bgCol;
            cols.highlightedColor = new Color(Mathf.Min(bgCol.r + 0.06f, 1f), Mathf.Min(bgCol.g + 0.04f, 1f), Mathf.Min(bgCol.b + 0.02f, 1f), bgCol.a);
            cols.pressedColor = new Color(bgCol.r - 0.08f, bgCol.g - 0.06f, bgCol.b - 0.04f, bgCol.a);
            cols.colorMultiplier = 1f;
            btn.colors = cols; btn.targetGraphic = img;

            var bT = new GameObject("BT"); bT.transform.SetParent(go.transform, false);
            var bTI = bT.AddComponent<Image>(); bTI.color = borderCol; bTI.raycastTarget = false;
            var bTR = bTI.rectTransform; bTR.anchorMin = V(0, 1); bTR.anchorMax = V(1, 1); bTR.sizeDelta = new Vector2(0, 2); bTR.offsetMin = bTR.offsetMax = Vector2.zero;

            var gl = new GameObject("Gl"); gl.transform.SetParent(go.transform, false);
            var glI = gl.AddComponent<Image>(); glI.color = new Color(1, 1, 1, 0.28f); glI.raycastTarget = false;
            var glR = glI.rectTransform; glR.anchorMin = V(0, 0.58f); glR.anchorMax = V(1, 1); glR.offsetMin = glR.offsetMax = Vector2.zero;

            var tGO = new GameObject("Lbl"); tGO.transform.SetParent(go.transform, false);
            var txt = tGO.AddComponent<Text>();
            txt.font = font; txt.fontSize = fontSize; txt.fontStyle = FontStyle.Bold;
            txt.text = label; txt.color = txtCol; txt.alignment = TextAnchor.MiddleCenter; txt.raycastTarget = false;
            var tR = txt.rectTransform; tR.anchorMin = Vector2.zero; tR.anchorMax = Vector2.one; tR.offsetMin = tR.offsetMax = Vector2.zero;
            return btn;
        }

        private static Vector2 V(float x, float y) => new Vector2(x, y);

        private static Image Img(Transform p, string n, Vector2 aMin, Vector2 aMax, Color c)
        {
            var go = new GameObject(n); go.transform.SetParent(p, false);
            var img = go.AddComponent<Image>(); img.color = c; img.raycastTarget = false;
            var rt = img.rectTransform; rt.anchorMin = aMin; rt.anchorMax = aMax; rt.offsetMin = rt.offsetMax = Vector2.zero;
            return img;
        }

        private static Text Txt(Transform p, string n, Font f, int sz, FontStyle sty,
            string text, Color c, TextAnchor anch, Vector2 aMin, Vector2 aMax)
        {
            var go = new GameObject(n); go.transform.SetParent(p, false);
            var t = go.AddComponent<Text>(); t.font = f; t.fontSize = sz; t.fontStyle = sty; t.text = text;
            t.color = c; t.alignment = anch; t.raycastTarget = false;
            t.horizontalOverflow = HorizontalWrapMode.Wrap; t.verticalOverflow = VerticalWrapMode.Overflow;
            var rt = t.rectTransform; rt.anchorMin = aMin; rt.anchorMax = aMax; rt.offsetMin = rt.offsetMax = Vector2.zero;
            return t;
        }
    }
}
