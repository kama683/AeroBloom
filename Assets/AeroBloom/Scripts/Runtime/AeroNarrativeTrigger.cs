using UnityEngine;

namespace AeroBloom
{
    [RequireComponent(typeof(Collider))]
    public sealed class AeroNarrativeTrigger : MonoBehaviour
    {
        [TextArea(2, 4)]
        public string storyText = "";

        private bool fired;

        private void Awake()
        {
            Collider col = GetComponent<Collider>();
            if (col == null) col = gameObject.AddComponent<BoxCollider>();
            col.isTrigger = true;
        }

        private void OnTriggerEnter(Collider other) { }
    }
}
