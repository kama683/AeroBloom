using System.Collections;
using UnityEngine;

namespace AeroBloom
{
    public sealed class AeroLevelDirector : MonoBehaviour
    {
        public static AeroLevelDirector Instance { get; private set; }

        public AeroPlayerController player;
        public AeroHudView hud;

        private AudioSource audioSource;
        private AudioClip packSfxPop;
        private AudioClip packSfxSwoosh;
        [SerializeField] private int seedTotal;
        [SerializeField] private int checkpointTotal;
        [SerializeField] private Vector3 checkpointPosition;
        [SerializeField] private Quaternion checkpointRotation;
        [SerializeField] private bool checkpointConfigured;

        private float startTime;
        private float finishTime;
        private int seedsCollected;
        private int checkpointsActivated;
        private float playerSpeed;
        private bool finished;
        private bool isRespawning;
        private string message = "";
        private float messageUntil;

        public bool Finished
        {
            get { return finished; }
        }

        public float ElapsedTime
        {
            get { return finished ? finishTime : Time.time - startTime; }
        }

        private void Awake()
        {
            Instance = this;
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 0f;
        }

        private void Start()
        {
            if (player == null)
            {
                player = Object.FindFirstObjectByType<AeroPlayerController>();
            }

            if (player != null)
            {
                player.director = this;
            }

            if (seedTotal <= 0)
            {
                seedTotal = Object.FindObjectsByType<AeroCollectible>(FindObjectsSortMode.None).Length;
            }

            if (checkpointTotal <= 0)
            {
                checkpointTotal = Object.FindObjectsByType<AeroCheckpoint>(FindObjectsSortMode.None).Length;
            }

            if (!checkpointConfigured && player != null)
            {
                checkpointPosition = player.transform.position;
                checkpointRotation = player.transform.rotation;
                checkpointConfigured = true;
            }

            startTime = Time.time;
            finishTime = 0f;
            seedsCollected = 0;
            checkpointsActivated = 0;
            finished = false;

            if (hud == null)
            {
                hud = AeroHudView.Create(this);
            }

            AeroPackConfig pack = Resources.Load<AeroPackConfig>("AeroPackConfig");
            if (pack != null)
            {
                packSfxPop = pack.sfxPop;
                packSfxSwoosh = pack.sfxSwoosh;
            }

            AudioClip musicClip = (pack != null && pack.musicMain != null)
                ? pack.musicMain
                : AeroAudioSynth.GetAmbientPad();
            float musicVolume = (pack != null && pack.musicMain != null) ? 0.15f : 0.18f;

            AudioSource ambient = gameObject.AddComponent<AudioSource>();
            ambient.clip = musicClip;
            ambient.volume = musicVolume;
            ambient.loop = true;
            ambient.spatialBlend = 0f;
            ambient.Play();
        }

        private void Update()
        {
            if (hud != null)
            {
                string activeMessage = Time.time < messageUntil ? message : "";
                hud.SetHud(
                    FormatTime(ElapsedTime),
                    seedsCollected,
                    seedTotal,
                    checkpointsActivated,
                    checkpointTotal,
                    playerSpeed,
                    activeMessage,
                    finished);
            }
        }

        public void Configure(AeroPlayerController controlledPlayer, int totalSeeds, int totalCheckpoints, Vector3 startPosition, Quaternion startRotation)
        {
            player = controlledPlayer;
            seedTotal = totalSeeds;
            checkpointTotal = totalCheckpoints;
            checkpointPosition = startPosition;
            checkpointRotation = startRotation;
            checkpointConfigured = true;
            startTime = Time.time;
            finishTime = 0f;
            finished = false;

            if (player != null)
            {
                player.director = this;
            }
        }

        public void ReportSpeed(float speed)
        {
            playerSpeed = speed;
        }

        public void ActivateCheckpoint(AeroCheckpoint checkpoint)
        {
            if (checkpoint == null)
            {
                return;
            }

            checkpointPosition = checkpoint.RespawnPosition;
            checkpointRotation = checkpoint.RespawnRotation;

            if (!checkpoint.IsActivated)
            {
                checkpoint.SetActivated();
                checkpointsActivated++;
                ShowMessage(checkpoint.checkpointName + " synced.", 1.3f);
                PlayUiTone(523f + checkpointsActivated * 70f, 0.12f, 0.18f);
            }
            else
            {
                ShowMessage("Signal already synced.", 0.8f);
            }
        }

        public void CollectSeed(AeroCollectible collectible)
        {
            if (collectible == null)
            {
                return;
            }

            seedsCollected++;
            ShowMessage("Aero seed " + seedsCollected + "/" + seedTotal + " collected.", 1.05f);

            if (packSfxPop != null)
                audioSource.PlayOneShot(packSfxPop, 1.0f);
            else
                PlayUiTone(740f + seedsCollected * 8f, 0.09f, 0.16f);

            if (seedsCollected == seedTotal)
            {
                ShowMessage("All Aero seeds cached. Hit the final portal.", 2f);
                PlayUiTone(1174f, 0.22f, 0.22f);
            }
        }

        public void PlaySwoosh()
        {
            if (packSfxSwoosh != null)
                audioSource.PlayOneShot(packSfxSwoosh, 0.9f);
        }

        public void FinishRun()
        {
            if (finished)
            {
                return;
            }

            finished = true;
            finishTime = Time.time - startTime;
            ShowMessage("AeroBloom restored in " + FormatTime(finishTime) + ".", 8f);
            PlayUiTone(1318f, 0.25f, 0.24f);
            StartCoroutine(VictoryCameraDelay());
        }

        private IEnumerator VictoryCameraDelay()
        {
            yield return new WaitForSeconds(1.0f);
            if (player != null)
                player.BeginVictory();
        }

        public void RespawnPlayer(string reason)
        {
            if (player == null || isRespawning)
            {
                return;
            }

            isRespawning = true;
            PlayUiTone(392f, 0.12f, 0.16f);
            StartCoroutine(RespawnSequence(reason));
        }

        private IEnumerator RespawnSequence(string reason)
        {
            // Player falls visibly for 1.4 s before the fade kicks in
            yield return new WaitForSeconds(0.8f);

            ShowMessage(reason, 2.2f);

            Vector3    savedPos = checkpointPosition;
            Quaternion savedRot = checkpointRotation;

            if (hud != null)
            {
                hud.TriggerRespawnFade(() =>
                {
                    player.Teleport(savedPos, savedRot);
                    isRespawning = false;
                });
            }
            else
            {
                player.Teleport(savedPos, savedRot);
                isRespawning = false;
            }
        }

        public void ShowMessage(string text, float duration)
        {
            message = text;
            messageUntil = Time.time + duration;
        }

        public void PlayUiTone(float frequency, float duration, float volume)
        {
            if (audioSource == null)
            {
                return;
            }

            audioSource.PlayOneShot(AeroAudioSynth.GetTone("tone", frequency, duration, volume * 2.8f));
        }

        public static string FormatTime(float seconds)
        {
            int minutes = Mathf.FloorToInt(seconds / 60f);
            float remainder = seconds - minutes * 60f;
            return minutes.ToString("00") + ":" + remainder.ToString("00.00");
        }
    }
}
