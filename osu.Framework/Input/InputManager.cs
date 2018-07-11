// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using System.Collections.Generic;
using System.Linq;
using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Input.EventArgs;
using osu.Framework.Input.Handlers;
using osu.Framework.Logging;
using osu.Framework.Platform;
using osu.Framework.Statistics;
using OpenTK;
using OpenTK.Input;
using JoystickButtonEventArgs = osu.Framework.Input.EventArgs.JoystickButtonEventArgs;
using MouseMoveEventArgs = osu.Framework.Input.EventArgs.MouseMoveEventArgs;

namespace osu.Framework.Input
{
    public abstract class InputManager : Container, IInputStateChangeHandler
    {
        /// <summary>
        /// The initial delay before key repeat begins.
        /// </summary>
        private const int repeat_initial_delay = 250;

        /// <summary>
        /// The delay between key repeats after the initial repeat.
        /// </summary>
        private const int repeat_tick_rate = 70;

        protected GameHost Host;

        internal Drawable FocusedDrawable;

        protected abstract IEnumerable<InputHandler> InputHandlers { get; }

        private double keyboardRepeatTime;
        private Key? keyboardRepeatKey;

        /// <summary>
        /// The initial input state. <see cref="CurrentState"/> is always equal (as a reference) to the value returned from this.
        /// <see cref="InputState.Mouse"/>, <see cref="InputState.Keyboard"/> and <see cref="InputState.Joystick"/> should be non-null.
        /// </summary>
        protected virtual InputState CreateInitialState() => new InputState
        {
            Mouse = new MouseState { IsPositionValid = false },
            Keyboard = new KeyboardState(),
            Joystick = new JoystickState(),
        };

        /// <summary>
        /// The last processed state.
        /// </summary>
        public readonly InputState CurrentState;

        /// <summary>
        /// The <see cref="Drawable"/> which is currently being dragged. null if none is.
        /// </summary>
        public Drawable DraggedDrawable
        {
            get
            {
                mouseButtonEventManagers.TryGetValue(MouseButton.Left, out var manager);
                return manager?.DraggedDrawable;
            }
        }

        /// <summary>
        /// Contains the previously hovered <see cref="Drawable"/>s prior to when
        /// <see cref="hoveredDrawables"/> got updated.
        /// </summary>
        private readonly List<Drawable> lastHoveredDrawables = new List<Drawable>();

        /// <summary>
        /// Contains all hovered <see cref="Drawable"/>s in top-down order up to the first
        /// which returned true in its <see cref="Drawable.OnHover"/> method.
        /// Top-down in this case means reverse draw order, i.e. the front-most visible
        /// <see cref="Drawable"/> first, and <see cref="Container"/>s after their children.
        /// </summary>
        private readonly List<Drawable> hoveredDrawables = new List<Drawable>();

        /// <summary>
        /// The <see cref="Drawable"/> which returned true in its
        /// <see cref="Drawable.OnHover"/> method, or null if none did so.
        /// </summary>
        private Drawable hoverHandledDrawable;

        /// <summary>
        /// Contains all hovered <see cref="Drawable"/>s in top-down order up to the first
        /// which returned true in its <see cref="Drawable.OnHover"/> method.
        /// Top-down in this case means reverse draw order, i.e. the front-most visible
        /// <see cref="Drawable"/> first, and <see cref="Container"/>s after their children.
        /// </summary>
        public IReadOnlyList<Drawable> HoveredDrawables => hoveredDrawables;

        /// <summary>
        /// Contains all <see cref="Drawable"/>s in top-down order which are considered
        /// for positional input. This list is the same as <see cref="HoveredDrawables"/>, only
        /// that the return value of <see cref="Drawable.OnHover"/> is not taken
        /// into account.
        /// </summary>
        public IEnumerable<Drawable> PositionalInputQueue => buildMouseInputQueue(CurrentState);

        /// <summary>
        /// Contains all <see cref="Drawable"/>s in top-down order which are considered
        /// for non-positional input.
        /// </summary>
        public IEnumerable<Drawable> InputQueue => buildInputQueue();

        private readonly Dictionary<MouseButton, MouseButtonEventManager> mouseButtonEventManagers = new Dictionary<MouseButton, MouseButtonEventManager>();

