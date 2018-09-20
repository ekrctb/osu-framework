// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using osu.Framework.Extensions.TypeExtensions;
using osu.Framework.Input.Events;
using osu.Framework.Input.States;

namespace osu.Framework.Input.Bindings.Events
{
    public abstract class ActionEvent<T> : UIEvent
    where T : struct
    {
        public readonly T Action;

        protected ActionEvent(InputState state, T action)
            : base(state)
        {
            Action = action;
        }

        public override string ToString() => $"{GetType().ReadableName()}({Action})";
    }
}
