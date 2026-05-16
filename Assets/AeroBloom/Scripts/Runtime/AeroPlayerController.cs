using UnityEngine;

namespace AeroBloom
{
    [RequireComponent(typeof(CharacterController))]
    public sealed class AeroPlayerController : MonoBehaviour
    {
        [Header("References")]
        public Camera playerCamera;
        public Transform cameraPivot;
        public AeroLevelDirector director;

        [Header("Look")]
        public float mouseSensitivity = 0.08f;
        public float gamepadSensitivity = 185f;
        public float cameraHeight = 1.62f;
        public float slideCameraHeight = 0.9f;
        public float defaultFov = 74f;
        public float fastFov = 91f;

        [Header("Movement")]
        public float walkSpeed = 7f;
        public float sprintSpeed = 12f;
        public float groundAcceleration = 42f;
        public float airAcceleration = 18f;
        public float jumpSpeed = 10.0f;
        public int extraAirJumps = 1;
        public float gravity = -25f;
        public float coyoteTime = 0.12f;

        [Header("Parkour")]
        public float slideSpeed = 15f;
        public float slideBoost = 5f;
        public float slideDuration = 0.82f;
        public float dashImpulse = 13f;
        public float dashCooldown = 1.3f;
        public float wallRunSpeed = 13f;
        public float wallRunGravityMultiplier = 0.2f;
        public float wallRunDuration = 1.35f;
        public float wallCheckDistance = 0.72f;
        public float wallJumpSideImpulse = 8f;
        public LayerMask wallRunLayerMask = ~0;

        [Header("Third Person")]
        public bool thirdPersonMode = true;
        public float tpCameraDistance = 6f;
        public float tpCameraHeight = 1.2f;
        public float tpBodyRotateSpeed = 14f;
        public Transform bodyTransform;
        public Animator bodyAnimator;

        [Header("Fail State")]
        public float fallRespawnY = -18f;

        private CharacterController controller;
        private Vector3 planarVelocity;
        private float verticalVelocity;
        private float cameraPitch;
        private float tpYaw;
        private bool victoryMode;
        private float victoryYawTarget;
        private float coyoteTimer;
        private int airJumpsRemaining;
        private bool isSliding;
        private float slideTimer;
        private float dashCooldownTimer;
        private bool isWallRunning;
        private bool wasWallRunning;
        private float wallRunTimer;
        private Vector3 wallNormal;
        private Transform currentPlatformTransform;
        private Transform touchedPlatformTransform;
        private Vector3 previousPlatformPosition;
        private float standingHeight;
        private Vector3 standingCenter;

        public float CurrentSpeed
        {
            get { return new Vector3(planarVelocity.x, 0f, planarVelocity.z).magnitude; }
        }

        private void Awake()
        {
            controller = GetComponent<CharacterController>();
            standingHeight = controller.height;
            standingCenter = controller.center;
            airJumpsRemaining = extraAirJumps;
            EnsureCamera();
        }

        private void Start()
        {
            LockCursor();
            tpYaw = transform.eulerAngles.y;
        }

        public void BeginVictory()
        {
            victoryMode      = true;
            victoryYawTarget = tpYaw + 180f;
            planarVelocity   = Vector3.zero;
            verticalVelocity = 0f;
        }

        public void ExitVictoryMode()
        {
            victoryMode = false;
        }

        private void Update()
        {
            float dt = Time.deltaTime;

            if (victoryMode)
            {
                // Slowly pan camera 180° to show the path the player traveled
                tpYaw        = Mathf.LerpAngle(tpYaw, victoryYawTarget, dt * 0.65f);
                cameraPitch  = Mathf.Lerp(cameraPitch, -10f, dt * 0.45f);
                UpdateCamera(dt);
                UpdateAnimator();
                return;
            }

            if (AeroInput.CancelPressed())
                ToggleCursor();

            if (AeroInput.PrimaryPressed())
                LockCursor();

            if (Cursor.lockState == CursorLockMode.Locked)
                UpdateLook();

            if (AeroInput.ResetPressed() && director != null)
                director.RespawnPlayer("Checkpoint restored.");

            Move(dt);
            UpdateCamera(dt);
            UpdateAnimator();

            if (director != null)
                director.ReportSpeed(CurrentSpeed);

            if (transform.position.y < fallRespawnY && director != null)
                director.RespawnPlayer("Recovered from the water layer.");
        }

