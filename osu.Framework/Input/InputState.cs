// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using JetBrains.Annotations;

namespace osu.Framework.Input
{
    public class InputState
    {
        [NotNull] public readonly KeyboardState Keyboard;
        [NotNull] public readonly MouseState Mouse;
        [NotNull] public readonly JoystickState Joystick;

        public InputState(KeyboardState keyboard, MouseState mouse, JoystickState joystick)
        {
            Keyboard = keyboard;
            Mouse = mouse;
            Joystick = joystick;
        }
    }
}
