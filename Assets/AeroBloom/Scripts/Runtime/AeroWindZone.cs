using UnityEngine;
namespace AeroBloom
{
    [RequireComponent(typeof(Collider))]
    public class AeroWindZone : MonoBehaviour
    {
        public Vector3 windDirection = Vector3.right;
        public float windStrength = 3f;
        public bool fluctuate = true;

        void Awake() => GetComponent<Collider>().isTrigger = true;

        void OnTriggerStay(Collider other)
        {
            if (!other.CompareTag("Player")) return;
            float str = windStrength;
            if (fluctuate) str *= 0.7f + 0.3f * Mathf.Sin(Time.time * 2.3f);

            // Works with CharacterController-based player
            var cc = other.GetComponent<CharacterController>();
            if (cc != null) cc.Move(windDirection.normalized * (str * Time.deltaTime));
        }
    }
}
