// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Lists;
using osu.Framework.Timing;
using System.Linq;
using osu.Framework.Allocation;
using System.Collections.Generic;
using System.Diagnostics;
using osu.Framework.MathUtils;

namespace osu.Framework.Graphics.Transforms
{
    /// <summary>
    /// A type of object which can have <see cref="Transform"/>s operating upon it.
    /// An implementer of this class must call <see cref="UpdateTransforms"/> to
    /// update and apply its <see cref="Transform"/>s.
    /// </summary>
    public class TransformState
    {
        public IFrameBasedClock Clock;

        private double currentTime => Clock?.CurrentTime ?? 0;

        /// <summary>
        /// The starting time to use for new <see cref="Transform"/>s.
        /// </summary>
        public double TransformStartTime => currentTime + TransformDelay;

        /// <summary>
        /// Delay from the current time until new <see cref="Transform"/>s are started, in milliseconds.
        /// </summary>
        public double TransformDelay { get; private set; }

        private readonly Lazy<SortedList<Transform>> transformsLazy = new Lazy<SortedList<Transform>>(() => new SortedList<Transform>(Transform.COMPARER));

        /// <summary>
        /// A lazily-initialized list of <see cref="Transform"/>s applied to this object.
        /// </summary>
        public IReadOnlyList<Transform> Transforms => transformsLazy.IsValueCreated ? (IReadOnlyList<Transform>)transformsLazy.Value : Array.Empty<Transform>();

        /// <summary>
        /// The end time in milliseconds of the latest transform enqueued for this <see cref="TransformState"/>.
        /// Will return the current time value if no transforms are present.
        /// </summary>
        public double LatestTransformEndTime
        {
            get
            {
                //expiry should happen either at the end of the last transform or using the current sequence delay (whichever is highest).
                double max = TransformStartTime;
                foreach (Transform t in Transforms)
                    if (t.EndTime > max)
                        max = t.EndTime + 1; //adding 1ms here ensures we can expire on the current frame without issue.

                return max;
            }
        }

        /// <summary>
        /// Resets <see cref="TransformDelay"/> and processes updates to this class based on loaded <see cref="Transform"/>s.
        /// </summary>
        public void UpdateTransforms(bool removeCompletedTransforms)
        {
            TransformDelay = 0;
            updateTransforms(currentTime, removeCompletedTransforms);
        }

        private readonly Lazy<List<Action>> removalActions = new Lazy<List<Action>>(() => new List<Action>());

        private double lastUpdateTransformsTime;

