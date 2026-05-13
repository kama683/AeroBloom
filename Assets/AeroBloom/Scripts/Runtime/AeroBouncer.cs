using UnityEngine;

namespace AeroBloom
{
    [RequireComponent(typeof(Collider))]
    public sealed class AeroBouncer : MonoBehaviour
    {
        public Vector3 localLaunchVelocity = new Vector3(0f, 15f, 13f);

        private void Awake()
        {
            GetComponent<Collider>().isTrigger = true;
        }

        private void OnTriggerEnter(Collider other)
        {
            AeroPlayerController player = other.GetComponentInParent<AeroPlayerController>();
            if (player == null)
            {
                return;
            }

            Vector3 launch = transform.TransformDirection(localLaunchVelocity);
            player.Bounce(launch);

            if (AeroLevelDirector.Instance != null)
            {
                AeroLevelDirector.Instance.ShowMessage("Bubble spring launched.", 1f);
                AeroLevelDirector.Instance.PlayUiTone(932f, 0.11f, 0.2f);
            }
        }
    }
}
