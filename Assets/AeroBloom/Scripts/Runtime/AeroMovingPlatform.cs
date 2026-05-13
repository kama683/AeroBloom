using UnityEngine;

namespace AeroBloom
{
    public sealed class AeroMovingPlatform : MonoBehaviour
    {
        public Vector3 localOffset = new Vector3(8f, 0f, 0f);
        public float period = 4f;
        public float phase;

        private Vector3 origin;

        private void Awake()
        {
            origin = transform.position;
        }

        private void Update()
        {
            float safePeriod = Mathf.Max(0.1f, period);
            float t = (Mathf.Sin(((Time.time + phase) / safePeriod) * Mathf.PI * 2f) + 1f) * 0.5f;
            transform.position = origin + transform.TransformDirection(localOffset) * t;
        }
    }
}
