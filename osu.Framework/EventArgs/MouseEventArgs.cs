// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using osu.Framework.Input;
using OpenTK;

namespace osu.Framework.EventArgs
{
    public class MouseEventArgs : InputEventArgs
    {
        /// <summary>
        /// The current mouse position in screen space.
        /// </summary>
        public Vector2 ScreenMousePosition => State.Mouse.Position;

        /// <summary>
        /// The current mouse position in local space.
        /// </summary>
        public Vector2 MousePosition => ToLocalSpace(ScreenMousePosition);

        public MouseEventArgs(InputState state)
            : base(state)
        {
        }
    }
}
