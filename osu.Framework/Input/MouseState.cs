// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using OpenTK;
using OpenTK.Input;

namespace osu.Framework.Input
{
    public class MouseState
    {
        public Vector2 Position
        {
            get
            {
                // for now don't throw an exception because invalid position is almost always not harmless
                if (!position.HasValue)
                    return new Vector2(float.MaxValue);
                return position.Value;
            }
            set => position = value;
        }

        private Vector2? position;

        public bool HasUninitializedPosition => !position.HasValue;

        public Vector2 Scroll;

        public readonly ButtonStates<MouseButton> Buttons = new ButtonStates<MouseButton>();

        public bool IsPressed(MouseButton button) => Buttons.IsPressed(button);

        public bool HasMainButtonPressed => Buttons.IsPressed(MouseButton.Left) || Buttons.IsPressed(MouseButton.Right);

        public bool HasAnyButtonPressed => Buttons.HasAnyButtonPressed;
    }
}
