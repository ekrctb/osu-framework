// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using OpenTK;

namespace osu.Framework.Input.EventArgs
{
    public class MouseScrollChangeArgs : InputStateChangeArgs
    {
        public Vector2 LastScroll;

        public bool IsPrecise;

        public MouseScrollChangeArgs(InputState state, IInput input, Vector2 lastScroll, bool isPrecise)
            : base(state, input)
        {
            LastScroll = lastScroll;
            IsPrecise = isPrecise;
        }
    }
}
