using UnityEngine;

namespace FrutigerAeroExample {
    public class PlayerSounds : MonoBehaviour {
        [SerializeField] AudioSource footstepSource;
        [SerializeField] AudioSource waveSource;

        public void Footstep() {
            footstepSource.pitch = Random.Range(0.9f, 1.1f);
            footstepSource.Play();
        }

        public void Wave() {
            waveSource.pitch = Random.Range(0.9f, 1.1f);
            waveSource.Play();
        }
    }
}
