using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace AeroBloom
{
    public sealed class AeroMainMenu : MonoBehaviour
    {
        [Header("Scene Flow")]
        [SerializeField] private string gameplaySceneName = "AeroBloomPrototype";
        [SerializeField] private int fallbackSceneIndex = 1;

        [Header("UI")]
        [SerializeField] private Button playButton;
        [SerializeField] private Button quitButton;

        [Header("Audio")]
        [SerializeField] private AudioSource musicSource;

        private void Start()
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            EnsureEventSystem();
            BindButtons();
            PlayMusic();
        }

        private void EnsureEventSystem()
        {
            if (FindFirstObjectByType<EventSystem>() != null) return;

            var eventSystem = new GameObject("EventSystem");
            eventSystem.AddComponent<EventSystem>();
#if ENABLE_INPUT_SYSTEM
            eventSystem.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
#else
            eventSystem.AddComponent<StandaloneInputModule>();
#endif
        }

        private void BindButtons()
        {
            if (playButton == null)
            {
                playButton = FindButtonByName("PLAY_Button");
            }

            if (quitButton == null)
            {
                quitButton = FindButtonByName("QUIT_Button");
            }

            if (playButton != null)
            {
                playButton.onClick.AddListener(LoadGameplayScene);
            }
            else
            {
                Debug.LogWarning("[AeroMainMenu] PLAY button is not assigned.");
            }

            if (quitButton != null)
            {
                quitButton.onClick.AddListener(QuitGame);
            }
            else
            {
                Debug.LogWarning("[AeroMainMenu] QUIT button is not assigned.");
            }
        }

        private Button FindButtonByName(string objectName)
        {
            Button[] buttons = FindObjectsByType<Button>(FindObjectsSortMode.None);
            for (int i = 0; i < buttons.Length; i++)
            {
                if (buttons[i].name == objectName) return buttons[i];
            }

            return null;
        }

        private void PlayMusic()
        {
            AeroPackConfig pack = Resources.Load<AeroPackConfig>("AeroPackConfig");

            if (musicSource == null)
            {
                musicSource = GetComponent<AudioSource>();
            }

            if (musicSource == null)
            {
                musicSource = gameObject.AddComponent<AudioSource>();
            }

            if (musicSource.isPlaying) return;

            musicSource.loop = true;
            musicSource.volume = 0.5f;
            musicSource.spatialBlend = 0f;

            if (pack != null && pack.musicAlt != null) musicSource.clip = pack.musicAlt;
            else if (pack != null && pack.musicMain != null) musicSource.clip = pack.musicMain;

            if (musicSource.clip != null)
            {
                musicSource.Play();
            }
        }

        public void LoadGameplayScene()
        {
            if (Application.CanStreamedLevelBeLoaded(gameplaySceneName))
            {
                SceneManager.LoadScene(gameplaySceneName);
                return;
            }

            if (SceneManager.sceneCountInBuildSettings > fallbackSceneIndex)
            {
                SceneManager.LoadScene(fallbackSceneIndex);
                return;
            }

            Debug.LogWarning("[AeroMainMenu] Gameplay scene is missing in Build Settings.");
        }

        public void QuitGame()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
