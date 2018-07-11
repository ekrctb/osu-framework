// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using System.Collections.Generic;
using osu.Framework.Graphics;
using OpenTK;
using OpenTK.Input;
using System.Linq;
using System.Diagnostics;
using NUnit.Framework;
using osu.Framework.EventArgs;
using osu.Framework.Logging;

namespace osu.Framework.Input
{
    /// <summary>
    /// Manages state and events (click, drag and double-click) for a single mouse button.
    /// </summary>
    public abstract class MouseButtonEventManager
    {
        /// <summary>
        /// The mouse button this manager manages.
        /// </summary>
        public readonly MouseButton Button;

        /// <summary>
        /// Used for requesting focus from click.
        /// </summary>
        public Action<Drawable> RequestFocus;

        /// <summary>
        /// Used for get a positional input queue.
        /// </summary>
        public Func<IEnumerable<Drawable>> GetPositionalInputQueue;

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

        protected MouseButtonEventManager(MouseButton button)
        {
            Button = button;
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
        protected Drawable ClickedDrawable;

        /// <summary>
        /// Whether a drag operation has started and <see cref="DraggedDrawable"/> has been searched for.
        /// </summary>
        protected bool DragStarted;

        /// <summary>
        /// The positional input queue.
        /// </summary>
        protected IEnumerable<Drawable> PositionalInputQueue => GetPositionalInputQueue?.Invoke() ?? Enumerable.Empty<Drawable>();

        /// <summary>
        /// The input queue for propagating <see cref="Drawable.OnMouseUp"/>.
        /// This is created from the <see cref="PositionalInputQueue"/> when the last time the button is pressed.
        /// </summary>
        protected List<Drawable> MouseDownInputQueue;

        /// <summary>
        /// The <see cref="Drawable"/> which is currently being dragged. null if none is.
        /// </summary>
        public Drawable DraggedDrawable { get; protected set; }

        public virtual void HandlePositionChange(InputStateChangeArgs args)
        {
            if (EnableDrag)
            {
                if (!DragStarted)
                {
                    var mouse = args.State.Mouse;
                    if (mouse.IsPressed(Button) && Vector2Extensions.Distance(MouseDownPosition ?? mouse.Position, mouse.Position) > ClickDragDistance)
                        HandleMouseDragStart(args);
                }
                else
                {
                    HandleMouseDrag(args);
                }
            }
        }

        public virtual void HandleButtonStateChange(ButtonStateChangeArgs<MouseButton> args, double currentTime)
        {
            var mouse = args.State.Mouse;

            Trace.Assert(args.Button == Button);
            Trace.Assert(mouse.IsPressed(Button) == (args.Kind == ButtonStateChangeKind.Pressed));

            if (args.Kind == ButtonStateChangeKind.Pressed)
            {
                if (mouse.IsPositionValid)
                    MouseDownPosition = mouse.Position;
                HandleMouseDown(args);
            }
            else
            {
                HandleMouseUp(args);

                if (EnableClick && DraggedDrawable == null)
                {
                    bool isValidClick = true;
                    if (LastClickTime != null && currentTime - LastClickTime < DoubleClickTime)
                    {
                        if (HandleMouseDoubleClick(args))
                        {
                            //when we handle a double-click we want to block a normal click from firing.
                            isValidClick = false;
                            LastClickTime = null;
                        }
                    }

                    if (isValidClick)
                    {
                        LastClickTime = currentTime;
                        HandleMouseClick(args);
                    }
                }

                if (EnableDrag)
                    HandleMouseDragEnd(args);

                MouseDownPosition = null;
            }
        }

        protected virtual bool HandleMouseDown(ButtonStateChangeArgs<MouseButton> args)
        {
            var mousePosition = args.State.Mouse.Position;
            var eventArgs = new MouseDownEventArgs(args.State, Button)
            {
                ScreenMouseDownPosition = mousePosition
            };

            var positionalInputQueue = PositionalInputQueue.ToList();
            var result = PropagateMouseDown(positionalInputQueue, eventArgs, out Drawable handledBy);

            // only drawables up to the one that handled mouse down should handle mouse up
            MouseDownInputQueue = positionalInputQueue;
            if (result)
            {
                var count = MouseDownInputQueue.IndexOf(handledBy) + 1;
                MouseDownInputQueue.RemoveRange(count, MouseDownInputQueue.Count - count);
            }

            return result;
        }

        protected virtual bool HandleMouseUp(ButtonStateChangeArgs<MouseButton> args)
        {
            if (MouseDownInputQueue == null) return false;

            var eventArgs = new MouseUpEventArgs(args.State, Button)
            {
                ScreenMouseDownPosition = MouseDownPosition ?? throw new AssertionException($"{nameof(MouseDownPosition)} must be non-null at {nameof(HandleMouseUp)}")
            };

            return PropagateMouseUp(MouseDownInputQueue, eventArgs);
        }

        protected virtual bool HandleMouseClick(ButtonStateChangeArgs<MouseButton> args)
        {
            var mousePosition = args.State.Mouse.Position;
            var eventArgs = new ClickEventArgs(args.State, Button)
            {
                ScreenMouseDownPosition = mousePosition
            };

            var intersectingQueue = MouseDownInputQueue.Intersect(PositionalInputQueue);

            // click pass, triggering an OnClick on all drawables up to the first which returns true.
            // an extra IsHovered check is performed because we are using an outdated queue (for valid reasons which we need to document).
            ClickedDrawable = intersectingQueue.FirstOrDefault(t => t.CanReceiveMouseInput && t.ReceiveMouseInputAt(mousePosition) && t.TriggerOnClick(eventArgs));

            if (ChangeFocusOnClick)
                RequestFocus?.Invoke(ClickedDrawable);

            if (ClickedDrawable != null)
                Logger.Log($"MouseClick handled by {ClickedDrawable}.", LoggingTarget.Runtime, LogLevel.Debug);

            return ClickedDrawable != null;
        }

        protected virtual bool HandleMouseDoubleClick(ButtonStateChangeArgs<MouseButton> args)
        {
            if (ClickedDrawable == null) return false;

            var mousePosition = args.State.Mouse.Position;
            var eventArgs = new DoubleClickEventArgs(args.State, Button)
            {
                ScreenMouseDownPosition = mousePosition
            };

            return ClickedDrawable.ReceiveMouseInputAt(mousePosition) && ClickedDrawable.TriggerOnDoubleClick(eventArgs);
        }

        protected virtual bool HandleMouseDrag(InputStateChangeArgs args)
        {
            var eventArgs = new DragEventArgs(args.State, Button)
            {
                ScreenMouseDownPosition = MouseDownPosition ?? throw new AssertionException($"{nameof(MouseDownPosition)} must be non-null at {nameof(HandleMouseDrag)}")
            };

            //Once a drawable is dragged, it remains in a dragged state until the drag is finished.
            return DraggedDrawable?.TriggerOnDrag(eventArgs) ?? false;
        }

        protected virtual bool HandleMouseDragStart(InputStateChangeArgs args)
        {
            Trace.Assert(DraggedDrawable == null, $"The {nameof(DraggedDrawable)} was not set to null by {nameof(HandleMouseDragEnd)}.");
            Trace.Assert(!DragStarted, $"A {nameof(DraggedDrawable)} was already searched for. Call {nameof(HandleMouseDragEnd)} first.");

            var eventArgs = new DragStartEventArgs(args.State, Button)
            {
                ScreenMouseDownPosition = MouseDownPosition ?? throw new AssertionException($"{nameof(MouseDownPosition)} must be non-null at {nameof(HandleMouseDragStart)}")
            };

            DragStarted = true;

            DraggedDrawable = MouseDownInputQueue?.FirstOrDefault(target => target.IsAlive && target.IsPresent && target.TriggerOnDragStart(eventArgs));
            if (DraggedDrawable != null)
            {
                DraggedDrawable.IsDragged = true;
                Logger.Log($"MouseDragStart handled by {DraggedDrawable}.", LoggingTarget.Runtime, LogLevel.Debug);
            }

            return DraggedDrawable != null;
        }

        protected virtual bool HandleMouseDragEnd(InputStateChangeArgs args)
        {
            DragStarted = false;

            if (DraggedDrawable == null)
                return false;

            var eventArgs = new DragEndEventArgs(args.State, Button)
            {
                ScreenMouseDownPosition = MouseDownPosition ?? throw new AssertionException($"{nameof(MouseDownPosition)} must be non-null at {nameof(HandleMouseDragEnd)}")
            };

            bool result = DraggedDrawable.TriggerOnDragEnd(eventArgs);
            DraggedDrawable.IsDragged = false;
            DraggedDrawable = null;

            return result;
        }

        /// <summary>
        /// Triggers mouse down events on drawables in <paramref cref="drawables"/> until it is handled.
        /// </summary>
        /// <param name="drawables">The drawables in the queue.</param>
        /// <param name="args">The args.</param>
        /// <param name="handledBy"></param>
        /// <returns>Whether the mouse down event was handled.</returns>
        protected virtual bool PropagateMouseDown(IEnumerable<Drawable> drawables, MouseDownEventArgs args, out Drawable handledBy)
        {
            handledBy = drawables.FirstOrDefault(target => target.TriggerOnMouseDown(args));

            if (handledBy != null)
                Logger.Log($"MouseDown ({args.Button}) handled by {handledBy}.", LoggingTarget.Runtime, LogLevel.Debug);

            return handledBy != null;
        }

        /// <summary>
        /// Triggers mouse up events on drawables in <paramref cref="drawables"/> until it is handled.
        /// </summary>
        /// <param name="drawables">The drawables in the queue.</param>
        /// <param name="args">The args.</param>
        /// <returns>Whether the mouse up event was handled.</returns>
        protected virtual bool PropagateMouseUp(IEnumerable<Drawable> drawables, MouseUpEventArgs args)
        {
            var handledBy = drawables.FirstOrDefault(target => target.TriggerOnMouseUp(args));

            if (handledBy != null)
                Logger.Log($"MouseUp ({args.Button}) handled by {handledBy}.", LoggingTarget.Runtime, LogLevel.Debug);

            return handledBy != null;
        }
    }
}
