// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using JetBrains.Annotations;
using osu.Framework.Graphics;
using osu.Framework.Input.Events;
using osu.Framework.Input.StateChanges;
using osu.Framework.Input.States;
using osuTK;
using osuTK.Input;

namespace osu.Framework.Input
{
    /// <summary>
    /// Manages state and events (click, drag and double-click) for a single mouse button.
    /// </summary>
    public abstract class MouseButtonEventManager : ButtonEventManager<MouseButton>
    {
        protected override UIEvent CreateButtonDownEvent(InputState state) =>
            new MouseDownEvent(state, Button, MouseDownPosition);

        protected override UIEvent CreateButtonUpEvent(InputState state) =>
            new MouseUpEvent(state, Button, MouseDownPosition);

        /// <summary>
        /// Used for requesting focus from click.
        /// </summary>
        [CanBeNull] public Action<Drawable> RequestFocus;

        /// <summary>
        /// Whether dragging is handled by the managed button.
        /// </summary>
        public abstract bool EnableDrag { get; }

        /// <summary>
        /// Whether click and double click are handled by the managed button.
        /// </summary>
        public abstract bool EnableClick { get; }

        /// <summary>
        /// Whether focus is changed when the button is clicked.
        /// </summary>
        public abstract bool ChangeFocusOnClick { get; }

        protected MouseButtonEventManager(MouseButton button, Func<IEnumerable<Drawable>> inputQueueProvider)
            : base(button, inputQueueProvider)
        {
        }

        /// <summary>
        /// The maximum time between two clicks for a double-click to be considered.
        /// </summary>
        public virtual float DoubleClickTime => 250;

        /// <summary>
        /// The distance that must be moved until a dragged click becomes invalid.
        /// </summary>
        public virtual float ClickDragDistance => 10;

        /// <summary>
        /// The position of the mouse when the last time the button is pressed.
        /// </summary>
        public Vector2? MouseDownPosition { get; protected set; }

        /// <summary>
        /// The time of last click.
        /// </summary>
        protected double? LastClickTime;

        /// <summary>
        /// The drawable which is clicked by the last click.
        /// </summary>
        protected WeakReference<Drawable> ClickedDrawable = new WeakReference<Drawable>(null);

        /// <summary>
        /// Whether a drag operation has started and <see cref="DraggedDrawable"/> has been searched for.
        /// </summary>
        protected bool DragStarted;

        /// <summary>
        /// The <see cref="Drawable"/> which is currently being dragged. null if none is.
        /// </summary>
        public Drawable DraggedDrawable { get; protected set; }

        public virtual void HandlePositionChange(InputState state, Vector2 lastPosition)
        {
            if (EnableDrag)
            {
                if (!DragStarted)
                {
                    var mouse = state.Mouse;
                    if (mouse.IsPressed(Button) && Vector2Extensions.Distance(MouseDownPosition ?? mouse.Position, mouse.Position) > ClickDragDistance)
                        HandleMouseDragStart(state);
                }
                else
                {
                    HandleMouseDrag(state, lastPosition);
                }
            }
        }

        protected bool BlockNextClick;

        public override void HandleButtonStateChange(InputState state, ButtonStateChangeKind kind, double currentTime)
        {
            Trace.Assert(state.Mouse.IsPressed(Button) == (kind == ButtonStateChangeKind.Pressed));

            base.HandleButtonStateChange(state, kind, currentTime);
        }

        protected override void HandleButtonDown(InputState state)
        {
            if (state.Mouse.IsPositionValid)
                MouseDownPosition = state.Mouse.Position;

            base.HandleButtonDown(state);

            if (LastClickTime != null && ButtonStateChangeTime - LastClickTime < DoubleClickTime)
            {
                if (HandleMouseDoubleClick(state))
                {
                    //when we handle a double-click we want to block a normal click from firing.
                    BlockNextClick = true;
                    LastClickTime = null;
                }
            }
        }

        protected override void HandleButtonUp(InputState state)
        {
            if (EnableClick && DraggedDrawable == null)
            {
                if (!BlockNextClick)
                {
                    LastClickTime = ButtonStateChangeTime;
                    HandleMouseClick(state);
                }
            }

            BlockNextClick = false;

            if (EnableDrag)
                HandleMouseDragEnd(state);

            base.HandleButtonUp(state);

            MouseDownPosition = null;
        }

        protected virtual void HandleMouseClick(InputState state)
        {
            // due to the laziness of IEnumerable, .Where check should be done right before it is triggered for the event.
            var drawables = ButtonDownInputQueue.Intersect(InputQueue)
                                                .Where(t => t.IsAlive && t.IsPresent && t.ReceivePositionalInputAt(state.Mouse.Position));

            var clicked = PropagateEvent(drawables, new ClickEvent(state, Button, MouseDownPosition));
            ClickedDrawable.SetTarget(clicked);

            if (ChangeFocusOnClick)
                RequestFocus?.Invoke(clicked);
        }

        protected virtual bool HandleMouseDoubleClick(InputState state)
        {
            if (!ClickedDrawable.TryGetTarget(out Drawable clicked))
                return false;

            if (!InputQueue.Contains(clicked))
                return false;

            return PropagateEvent(new[] { clicked }, new DoubleClickEvent(state, Button, MouseDownPosition)) != null;
        }

        protected virtual bool HandleMouseDrag(InputState state, Vector2 lastPosition)
        {
            if (DraggedDrawable == null) return false;

            //Once a drawable is dragged, it remains in a dragged state until the drag is finished.
            return PropagateEvent(new[] { DraggedDrawable }, new DragEvent(state, Button, MouseDownPosition, lastPosition)) != null;
        }

        protected virtual bool HandleMouseDragStart(InputState state)
        {
            Trace.Assert(DraggedDrawable == null, $"The {nameof(DraggedDrawable)} was not set to null by {nameof(HandleMouseDragEnd)}.");
            Trace.Assert(!DragStarted, $"A {nameof(DraggedDrawable)} was already searched for. Call {nameof(HandleMouseDragEnd)} first.");

            Trace.Assert(MouseDownPosition != null);

            DragStarted = true;

            // also the laziness of IEnumerable here
            var drawables = ButtonDownInputQueue.Where(t => t.IsAlive && t.IsPresent);

            DraggedDrawable = PropagateEvent(drawables, new DragStartEvent(state, Button, MouseDownPosition));
            if (DraggedDrawable != null)
                DraggedDrawable.IsDragged = true;

            return DraggedDrawable != null;
        }

        protected virtual bool HandleMouseDragEnd(InputState state)
        {
            DragStarted = false;

            if (DraggedDrawable == null) return false;

            var result = PropagateEvent(new[] { DraggedDrawable }, new DragEndEvent(state, Button, MouseDownPosition)) != null;

            DraggedDrawable.IsDragged = false;
            DraggedDrawable = null;

            return result;
        }
    }
}
