// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using osu.Framework.Graphics;
using osu.Framework.Input.Events;
using osu.Framework.Input.States;

namespace osu.Framework.Input
{
    public class JoystickButtonEventManager : ButtonEventManager<JoystickButton>
    {

        public JoystickButtonEventManager(JoystickButton button, Func<IEnumerable<Drawable>> inputQueueProvider)
            : base(button, inputQueueProvider)
        {
        }

        protected override UIEvent CreateButtonDownEvent(InputState state) =>
            new JoystickPressEvent(state, Button);

        protected override UIEvent CreateButtonUpEvent(InputState state) =>
            new JoystickReleaseEvent(state, Button);

    }
}
