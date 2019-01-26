// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using osu.Framework.Graphics;
using osu.Framework.Input.Events;
using osu.Framework.Input.StateChanges;
using osu.Framework.Input.States;
using osuTK.Input;

namespace osu.Framework.Input
{
    public class KeyboardKeyEventManager : ButtonEventManager<Key>
    {
        public KeyboardKeyEventManager(Key button, Func<IEnumerable<Drawable>> inputQueueProvider)
            : base(button, inputQueueProvider)
        {
        }

        protected override UIEvent CreateButtonDownEvent(InputState state) =>
            new KeyDownEvent(state, Button);

        protected override UIEvent CreateButtonUpEvent(InputState state) =>
            new KeyUpEvent(state, Button);

        public override void HandleButtonStateChange(InputState state, ButtonStateChangeKind kind, double currentTime)
        {
            Trace.Assert(state.Keyboard.Keys.IsPressed(Button) == (kind == ButtonStateChangeKind.Pressed));
            base.HandleButtonStateChange(state, kind, currentTime);
        }
    }
}
