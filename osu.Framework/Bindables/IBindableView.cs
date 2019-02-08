// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using JetBrains.Annotations;

namespace osu.Framework.Bindables
{
    public interface IBindableView<out T>
    {
        T Value { get; }

        [NotNull]
        IReadonlyBindable<T> GetReadonlyBindable();
    }
}
