// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

namespace osu.Framework.Input.EventArgs
{
    public class JoystickReleaseEventArgs : JoystickButtonEventArgs
    {
        public JoystickReleaseEventArgs(InputState state, JoystickButton button)
            : base(state, button)
        {
        }
    }
}
