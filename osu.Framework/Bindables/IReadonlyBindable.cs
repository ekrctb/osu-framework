// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using JetBrains.Annotations;

namespace osu.Framework.Bindables
{
    public interface IReadonlyBindable : IDisposable
    {
        void ClearValueChanged();
    }

    public interface IReadonlyBindable<out T> : IBindableView<T>, IReadonlyBindable
    {
        [NotNull]
        IBindableView<T> View { get; }

        event Action<T> ValueChanged;
    }
}
