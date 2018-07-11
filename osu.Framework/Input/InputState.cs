// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using JetBrains.Annotations;

namespace osu.Framework.Input
{
    public class InputState
    {
        [NotNull] public KeyboardState Keyboard;
        [NotNull] public MouseState Mouse;
        [NotNull] public JoystickState Joystick;

        public InputState()
        {
            Keyboard = new KeyboardState();
            Mouse = new MouseState();
            Joystick = new JoystickState();
        }
    }
}
