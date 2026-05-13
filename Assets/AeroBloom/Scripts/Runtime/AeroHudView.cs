using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace AeroBloom
{
    public sealed class AeroHudView : MonoBehaviour
    {
        private Text timerText;
        private Text seedText;
        private Text checkpointText;
        private Text speedText;
        private Text messageText;
        private Text storyText;
        private Text finishTitle;
        private Text finishDetails;
        private Image messagePanel;
        private Image finishPanel;

        public static AeroHudView Create(AeroLevelDirector director)
        {
            GameObject canvasObject = new GameObject("AeroBloom HUD");
            Canvas canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 20;
            CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            canvasObject.AddComponent<GraphicRaycaster>();

            AeroHudView view = canvasObject.AddComponent<AeroHudView>();
            view.Build(canvas.transform, director);
            return view;
        }

        public void SetHud(string time, int seeds, int seedTotal, int checkpoints, int checkpointTotal, float speed, string message, bool finished)
        {
            timerText.text = "TIME  " + time;
            seedText.text = "SEEDS  " + seeds + "/" + seedTotal;
            checkpointText.text = "RELAYS  " + checkpoints + "/" + checkpointTotal;
            speedText.text = "SPEED  " + (Mathf.RoundToInt(speed * 10f) / 10f).ToString("F1") + " m/s";

            bool showMessage = !string.IsNullOrEmpty(message);
            messagePanel.enabled = showMessage;
            messageText.enabled = showMessage;
            messageText.text = message;

            finishPanel.enabled = finished;
            finishTitle.enabled = finished;
            finishDetails.enabled = finished;

            if (finished)
            {
                bool fullBloom = seeds >= seedTotal;
                finishTitle.text = fullBloom ? "FULL BLOOM" : "RUN COMPLETE";
                finishTitle.color = fullBloom
                    ? new Color(0.18f, 0.85f, 0.42f, 1f)
                    : new Color(0.02f, 0.28f, 0.52f, 1f);
                finishDetails.text = "TIME  " + time + "\nSEEDS  " + seeds + "/" + seedTotal + "  |  RELAYS  " + checkpoints + "/" + checkpointTotal;
            }
        }

        private void Build(Transform root, AeroLevelDirector director)
        {
            Font font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            if (font == null)
                font = Font.CreateDynamicFontFromOSFont("Arial", 16);

            // Stats panel — top left
            Image statsPanel = CreatePanel(root, "Stats Panel",
                new Vector2(18f, -18f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(320f, 140f),
                new Color(0.06f, 0.32f, 0.58f, 0.54f));

            timerText      = CreateLabel(statsPanel.transform, "Timer",       font, 20, new Vector2(14f, -12f),  new Vector2(290f, 26f));
            seedText       = CreateLabel(statsPanel.transform, "Seeds",       font, 18, new Vector2(14f, -42f),  new Vector2(290f, 24f));
            checkpointText = CreateLabel(statsPanel.transform, "Checkpoints", font, 18, new Vector2(14f, -70f),  new Vector2(290f, 24f));
            speedText      = CreateLabel(statsPanel.transform, "Speed",       font, 18, new Vector2(14f, -98f),  new Vector2(290f, 24f));

            // Story hint — bottom left
            storyText = CreateRawText(root, "Story", font, 14, FontStyle.Bold,
                "AEROBLOOM  //  restore sky-garden relays  •  cache Aero seeds  •  reach the Bloom Portal",
                new Color(0.04f, 0.24f, 0.42f, 0.78f),
                new Vector2(18f, 18f), new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(680f, 38f));

            // Message bar — top center
            messagePanel = CreatePanel(root, "Message Panel",
                new Vector2(0f, -14f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(620f, 48f),
                new Color(0.72f, 0.96f, 1f, 0.62f));
            messageText = CreateRawText(messagePanel.transform, "Message", font, 22, FontStyle.Bold,
                "", new Color(0.02f, 0.22f, 0.38f, 1f),
                Vector2.zero, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(590f, 38f));

            // Crosshair
            Text crosshair = CreateRawText(root, "Crosshair", font, 24, FontStyle.Bold,
                "+", new Color(1f, 1f, 1f, 0.8f),
                Vector2.zero, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(28f, 28f));

            // Finish panel — center screen
            finishPanel = CreatePanel(root, "Finish Panel",
                Vector2.zero, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(580f, 340f),
                new Color(0.88f, 1f, 0.96f, 0.92f));

            finishTitle = CreateRawText(finishPanel.transform, "Finish Title", font, 42, FontStyle.Bold,
                "RUN COMPLETE", new Color(0.02f, 0.28f, 0.52f, 1f),
                new Vector2(0f, 80f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(540f, 60f));

            finishDetails = CreateRawText(finishPanel.transform, "Finish Details", font, 22, FontStyle.Bold,
                "", new Color(0.04f, 0.22f, 0.38f, 0.92f),
                new Vector2(0f, 20f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(520f, 50f));

            // Restart button
            Button restartBtn = CreateMenuButton(finishPanel.transform, "RESTART", font,
                new Vector2(0f, -60f), new Color(0.18f, 0.78f, 1f, 0.92f));
            restartBtn.onClick.AddListener(() =>
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
                SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
            });

            // Quit button
            Button quitBtn = CreateMenuButton(finishPanel.transform, "QUIT TO MENU", font,
                new Vector2(0f, -130f), new Color(0.58f, 0.88f, 1f, 0.72f));
            quitBtn.onClick.AddListener(() =>
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
                if (SceneManager.sceneCountInBuildSettings > 1)
                    SceneManager.LoadScene(0);
                else
                    Application.Quit();
            });

            messagePanel.enabled = false;
            messageText.enabled = false;
            finishPanel.enabled = false;
            finishTitle.enabled = false;
            finishDetails.enabled = false;
        }

        private static Image CreatePanel(Transform parent, string name, Vector2 pos, Vector2 anchorMin, Vector2 anchorMax, Vector2 size, Color color)
        {
            GameObject obj = new GameObject(name);
            obj.transform.SetParent(parent, false);
            Image img = obj.AddComponent<Image>();
            img.color = color;
            RectTransform rect = img.rectTransform;
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = (anchorMin == new Vector2(0.5f, 0.5f)) ? new Vector2(0.5f, 0.5f) : anchorMin;
            rect.anchoredPosition = pos;
            rect.sizeDelta = size;
            return img;
        }

        private static Text CreateLabel(Transform parent, string name, Font font, int size, Vector2 pos, Vector2 rectSize)
        {
            return CreateRawText(parent, name, font, size, FontStyle.Bold, "",
                new Color(0.94f, 0.99f, 1f, 0.96f),
                pos, new Vector2(0f, 1f), new Vector2(0f, 1f), rectSize);
        }

        private static Text CreateRawText(Transform parent, string name, Font font, int size, FontStyle style, string text, Color color, Vector2 pos, Vector2 anchorMin, Vector2 anchorMax, Vector2 rectSize)
        {
            GameObject obj = new GameObject(name);
            obj.transform.SetParent(parent, false);
            Text t = obj.AddComponent<Text>();
            t.font = font;
            t.fontSize = size;
            t.fontStyle = style;
            t.text = text;
            t.color = color;
            t.alignment = TextAnchor.MiddleCenter;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Truncate;
            RectTransform rect = t.rectTransform;
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = (anchorMin == new Vector2(0.5f, 0.5f)) ? new Vector2(0.5f, 0.5f) : anchorMin;
            rect.anchoredPosition = pos;
            rect.sizeDelta = rectSize;
            return t;
        }

        private static Button CreateMenuButton(Transform parent, string label, Font font, Vector2 pos, Color bgColor)
        {
            GameObject obj = new GameObject(label + " Btn");
            obj.transform.SetParent(parent, false);
            Image img = obj.AddComponent<Image>();
            img.color = bgColor;
            Button btn = obj.AddComponent<Button>();
            ColorBlock cb = btn.colors;
            cb.highlightedColor = Color.white;
            cb.pressedColor = new Color(0.34f, 0.76f, 1f, 1f);
            btn.colors = cb;
            RectTransform rect = obj.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = pos;
            rect.sizeDelta = new Vector2(260f, 52f);

            Text txt = CreateRawText(obj.transform, "Label", font, 24, FontStyle.Bold, label,
                new Color(0.02f, 0.18f, 0.36f, 1f),
                Vector2.zero, Vector2.zero, Vector2.one, Vector2.zero);
            txt.rectTransform.anchorMin = Vector2.zero;
            txt.rectTransform.anchorMax = Vector2.one;
            txt.rectTransform.offsetMin = Vector2.zero;
            txt.rectTransform.offsetMax = Vector2.zero;

            return btn;
        }
    }
}