        /// <summary>
        /// Process updates to this class based on loaded <see cref="Transform"/>s. This does not reset <see cref="TransformDelay"/>.
        /// This is used for performing extra updates on <see cref="Transform"/>s when new <see cref="Transform"/>s are added.
        /// </summary>
        private void updateTransforms(double time, bool removeCompletedTransforms)
        {
            bool rewinding = lastUpdateTransformsTime > time;
            lastUpdateTransformsTime = time;

            if (!transformsLazy.IsValueCreated)
                return;

            var transforms = transformsLazy.Value;

            if (rewinding && !removeCompletedTransforms)
            {
                var appliedToEndReverts = new List<string>();

                // Under the case that completed transforms are not removed, reversing the clock is permitted.
                // We need to first look back through all the transforms and apply the start values of the ones that were previously
                // applied, but now exist in the future relative to the current time.
                for (int i = transforms.Count - 1; i >= 0; i--)
                {
                    var t = transforms[i];

                    // rewind logic needs to only run on transforms which have been applied at least once.
                    if (!t.Applied)
                        continue;

                    // some specific transforms can be marked as non-rewindable.
                    if (!t.Rewindable)
                        continue;

                    if (time >= t.StartTime)
                    {
                        // we are in the middle of this transform, so we want to mark as not-completely-applied.
                        // note that we should only do this for the last transform of each TargetMemeber to avoid incorrect application order.
                        // the actual application will be in the main loop below now that AppliedToEnd is false.
                        if (!appliedToEndReverts.Contains(t.TargetMember))
                        {
                            if (time < t.EndTime)
                                t.AppliedToEnd = false;
                            appliedToEndReverts.Add(t.TargetMember);
                        }
                    }
                    else
                    {
                        // we are before the start time of this transform, so we want to eagerly apply the value at current time and mark as not-yet-applied.
                        // this transform will not be applied again unless we play forward in the future.
                        t.Apply(time);
                        t.Applied = false;
                        t.AppliedToEnd = false;
                    }
                }
            }

            for (int i = 0; i < transforms.Count; ++i)
            {
                var t = transforms[i];

                var tCanRewind = !removeCompletedTransforms && t.Rewindable;

                if (time < t.StartTime)
                    break;

                if (!t.Applied)
                {
                    // This is the first time we are updating this transform.
                    // We will find other still active transforms which act on the same target member and remove them.
                    // Since following transforms acting on the same target member are immediately removed when a
                    // new one is added, we can be sure that previous transforms were added before this one and can
                    // be safely removed.
                    for (int j = 0; j < i; ++j)
                    {
                        var u = transforms[j];
                        if (u.TargetMember != t.TargetMember) continue;

                        if (!u.AppliedToEnd)
                            // we may have applied the existing transforms too far into the future.
                            // we want to prepare to potentially read into the newly activated transform's StartTime,
                            // so we should re-apply using its StartTime as a basis.
                            u.Apply(t.StartTime);

                        if (!tCanRewind)
                        {
                            transforms.RemoveAt(j--);
                            i--;

                            if (u.OnAbort != null)
                                removalActions.Value.Add(u.OnAbort);
                        }
                        else
                            u.AppliedToEnd = true;
                    }
                }

                if (!t.HasStartValue)
                {
                    t.ReadIntoStartValue();
                    t.HasStartValue = true;
                }

                if (!t.AppliedToEnd)
                {
                    t.Apply(time);

                    t.AppliedToEnd = time >= t.EndTime;

                    if (t.AppliedToEnd)
                    {
                        if (!tCanRewind)
                            transforms.RemoveAt(i--);

                        if (t.IsLooping)
                        {
                            if (tCanRewind)
                            {
                                t.IsLooping = false;
                                t = t.Clone();
                            }

                            t.AppliedToEnd = false;
                            t.Applied = false;
                            t.HasStartValue = false;

                            t.IsLooping = true;

                            t.StartTime += t.LoopDelay;
                            t.EndTime += t.LoopDelay;

                            // this could be added back at a lower index than where we are currently iterating, but
                            // running the same transform twice isn't a huge deal.
                            transforms.Add(t);
                        }
                        else if (t.OnComplete != null)
                            removalActions.Value.Add(t.OnComplete);
                    }
                }
            }

            invokePendingRemovalActions();
        }

        private void invokePendingRemovalActions()
        {
            if (removalActions.IsValueCreated && removalActions.Value.Count > 0)
            {
                var toRemove = removalActions.Value.ToArray();
                removalActions.Value.Clear();

                foreach (var action in toRemove)
                    action();
            }
        }

        /// <summary>
        /// Removes a <see cref="Transform"/>.
        /// </summary>
        /// <param name="toRemove">The <see cref="Transform"/> to remove.</param>
        public void RemoveTransform(Transform toRemove)
        {
            if (!transformsLazy.IsValueCreated || !transformsLazy.Value.Remove(toRemove))
                return;

            toRemove.OnAbort?.Invoke();
        }

