// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using OpenTK;

namespace osu.Framework.Input.EventArgs
{
    public class MouseMoveEventArgs : InputEventArgs
    {
        /// <summary>
        /// The last mouse position before this mouse move in the screen space.
        /// </summary>
        public Vector2 ScreenLastMousePosition;

        /// <summary>
        /// The last mouse position before this mouse move in local space.
        /// </summary>
        public Vector2 LastMousePosition => ToLocalSpace(ScreenLastMousePosition);

        /// <summary>
        /// The difference of mouse position from last position to current position in local space.
        /// </summary>
        public Vector2 MousePositionDelta => MousePosition - LastMousePosition;

        public MouseMoveEventArgs(InputState state)
            : base(state)
        {
            ScreenLastMousePosition = state.Mouse.Position;
        }
    }
}
