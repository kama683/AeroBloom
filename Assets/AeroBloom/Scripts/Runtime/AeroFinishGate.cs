using UnityEngine;

namespace AeroBloom
{
    [RequireComponent(typeof(Collider))]
    public sealed class AeroFinishGate : MonoBehaviour
    {
        private void Awake()
        {
            GetComponent<Collider>().isTrigger = true;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.GetComponentInParent<AeroPlayerController>() != null && AeroLevelDirector.Instance != null)
            {
                AeroLevelDirector.Instance.FinishRun();
            }
        }
    }
}
