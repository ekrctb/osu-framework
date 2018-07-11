// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using osu.Framework.Input.EventArgs;
using OpenTK;

namespace osu.Framework.Input
{
    /// <summary>
    /// Denotes an absolute change of mouse position.
    /// Pointing devices such as tablets provide absolute input.
    /// </summary>
    /// <remarks>
    /// This is the first input received from any pointing device.
    /// </remarks>
    public class MousePositionAbsoluteInput : IInput
    {
        /// <summary>
        /// The position which will be assigned to the current position.
        /// </summary>
        public Vector2 Position;

        public void Apply(InputState state, IInputStateChangeHandler handler)
        {
            var mouse = state.Mouse;
            if (!mouse.IsPositionValid || mouse.Position != Position)
            {
                var lastPosition = !mouse.IsPositionValid ? Position : mouse.Position;
                mouse.IsPositionValid = true;
                mouse.LastPosition = mouse.Position;
                mouse.Position = Position;
                handler.HandleMousePositionChange(new MousePositionChangeArgs(state, this, lastPosition));
            }
        }
    }
}
