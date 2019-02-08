// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using JetBrains.Annotations;

namespace osu.Framework.Bindables
{
    public static class BindableExtensions
    {
        public static FlattenBindable<T> Flatten<T>([NotNull] this IBindableView<IBindableView<T>> nested)
        {
            return new FlattenBindable<T>(nested);
        }
    }
}
