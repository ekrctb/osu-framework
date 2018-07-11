// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using osu.Framework.Configuration;
using osu.Framework.EventArgs;
using osu.Framework.Graphics.Containers;

namespace osu.Framework.Graphics.UserInterface
{
    public abstract class Checkbox : Container, IHasCurrentValue<bool>
    {
        public Bindable<bool> Current { get; } = new Bindable<bool>();

        protected override bool OnClick(ClickEventArgs args)
        {
            if (!Current.Disabled)
                Current.Value = !Current;

            base.OnClick(args);
            return true;
        }
    }
}
