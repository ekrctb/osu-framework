// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using osu.Framework.Input;

namespace osu.Framework.EventArgs
{
    public class JoystickPressEventArgs : JoystickButtonEventArgs
    {
        public JoystickPressEventArgs(InputState state, JoystickButton button)
            : base(state, button)
        {
        }
    }
}
