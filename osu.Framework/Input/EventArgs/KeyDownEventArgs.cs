// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE


using OpenTK.Input;

namespace osu.Framework.Input.EventArgs
{
    public class KeyDownEventArgs : KeyboardEventArgs
    {
        public bool Repeat;

        public KeyDownEventArgs(InputState state, Key key)
            : base(state, key)
        {
        }
    }
}