        /// <summary>Creates or re-enables the follow camera (used by TestSandbox open world build).</summary>
        public void EnsureExploreCamera()
        {
            EnsureCamera();
            if (playerCamera != null)
                playerCamera.gameObject.SetActive(true);
        }

        private void EnsureCamera()
        {
            if (thirdPersonMode)
            {
                if (cameraPivot == null)
                {
                    GameObject pivot = new GameObject("Camera Pivot 3P");
                    // Free-floating — NOT parented to player
                    pivot.transform.position = transform.position + Vector3.up * tpCameraHeight + Vector3.back * tpCameraDistance;
                    cameraPivot = pivot.transform;
                }
            }
            else
            {
                if (cameraPivot == null)
                {
                    GameObject pivot = new GameObject("Camera Pivot");
                    pivot.transform.SetParent(transform, false);
                    pivot.transform.localPosition = new Vector3(0f, cameraHeight, 0f);
                    cameraPivot = pivot.transform;
                }
            }

            if (playerCamera == null)
            {
                GameObject cameraObj = new GameObject("Aero Camera");
                cameraObj.transform.SetParent(cameraPivot, false);
                cameraObj.transform.localPosition = Vector3.zero;
                cameraObj.transform.localRotation = Quaternion.identity;
                playerCamera = cameraObj.AddComponent<Camera>();
                playerCamera.tag = "MainCamera";
                playerCamera.nearClipPlane = 0.08f;
                playerCamera.farClipPlane = 650f;
                cameraObj.AddComponent<AudioListener>();
            }

            playerCamera.fieldOfView = defaultFov;
        }

        private void UpdateLook()
        {
            Vector2 look = AeroInput.ReadLook(mouseSensitivity, gamepadSensitivity);

            if (thirdPersonMode)
            {
                tpYaw += look.x;
                cameraPitch = Mathf.Clamp(cameraPitch - look.y, -30f, 72f);
            }
            else
            {
                transform.Rotate(Vector3.up, look.x, Space.Self);
                cameraPitch = Mathf.Clamp(cameraPitch - look.y, -87f, 87f);
                cameraPivot.localRotation = Quaternion.Euler(cameraPitch, 0f, 0f);
            }
        }

        private void Move(float dt)
        {
            bool groundedAtStart = controller.isGrounded;
            ApplyMovingPlatform(groundedAtStart);
            touchedPlatformTransform = null;

            if (groundedAtStart)
            {
                coyoteTimer = coyoteTime;
                airJumpsRemaining = extraAirJumps;
                wallRunTimer = 0f;
                if (verticalVelocity < 0f)
                    verticalVelocity = -2f;
            }
            else
            {
                coyoteTimer -= dt;
            }

            Vector2 moveInput = AeroInput.ReadMove();

            // Compute wish direction
            Vector3 wishDirection;
            if (thirdPersonMode)
            {
                Vector3 camForward = Quaternion.Euler(0f, tpYaw, 0f) * Vector3.forward;
                Vector3 camRight   = Quaternion.Euler(0f, tpYaw, 0f) * Vector3.right;
                wishDirection = camRight * moveInput.x + camForward * moveInput.y;
            }
            else
            {
                wishDirection = transform.right * moveInput.x + transform.forward * moveInput.y;
            }

            if (wishDirection.sqrMagnitude > 1f)
                wishDirection.Normalize();

            // Rotate player body to face movement direction in third-person
            if (thirdPersonMode && wishDirection.sqrMagnitude > 0.02f)
            {
                float targetYaw = Mathf.Atan2(wishDirection.x, wishDirection.z) * Mathf.Rad2Deg;
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.Euler(0f, targetYaw, 0f), dt * tpBodyRotateSpeed);
            }

