// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using JetBrains.Annotations;

namespace osu.Framework.Bindables
{
    public struct ReadonlyBindable<T> : IReadonlyBindable<T>
    {
        private readonly ulong id;

        [NotNull]
        private readonly BindableContent<T> content;

        public static readonly ReadonlyBindable<T> DEFAULT = new ReadonlyBindable<T>((T)default);

        public ReadonlyBindable(T value)
            : this(new BindableContent<T>(value))
        {
        }

        internal ReadonlyBindable([NotNull] BindableContent<T> content)
        {
            id = content.GetId();
            this.content = content;
        }

        public T Value => content.Value;

        public ReadonlyBindable<T> GetReadonlyBindable()
        {
            return content.GetReadonlyBindable();
        }

        public event Action<T> ValueChanged
        {
            add => content.Callbacks.Add(id, value);
            remove => content.Callbacks.Remove(id, value);
        }

        public void ClearValueChanged()
        {
            content.Callbacks.Clear(id);
        }

        public IBindableView<T> View => content;

        public void Dispose()
        {
            ClearValueChanged();
        }
    }
}
