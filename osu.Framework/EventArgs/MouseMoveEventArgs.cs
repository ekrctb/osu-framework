// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using osu.Framework.Input;
using OpenTK;

namespace osu.Framework.EventArgs
{
    public class MouseMoveEventArgs : MouseEventArgs
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
        public Vector2 Delta => MousePosition - LastMousePosition;

        public MouseMoveEventArgs(InputState state)
            : base(state)
        {
            ScreenLastMousePosition = ScreenMousePosition;
        }
    }
}
