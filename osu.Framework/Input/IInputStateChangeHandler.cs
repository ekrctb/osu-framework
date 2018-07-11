// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using osu.Framework.EventArgs;
using OpenTK.Input;

namespace osu.Framework.Input
{
    /// <summary>
    /// An object which can handle <see cref="InputState"/> changes.
    /// </summary>
    public interface IInputStateChangeHandler
    {
        /// <summary>
        /// Handles a change of mouse position.
        /// </summary>
        void HandleMousePositionChange(MousePositionChangeArgs args);

        /// <summary>
        /// Handles a change of mouse scroll.
        /// </summary>
        void HandleMouseScrollChange(MouseScrollChangeArgs args);

        /// <summary>
        /// Handles a change of mouse button state.
        /// </summary>
        void HandleMouseButtonStateChange(ButtonStateChangeArgs<MouseButton> args);

        /// <summary>
        /// Handles a change of keyboard key state.
        /// </summary>
        void HandleKeyboardKeyStateChange(ButtonStateChangeArgs<Key> args);

        /// <summary>
        /// Handles a change of joystick button state.
        /// </summary>
        void HandleJoystickButtonStateChange(ButtonStateChangeArgs<JoystickButton> args);
    }
}
