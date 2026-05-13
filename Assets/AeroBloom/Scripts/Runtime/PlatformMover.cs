using UnityEngine;
namespace AeroBloom
{
    public class PlatformMover : MonoBehaviour
    {
        public Vector3 moveAxis = Vector3.right;
        public float amplitude = 4f;
        public float speed = 1f;
        public float phaseOffset = 0f;
        public bool rotates = false;
        public float rotateSpeed = 0f;

        private Vector3 startPos;
        void Start() => startPos = transform.position;
        void Update()
        {
            transform.position = startPos + moveAxis.normalized *
                Mathf.Sin((Time.time + phaseOffset) * speed) * amplitude;
            if (rotates) transform.Rotate(0f, rotateSpeed * Time.deltaTime, 0f);
        }
    }
}
