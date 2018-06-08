﻿// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System.Collections.Generic;
using NUnit.Framework;
using osu.Framework.Caching;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Colour;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using OpenTK;
using OpenTK.Graphics;

namespace osu.Framework.Tests.Layout.ContainerTests
{
    [TestFixture]
    public class ContainerInvalidationTest
    {
        [Test]
        public void TestFixedSizeContainerDoesNotInvalidateSizeDependenciesChildIsAdded()
        {
            var container = new LoadedContainer { Child = new LoadedBox() };
            Assert.IsTrue(container.ChildrenSizeDependencies.IsValid, "container should not have been invalidated");
        }

        [Test]
        public void TestFixedSizeContainerDoesNotSizeDependenciesWhenChildIsRemoved()
        {
            LoadedBox child;
            // ReSharper disable once CollectionNeverQueried.Local : Keeping a local reference
            var container = new LoadedContainer { Child = child = new LoadedBox() };

            container.Remove(child);
            Assert.IsTrue(container.ChildrenSizeDependencies.IsValid, "container should not have been invalidated");
        }

        [Test]
        public void TestAutoSizingContainerInvalidatesSizeDependenciesWhenChildIsAdded()
        {
            var container = new LoadedContainer
            {
                AutoSizeAxes = Axes.Both,
                Child = new LoadedBox()
            };

            Assert.IsFalse(container.ChildrenSizeDependencies.IsValid, "container should have been invalidated");
        }

        [Test]
        public void TestAutoSizingContainerInvalidatesSizeDependenciesWhenChildIsRemoved()
        {
            LoadedBox box;
            // ReSharper disable once CollectionNeverQueried.Local : Keeping a local reference
            var container = new LoadedContainer
            {
                AutoSizeAxes = Axes.Both,
                Child = box = new LoadedBox()
            };

            container.ValidateChildrenSizeDependencies();
            container.Remove(box);
            Assert.IsFalse(container.ChildrenSizeDependencies.IsValid, "container should have been invalidated");
        }

        [Test]
        public void TestAutoSizingContainerInvalidatesSizeDependenciesWhenChildInvalidatesAll()
        {
            LoadedBox child;
            var container = new LoadedContainer
            {
                AutoSizeAxes = Axes.Both,
                Child = child = new LoadedBox()
            };

            container.ValidateChildrenSizeDependencies();
            child.Invalidate();
            Assert.IsFalse(container.ChildrenSizeDependencies.IsValid, "container should have been invalidated");
        }

        private static readonly object[] child_property_cases =
        {
            new object[] { nameof(Drawable.Origin), Anchor.Centre, null },
            new object[] { nameof(Drawable.Anchor), Anchor.Centre, null },
            new object[] { nameof(Drawable.Position), Vector2.One, null },
            new object[] { nameof(Drawable.RelativePositionAxes), Axes.Both, null },
            new object[] { nameof(Drawable.X), 0.5f, null },
            new object[] { nameof(Drawable.Y), 0.5f, null },
            new object[] { nameof(Drawable.RelativeSizeAxes), Axes.Both, null },
            new object[] { nameof(Drawable.Size), Vector2.One, null },
            new object[] { nameof(Drawable.Width), 0.5f, null },
            new object[] { nameof(Drawable.Height), 0.5f, null },
            new object[] { nameof(Drawable.FillMode), FillMode.Fit, null },
            new object[] { nameof(Drawable.FillAspectRatio), 0.5f, new[]
            {
                new KeyValuePair<string, object>(nameof(Drawable.RelativeSizeAxes), Axes.Both),
                new KeyValuePair<string, object>(nameof(Drawable.FillMode), FillMode.Fit)
            }},
            new object[] { nameof(Drawable.FillAspectRatio), 0.5f, new[]
            {
                new KeyValuePair<string, object>(nameof(Drawable.RelativeSizeAxes), Axes.Both),
                new KeyValuePair<string, object>(nameof(Drawable.FillMode), FillMode.Fill)
            }},
            new object[] { nameof(Drawable.Scale), new Vector2(2), null },
            new object[] { nameof(Drawable.Margin), new MarginPadding(2), null },
            new object[] { nameof(Drawable.Colour), (ColourInfo)Color4.Black, null },
            new object[] { nameof(Drawable.Rotation), 0.5f, null },
            new object[] { nameof(Drawable.Shear), Vector2.One, null },
        };

        [TestCaseSource(nameof(child_property_cases))]
        public void TestAutoSizingContainerInvalidatesSizeDependenciesWhenChildPropertyChanges(string propertyName, object newValue, params KeyValuePair<string, object>[] unobservedProperties)
        {
            LoadedBox child;
            var container = new LoadedContainer
            {
                AutoSizeAxes = Axes.Both,
                Child = child = new LoadedBox { Alpha = 0 }
            };

            if (unobservedProperties != null)
                foreach (var kvp in unobservedProperties)
                    child.Set(kvp.Key, kvp.Value);

            container.ValidateChildrenSizeDependencies();
            child.Set(propertyName, newValue);
            Assert.IsFalse(container.ChildrenSizeDependencies.IsValid, "container should have been invalidated");
        }

        [Test]
        public void TestInvalidatingChildInNestedAutoSizeContainerOnlyInvalidatesParentSizeDependencies()
        {
            LoadedBox child;
            LoadedContainer innerContainer;
            var outerContainer = new LoadedContainer
            {
                AutoSizeAxes = Axes.Both,
                Child = innerContainer = new LoadedContainer
                {
                    AutoSizeAxes = Axes.Both,
                    Child = child = new LoadedBox()
                }
            };

            outerContainer.ValidateChildrenSizeDependencies();
            innerContainer.ValidateChildrenSizeDependencies();
            child.Width = 10;
            Assert.IsTrue(outerContainer.ChildrenSizeDependencies.IsValid, "invalidation should not propagate to outer container");
            Assert.IsFalse(innerContainer.ChildrenSizeDependencies.IsValid, "inner container should have been invalidated");
        }

        private class LoadedContainer : Container
        {
            public Cached ChildrenSizeDependencies => this.Get<Cached>("childrenSizeDependencies");
            public void ValidateChildrenSizeDependencies() => this.Validate("childrenSizeDependencies");
            public void InvalidateChildrenSizeDependencies() => this.Invalidate("childrenSizeDependencies");

            public LoadedContainer()
            {
                // These tests are running without a gamehost, but we need to fake ourselves to be loaded
                this.Set("loadState", LoadState.Loaded);
            }
        }

        private class LoadedBox : Box
        {
            public LoadedBox()
            {
                // These tests are running without a gamehost, but we need to fake ourselves to be loaded
                this.Set("loadState", LoadState.Loaded);
            }
        }
    }
}
