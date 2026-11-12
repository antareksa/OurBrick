using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace BrickbreakerBase
{
    // One pointer (mouse / touch / pen) read through whichever input backend the project uses --
    // the legacy Input class throws when Active Input Handling is "Input System Package (New)",
    // which is what the URP templates default to.
    public static class PointerInput
    {
        public static bool PressedThisFrame
        {
            get
            {
#if ENABLE_INPUT_SYSTEM
                Pointer pointer = Pointer.current;
                return pointer != null && pointer.press.wasPressedThisFrame;
#else
                return Input.GetMouseButtonDown(0);
#endif
            }
        }

        public static bool IsPressed
        {
            get
            {
#if ENABLE_INPUT_SYSTEM
                Pointer pointer = Pointer.current;
                return pointer != null && pointer.press.isPressed;
#else
                return Input.GetMouseButton(0);
#endif
            }
        }

        public static bool ReleasedThisFrame
        {
            get
            {
#if ENABLE_INPUT_SYSTEM
                Pointer pointer = Pointer.current;
                return pointer != null && pointer.press.wasReleasedThisFrame;
#else
                return Input.GetMouseButtonUp(0);
#endif
            }
        }

        public static Vector2 Position
        {
            get
            {
#if ENABLE_INPUT_SYSTEM
                Pointer pointer = Pointer.current;
                return pointer != null ? pointer.position.ReadValue() : Vector2.zero;
#else
                return Input.mousePosition;
#endif
            }
        }
    }
}
