// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using OpenTK;

namespace osu.Framework.Input.EventArgs
{
    public class MousePositionChangeArgs : InputStateChangeArgs
    {
        /// <summary>
        /// The last mouse position.
        /// </summary>
        public Vector2 LastPosition;

        public MousePositionChangeArgs(InputState state, IInput input, Vector2 lastPosition)
            : base(state, input)
        {
            LastPosition = lastPosition;
        }
    }
}
