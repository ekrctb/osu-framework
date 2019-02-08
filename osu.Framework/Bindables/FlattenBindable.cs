// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using JetBrains.Annotations;

namespace osu.Framework.Bindables
{
    public sealed class FlattenBindable<T> : IReadonlyBindable<T>
    {
        public T Value => binding.Value;

        public IBindableView<T> View => binding.View;

        [NotNull]
        private readonly IReadonlyBindable<IBindableView<T>> outerBindable;

        [NotNull]
        private readonly MutableBindable<T> binding;

        [NotNull]
        private IReadonlyBindable<T> innerBindable;

        public FlattenBindable([NotNull] IBindableView<IBindableView<T>> nested)
        {
            outerBindable = nested.GetReadonlyBindable();
            innerBindable = nested.Value.GetReadonlyBindable();
            binding = new MutableBindable<T>(innerBindable.Value);
            outerBindable.ValueChanged += outerValueChanged;
        }

        #region Disposal

        public void Dispose()
        {
            outerBindable.Dispose();
            innerBindable.Dispose();
            binding.Dispose();
        }

        #endregion

        public IReadonlyBindable<T> GetReadonlyBindable()
        {
            return binding.GetReadonlyBindable();
        }

        public void ClearValueChanged()
        {
            binding.ClearValueChanged();
        }

        private void outerValueChanged(IBindableView<T> inner)
        {
            innerBindable.Dispose();
            innerBindable = inner.GetReadonlyBindable();
            innerBindable.ValueChanged += innerValueChanged;
            binding.Value = inner.Value;
        }

        private void innerValueChanged(T value)
        {
            binding.Value = value;
        }

        public event Action<T> ValueChanged
        {
            add => binding.ValueChanged += value;
            remove => binding.ValueChanged -= value;
        }
    }
}
