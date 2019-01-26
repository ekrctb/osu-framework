// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Linq;
using osu.Framework.Graphics;
using osu.Framework.Input.Events;
using osu.Framework.Input.States;
using osuTK.Input;

namespace osu.Framework.Input
{
    public class KeyboardKeyEventManager : ButtonEventManager<Key>
    {
        public KeyboardKeyEventManager(Key key)
            : base(key)
        {
        }

        public Func<IEnumerable<Drawable>> GetNonPositionalInputQueue;

        protected override IEnumerable<Drawable> InputQueue => GetNonPositionalInputQueue?.Invoke() ?? Enumerable.Empty<Drawable>();
        protected override UIEvent CreateButtonDownEvent(InputState state) =>
            new KeyDownEvent(state, Button);

        protected override UIEvent CreateButtonUpEvent(InputState state) =>
            new KeyUpEvent(state, Button);
    }
}
