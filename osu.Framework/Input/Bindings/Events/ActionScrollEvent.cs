// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using osu.Framework.Extensions.TypeExtensions;
using osu.Framework.Input.States;

namespace osu.Framework.Input.Bindings.Events
{
    public class ActionScrollEvent<T> : ActionPressEvent<T>
    where T : struct
    {
        public readonly float Amount;
        public readonly bool IsPrecise;

        public ActionScrollEvent(InputState state, T action, float amount, bool isPrecise = false)
            : base(state, action)
        {
            Amount = amount;
            IsPrecise = isPrecise;
        }

        public override string ToString() => $"{GetType().ReadableName()}({Action}, {Amount}, {IsPrecise})";
    }
}