            UpdateSlideState(groundedAtStart, moveInput, dt);
            UpdateWallRunState(groundedAtStart, moveInput, dt);
            ApplyJumpInput();
            ApplyDashInput(wishDirection);

            float targetSpeed = AeroInput.SprintHeld() && moveInput.y > 0.05f ? sprintSpeed : walkSpeed;
            float acceleration = groundedAtStart ? groundAcceleration : airAcceleration;

            if (isSliding)
            {
                targetSpeed = slideSpeed;
                wishDirection = Vector3.ProjectOnPlane(transform.forward, Vector3.up).normalized;
                acceleration *= 0.65f;
            }

            if (isWallRunning)
            {
                Vector3 alongWall = Vector3.Cross(wallNormal, Vector3.up);
                if (Vector3.Dot(alongWall, transform.forward) < 0f)
                    alongWall = -alongWall;
                wishDirection = alongWall.normalized;
                targetSpeed = wallRunSpeed;
                acceleration = groundAcceleration;
                if (verticalVelocity < -1.35f)
                    verticalVelocity = -1.35f;
            }

            Vector3 targetPlanarVelocity = wishDirection * targetSpeed;
            planarVelocity = Vector3.MoveTowards(planarVelocity, targetPlanarVelocity, acceleration * dt);

            float gravityMultiplier = isWallRunning ? wallRunGravityMultiplier : 1f;
            verticalVelocity += gravity * gravityMultiplier * dt;

            CollisionFlags flags = controller.Move((planarVelocity + Vector3.up * verticalVelocity) * dt);

            if ((flags & CollisionFlags.Below) != 0)
            {
                verticalVelocity = -2f;
                airJumpsRemaining = extraAirJumps;
                coyoteTimer = coyoteTime;
            }

            if ((flags & CollisionFlags.Above) != 0 && verticalVelocity > 0f)
                verticalVelocity = 0f;

            if (touchedPlatformTransform != null)
            {
                currentPlatformTransform = touchedPlatformTransform;
                previousPlatformPosition = currentPlatformTransform.position;
            }
            else if (!controller.isGrounded)
            {
                currentPlatformTransform = null;
            }
        }

        private void UpdateSlideState(bool grounded, Vector2 moveInput, float dt)
        {
            bool wantsSlide = AeroInput.CrouchHeld();
            bool canStartSlide = grounded && wantsSlide && !isSliding && moveInput.y > 0.2f && CurrentSpeed > sprintSpeed * 0.65f;

            if (canStartSlide)
            {
                isSliding = true;
                slideTimer = slideDuration;
                planarVelocity += transform.forward * slideBoost;
                if (director != null)
                {
                    director.ShowMessage("Hydro-slide linked.", 1.1f);
                    director.PlayUiTone(660f, 0.08f, 0.18f);
                }
            }

            if (isSliding)
            {
                slideTimer -= dt;
                if (slideTimer <= 0f || !wantsSlide || !grounded)
                    isSliding = false;
            }

            float targetHeight = isSliding ? standingHeight * 0.55f : standingHeight;
            controller.height = Mathf.Lerp(controller.height, targetHeight, dt * 12f);
            controller.center = new Vector3(standingCenter.x, controller.height * 0.5f, standingCenter.z);
        }

        private void UpdateWallRunState(bool grounded, Vector2 moveInput, float dt)
        {
            wasWallRunning = isWallRunning;
            isWallRunning = false;

            if (grounded || moveInput.y <= 0.1f || !AeroInput.SprintHeld())
                return;

            RaycastHit hit;
            if (!TryFindWall(out hit))
            {
                wallRunTimer = 0f;
                return;
            }

            wallNormal = hit.normal;
            wallRunTimer += dt;
            isWallRunning = wallRunTimer <= wallRunDuration;

            if (isWallRunning && !wasWallRunning && director != null)
            {
                director.ShowMessage("Glass wall-run engaged.", 1.1f);
                director.PlaySwoosh();
                director.PlayUiTone(784f, 0.09f, 0.16f);
            }
        }

