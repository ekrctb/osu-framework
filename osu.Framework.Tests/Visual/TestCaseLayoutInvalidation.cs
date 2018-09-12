﻿// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using System.Linq;
using NUnit.Framework;
using osu.Framework.Extensions.Color4Extensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Testing;
using OpenTK;
using OpenTK.Graphics;

namespace osu.Framework.Tests.Visual
{
    public class TestCaseLayoutInvalidation : GridTestCase
    {
        private readonly Container<DrawQuadOverlayBox> overlayBoxContainer;

        public TestCaseLayoutInvalidation()
            : base(1, 2)
        {
            Add(overlayBoxContainer = new Container<DrawQuadOverlayBox>());
            addTest<AutoSize1>();
            addTest<AutoSize2>();
            addTest<AutoSize3>();
            addTest<PaddingChange1>();
            addTest<PaddingChange2>();
            addTest<PaddingChange3>();
            addTest<FillModeChange>();
            addTest<OriginChange>();
            addTest<AutoSizeWithRotation1>();
            addTest<AutoSizeWithRotation2>();
            addTest<AutoSizeWithRotation3>();
            addTest<AutoSizeWithShear>();
            addTest<AutoSizeExtras1>();
            addTest<AutoSizeExtras2>();
            addTest<AutoSizeExtras3>();
            addTest<AutoSizeExtras4>();
            addTest<AutoSizeExtras5>();
            addTest<FillFlow1>();
            addTest<FillFlow2>();
            addTest<FillFlowAnchorChange>();
            addTest<FillFlowOriginChange>();
        }

        private void addTest<T>() where T : LayoutInvalidationTest, new()
        {
            var name = typeof(T).Name;
            T instance1 = null, instance2 = null;
            AddStep($"{name} init", () =>
            {
                instance1 = new T();
                instance2 = new T();
                overlayBoxContainer.Clear();
                for (int i = 0; i < 2; i++)
                {
                    var instance = i == 0 ? instance1 : instance2;
                    instance.Container.Name = i == 0 ? "WithoutInvalidation" : "WithInvalidation";
                    Cell(i).Child = instance.Container;
                    for (int j = 0; j < instance.Drawables.Length; j++)
                        overlayBoxContainer.Add(new DrawQuadOverlayBox(instance.Drawables[j]) { Colour = RandomColorPalette.Get(j).Opacity(.5f) });
                }
            });
            AddStep($"{name} update", () =>
            {
                instance1.DoModification();
                instance2.DoModification();
                foreach (var d in instance2.Drawables)
                    d.PropagateInvalidateAll();
            });
            AddStep($"{name} check", () =>
            {
                var state1 = instance1.GetRoundedDrawVectors();
                var state2 = instance2.GetRoundedDrawVectors();
                Assert.AreEqual(state2, state1, $"{name}: Same {nameof(DrawPosition)} and {nameof(DrawSize)}s");
            });
        }

        public static class RandomColorPalette
        {
            public const double GOLDEN_RATIO = 1.6180339887498948482;

            public static Color4 Get(int index) =>
                Color4.FromHsv(new Vector4((float)(index * GOLDEN_RATIO % 1), 1, 1, .5f));
        }

        public abstract class LayoutInvalidationTest
        {
            public Container Container;
            public abstract Drawable[] Drawables { get; }

            protected LayoutInvalidationTest()
            {
                Container = new Container
                {
                    Anchor = Anchor.Centre,
                    Scale = new Vector2(100)
                };
            }

            public Vector2[] GetRoundedDrawVectors() => Drawables.SelectMany(d => new[] { d.DrawPosition, d.DrawSize }).Select(v => new Vector2(round(v.X), round(v.Y))).ToArray();
            private static float round(float x) => (float)Math.Round(x, 2);

            public abstract void DoModification();
        }

        public abstract class Size2Case : LayoutInvalidationTest
        {
            protected readonly Container Root, Child;
            public override Drawable[] Drawables => new Drawable[] { Root, Child };

            protected Size2Case()
            {
                Container.Child = Root = new Container
                {
                    Name = "Root",
                    Size = new Vector2(1),
                    Child = Child = new Container { Name = "Child", Size = new Vector2(1) },
                };
            }
        }

        public abstract class Size3Case1 : LayoutInvalidationTest
        {
            protected readonly Container Root, Child, GrandChild;
            public override Drawable[] Drawables => new Drawable[] { Root, Child, GrandChild };

            protected Size3Case1()
            {
                Container.Child = Root = new Container
                {
                    Name = "Root",
                    Size = new Vector2(1),
                    Child = Child = new Container
                    {
                        Name = "Child",
                        Size = new Vector2(1),
                        Child = GrandChild = new Container { Name = "GrandChild", Size = new Vector2(1) }
                    },
                };
            }
        }


        public abstract class Size3Case2 : LayoutInvalidationTest
        {
            protected readonly Container Root, Child1, Child2;
            public override Drawable[] Drawables => new Drawable[] { Root, Child1, Child2 };

