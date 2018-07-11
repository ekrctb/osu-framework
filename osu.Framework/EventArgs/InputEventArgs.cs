// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using JetBrains.Annotations;
using osu.Framework.Graphics;
using osu.Framework.Input;
using OpenTK;

namespace osu.Framework.EventArgs
{
    /// <summary>
    /// Denotes information of input event with positions represented in a local choordinate.
    /// </summary>
    public class InputEventArgs : ArgsBase
    {
        [CanBeNull] public Drawable Drawable;

        protected Vector2 ToLocalSpace(Vector2 screenSpacePosition) => Drawable?.ToLocalSpace(screenSpacePosition) ?? screenSpacePosition;

        public InputEventArgs(InputState state) : base(state)
        {
        }
    }
}
