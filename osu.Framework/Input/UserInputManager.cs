// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System.Collections.Generic;
using osu.Framework.Input.EventArgs;
using osu.Framework.Input.Handlers;
using osu.Framework.Platform;
using OpenTK;

namespace osu.Framework.Input
{
    public class UserInputManager : PassThroughInputManager
    {
        protected override IEnumerable<InputHandler> InputHandlers => Host.AvailableInputHandlers;

        protected override bool HandleHoverEvents => Host.Window?.CursorInWindow ?? true;

        public UserInputManager()
        {
            UseParentInput = false;
        }

        public override void HandleMousePositionChange(MousePositionChangeArgs args)
        {
            var mouse = args.State.Mouse;
            // confine cursor
            if (Host.Window != null && (Host.Window.CursorState & CursorState.Confined) > 0)
            {
                mouse.Position = Vector2.Clamp(mouse.Position, Vector2.Zero, new Vector2(Host.Window.Width, Host.Window.Height));
            }

            base.HandleMousePositionChange(args);
        }

        public override void HandleMouseScrollChange(MouseScrollChangeArgs args)
        {
            if (Host.Window != null && !Host.Window.CursorInWindow) return;
            base.HandleMouseScrollChange(args);
        }
    }
}
