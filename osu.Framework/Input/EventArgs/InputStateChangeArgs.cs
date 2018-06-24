// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using JetBrains.Annotations;

namespace osu.Framework.Input.EventArgs
{
    /// <summary>
    /// Denotes information of input state change.
    /// </summary>
    public class InputStateChangeArgs
    {
        /// <summary>
        /// The current input state.
        /// </summary>
        [NotNull] public readonly InputState State;

        /// <summary>
        /// The <see cref="IInput"/> that caused this input state change.
        /// </summary>
        [CanBeNull] public readonly IInput Input;

        public InputStateChangeArgs([NotNull] InputState state, [CanBeNull] IInput input)
        {
            State = state ?? throw new ArgumentNullException(nameof(state));
            Input = input;
        }
    }
}