        protected InputManager()
        {
            CurrentState = CreateInitialState();
            RelativeSizeAxes = Axes.Both;

            foreach (var button in Enum.GetValues(typeof(MouseButton)).Cast<MouseButton>())
            {
                var manager = CreateButtonManagerFor(button);
                manager.RequestFocus = ChangeFocusFromClick;
                manager.GetPositionalInputQueue = () => PositionalInputQueue;
                mouseButtonEventManagers.Add(button, manager);
            }
        }

        /// <summary>
        /// Create a <see cref="MouseButtonEventManager"/> for a specified mouse button.
        /// </summary>
        /// <param name="button">The button to be handled by the returned manager.</param>
        /// <returns>The <see cref="MouseButtonEventManager"/>.</returns>
        protected virtual MouseButtonEventManager CreateButtonManagerFor(MouseButton button)
        {
            switch (button)
            {
                case MouseButton.Left:
                    return new MouseLeftButtonEventManager(button);
                default:
                    return new MouseMinorButtonEventManager(button);
            }
        }

        [BackgroundDependencyLoader(permitNulls: true)]
        private void load(GameHost host)
        {
            Host = host;
        }

        /// <summary>
        /// Reset current focused drawable to the top-most drawable which is <see cref="Drawable.RequestsFocus"/>.
        /// </summary>
        /// <param name="triggerSource">The source which triggered this event.</param>
        public void TriggerFocusContention(Drawable triggerSource)
        {
            if (FocusedDrawable == null) return;

            Logger.Log($"Focus contention triggered by {triggerSource}.");
            ChangeFocus(null);
        }

        /// <summary>
        /// Changes the currently-focused drawable. First checks that <see cref="potentialFocusTarget"/> is in a valid state to receive focus,
        /// then unfocuses the current <see cref="FocusedDrawable"/> and focuses <see cref="potentialFocusTarget"/>.
        /// <see cref="potentialFocusTarget"/> can be null to reset focus.
        /// If the given drawable is already focused, nothing happens and no events are fired.
        /// </summary>
        /// <param name="potentialFocusTarget">The drawable to become focused.</param>
        /// <returns>True if the given drawable is now focused (or focus is dropped in the case of a null target).</returns>
        public bool ChangeFocus(Drawable potentialFocusTarget) => ChangeFocus(potentialFocusTarget, new FocusEventArgs(CurrentState));

        /// <summary>
        /// Changes the currently-focused drawable. First checks that <see cref="potentialFocusTarget"/> is in a valid state to receive focus,
        /// then unfocuses the current <see cref="FocusedDrawable"/> and focuses <see cref="potentialFocusTarget"/>.
        /// <see cref="potentialFocusTarget"/> can be null to reset focus.
        /// If the given drawable is already focused, nothing happens and no events are fired.
        /// </summary>
        /// <param name="potentialFocusTarget">The drawable to become focused.</param>
        /// <param name="args">The event arguments.</param>
        /// <returns>True if the given drawable is now focused (or focus is dropped in the case of a null target).</returns>
        protected bool ChangeFocus(Drawable potentialFocusTarget, FocusEventArgs args)
        {
            if (potentialFocusTarget == FocusedDrawable)
                return true;

            if (potentialFocusTarget != null && (!potentialFocusTarget.IsPresent || !potentialFocusTarget.AcceptsFocus))
                return false;

            var previousFocus = FocusedDrawable;

            FocusedDrawable = null;

            if (previousFocus != null)
            {
                previousFocus.HasFocus = false;
                previousFocus.TriggerOnFocusLost(new FocusLostEventArgs(args.State));

                if (FocusedDrawable != null) throw new InvalidOperationException($"Focus cannot be changed inside {nameof(OnFocusLost)}");
            }

            FocusedDrawable = potentialFocusTarget;

            Logger.Log($"Focus switched to {FocusedDrawable?.ToString() ?? "nothing"}.", LoggingTarget.Runtime, LogLevel.Debug);

            if (FocusedDrawable != null)
            {
                FocusedDrawable.HasFocus = true;
                FocusedDrawable.TriggerOnFocus(args);
            }

            return true;
        }

        internal override bool BuildKeyboardInputQueue(List<Drawable> queue, bool allowBlocking = true)
        {
            if (!allowBlocking)
                base.BuildKeyboardInputQueue(queue, false);

            return false;
        }

        internal override bool BuildMouseInputQueue(Vector2 screenSpaceMousePos, List<Drawable> queue) => false;