        /// <summary>
        /// Clears <see cref="Transform"/>s.
        /// </summary>
        /// <param name="propagateChildren">Whether we also clear the <see cref="Transform"/>s of children.</param>
        /// <param name="targetMember">
        /// An optional <see cref="Transform.TargetMember"/> name of <see cref="Transform"/>s to clear.
        /// Null for clearing all <see cref="Transform"/>s.
        /// </param>
        public void ClearTransforms(bool propagateChildren = false, string targetMember = null) => ClearTransformsAfter(double.NegativeInfinity, propagateChildren, targetMember);

        /// <summary>
        /// Removes <see cref="Transform"/>s that start after <paramref name="time"/>.
        /// </summary>
        /// <param name="time">The time to clear <see cref="Transform"/>s after.</param>
        /// <param name="propagateChildren">Whether to also clear such <see cref="Transform"/>s of children.</param>
        /// <param name="targetMember">
        /// An optional <see cref="Transform.TargetMember"/> name of <see cref="Transform"/>s to clear.
        /// Null for clearing all <see cref="Transform"/>s.
        /// </param>
        public void ClearTransformsAfter(double time, bool propagateChildren = false, string targetMember = null)
        {
            if (!transformsLazy.IsValueCreated)
                return;

            Transform[] toAbort;
            if (targetMember == null)
            {
                toAbort = transformsLazy.Value.Where(t => t.StartTime >= time).ToArray();
                transformsLazy.Value.RemoveAll(t => t.StartTime >= time);
            }
            else
            {
                toAbort = transformsLazy.Value.Where(t => t.StartTime >= time && t.TargetMember == targetMember).ToArray();
                transformsLazy.Value.RemoveAll(t => t.StartTime >= time && t.TargetMember == targetMember);
            }

            foreach (var t in toAbort)
                t.OnAbort?.Invoke();
        }

        public void ApplyTransformsAt(double time, bool removeCompletedTransforms)
        {
            updateTransforms(time, removeCompletedTransforms);
        }

        /// <summary>
        /// Finishes specified <see cref="Transform"/>s, using their <see cref="Transform{TValue}.EndValue"/>.
        /// </summary>
        /// <param name="propagateChildren">Whether we also finish the <see cref="Transform"/>s of children.</param>
        /// <param name="targetMember">
        /// An optional <see cref="Transform.TargetMember"/> name of <see cref="Transform"/>s to finish.
        /// Null for finishing all <see cref="Transform"/>s.
        /// </param>
        public void FinishTransforms(bool propagateChildren = false, string targetMember = null)
        {
            if (!transformsLazy.IsValueCreated)
                return;

            Func<Transform, bool> toFlushPredicate;
            if (targetMember == null)
                toFlushPredicate = t => !t.IsLooping;
            else
                toFlushPredicate = t => !t.IsLooping && t.TargetMember == targetMember;

            // Flush is undefined for endlessly looping transforms
            var toFlush = transformsLazy.Value.Where(toFlushPredicate).ToArray();

            transformsLazy.Value.RemoveAll(t => toFlushPredicate(t));

            foreach (Transform t in toFlush)
            {
                t.Apply(t.EndTime);
                t.OnComplete?.Invoke();
            }
        }

        /// <summary>
        /// Add a delay duration to <see cref="TransformDelay"/>, in milliseconds.
        /// </summary>
        /// <param name="duration">The delay duration to add.</param>
        /// <param name="propagateChildren">Whether we also delay down the child tree.</param>
        /// <returns>This</returns>
        internal void AddDelay(double duration, bool propagateChildren = false) => TransformDelay += duration;

        /// <summary>
        /// Start a sequence of <see cref="Transform"/>s with a (cumulative) relative delay applied.
        /// </summary>
        /// <param name="delay">The offset in milliseconds from current time. Note that this stacks with other nested sequences.</param>
        /// <param name="recursive">Whether this should be applied to all children.</param>
        /// <returns>A <see cref="InvokeOnDisposal"/> to be used in a using() statement.</returns>
        public InvokeOnDisposal BeginDelayedSequence(double delay, bool recursive = false)
        {
            if (delay == 0)
                return null;

            AddDelay(delay, recursive);
            double newTransformDelay = TransformDelay;

            return new InvokeOnDisposal(() =>
            {
                if (!Precision.AlmostEquals(newTransformDelay, TransformDelay))
                    throw new InvalidOperationException(
                        $"{nameof(TransformStartTime)} at the end of delayed sequence is not the same as at the beginning, but should be. " +
                        $"(begin={newTransformDelay} end={TransformDelay})");

                AddDelay(-delay, recursive);
            });
        }

