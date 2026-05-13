using UnityEngine;
namespace AeroBloom
{
    public class SwingingPlatform : MonoBehaviour
    {
        public float angle = 15f;
        public float period = 3f;

        private Quaternion startRot;
        void Start() => startRot = transform.rotation;
        void Update()
        {
            float t = Mathf.Sin(Time.time * (Mathf.PI * 2f / Mathf.Max(0.1f, period)));
            transform.rotation = startRot * Quaternion.Euler(0f, 0f, angle * t);
        }
    }
}
