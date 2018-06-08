﻿// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using osu.Framework.Caching;

namespace osu.Framework.Graphics.Containers
{
    /// <summary>
    /// A container which is rounded (via automatic corner-radius) on the shortest edge.
    /// </summary>
    public class CircularContainer : Container
    {
        private Cached layout = new Cached();

        public override bool Invalidate(Invalidation invalidation = Invalidation.All, Drawable source = null, bool shallPropagate = true)
        {
            bool result = base.Invalidate(invalidation, source, shallPropagate);

            if ((invalidation & (Invalidation.DrawInfo | Invalidation.RequiredParentSizeToFit)) > 0)
                layout.Invalidate();

            return result;
        }

        protected override bool RequiresLayoutValidation => base.RequiresLayoutValidation || !layout.IsValid;

        protected override void UpdateLayout()
        {
            base.UpdateLayout();

            if (!layout.IsValid)
            {
                CornerRadius = Math.Min(DrawSize.X, DrawSize.Y) / 2f;
                layout.Validate();
            }
        }
    }
}
