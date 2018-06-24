// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using JetBrains.Annotations;
using OpenTK;

namespace osu.Framework.Input.EventArgs
{
    public class MouseScrolChangeArgs : InputStateChangeArgs
    {
        /// <summary>
        /// The amount of scroll.
        /// </summary>
        public readonly Vector2 ScrollDelta;

        /// <summary>
        /// Whether the scroll is a precise scrolling.
        /// </summary>
        public readonly bool IsPrecise;

        public MouseScrolChangeArgs([NotNull] InputState state, [CanBeNull] IInput input, Vector2 scrollDelta, bool isPrecise)
            : base(state, input)
        {
            ScrollDelta = scrollDelta;
            IsPrecise = isPrecise;
        }
    }
}
