// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using JetBrains.Annotations;
using osu.Framework.Input;

namespace osu.Framework.EventArgs
{
    /// <summary>
    /// Denotes information of input event.
    /// </summary>
    public class InputStateChangeArgs : ArgsBase
    {
        /// <summary>
        /// An <see cref="IInput"/> that caused this input state change.
        /// </summary>
        [NotNull] public readonly IInput Input;

        public InputStateChangeArgs(InputState state, IInput input)
            : base(state)
        {
            Input = input;
        }
    }
}
