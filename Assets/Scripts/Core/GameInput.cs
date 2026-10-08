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
        public static bool HornPressed => Pressed(KeyCode.H);
        /// <summary>Ручник в машине (пешком тот же пробел — прыжок).</summary>
        public static bool Handbrake => Held(KeyCode.Space);
        public static bool SwitchCameraPressed => Pressed(KeyCode.C);
        public static bool RadioPressed => Pressed(KeyCode.R);
        public static bool InteractPressed => Pressed(KeyCode.E);
        public static bool RestartPressed => Pressed(KeyCode.Return);
        public static bool HelpPressed => Pressed(KeyCode.Tab);
        public static bool BlinkLeftPressed => Pressed(KeyCode.Q);
        public static bool BlinkRightPressed => Pressed(KeyCode.E);
        public static bool JumpPressed => Pressed(KeyCode.Space);
        public static bool IgnitionPressed => Pressed(KeyCode.I);
        public static bool CarDoorPressed => Pressed(KeyCode.F);
        /// <summary>Выбросить то, что в руках (сигарету, шампур).</summary>
        public static bool DropPressed => Pressed(KeyCode.G);
        /// <summary>Удар в драке — левая кнопка мыши.</summary>
        public static bool AttackPressed => ClickPressed;
        public static bool Run => Held(KeyCode.LeftShift) || Held(KeyCode.RightShift);

        /// <summary>Руль: −1 влево, +1 вправо.</summary>
        public static float Steer =>
            ((Held(KeyCode.D) || Held(KeyCode.RightArrow)) ? 1f : 0f) - ((Held(KeyCode.A) || Held(KeyCode.LeftArrow)) ? 1f : 0f);

        /// <summary>Ходьба пешком: x — вбок, y — вперёд.</summary>
        public static Vector2 Move => new Vector2(Steer, (Gas ? 1f : 0f) - (Brake ? 1f : 0f));

        /// <summary>Выбор варианта в диалоге: 1, 2, 3 (или 0, если ничего не нажато).</summary>
        public static int DialogChoice =>
            Pressed(KeyCode.Alpha1) || Pressed(KeyCode.Keypad1) ? 1 :
            Pressed(KeyCode.Alpha2) || Pressed(KeyCode.Keypad2) ? 2 :
            Pressed(KeyCode.Alpha3) || Pressed(KeyCode.Keypad3) ? 3 :
            Pressed(KeyCode.Alpha4) || Pressed(KeyCode.Keypad4) ? 4 : 0;

        /// <summary>Пока открыта пауза, игра не получает нажатий (кроме Esc и мыши для меню).</summary>
        public static bool Paused;
        public static bool PausePressed => RawPressed(KeyCode.Escape);

#if ENABLE_LEGACY_INPUT_MANAGER
        static bool Held(KeyCode key) => !Paused && Input.GetKey(key);
        static bool Pressed(KeyCode key) => !Paused && Input.GetKeyDown(key);
        static bool RawPressed(KeyCode key) => Input.GetKeyDown(key);

        public static Vector2 MouseDelta => Paused ? Vector2.zero : new Vector2(Input.GetAxisRaw("Mouse X"), Input.GetAxisRaw("Mouse Y"));
        public static bool ClickPressed => !Paused && Input.GetMouseButtonDown(0);
        /// <summary>Любая клавиша или клик (пропустить заставку), даже на паузе.</summary>
        public static bool AnyPressed => Input.anyKeyDown;
#elif ENABLE_INPUT_SYSTEM
        static bool Held(KeyCode key)
        {
            var control = ToControl(key);
            return !Paused && control != null && control.isPressed;
        }

        static bool Pressed(KeyCode key) => !Paused && RawPressed(key);

        static bool RawPressed(KeyCode key)
        {
            var control = ToControl(key);
            return control != null && control.wasPressedThisFrame;
        }

        // Масштаб подобран так, чтобы чувствительность совпадала со старым Input Manager.
        public static Vector2 MouseDelta => !Paused && Mouse.current != null ? Mouse.current.delta.ReadValue() * 0.05f : Vector2.zero;
        public static bool ClickPressed => !Paused && Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame;
        /// <summary>Любая клавиша или клик (пропустить заставку), даже на паузе.</summary>
        public static bool AnyPressed => (Keyboard.current != null && Keyboard.current.anyKey.wasPressedThisFrame)
            || (Mouse.current != null && (Mouse.current.leftButton.wasPressedThisFrame || Mouse.current.rightButton.wasPressedThisFrame));

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
                case KeyCode.Tab: return kb.tabKey;
                case KeyCode.Q: return kb.qKey;
                case KeyCode.I: return kb.iKey;
                case KeyCode.A: return kb.aKey;
                case KeyCode.D: return kb.dKey;
                case KeyCode.F: return kb.fKey;
                case KeyCode.G: return kb.gKey;
                case KeyCode.LeftArrow: return kb.leftArrowKey;
                case KeyCode.RightArrow: return kb.rightArrowKey;
                case KeyCode.LeftShift: return kb.leftShiftKey;
                case KeyCode.RightShift: return kb.rightShiftKey;
                case KeyCode.Alpha1: return kb.digit1Key;
                case KeyCode.Alpha2: return kb.digit2Key;
                case KeyCode.Alpha3: return kb.digit3Key;
                case KeyCode.Alpha4: return kb.digit4Key;
                case KeyCode.Keypad1: return kb.numpad1Key;
                case KeyCode.Keypad2: return kb.numpad2Key;
                case KeyCode.Keypad3: return kb.numpad3Key;
                case KeyCode.Keypad4: return kb.numpad4Key;
                default: return null;
            }
        }
#else
        static bool Held(KeyCode key) => false;
        static bool Pressed(KeyCode key) => false;
        static bool RawPressed(KeyCode key) => false;
        public static Vector2 MouseDelta => Vector2.zero;
        public static bool ClickPressed => false;
        public static bool AnyPressed => false;
#endif
    }
}
