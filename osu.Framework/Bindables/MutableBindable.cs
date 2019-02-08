// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using JetBrains.Annotations;

namespace osu.Framework.Bindables
{
    public sealed class MutableBindable<T> : IReadonlyBindable<T>, IMutableBindableView<T>
    {
        public IBindableView<T> View => content;

        public T Value
        {
            get => content.Value;
            set => content.Value = value;
        }

        private readonly ulong id;

        [NotNull]
        private readonly BindableContent<T> content;

        public MutableBindable()
            : this((T)default)
        {
        }

        public MutableBindable(T value)
            : this(new BindableContent<T>(value))
        {
        }

        internal MutableBindable([NotNull] BindableContent<T> content)
        {
            id = content.GetId();
            this.content = content;
        }

        #region Disposal

        public void Dispose()
        {
            ClearValueChanged();
        }

        #endregion

        public void TriggerValueChange()
        {
            content.TriggerValueChange();
        }

        public IReadonlyBindable<T> GetReadonlyBindable()
        {
            return content.GetReadonlyBindable();
        }

        public MutableBindable<T> GetMutableBindable()
        {
            return content.GetMutableBindable();
        }

        public void ClearValueChanged()
        {
            content.Callbacks.Clear(id);
        }

        public event Action<T> ValueChanged
        {
            add => content.Callbacks.Add(id, value);
            remove => content.Callbacks.Remove(id, value);
        }
    }
}
