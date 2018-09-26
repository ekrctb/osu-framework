﻿// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using osu.Framework.Extensions.TypeExtensions;
using osu.Framework.Input.States;

namespace osu.Framework.Input.Events
{
    /// <summary>
    /// Events of a joystick button.
    /// </summary>
    public abstract class JoystickButtonEvent : UIEvent
    {
        public readonly JoystickButton Button;

        protected JoystickButtonEvent(InputState state, JoystickButton button)
            : base(state)
        {
            Button = button;
        }

        public override string ToString() => $"{GetType().ReadableName()}({Button})";
    }
}
