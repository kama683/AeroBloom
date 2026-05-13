using UnityEngine;

namespace AeroBloom
{
    public sealed class AeroOrbitProp : MonoBehaviour
    {
        public Vector3 axis = Vector3.up;
        public float degreesPerSecond = 20f;
        public float bobHeight = 0.2f;
        public float bobSpeed = 1.2f;

        private Vector3 startPosition;

        private void Awake()
        {
            startPosition = transform.position;
        }

        private void Update()
        {
            transform.Rotate(axis.normalized, degreesPerSecond * Time.deltaTime, Space.World);
            if (bobHeight > 0f)
            {
                transform.position = startPosition + Vector3.up * (Mathf.Sin(Time.time * bobSpeed) * bobHeight);
            }
        }
    }
}
