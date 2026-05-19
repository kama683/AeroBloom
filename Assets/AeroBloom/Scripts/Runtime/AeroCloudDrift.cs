using UnityEngine;

namespace AeroBloom
{
    /// <summary>Gentle sway for mist cloud puffs (no vertical drift).</summary>
    public sealed class AeroCloudDrift : MonoBehaviour
    {
        public float swayAmplitude = 0.35f;
        public float swayFreqX = 0.45f;
        public float swayFreqY = 0.32f;
        public float swayFreqZ = 0.38f;
        public float rotateSpeed = 4f;

        private Vector3 _origin;
        private float _phase;

        private void Start()
        {
            _origin = transform.position;
            _phase = Random.Range(0f, 20f);
        }

        private void Update()
        {
            float t = Time.time + _phase;
            transform.position = _origin + new Vector3(
                Mathf.Sin(t * swayFreqX) * swayAmplitude,
                Mathf.Sin(t * swayFreqY) * swayAmplitude * 0.5f,
                Mathf.Cos(t * swayFreqZ) * swayAmplitude);
            transform.Rotate(0f, rotateSpeed * Time.deltaTime, 0f, Space.World);
        }
    }
}