        protected override void Update()
        {
            unfocusIfNoLongerValid();

            foreach (var result in GetPendingInputs())
            {
                result.Apply(CurrentState, this);
            }

            if (CurrentState.Mouse.IsPositionValid)
            {
                foreach (var d in PositionalInputQueue)
                    if (d is IRequireHighFrequencyMousePosition)
                        if (d.TriggerOnMouseMove(new MouseMoveEventArgs(CurrentState)))
                            break;
            }

            updateKeyRepeat(CurrentState);

            updateHoverEvents(CurrentState);

            if (FocusedDrawable == null)
                focusTopMostRequestingDrawable();

            base.Update();
        }

        private void updateKeyRepeat(InputState state)
        {
            if (!(keyboardRepeatKey is Key key)) return;

            keyboardRepeatTime -= Time.Elapsed;
            while (keyboardRepeatTime < 0)
            {
                handleKeyDown(state, key, true);
                keyboardRepeatTime += repeat_tick_rate;
            }
        }

        protected virtual List<IInput> GetPendingInputs()
        {
            var inputs = new List<IInput>();

            foreach (var h in InputHandlers)
            {
                var list = h.GetPendingInputs();
                if (h.IsActive && h.Enabled)
                    inputs.AddRange(list);
            }

            return inputs;
        }

        private IEnumerable<Drawable> buildInputQueue()
        {
            var inputQueue = new List<Drawable>();

            if (this is UserInputManager)
                FrameStatistics.Increment(StatisticsCounterType.KeyboardQueue);

            foreach (Drawable d in AliveInternalChildren)
                d.BuildKeyboardInputQueue(inputQueue);

            if (!unfocusIfNoLongerValid())
                inputQueue.Append(FocusedDrawable);

            // Keyboard and mouse queues were created in back-to-front order.
            // We want input to first reach front-most drawables, so the queues
            // need to be reversed.
            inputQueue.Reverse();

            return inputQueue;
        }

        private IEnumerable<Drawable> buildMouseInputQueue(InputState state)
        {
            var positionalInputQueue = new List<Drawable>();

            if (this is UserInputManager)
                FrameStatistics.Increment(StatisticsCounterType.MouseQueue);

            foreach (Drawable d in AliveInternalChildren)
                d.BuildMouseInputQueue(state.Mouse.Position, positionalInputQueue);

            positionalInputQueue.Reverse();
            return positionalInputQueue;
        }

        protected virtual bool HandleHoverEvents => true;

        private void updateHoverEvents(InputState state)
        {
            Drawable lastHoverHandledDrawable = hoverHandledDrawable;
            hoverHandledDrawable = null;

            lastHoveredDrawables.Clear();
            lastHoveredDrawables.AddRange(hoveredDrawables);
            hoveredDrawables.Clear();

            // New drawables shouldn't be hovered if the cursor isn't in the window
            if (HandleHoverEvents)
            {
                // First, we need to construct hoveredDrawables for the current frame
                foreach (Drawable d in PositionalInputQueue)
                {
                    hoveredDrawables.Add(d);

                    // Don't need to re-hover those that are already hovered
                    if (d.IsHovered)
                    {
                        // Check if this drawable previously handled hover, and assume it would once more
                        if (d == lastHoverHandledDrawable)
                        {
                            hoverHandledDrawable = lastHoverHandledDrawable;
                            break;
                        }

                        continue;
                    }

                    d.IsHovered = true;
                    if (d.TriggerOnHover(new HoverEventArgs(state)))
                    {
                        hoverHandledDrawable = d;
                        break;
                    }
                }
            }

            // Unhover all previously hovered drawables which are no longer hovered.
            foreach (Drawable d in lastHoveredDrawables.Except(hoveredDrawables))
            {
                d.IsHovered = false;
                d.TriggerOnHoverLost(new HoverLostEventArgs(state));
            }
        }

        private bool isModifierKey(Key k)
        {
            return k == Key.LControl || k == Key.RControl
                                     || k == Key.LAlt || k == Key.RAlt
                                     || k == Key.LShift || k == Key.RShift
                                     || k == Key.LWin || k == Key.RWin;
        }

        public virtual void HandleKeyboardKeyStateChange(ButtonStateChangeArgs<Key> args)
        {
            var key = args.Button;
            if (args.Kind == ButtonStateChangeKind.Pressed)
            {
                handleKeyDown(args.State, key, false);

                if (!isModifierKey(key))
                {
                    keyboardRepeatKey = key;
                    keyboardRepeatTime = repeat_initial_delay;
                }
            }
            else
            {
                handleKeyUp(args.State, key);

                keyboardRepeatKey = null;
                keyboardRepeatTime = 0;
            }
        }

