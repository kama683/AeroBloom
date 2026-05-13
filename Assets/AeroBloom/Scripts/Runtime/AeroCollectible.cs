using UnityEngine;

namespace AeroBloom
{
    [RequireComponent(typeof(Collider))]
    public sealed class AeroCollectible : MonoBehaviour
    {
        public AeroLevelDirector director;
        public float bobHeight = 0.28f;
        public float bobSpeed = 2.4f;
        public float spinSpeed = 96f;

        private Vector3 startPosition;
        private bool collected;

        private void Awake()
        {
            startPosition = transform.position;
            Collider ownCollider = GetComponent<Collider>();
            ownCollider.isTrigger = true;
        }

        private void Update()
        {
            if (collected)
            {
                transform.localScale = Vector3.Lerp(transform.localScale, Vector3.zero, Time.deltaTime * 9f);
                return;
            }

            transform.Rotate(Vector3.up, spinSpeed * Time.deltaTime, Space.World);
            transform.position = startPosition + Vector3.up * (Mathf.Sin(Time.time * bobSpeed) * bobHeight);
        }

        private void OnTriggerEnter(Collider other)
        {
            if (collected || other.GetComponentInParent<AeroPlayerController>() == null)
            {
                return;
            }

            collected = true;
            Collider ownCollider = GetComponent<Collider>();
            if (ownCollider != null)
            {
                ownCollider.enabled = false;
            }

            Renderer renderer = GetComponentInChildren<Renderer>();
            if (renderer != null)
            {
                renderer.enabled = false;
            }

            if (director != null)
            {
                director.CollectSeed(this);
            }

            Destroy(gameObject, 0.4f);
        }
    }
}
