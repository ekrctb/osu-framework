// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

namespace osu.Framework.Bindables
{
    /// <summary>
    /// Interface for objects that support publicly unbinding events or <see cref="ILegacyBindable"/>s.
    /// </summary>
    public interface IUnbindable
    {
        /// <summary>
        /// Unbinds all bound events.
        /// </summary>
        void UnbindEvents();

        /// <summary>
        /// Unbinds all bound <see cref="ILegacyBindable"/>s.
        /// </summary>
        void UnbindBindings();

        /// <summary>
        /// Calls <see cref="UnbindEvents"/> and <see cref="UnbindBindings"/>
        /// </summary>
        void UnbindAll();

        /// <summary>
        /// Unbinds ourselves from another <see cref="ILegacyBindable"/> such that we stop receiving updates it.
        /// The other <see cref="ILegacyBindable"/> will also stop receiving any events from us.
        /// </summary>
        /// <param name="them">The other bindable.</param>
        void UnbindFrom(IUnbindable them);
    }
}