            protected Size3Case2()
            {
                Container.Child = Root = new Container
                {
                    Name = "Root",
                    Size = new Vector2(1),
                    Children = new[]
                    {
                        Child1 = new Container { Name = "Child1", Size = new Vector2(1) },
                        Child2 = new Container { Name = "Child2", Size = new Vector2(1) }
                    },
                };
            }
        }

        public class AutoSize1 : Size3Case2
        {
            public AutoSize1()
            {
                Root.AutoSizeAxes = Axes.Y;
                Child2.Origin = Anchor.BottomCentre;
            }

            public override void DoModification() => Child1.RelativePositionAxes = Axes.Y;
        }

        public class AutoSize2 : Size3Case2
        {
            public AutoSize2()
            {
                Root.AutoSizeAxes = Axes.X;
                Child1.Anchor = Anchor.TopRight;
                Child2.Anchor = Anchor.TopRight;
                Child2.X = 1;
            }

            public override void DoModification() => Child2.RelativePositionAxes = Axes.X;
        }

        public class AutoSize3 : Size3Case2
        {
            public AutoSize3()
            {
                Root.AutoSizeAxes = Axes.X;
                Child1.Anchor = Anchor.TopRight;
                Child2.Anchor = Anchor.TopRight;
            }

            public override void DoModification() => Child1.RelativeSizeAxes = Axes.X;
        }

        public class PaddingChange1 : Size2Case
        {
            public PaddingChange1()
            {
                Root.AutoSizeAxes = Axes.Y;
            }

            public override void DoModification() => Root.Padding = new MarginPadding { Top = 1 };
        }

        public class PaddingChange2 : Size2Case
        {
            public PaddingChange2()
            {
                Root.RelativeSizeAxes = Axes.Y;
            }

            public override void DoModification() => Root.Padding = new MarginPadding { Top = 1 };
        }

        public class PaddingChange3 : Size2Case
        {
            public PaddingChange3()
            {
                Root.AutoSizeAxes = Axes.X;
                Child.RelativeSizeAxes = Axes.Y;
                Child.Rotation = 45;
            }

            public override void DoModification() => Root.Padding = new MarginPadding { Top = 2 };
        }

        public class FillModeChange : Size2Case
        {
            public FillModeChange()
            {
                Root.Padding = new MarginPadding { Right = 1 };
                Child.RelativeSizeAxes = Axes.Both;
                Child.FillMode = FillMode.Fill;
            }

            public override void DoModification() => Child.FillMode = FillMode.Stretch;
        }

        public class OriginChange : Size2Case
        {
            public OriginChange()
            {
                Root.AutoSizeAxes = Axes.Y;
            }

            public override void DoModification() => Child.Origin = Anchor.CentreLeft;
        }

        public class AutoSizeWithRotation1 : Size2Case
        {
            public AutoSizeWithRotation1()
            {
                Root.AutoSizeAxes = Axes.X;
                Child.RelativeSizeAxes = Axes.Y;
                Child.Rotation = -45;
            }

            public override void DoModification() => Root.Height = 2;
        }

        public class AutoSizeWithRotation2 : Size3Case2
        {
            public AutoSizeWithRotation2()
            {
                Root.AutoSizeAxes = Axes.Both;
                Child1.RelativeSizeAxes = Axes.Y;
                Child1.Rotation = -45;
            }

            public override void DoModification() => Child2.Height = 2;
        }

        public class AutoSizeWithRotation3 : LayoutInvalidationTest
        {
            protected readonly Container Root, Child1, Child2, Child3;
            public override Drawable[] Drawables => new Drawable[] { Root, Child1, Child2, Child3 };

            public AutoSizeWithRotation3()
            {
                Container.Child = Root = new Container
                {
                    Name = "Root",
                    Size = new Vector2(1),
                    Children = new Drawable[]
                    {
                        Child1 = new Container
                        {
                            Name = "Child1",
                            Size = new Vector2(1),
                        },
                        Child2 = new Container
                        {
                            Name = "Child2",
                            Size = new Vector2(1)
                        },
                        Child3 = new Container
                        {
                            Name = "Child3",
                            Size = new Vector2(1)
                        }
                    },
                };
                Root.AutoSizeAxes = Axes.Both;
                Child1.RelativeSizeAxes = Axes.Y;
                Child1.Rotation = -45;
                Child3.RelativeSizeAxes = Axes.X;
                Child3.Rotation = 90;
            }

            public override void DoModification() => Child2.Height = 2;
        }

        public class AutoSizeWithShear : Size2Case
        {
            public AutoSizeWithShear()
            {
                Root.AutoSizeAxes = Axes.X;
                Child.RelativeSizeAxes = Axes.Y;
                Child.Anchor = Anchor.BottomRight;
                Child.Shear = new Vector2(1, 0);
            }