        private bool TryFindWall(out RaycastHit bestHit)
        {
            Vector3 origin = transform.position + Vector3.up * 0.9f;
            bool hitLeft  = Physics.Raycast(origin, -transform.right, out RaycastHit leftHit,  wallCheckDistance, wallRunLayerMask, QueryTriggerInteraction.Ignore);
            bool hitRight = Physics.Raycast(origin,  transform.right, out RaycastHit rightHit, wallCheckDistance, wallRunLayerMask, QueryTriggerInteraction.Ignore);

            if (hitLeft  && IsOwnCollider(leftHit.collider))  hitLeft  = false;
            if (hitRight && IsOwnCollider(rightHit.collider)) hitRight = false;

            if (hitLeft && hitRight) { bestHit = leftHit.distance < rightHit.distance ? leftHit : rightHit; return true; }
            if (hitLeft)  { bestHit = leftHit;  return true; }
            if (hitRight) { bestHit = rightHit; return true; }

            bestHit = default;
            return false;
        }

        private bool IsOwnCollider(Collider target)
        {
            return target != null && target.transform.IsChildOf(transform);
        }

        private void ApplyJumpInput()
        {
            if (!AeroInput.JumpPressed())
                return;

            if (isWallRunning)
            {
                Vector3 sideImpulse = wallNormal * wallJumpSideImpulse;
                planarVelocity += new Vector3(sideImpulse.x, 0f, sideImpulse.z);
                verticalVelocity = jumpSpeed * 0.92f;
                isWallRunning = false;
                wallRunTimer = wallRunDuration;
                if (director != null)
                {
                    director.ShowMessage("Wall-jump burst.", 0.9f);
                    director.PlayUiTone(988f, 0.08f, 0.18f);
                }
                return;
            }

            if (coyoteTimer > 0f)
            {
                verticalVelocity = jumpSpeed;
                coyoteTimer = 0f;
                return;
            }

            if (airJumpsRemaining > 0)
            {
                airJumpsRemaining--;
                verticalVelocity = jumpSpeed * 0.86f;
                planarVelocity += transform.forward * 1.35f;
                if (director != null)
                {
                    director.ShowMessage("Aero hop.", 0.7f);
                    director.PlayUiTone(880f, 0.07f, 0.14f);
                }
            }
        }

        private void ApplyDashInput(Vector3 wishDirection)
        {
            dashCooldownTimer -= Time.deltaTime;
            if (!AeroInput.DashPressed() || dashCooldownTimer > 0f)
                return;

            Vector3 dashDir;
            if (thirdPersonMode)
                dashDir = wishDirection.sqrMagnitude > 0.05f ? wishDirection.normalized : Quaternion.Euler(0f, tpYaw, 0f) * Vector3.forward;
            else
                dashDir = wishDirection.sqrMagnitude > 0.05f ? wishDirection.normalized : transform.forward;

            planarVelocity += dashDir * dashImpulse;
            dashCooldownTimer = dashCooldown;

            if (director != null)
            {
                director.ShowMessage("Aero dash cached.", 0.85f);
                director.PlaySwoosh();
                director.PlayUiTone(1046f, 0.06f, 0.16f);
            }
        }

        private void ApplyMovingPlatform(bool grounded)
        {
            if (currentPlatformTransform == null || !grounded)
                return;
            Vector3 delta = currentPlatformTransform.position - previousPlatformPosition;
            if (delta.sqrMagnitude > 0f)
                controller.Move(delta);
            previousPlatformPosition = currentPlatformTransform.position;
        }

