// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using JetBrains.Annotations;
using osu.Framework.Input;

namespace osu.Framework.EventArgs
{
    /// <summary>
    /// Denotes information of input event.
    /// </summary>
    public abstract class ArgsBase
    {
        /// <summary>
        /// The current input state.
        /// Positions are in screen space.
        /// </summary>
        [NotNull] public readonly InputState State;

        protected ArgsBase([NotNull] InputState state)
        {
            State = state ?? throw new ArgumentNullException(nameof(state));
        }

        /// <summary>
        /// Create a shallow clone of this object.
        /// </summary>
        /// <returns>A cloned object.</returns>
        public virtual object Clone()
        {
            return (object)MemberwiseClone();
        }
    }
}