            public override void DoModification() => Root.Height = 2;
        }

        public class AutoSizeExtras1 : Size3Case1
        {
            public AutoSizeExtras1()
            {
                Root.AutoSizeAxes = Axes.Y;
                Child.AutoSizeAxes = Axes.X;
                GrandChild.RelativeSizeAxes = Axes.Y;
                GrandChild.Margin = new MarginPadding { Bottom = -1 };
                GrandChild.Shear = new Vector2(1, 0);
            }

            public override void DoModification() => Child.RelativeSizeAxes = Axes.Y;
        }

        public class AutoSizeExtras2 : Size3Case1
        {
            public AutoSizeExtras2()
            {
                Root.AutoSizeAxes = Axes.Y;
                Child.AutoSizeAxes = Axes.X;
                Child.RelativeSizeAxes = Axes.Y;
                GrandChild.RelativeSizeAxes = Axes.Y;
                GrandChild.Rotation = 90;
            }

            public override void DoModification() => Root.Padding = new MarginPadding { Bottom = 1 };
        }

        public class AutoSizeExtras3 : Size3Case1
        {
            public AutoSizeExtras3()
            {
                Child.AutoSizeAxes = Axes.Both;
                GrandChild.Shear = new Vector2(-1, 0);
            }

            public override void DoModification() => GrandChild.RelativeSizeAxes = Axes.Y;
        }

        public class AutoSizeExtras4 : Size3Case1
        {
            public AutoSizeExtras4()
            {
                Root.AutoSizeAxes = Axes.Y;
                Root.Padding = new MarginPadding { Bottom = 1 };
                Child.RelativeSizeAxes = Axes.Y;
                Child.AutoSizeAxes = Axes.X;
                GrandChild.RelativeSizeAxes = Axes.Y;
                GrandChild.Rotation = 90;
            }

            public override void DoModification() => Child.Height = 2;
        }

        public class AutoSizeExtras5 : Size3Case2
        {
            public AutoSizeExtras5()
            {
                Root.AutoSizeAxes = Axes.Both;
                Child1.X = 1;
                Child1.RelativePositionAxes = Axes.X;
            }

            public override void DoModification() => Child1.RelativePositionAxes = Axes.None;
        }

        public class FillFlow1 : LayoutInvalidationTest
        {
            protected readonly FillFlowContainer Root;
            public override Drawable[] Drawables => new[] { Root, Root.Children[0], Root.Children[1] };

            public FillFlow1()
            {
                Container.Child = Root = new FillFlowContainer
                {
                    Name = "Root",
                    Size = new Vector2(1),
                    Children = new[]
                    {
                        new Container { Name = "Child1", Size = new Vector2(1) },
                        new Container { Name = "Child2", Size = new Vector2(1) }
                    },
                };
            }

            public override void DoModification() => Root.AutoSizeAxes = Axes.X;
        }

        public class FillFlow2 : LayoutInvalidationTest
        {
            protected readonly Container Root, Child2;
            protected readonly FillFlowContainer Child1;
            public override Drawable[] Drawables => new[] { Root, Child1, Child1.Children[0], Child1.Children[1], Child2 };

            public FillFlow2()
            {
                Container.Child = Root = new Container
                {
                    Name = "Root",
                    Size = new Vector2(1),
                    Children = new Drawable[]
                    {
                        Child1 = new FillFlowContainer
                        {
                            Name = "Child1",
                            Size = new Vector2(1),
                            Children = new Drawable[] { new Container { Size = new Vector2(1) }, new Container { Size = new Vector2(1) } }
                        },
                        Child2 = new Container { Name = "Child2", Size = new Vector2(1) }
                    },
                };
                Root.AutoSizeAxes = Axes.Y;
                Root.Margin = new MarginPadding { Right = 1 };
                Child1.RelativeSizeAxes = Axes.Both;
                Child1.FillMode = FillMode.Fill;
            }

            public override void DoModification() => Child2.Margin = new MarginPadding { Top = 1 };
        }

        public abstract class FillFlowSiz2Case : LayoutInvalidationTest
        {
            protected readonly FillFlowContainer Root;
            protected readonly Container Child;
            public override Drawable[] Drawables => new Drawable[] { Root, Child };

            protected FillFlowSiz2Case()
            {
                Container.Child = Root = new FillFlowContainer
                {
                    Name = "Root",
                    Size = new Vector2(1),
                    Child = Child = new Container { Name = "Child", Size = new Vector2(1) },
                };
            }
        }

        public class FillFlowAnchorChange : FillFlowSiz2Case
        {
            public override void DoModification() => Child.Anchor = Anchor.TopRight;
        }

        public class FillFlowOriginChange : FillFlowSiz2Case
        {
            public FillFlowOriginChange()
            {
                Child.Anchor = Anchor.Centre;
            }

            public override void DoModification() => Child.Origin = Anchor.TopCentre;
        }

    }
}