        private void UpdateCamera(float dt)
        {
            if (thirdPersonMode && cameraPivot != null)
            {
                Vector3 lookTarget = transform.position + Vector3.up * tpCameraHeight;
                Vector3 camOffset = Quaternion.Euler(cameraPitch, tpYaw, 0f) * new Vector3(0f, 0f, -tpCameraDistance);
                Vector3 desired = lookTarget + camOffset;

                // Keep camera above ground
                if (desired.y < transform.position.y + 0.4f)
                    desired.y = transform.position.y + 0.4f;

                // Camera collision — push camera forward when geometry is in the way
                Vector3 toDesired = desired - lookTarget;
                float wantedDist = toDesired.magnitude;
                if (wantedDist > 0.01f)
                {
                    RaycastHit camHit;
                    if (Physics.SphereCast(lookTarget, 0.12f, toDesired / wantedDist, out camHit,
                            wantedDist, ~0, QueryTriggerInteraction.Ignore))
                        desired = lookTarget + (toDesired / wantedDist) * Mathf.Max(camHit.distance - 0.1f, 0.4f);
                }

                cameraPivot.position = Vector3.Lerp(cameraPivot.position, desired, dt * 9f);
                cameraPivot.LookAt(lookTarget);

                if (playerCamera != null)
                {
                    float speed01 = Mathf.InverseLerp(walkSpeed, sprintSpeed + dashImpulse, CurrentSpeed);
                    playerCamera.fieldOfView = Mathf.Lerp(playerCamera.fieldOfView, Mathf.Lerp(defaultFov, fastFov, speed01), dt * 7f);
                }
            }
            else if (!thirdPersonMode && cameraPivot != null)
            {
                float targetHeight = isSliding ? slideCameraHeight : cameraHeight;
                cameraPivot.localPosition = Vector3.Lerp(cameraPivot.localPosition, new Vector3(0f, targetHeight, 0f), dt * 12f);

                if (playerCamera != null)
                {
                    float speed01 = Mathf.InverseLerp(walkSpeed, sprintSpeed + dashImpulse, CurrentSpeed);
                    playerCamera.fieldOfView = Mathf.Lerp(playerCamera.fieldOfView, Mathf.Lerp(defaultFov, fastFov, speed01), dt * 7f);
                }
            }
        }

        private void UpdateAnimator()
        {
            if (bodyAnimator == null)
                return;
            bool moving   = planarVelocity.sqrMagnitude > 0.1f;
            bool sprinting = AeroInput.SprintHeld() && moving;
            bool jumping  = verticalVelocity > 1.5f;
            bodyAnimator.SetBool("Walking",   moving && !sprinting);
            bodyAnimator.SetBool("Sprinting", sprinting);
            bodyAnimator.SetBool("Jumping",   jumping);
        }

        public void AddImpulse(Vector3 impulse)
        {
            planarVelocity += new Vector3(impulse.x, 0f, impulse.z);
            if (impulse.y > verticalVelocity)
                verticalVelocity = impulse.y;
        }

        public void Bounce(Vector3 launchVelocity)
        {
            planarVelocity = new Vector3(launchVelocity.x, 0f, launchVelocity.z);
            verticalVelocity = launchVelocity.y;
        }

        public void Teleport(Vector3 position, Quaternion rotation)
        {
            controller.enabled = false;
            transform.SetPositionAndRotation(position, rotation);
            controller.enabled = true;

            planarVelocity = Vector3.zero;
            verticalVelocity = -2f;
            currentPlatformTransform = null;
            cameraPitch = 0f;
            tpYaw = rotation.eulerAngles.y;

            if (!thirdPersonMode && cameraPivot != null)
                cameraPivot.localRotation = Quaternion.identity;
        }

        private void OnControllerColliderHit(ControllerColliderHit hit)
        {
            if (hit.normal.y < 0.45f)
                return;
            Transform root = hit.collider.transform;
            PlatformMover mover = root.GetComponentInParent<PlatformMover>();
            if (mover != null) { touchedPlatformTransform = mover.transform; return; }
            AeroMovingPlatform amp = root.GetComponentInParent<AeroMovingPlatform>();
            if (amp != null) touchedPlatformTransform = amp.transform;
        }

        private static void LockCursor()
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        private static void ToggleCursor()
        {
            if (Cursor.lockState == CursorLockMode.Locked)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
            else
            {
                LockCursor();
            }
        }
    }
}
