using UnityEngine;

namespace AeroBloom
{
    [RequireComponent(typeof(Collider))]
    public sealed class AeroCheckpoint : MonoBehaviour
    {
        public string checkpointName = "Relay";
        public Transform respawnPoint;
        public Material inactiveMaterial;
        public Material activeMaterial;

        private bool activated;
        private Renderer[] renderers;

        public bool IsActivated
        {
            get { return activated; }
        }

        public Vector3 RespawnPosition
        {
            get { return respawnPoint != null ? respawnPoint.position : transform.position + Vector3.up * 1.5f; }
        }

        public Quaternion RespawnRotation
        {
            get { return respawnPoint != null ? respawnPoint.rotation : transform.rotation; }
        }

        private void Awake()
        {
            Collider ownCollider = GetComponent<Collider>();
            ownCollider.isTrigger = true;
            renderers = GetComponentsInChildren<Renderer>();
            ApplyMaterial(inactiveMaterial);
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.GetComponentInParent<AeroPlayerController>() == null || AeroLevelDirector.Instance == null)
            {
                return;
            }

            AeroLevelDirector.Instance.ActivateCheckpoint(this);
        }

        public void SetActivated()
        {
            activated = true;
            ApplyMaterial(activeMaterial);
        }

        private void ApplyMaterial(Material material)
        {
            if (material == null || renderers == null)
            {
                return;
            }

            for (int i = 0; i < renderers.Length; i++)
            {
                renderers[i].sharedMaterial = material;
            }
        }
    }
}
