using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;
using Cursor = UnityEngine.Cursor;

namespace FrutigerAeroExample {
    [RequireComponent(typeof(CharacterController))]
    public class PlayerController : MonoBehaviour {
        static readonly int WalkingBool = Animator.StringToHash("Walking");
        static readonly int SprintingBool = Animator.StringToHash("Sprinting");
        static readonly int JumpingBool = Animator.StringToHash("Jumping");
        static readonly int EmoteBool = Animator.StringToHash("Emote");

        [SerializeField] float playerSpeed = 7.0f;
        [SerializeField] float jumpHeight = 1.5f;
        [SerializeField] float gravityValue = -9.81f;
        [SerializeField] float sprintSpeed = 2f;

        [SerializeField] UIDocument uiDocument;
        [SerializeField] Camera emoteCamera;
        [SerializeField] ParticleSystem footstepParticles;
        [SerializeField] CharacterController controller;
        [SerializeField] Animator animator;
        [SerializeField] GameObject pivot;

        [Header("Input Actions")]
        [SerializeField] InputActionReference moveAction;

        [SerializeField] InputActionReference jumpAction;
        [SerializeField] InputActionReference sprintAction;
        [SerializeField] InputActionReference emoteAction;
        [SerializeField] [CanBeNull] InputActionReference escapeAction;

        Vector3 playerVelocity;
        Vector3 moveVelocity;
        bool groundedPlayer;
        Transform cameraTransform;

        void OnEnable() {
            moveAction.action.Enable();
            jumpAction.action.Enable();
            cameraTransform = Camera.main?.transform;
            if (escapeAction != null) Cursor.lockState = CursorLockMode.Locked;
        }

        void OnDisable() {
            moveAction.action.Disable();
            jumpAction.action.Disable();
            if (escapeAction != null) Cursor.lockState = CursorLockMode.None;
        }

        void Update() {
            groundedPlayer = controller.isGrounded;

            // Slight downward velocity to keep grounded stable
            if (groundedPlayer && playerVelocity.y < -2f) {
                playerVelocity.y = -2f;
            }

            // Finding if we're emoting or not based off the animation name (scuffed I know)
            bool isEmoting = false;
            if (animator.GetCurrentAnimatorClipInfoCount(0) > 0) {
                isEmoting = animator.GetCurrentAnimatorClipInfo(0)[0].clip.name.ToLower().Contains("emote");
            }

            var emotePopup = uiDocument.rootVisualElement.Q<VisualElement>("EmotePopup");
            if (!emoteCamera.gameObject.activeSelf && isEmoting) {
                emoteCamera.gameObject.SetActive(true);
                if (emotePopup != null) emotePopup.style.display = DisplayStyle.Flex;
            } else if (emoteCamera.gameObject.activeSelf && !isEmoting) {
                emoteCamera.gameObject.SetActive(false);
                if (emotePopup != null) emotePopup.style.display = DisplayStyle.None;
            }

            // Read input
            Vector3 move = new Vector3();
            if (!isEmoting) {
                Vector2 input = moveAction.action.ReadValue<Vector2>();
                move = new Vector3(input.x, 0, input.y);
                move = cameraTransform.forward * move.z + cameraTransform.right * move.x;
                move.y = 0f;
                move = Vector3.ClampMagnitude(move, 1f);
            }

            // Model pivot
            if (move != Vector3.zero) {
                Quaternion targetRotation = Quaternion.LookRotation(move);
                pivot.transform.rotation = Quaternion.Slerp(
                    pivot.transform.rotation,
                    targetRotation,
                    10f * Time.deltaTime
                );
            }

            // Jump
            if (groundedPlayer && jumpAction.action.WasPressedThisFrame()) {
                playerVelocity.y = Mathf.Sqrt(jumpHeight * -2f * gravityValue);
            }

            // Apply gravity
            playerVelocity.y += gravityValue * Time.deltaTime;

            // Move
            moveVelocity = Vector3.Lerp(moveVelocity, move, 3.0f * Time.deltaTime);
            float speedMultiplier = (sprintAction.action.IsPressed()) ? sprintSpeed : 1.0f;
            Vector3 finalMove = moveVelocity * (playerSpeed * speedMultiplier) + Vector3.up * playerVelocity.y;
            controller.Move(finalMove * Time.deltaTime);

            // Lock
            if (escapeAction?.action.WasPressedThisFrame() == true) {
                Cursor.lockState = (Cursor.lockState == CursorLockMode.None) ? CursorLockMode.Locked : CursorLockMode.None;
            }

            // Animations
            animator.SetBool(WalkingBool, move.magnitude > 0f);
            animator.SetBool(SprintingBool, sprintAction.action.IsPressed());
            animator.SetBool(JumpingBool, jumpAction.action.WasPressedThisFrame());
            animator.SetInteger(EmoteBool, emoteAction.action.WasPressedThisFrame() ? 1 : 0);
            var emission = footstepParticles.emission;
            emission.enabled = move.magnitude > 0 || playerVelocity.y > 0;
        }
    }
}
