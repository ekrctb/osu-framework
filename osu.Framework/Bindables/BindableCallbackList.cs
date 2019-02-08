// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using JetBrains.Annotations;

namespace osu.Framework.Bindables
{
    internal struct BindableCallbackList<T>
    {
        [CanBeNull]
        private List<Entry> callbacks;

        public void Add(ulong id, Action<T> action)
        {
            if (callbacks == null) callbacks = new List<Entry>();
            callbacks.Add(new Entry(id, action));
        }

        public void Remove(ulong id, Action<T> action)
        {
            callbacks?.Remove(new Entry(id, action));
        }

        public void Clear(ulong id)
        {
            callbacks?.RemoveAll(c => c.Id == id);
        }

        public void Trigger(T value)
        {
            if (callbacks == null) return;

            foreach (var callback in callbacks)
                callback.Action(value);
        }

        private struct Entry : IEquatable<Entry>
        {
            public readonly ulong Id;

            [NotNull]
            public readonly Action<T> Action;

            public Entry(ulong id, [NotNull] Action<T> action)
            {
                Id = id;
                Action = action;
            }

            public bool Equals(Entry other)
            {
                return Id == other.Id && Action.Equals(other.Action);
            }
        }
    }
}