        /// <summary>
        /// Start a sequence of <see cref="Transform"/>s from an absolute time value (adjusts <see cref="TransformStartTime"/>).
        /// </summary>
        /// <param name="newTransformStartTime">The new value for <see cref="TransformStartTime"/>.</param>
        /// <param name="recursive">Whether this should be applied to all children.</param>
        /// <returns>A <see cref="InvokeOnDisposal"/> to be used in a using() statement.</returns>
        /// <exception cref="InvalidOperationException">Absolute sequences should never be nested inside another existing sequence.</exception>
        public InvokeOnDisposal BeginAbsoluteSequence(double newTransformStartTime, bool recursive = false)
        {
            double oldTransformDelay = TransformDelay;
            double newTransformDelay = TransformDelay = newTransformStartTime - (Clock?.CurrentTime ?? 0);

            return new InvokeOnDisposal(() =>
            {
                if (!Precision.AlmostEquals(newTransformDelay, TransformDelay))
                    throw new InvalidOperationException(
                        $"{nameof(TransformStartTime)} at the end of absolute sequence is not the same as at the beginning, but should be. " +
                        $"(begin={newTransformDelay} end={TransformDelay})");

                TransformDelay = oldTransformDelay;
            });
        }

        /// <summary>
        /// Used to assign a monotonically increasing ID to <see cref="Transform"/>s as they are added. This member is
        /// incremented whenever a <see cref="Transform"/> is added.
        /// </summary>
        private ulong currentTransformID;

        /// <summary>
        /// Adds to this object a <see cref="Transform"/> which was previously populated using this object via
        /// <see cref="TransformableExtensions.PopulateTransform{TValue, TThis}(TThis, Transform{TValue, TThis}, TValue, double, Easing)"/>.
        /// Added <see cref="Transform"/>s are immediately applied, and therefore have an immediate effect on this object if the current time of this
        /// object falls within <see cref="Transform.StartTime"/> and <see cref="Transform.EndTime"/>.
        /// If <see cref="Clock"/> is null, e.g. because this object has just been constructed, then the given transform will be finished instantaneously.
        /// </summary>
        /// <param name="transform">The <see cref="Transform"/> to be added.</param>
        /// <param name="customTransformID">When not null, the <see cref="Transform.TransformID"/> to assign for ordering.</param>
        public void AddTransform(Transform transform, ulong? customTransformID = null)
        {
            var transforms = transformsLazy.Value;

            Debug.Assert(!(transform.TransformID == 0 && transforms.Contains(transform)), $"Zero-id {nameof(Transform)}s should never be contained already.");

            // This contains check may be optimized away in the future, should it become a bottleneck
            if (transform.TransformID != 0 && transforms.Contains(transform))
                throw new InvalidOperationException($"{nameof(TransformState)} may not contain the same {nameof(Transform)} more than once.");

            transform.TransformID = customTransformID ?? ++currentTransformID;
            int insertionIndex = transforms.Add(transform);

            // Remove all existing following transforms touching the same property as this one.
            for (int i = insertionIndex + 1; i < transforms.Count; ++i)
            {
                var t = transforms[i];
                if (t.TargetMember == transform.TargetMember)
                {
                    transforms.RemoveAt(i--);
                    if (t.OnAbort != null)
                        removalActions.Value.Add(t.OnAbort);
                }
            }

            invokePendingRemovalActions();
        }
    }
}
