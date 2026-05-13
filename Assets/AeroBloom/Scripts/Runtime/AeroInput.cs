using UnityEngine;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
#endif

namespace AeroBloom
{
    public static class AeroInput
    {
        public static Vector2 ReadMove()
        {
            Vector2 value = Vector2.zero;

#if ENABLE_INPUT_SYSTEM
            Keyboard keyboard = Keyboard.current;
            if (keyboard != null)
            {
                if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed)
                {
                    value.x -= 1f;
                }

                if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed)
                {
                    value.x += 1f;
                }

                if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed)
                {
                    value.y -= 1f;
                }

                if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed)
                {
                    value.y += 1f;
                }
            }

            Gamepad gamepad = Gamepad.current;
            if (gamepad != null)
            {
                Vector2 stick = gamepad.leftStick.ReadValue();
                if (stick.sqrMagnitude > value.sqrMagnitude)
                {
                    value = stick;
                }
            }
#else
            value = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
#endif

            return Vector2.ClampMagnitude(value, 1f);
        }

        public static Vector2 ReadLook(float mouseSensitivity, float gamepadSensitivity)
        {
            Vector2 value = Vector2.zero;

#if ENABLE_INPUT_SYSTEM
            Mouse mouse = Mouse.current;
            if (mouse != null)
            {
                value += mouse.delta.ReadValue() * mouseSensitivity;
            }

            Gamepad gamepad = Gamepad.current;
            if (gamepad != null)
            {
                value += gamepad.rightStick.ReadValue() * (gamepadSensitivity * Time.deltaTime);
            }
#else
            value.x += Input.GetAxis("Mouse X") * mouseSensitivity * 12f;
            value.y += Input.GetAxis("Mouse Y") * mouseSensitivity * 12f;
#endif

            return value;
        }

        public static bool JumpPressed()
        {
#if ENABLE_INPUT_SYSTEM
            return IsPressedThisFrame(Keyboard.current != null ? Keyboard.current.spaceKey : null)
                || IsPressedThisFrame(Gamepad.current != null ? Gamepad.current.buttonSouth : null);
#else
            return Input.GetButtonDown("Jump");
#endif
        }

        public static bool SprintHeld()
        {
#if ENABLE_INPUT_SYSTEM
            Keyboard keyboard = Keyboard.current;
            Gamepad gamepad = Gamepad.current;
            return (keyboard != null && (keyboard.leftShiftKey.isPressed || keyboard.rightShiftKey.isPressed))
                || (gamepad != null && gamepad.leftStickButton.isPressed);
#else
            return Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
#endif
        }

        public static bool CrouchHeld()
        {
#if ENABLE_INPUT_SYSTEM
            Keyboard keyboard = Keyboard.current;
            Gamepad gamepad = Gamepad.current;
            return (keyboard != null && (keyboard.leftCtrlKey.isPressed || keyboard.rightCtrlKey.isPressed || keyboard.cKey.isPressed))
                || (gamepad != null && gamepad.buttonEast.isPressed);
#else
            return Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl) || Input.GetKey(KeyCode.C);
#endif
        }

        public static bool DashPressed()
        {
#if ENABLE_INPUT_SYSTEM
            Keyboard keyboard = Keyboard.current;
            Gamepad gamepad = Gamepad.current;
            return (keyboard != null && keyboard.eKey.wasPressedThisFrame)
                || (gamepad != null && gamepad.rightShoulder.wasPressedThisFrame);
#else
            return Input.GetKeyDown(KeyCode.E);
#endif
        }

        public static bool ResetPressed()
        {
#if ENABLE_INPUT_SYSTEM
            Keyboard keyboard = Keyboard.current;
            Gamepad gamepad = Gamepad.current;
            return (keyboard != null && keyboard.rKey.wasPressedThisFrame)
                || (gamepad != null && gamepad.selectButton.wasPressedThisFrame);
#else
            return Input.GetKeyDown(KeyCode.R);
#endif
        }

        public static bool CancelPressed()
        {
#if ENABLE_INPUT_SYSTEM
            Keyboard keyboard = Keyboard.current;
            Gamepad gamepad = Gamepad.current;
            return (keyboard != null && keyboard.escapeKey.wasPressedThisFrame)
                || (gamepad != null && gamepad.startButton.wasPressedThisFrame);
#else
            return Input.GetKeyDown(KeyCode.Escape);
#endif
        }

        public static bool PrimaryPressed()
        {
#if ENABLE_INPUT_SYSTEM
            Mouse mouse = Mouse.current;
            Gamepad gamepad = Gamepad.current;
            return (mouse != null && mouse.leftButton.wasPressedThisFrame)
                || (gamepad != null && gamepad.buttonSouth.wasPressedThisFrame);
#else
            return Input.GetMouseButtonDown(0);
#endif
        }

#if ENABLE_INPUT_SYSTEM
        private static bool IsPressedThisFrame(ButtonControl control)
        {
            return control != null && control.wasPressedThisFrame;
        }
#endif
    }
}
