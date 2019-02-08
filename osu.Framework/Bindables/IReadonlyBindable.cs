// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;

namespace osu.Framework.Bindables
{
    public interface IReadonlyBindable : IDisposable
    {
        void ClearValueChanged();
    }

    public interface IReadonlyBindable<T> : IBindableView<T>, IReadonlyBindable
    {
        IBindableView<T> View { get; }
        event Action<T> ValueChanged;
    }
}
