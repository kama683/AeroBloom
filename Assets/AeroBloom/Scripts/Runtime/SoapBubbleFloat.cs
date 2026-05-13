using UnityEngine;
namespace AeroBloom
{
    public class SoapBubbleFloat : MonoBehaviour
    {
        public float riseSpeed     = 0.5f;
        public float swayAmplitude = 0.5f;
        public float swayFreqX     = 0.7f;
        public float swayFreqZ     = 0.9f;
        public float rotateSpeed   = 10f;
        public float maxHeight     = 80f;

        private Vector3 _startPos;
        private float   _tOff;

        void Start()
        {
            _startPos = transform.position;
            _tOff     = Random.Range(0f, 12f);
        }

        void Update()
        {
            float t = Time.time + _tOff;
            Vector3 p = transform.position;
            p.y     += riseSpeed * Time.deltaTime;
            p.x      = _startPos.x + Mathf.Sin(t * swayFreqX) * swayAmplitude;
            p.z      = _startPos.z + Mathf.Sin(t * swayFreqZ) * swayAmplitude;
            transform.position = p;
            transform.Rotate(0f, rotateSpeed * Time.deltaTime, 0f);

            if (p.y > maxHeight)
            {
                _startPos = new Vector3(
                    _startPos.x + Random.Range(-30f, 30f),
                    Random.Range(0.5f, 3f),
                    _startPos.z + Random.Range(-30f, 30f));
                transform.position = _startPos;
            }
        }
    }
}
