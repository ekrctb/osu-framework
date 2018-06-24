// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using JetBrains.Annotations;

namespace osu.Framework.Input.EventArgs
{
    public class ButtonStateChangeArgs<TButton> : InputStateChangeArgs
    where TButton : struct
    {
        /// <summary>
        /// The button which state changed.
        /// </summary>
        public readonly TButton Button;

        /// <summary>
        /// The kind of button state change. either pressed or released.
        /// </summary>
        public readonly ButtonStateChangeKind Kind;

        public ButtonStateChangeArgs([NotNull] InputState state, [CanBeNull] IInput input, TButton button, ButtonStateChangeKind kind)
            : base(state, input)
        {
            Button = button;
            Kind = kind;
        }
    }
}