        public virtual void HandleJoystickButtonStateChange(ButtonStateChangeArgs<JoystickButton> args)
        {
            if (args.Kind == ButtonStateChangeKind.Pressed)
            {
                handleJoystickPress(args);
            }
            else
            {
                handleJoystickRelease(args);
            }
        }

        public virtual void HandleMousePositionChange(MousePositionChangeArgs args)
        {
            var mouse = args.State.Mouse;

            foreach (var h in InputHandlers)
                if (h.Enabled && h is INeedsMousePositionFeedback handler)
                    handler.FeedbackMousePositionChange(mouse.Position);

            handleMouseMove(args);

            foreach (var manager in mouseButtonEventManagers.Values)
                manager.HandlePositionChange(args);
        }

        public virtual void HandleMouseScrollChange(MouseScrollChangeArgs args)
        {
            handleScroll(args);
        }

        public virtual void HandleMouseButtonStateChange(ButtonStateChangeArgs<MouseButton> args)
        {
            if (mouseButtonEventManagers.TryGetValue(args.Button, out var manager))
                manager.HandleButtonStateChange(args, Time.Current);
        }

        private bool handleMouseMove(MousePositionChangeArgs args)
        {
            var eventArgs = new MouseMoveEventArgs(args.State) { ScreenLastMousePosition = args.LastPosition };
            return PositionalInputQueue.Any(target => target.TriggerOnMouseMove(eventArgs));
        }

        private bool handleScroll(MouseScrollChangeArgs args)
        {
            var scrollDelta = args.State.Mouse.Scroll - args.LastScroll;
            var eventArgs = new ScrollEventArgs(args.State, scrollDelta) { IsPrecise = args.IsPrecise };
            return PropagateScroll(PositionalInputQueue, eventArgs);
        }

        /// <summary>
        /// Triggers scroll events on drawables in <paramref cref="drawables"/> until it is handled.
        /// </summary>
        /// <param name="drawables">The drawables in the queue.</param>
        /// <param name="args">The event args.</param>
        /// <returns></returns>
        protected virtual bool PropagateScroll(IEnumerable<Drawable> drawables, ScrollEventArgs args)
        {
            var handledBy = drawables.FirstOrDefault(target => target.TriggerOnScroll(args));

            if (handledBy != null)
                Logger.Log($"Scroll ({args.ScrollDelta.X:#,2},{args.ScrollDelta.Y:#,2}) handled by {handledBy}.", LoggingTarget.Runtime, LogLevel.Debug);

            return handledBy != null;
        }

        private bool handleKeyDown(InputState state, Key key, bool repeat)
        {
            var eventArgs = new KeyDownEventArgs(state, key) { Repeat = repeat };
            return PropagateKeyDown(InputQueue, eventArgs);
        }

        /// <summary>
        /// Triggers key down events on drawables in <paramref cref="drawables"/> until it is handled.
        /// </summary>
        /// <param name="drawables">The drawables in the queue.</param>
        /// <param name="args">The args.</param>
        /// <returns>Whether the key down event was handled.</returns>
        protected virtual bool PropagateKeyDown(IEnumerable<Drawable> drawables, KeyDownEventArgs args)
        {
            var handledBy = drawables.FirstOrDefault(target => target.TriggerOnKeyDown(args));

            if (handledBy != null)
                Logger.Log($"KeyDown ({args.Key}) handled by {handledBy}.", LoggingTarget.Runtime, LogLevel.Debug);

            return handledBy != null;
        }

        private bool handleKeyUp(InputState state, Key key)
        {
            IEnumerable<Drawable> queue = InputQueue;
            if (!unfocusIfNoLongerValid())
                queue = queue.Prepend(FocusedDrawable);

            var eventArgs = new KeyUpEventArgs(state, key);
            return PropagateKeyUp(queue, eventArgs);
        }

        /// <summary>
        /// Triggers key up events on drawables in <paramref cref="drawables"/> until it is handled.
        /// </summary>
        /// <param name="drawables">The drawables in the queue.</param>
        /// <param name="args">The args.</param>
        /// <returns>Whether the key up event was handled.</returns>
        protected virtual bool PropagateKeyUp(IEnumerable<Drawable> drawables, KeyUpEventArgs args)
        {
            var handledBy = drawables.FirstOrDefault(target => target.TriggerOnKeyUp(args));

            if (handledBy != null)
                Logger.Log($"KeyUp ({args.Key}) handled by {handledBy}.", LoggingTarget.Runtime, LogLevel.Debug);

            return handledBy != null;
        }

