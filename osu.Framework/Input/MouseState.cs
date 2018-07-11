// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using OpenTK;
using OpenTK.Input;

namespace osu.Framework.Input
{
    public class MouseState
    {
        public Vector2 Position;

        public bool IsPositionValid { get; set; } = true;

        public Vector2 Scroll;

        public readonly ButtonStates<MouseButton> Buttons = new ButtonStates<MouseButton>();

        public bool IsPressed(MouseButton button) => Buttons.IsPressed(button);

        public bool HasMainButtonPressed => Buttons.IsPressed(MouseButton.Left) || Buttons.IsPressed(MouseButton.Right);

        public bool HasAnyButtonPressed => Buttons.HasAnyButtonPressed;
    }
}
