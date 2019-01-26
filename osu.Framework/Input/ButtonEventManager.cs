// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Linq;
using osu.Framework.Graphics;
using osu.Framework.Input.Events;
using osu.Framework.Input.StateChanges;
using osu.Framework.Input.States;
using osu.Framework.Logging;

namespace osu.Framework.Input
{
    public abstract class ButtonEventManager<TButton>
    {
        /// <summary>
        /// The button this manager manages.
        /// </summary>
        public readonly TButton Button;

        private readonly Func<IEnumerable<Drawable>> getInputQueue;

        protected IEnumerable<Drawable> InputQueue => getInputQueue?.Invoke() ?? Enumerable.Empty<Drawable>();

        /// <summary>
        /// The input queue for propagating button up event.
        /// This is created from the <see cref="InputQueue"/> when the last time the button is pressed.
        /// </summary>
        protected List<Drawable> ButtonDownInputQueue;

        protected double ButtonStateChangeTime { get; private set; }

        protected ButtonEventManager(TButton button, Func<IEnumerable<Drawable>> inputQueueProvider)
        {
            Button = button;
            getInputQueue = inputQueueProvider;
        }

        protected abstract UIEvent CreateButtonDownEvent(InputState state);

        protected abstract UIEvent CreateButtonUpEvent(InputState state);

        public virtual void HandleButtonStateChange(InputState state, ButtonStateChangeKind kind, double currentTime)
        {
            ButtonStateChangeTime = currentTime;

            if (kind == ButtonStateChangeKind.Pressed)
                HandleButtonDown(state);
            else
                HandleButtonUp(state);
        }

        protected virtual void HandleButtonDown(InputState state)
        {
            var inputQueue = InputQueue.ToList();
            var handledBy = PropagateEvent(inputQueue, CreateButtonDownEvent(state));

            // only drawables up to the one that handled mouse down should handle mouse up
            if (handledBy != null)
            {
                var count = inputQueue.IndexOf(handledBy) + 1;
                inputQueue.RemoveRange(count, inputQueue.Count - count);
            }

            ButtonDownInputQueue = inputQueue;
        }

        protected virtual void HandleButtonUp(InputState state)
        {
            if (ButtonDownInputQueue == null) return;

            PropagateEvent(ButtonDownInputQueue, CreateButtonUpEvent(state));

            ButtonDownInputQueue = null;
        }

        /// <summary>
        /// Triggers events on drawables in <paramref cref="drawables"/> until it is handled.
        /// </summary>
        /// <param name="drawables">The drawables in the queue.</param>
        /// <param name="e">The event.</param>
        /// <returns>The drawable which handled the event or null if none.</returns>
        protected virtual Drawable PropagateEvent(IEnumerable<Drawable> drawables, UIEvent e)
        {
            var handledBy = drawables.FirstOrDefault(target => target.TriggerEvent(e));

            if (handledBy != null)
                Logger.Log($"{e} handled by {handledBy}.", LoggingTarget.Runtime, LogLevel.Debug);

            return handledBy;
        }
    }
}
