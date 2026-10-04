using UnityEngine;
#if !ENABLE_LEGACY_INPUT_MANAGER && ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace GasQueue
{
    /// <summary>
    /// Все клавиши игры в одном месте. Работает и со старой системой ввода (Input Manager),
    /// и с новой (Input System), смотря что включено в Project Settings → Player.
    /// </summary>
    public static class GameInput
    {
        public static bool Gas => Held(KeyCode.W) || Held(KeyCode.UpArrow);
        public static bool Brake => Held(KeyCode.S) || Held(KeyCode.DownArrow);
        public static bool HornPressed => Pressed(KeyCode.H) || Pressed(KeyCode.Space);
        public static bool SwitchCameraPressed => Pressed(KeyCode.C);
        public static bool RadioPressed => Pressed(KeyCode.R);
        public static bool InteractPressed => Pressed(KeyCode.E);
        public static bool RestartPressed => Pressed(KeyCode.Return);
        public static bool EscapePressed => Pressed(KeyCode.Escape);
        public static bool HelpPressed => Pressed(KeyCode.F1);

#if ENABLE_LEGACY_INPUT_MANAGER
        static bool Held(KeyCode key) => Input.GetKey(key);
        static bool Pressed(KeyCode key) => Input.GetKeyDown(key);

        public static Vector2 MouseDelta => new Vector2(Input.GetAxisRaw("Mouse X"), Input.GetAxisRaw("Mouse Y"));
        public static bool ClickPressed => Input.GetMouseButtonDown(0);
#elif ENABLE_INPUT_SYSTEM
        static bool Held(KeyCode key)
        {
            var control = ToControl(key);
            return control != null && control.isPressed;
        }

        static bool Pressed(KeyCode key)
        {
            var control = ToControl(key);
            return control != null && control.wasPressedThisFrame;
        }

        // Масштаб подобран так, чтобы чувствительность совпадала со старым Input Manager.
        public static Vector2 MouseDelta => Mouse.current != null ? Mouse.current.delta.ReadValue() * 0.05f : Vector2.zero;
        public static bool ClickPressed => Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame;

        static UnityEngine.InputSystem.Controls.KeyControl ToControl(KeyCode key)
        {
            var kb = Keyboard.current;
            if (kb == null) return null;
            switch (key)
            {
                case KeyCode.W: return kb.wKey;
                case KeyCode.S: return kb.sKey;
                case KeyCode.H: return kb.hKey;
                case KeyCode.C: return kb.cKey;
                case KeyCode.R: return kb.rKey;
                case KeyCode.E: return kb.eKey;
                case KeyCode.Space: return kb.spaceKey;
                case KeyCode.UpArrow: return kb.upArrowKey;
                case KeyCode.DownArrow: return kb.downArrowKey;
                case KeyCode.Return: return kb.enterKey;
                case KeyCode.Escape: return kb.escapeKey;
                case KeyCode.F1: return kb.f1Key;
                default: return null;
            }
        }
#else
        static bool Held(KeyCode key) => false;
        static bool Pressed(KeyCode key) => false;
        public static Vector2 MouseDelta => Vector2.zero;
        public static bool ClickPressed => false;
#endif
    }
}