        private bool handleJoystickPress(ButtonStateChangeArgs<JoystickButton> args)
        {
            IEnumerable<Drawable> queue = InputQueue;
            if (!unfocusIfNoLongerValid())
                queue = queue.Prepend(FocusedDrawable);

            var eventArgs = new JoystickPressEventArgs(args.State, args.Button);
            return PropagateJoystickPress(queue, eventArgs);
        }

        protected virtual bool PropagateJoystickPress(IEnumerable<Drawable> drawables, JoystickPressEventArgs args)
        {
            var handledBy = drawables.FirstOrDefault(target => target.TriggerOnJoystickPress(args));

            if (handledBy != null)
                Logger.Log($"JoystickPress ({args.Button}) handled by {handledBy}.", LoggingTarget.Runtime, LogLevel.Debug);

            return handledBy != null;
        }

        private bool handleJoystickRelease(ButtonStateChangeArgs<JoystickButton> args)
        {
            IEnumerable<Drawable> queue = InputQueue;
            if (!unfocusIfNoLongerValid())
                queue = queue.Prepend(FocusedDrawable);

            var eventArgs = new JoystickReleaseEventArgs(args.State, args.Button);
            return PropagateJoystickRelease(queue, eventArgs);
        }

        protected virtual bool PropagateJoystickRelease(IEnumerable<Drawable> drawables, JoystickReleaseEventArgs args)
        {
            var handledBy = drawables.FirstOrDefault(target => target.TriggerOnJoystickRelease(args));

            if (handledBy != null)
                Logger.Log($"JoystickRelease ({args.Button}) handled by {handledBy}.", LoggingTarget.Runtime, LogLevel.Debug);

            return handledBy != null;
        }

        /// <summary>
        /// Unfocus the current focused drawable if it is no longer in a valid state.
        /// </summary>
        /// <returns>true if there is no longer a focus.</returns>
        private bool unfocusIfNoLongerValid()
        {
            if (FocusedDrawable == null) return true;

            bool stillValid = FocusedDrawable.IsPresent && FocusedDrawable.Parent != null;

            if (stillValid)
            {
                //ensure we are visible
                CompositeDrawable d = FocusedDrawable.Parent;
                while (d != null)
                {
                    if (!d.IsPresent)
                    {
                        stillValid = false;
                        break;
                    }

                    d = d.Parent;
                }
            }

            if (stillValid)
                return false;

            ChangeFocus(null);
            return true;
        }

        protected virtual void ChangeFocusFromClick(Drawable clickedDrawable)
        {
            Drawable focusTarget = null;

            if (clickedDrawable != null)
            {
                focusTarget = clickedDrawable;

                if (!focusTarget.AcceptsFocus)
                {
                    // search upwards from the clicked drawable until we find something to handle focus.
                    Drawable previousFocused = FocusedDrawable;

                    while (focusTarget?.AcceptsFocus == false)
                        focusTarget = focusTarget.Parent;

                    if (focusTarget != null && previousFocused != null)
                    {
                        // we found a focusable target above us.
                        // now search upwards from previousFocused to check whether focusTarget is a common parent.
                        Drawable search = previousFocused;
                        while (search != null && search != focusTarget)
                            search = search.Parent;

                        if (focusTarget == search)
                            // we have a common parent, so let's keep focus on the previously focused target.
                            focusTarget = previousFocused;
                    }
                }
            }

            ChangeFocus(focusTarget);
        }

        private void focusTopMostRequestingDrawable()
        {
            // todo: don't rebuild input queue every frame
            ChangeFocus(InputQueue.FirstOrDefault(target => target.RequestsFocus));
        }

        private class MouseLeftButtonEventManager : MouseButtonEventManager
        {
            public MouseLeftButtonEventManager(MouseButton button)
                : base(button)
            {
            }

            public override bool EnableDrag => true;

            public override bool EnableClick => true;

            public override bool ChangeFocusOnClick => true;
        }

        private class MouseMinorButtonEventManager : MouseButtonEventManager
        {
            public MouseMinorButtonEventManager(MouseButton button)
                : base(button)
            {
            }

            public override bool EnableDrag => false;

            public override bool EnableClick => false;

            public override bool ChangeFocusOnClick => false;
        }
    }
}
