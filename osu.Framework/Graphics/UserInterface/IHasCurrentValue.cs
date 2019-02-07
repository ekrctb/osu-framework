// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Bindables;

namespace osu.Framework.Graphics.UserInterface
{
    /// <summary>
    /// A UI element which supports a <see cref="LegacyBindable{T}"/> current value.
    /// You can bind to <see cref="Current"/>'s <see cref="LegacyBindable{T}.ValueChanged"/> to get updates.
    /// </summary>
    public interface IHasCurrentValue<T>
    {
        /// <summary>
        /// Gets or sets the <see cref="LegacyBindable{T}"/> that provides the current value.
        /// </summary>
        /// <remarks>
        /// A provided <see cref="LegacyBindable{T}"/> will be bound to, rather than be stored internally.
        /// </remarks>
        LegacyBindable<T> Current { get; set; }
    }
}
