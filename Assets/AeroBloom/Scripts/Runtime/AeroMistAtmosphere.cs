using UnityEngine;

namespace AeroBloom
{
    /// <summary>Local dreamy fog boost for mist crossing (Section 4). Uses RenderSettings fog (URP has no Volume Fog override).</summary>
    public sealed class AeroMistAtmosphere : MonoBehaviour
    {
        [SerializeField] private float fogDensity = 0.028f;
        [SerializeField] private Color fogColor = new Color(0.78f, 0.9f, 1f, 1f);

        private BoxCollider _zone;
        private float _baseDensity;

        private void Awake()
        {
            _zone = gameObject.AddComponent<BoxCollider>();
            _zone.isTrigger = true;
            _zone.center = new Vector3(0f, 8f, 52f);
            _zone.size = new Vector3(52f, 28f, 118f);
        }

        private void Start()
        {
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogColor = fogColor;
            _baseDensity = Mathf.Max(RenderSettings.fogDensity, fogDensity * 0.45f);
            RenderSettings.fogDensity = _baseDensity;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!other.GetComponentInParent<AeroPlayerController>()) return;
            ApplyMistFog(true);
        }

        private void OnTriggerExit(Collider other)
        {
            if (!other.GetComponentInParent<AeroPlayerController>()) return;
            ApplyMistFog(false);
        }

        private void ApplyMistFog(bool inside)
        {
            RenderSettings.fogColor = fogColor;
            RenderSettings.fogDensity = inside ? fogDensity : _baseDensity;
        }

        private void OnDestroy()
        {
            RenderSettings.fogDensity = _baseDensity;
        }
    }
}
