// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using JetBrains.Annotations;
using OpenTK;

namespace osu.Framework.Input.EventArgs
{
    public class MousePositionChangeArgs : InputStateChangeArgs
    {
        /// <summary>
        /// The difference of mouse position from last position to current position.
        /// </summary>
        public readonly Vector2 Delta;

        public MousePositionChangeArgs([NotNull] InputState state, [CanBeNull] IInput input, Vector2 delta)
            : base(state, input)
        {
            Delta = delta;
        }
    }
}
