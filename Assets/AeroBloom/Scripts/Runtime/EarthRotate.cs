using UnityEngine;
namespace AeroBloom
{
    public class EarthRotate : MonoBehaviour
    {
        public float rotateSpeed = 2f;
        void Update() => transform.Rotate(0f, rotateSpeed * Time.deltaTime, 0f);
    }
}
