// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using osu.Framework.Input;
using OpenTK;
using OpenTK.Input;

namespace osu.Framework.EventArgs
{
    public abstract class MouseButtonEventArgs : MouseEventArgs
    {
        public MouseButton Button;
        public Vector2 ScreenMouseDownPosition;

        public Vector2 MouseDownPosition => ToLocalSpace(ScreenMouseDownPosition);

        protected MouseButtonEventArgs(InputState state, MouseButton button)
            : base(state)
        {
            Button = button;
            ScreenMouseDownPosition = ScreenMousePosition;
        }
    }
}
