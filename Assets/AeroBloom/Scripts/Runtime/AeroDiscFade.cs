using System.Collections;
using UnityEngine;

namespace AeroBloom
{
    public sealed class AeroDiscFade : MonoBehaviour
    {
        public float standDelay   = 3f;   // seconds player must stand before warning
        public float hideDuration = 2f;   // seconds disc stays invisible

        private BoxCollider discBox;
        private Renderer[]  rends;
        private float       standTimer;
        private bool        fading;

        private void Awake()
        {
            discBox = GetComponent<BoxCollider>();
            rends   = GetComponentsInChildren<Renderer>(true);
        }

        private void Update()
        {
            if (fading) return;

            var dir = AeroLevelDirector.Instance;
            if (dir == null || dir.player == null) return;

            if (IsPlayerOnDisc(dir.player.transform.position))
            {
                standTimer += Time.deltaTime;
                if (standTimer >= standDelay)
                    StartCoroutine(FadeSequence());
            }
            else
            {
                standTimer = 0f;
            }
        }

        private bool IsPlayerOnDisc(Vector3 playerPos)
        {
            // disc.localScale = (radius*2, discThick*0.5, radius*2)
            // cylinder world half-height = localScale.y (unity cylinder is 2 units tall)
            float radius     = transform.localScale.x * 0.5f;
            float discTopY   = transform.position.y + transform.localScale.y;

            float dx          = playerPos.x - transform.position.x;
            float dz          = playerPos.z - transform.position.z;
            float aboveTop    = playerPos.y - discTopY;

            return (dx * dx + dz * dz) < radius * radius
                && aboveTop >= -0.4f && aboveTop < 3.0f;
        }

        private IEnumerator FadeSequence()
        {
            fading = true;

            // 4 quick flashes as a warning
            for (int i = 0; i < 4; i++)
            {
                SetVisible(false);
                yield return new WaitForSeconds(0.12f);
                SetVisible(true);
                yield return new WaitForSeconds(0.12f);
            }

            // Disappear — collider off so player falls
            SetVisible(false);
            if (discBox) discBox.enabled = false;

            yield return new WaitForSeconds(hideDuration);

            // Reappear
            if (discBox) discBox.enabled = true;
            SetVisible(true);
            standTimer = 0f;
            fading     = false;
        }

        private void SetVisible(bool v)
        {
            foreach (var r in rends) r.enabled = v;
        }
    }
}
