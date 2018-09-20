// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using osu.Framework.Input.States;

namespace osu.Framework.Input.Bindings.Events
{
    public class ActionPressEvent<T> : ActionEvent<T>
    where T : struct
    {
        public readonly bool Repeat;

        public ActionPressEvent(InputState state, T action, bool repeat = false)
            : base(state, action)
        {
            Repeat = repeat;
        }
    }
}
