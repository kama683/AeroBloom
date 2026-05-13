using UnityEngine;

namespace AeroBloom
{
    [RequireComponent(typeof(Collider))]
    public sealed class AeroSpeedGate : MonoBehaviour
    {
        public Vector3 localImpulse = new Vector3(0f, 1f, 16f);
        public float cooldown = 0.6f;

        private float lastTriggerTime = -999f;

        private void Awake()
        {
            GetComponent<Collider>().isTrigger = true;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (Time.time - lastTriggerTime < cooldown)
            {
                return;
            }

            AeroPlayerController player = other.GetComponentInParent<AeroPlayerController>();
            if (player == null)
            {
                return;
            }

            lastTriggerTime = Time.time;
            player.AddImpulse(transform.TransformDirection(localImpulse));

            if (AeroLevelDirector.Instance != null)
            {
                AeroLevelDirector.Instance.ShowMessage("Aero stream boosted.", 0.8f);
                AeroLevelDirector.Instance.PlayUiTone(1108f, 0.07f, 0.16f);
            }
        }
    }
}
