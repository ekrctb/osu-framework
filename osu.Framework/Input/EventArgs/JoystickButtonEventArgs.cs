﻿// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

namespace osu.Framework.Input.EventArgs
{
    public abstract class JoystickButtonEventArgs : InputEventArgs
    {
        public JoystickButton Button;
        protected JoystickButtonEventArgs(InputState state, JoystickButton button)
            : base(state)
        {
            Button = button;
        }
    }
}
