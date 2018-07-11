// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using OpenTK;
using OpenTK.Input;

namespace osu.Framework.Input
{
    public class MouseState
    {
        public ButtonStates<MouseButton> Buttons { get; private set; } = new ButtonStates<MouseButton>();

        public Vector2 Scroll { get; set; }

        public bool HasMainButtonPressed => Buttons.IsPressed(MouseButton.Left) || Buttons.IsPressed(MouseButton.Right);

        public bool HasAnyButtonPressed => Buttons.HasAnyButtonPressed;

        public Vector2 Position { get; set; }

        public bool IsPositionValid { get; set; } = true;

        public bool IsPressed(MouseButton button) => Buttons.IsPressed(button);
    }
}
