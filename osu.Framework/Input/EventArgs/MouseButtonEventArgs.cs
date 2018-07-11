// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using OpenTK;
using OpenTK.Input;

namespace osu.Framework.Input.EventArgs
{
    public abstract class MouseButtonEventArgs : InputEventArgs
    {
        public MouseButton Button;
        public Vector2 ScreenMouseDownPosition;

        public Vector2 MouseDownPosition => ToLocalSpace(ScreenMouseDownPosition);

        protected MouseButtonEventArgs(InputState state, MouseButton button)
            : base(state)
        {
            Button = button;
            ScreenMouseDownPosition = state.Mouse.Position;
        }
    }
}
