// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using JetBrains.Annotations;

namespace osu.Framework.Bindables
{
    public class ReadonlyBindable<T> : IReadonlyBindable<T>
    {
        public T Value => content.Value;

        public IBindableView<T> View => content;
        private readonly ulong id;

        [NotNull]
        private readonly BindableContent<T> content;

        private static readonly BindableContent<T> default_content = new BindableContent<T>(default);

        public ReadonlyBindable()
            : this(default_content)
        {
        }

        public ReadonlyBindable(T value)
            : this(new BindableContent<T>(value))
        {
        }

        internal ReadonlyBindable([NotNull] BindableContent<T> content)
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

        public IReadonlyBindable<T> GetReadonlyBindable()
        {
            return content.GetReadonlyBindable();
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
