// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;

namespace osu.Framework.Bindables
{
    public class BindableContent<T> : IMutableBindableView<T>
    {
        public T Value
        {
            get => value;
            set
            {
                if (Equals(this.value, value)) return;

                this.value = value;
                TriggerValueChange();
            }
        }

        internal BindableCallbackList<T> Callbacks;
        private T value;
        private ulong lastId;

        public BindableContent(T value)
        {
            this.value = value;
            Callbacks = new BindableCallbackList<T>();
        }

        public void TriggerValueChange()
        {
            Callbacks.Trigger(value);
        }

        public ReadonlyBindable<T> GetReadonlyBindable()
        {
            return new ReadonlyBindable<T>(this);
        }

        public MutableBindable<T> GetMutableBindable()
        {
            return new MutableBindable<T>(this);
        }

        internal ulong GetId()
        {
            return ++lastId;
        }

        protected virtual bool Equals(T x, T y)
        {
            return EqualityComparer<T>.Default.Equals(x, y);
        }
    }
}
