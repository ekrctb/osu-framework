// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using JetBrains.Annotations;
using osu.Framework.Extensions.Color4Extensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Primitives;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Input.States;
using osu.Framework.MathUtils;
using osu.Framework.Testing;
using osu.Framework.Tests.Layout;
using OpenTK;
using OpenTK.Graphics;

namespace osu.Framework.Tests.Visual
{
    public class StressTest : GridTestCase
    {
        private Action actionOnClick;
        protected override bool OnClick(InputState state)
        {
            actionOnClick?.Invoke();
            return base.OnClick(state);
        }

        public abstract class InvalidationFailureCase
        {
            public abstract IEnumerable<Drawable> Drawables { get; }
            public abstract void DoModification();
        }

        public void AddCase<TCase>() where TCase : InvalidationFailureCase, new()
        {
            var instance1 = new TCase();
            var instance2 = new TCase();

            for (int i = 0; i < 2; i++)
            {
                var instance = i == 0 ? instance1 : instance2;
                Cell(0, i).Child = new Container
                {
                    Child = instance.Drawables.First(),
                    Size = new Vector2(250),
                    Anchor = Anchor.Centre,
                    Origin = Anchor.TopLeft,
                    Name = i == 0 ? "WithoutInvalidation" : "WithInvalidation"
                };

                var index = 0;
                foreach (var c in instance.Drawables.Cast<Container>())
                {
                    c.Add(new Box
                    {
                        RelativeSizeAxes = Axes.Both,
                        Size = new Vector2(1),
                        Colour = (index == 0 ? Color4.Red : Color4.Blue).Opacity(.5f),
                        Depth = -1,
                    });
                    index += 1;
                }
            }

            actionOnClick = () =>
            {
                instance1.DoModification();
                instance2.DoModification();

                foreach (var d in instance2.Drawables)
                    d.Invalidate();
            };
        }

        public abstract class SizeTwoCase : InvalidationFailureCase
        {
            [NotNull]
            protected readonly Container Root, Child;

            public override IEnumerable<Drawable> Drawables => new Drawable[] { Root, Child };

            protected SizeTwoCase()
            {
                Root = new Container
                {
                    Size = new Vector2(100),
                    Child = Child = new Container
                    {
                        Size = new Vector2(100),
                        Name = "Child"
                    },
                    Name = "Root"
                };
            }
        }

        public class Case0 : SizeTwoCase
        {
            public Case0()
            {
                Child.RelativePositionAxes = Axes.Both;
                Root.AutoSizeAxes = Axes.X;
            }

            public override void DoModification()
            {
                Child.RelativePositionAxes = Axes.Y;
            }
        }

        public class Case1 : SizeTwoCase
        {
            public Case1()
            {
                Child.RelativeSizeAxes = Axes.Both;
                Child.FillMode = FillMode.Fit;
                Root.AutoSizeAxes = Axes.X;
            }

            public override void DoModification()
            {
                Child.RelativeSizeAxes = Axes.Y;
            }
        }

        public class Case2 : SizeTwoCase
        {
            public Case2()
            {
                Child.AutoSizeAxes = Axes.Both;
                Root.AutoSizeAxes = Axes.Both;
            }

            public override void DoModification()
            {
                Root.Padding = new MarginPadding(20f);
            }
        }

        public class Case3 : SizeTwoCase
        {
            public Case3()
            {
                Root.AutoSizeAxes = Axes.Both;
            }

            public override void DoModification()
            {
                Child.RelativePositionAxes = Axes.X;
            }
        }

        public class Case4 : SizeTwoCase
        {
            public Case4()
            {
                Child.Margin = new MarginPadding(20f);
                Child.RelativeSizeAxes = Axes.Both;
                Root.AutoSizeAxes = Axes.Both;
            }

            public override void DoModification()
            {
                Child.RelativeSizeAxes = Axes.None;
            }
        }

        public StressTest() : base(1, 2)
        {
            addStressStep();
            AddStep("Case0", AddCase<Case0>);
            AddStep("Case1", AddCase<Case1>);
            AddStep("Case2", AddCase<Case2>);
            AddStep("Case3", AddCase<Case3>);
            AddStep("Case4", AddCase<Case4>);
        }

        private int testIndex;
        private void addStressStep()
        {
            AddStep("Reset test index", () => testIndex = 0);
            AddStep("Stress test", () =>
            {
                testIndex += 1;
                var seed = testIndex;

                int size = seed % 5 + 2;
                const int max_num = 10000;

                IReadOnlyList<ModificationEntry> previousEntries = null;
                var iter = 0;
                var rng = new Random(unchecked(seed * 131231));
                var bestEntries = new ModificationEntry[]{};
                var prob = 0.5;
                string description = null;
                void next(bool ok, IReadOnlyList<ModificationEntry> modifications, RandomTestContainer root)
                {
                    if (description == null)
                        description = GetSceneDescription(root);
                    Remove(root);

                    var num = modifications.Count;
                    iter += 1;

                    if (ok)
                    {
                        Console.WriteLine($"{seed},{size}-{iter} Uncatched: {num}");

                        if (iter == 1) return;

                        modifications = previousEntries;
                        num = modifications.Count;
                        prob *= 0.95;
                    }
                    else
                    {
                        Console.WriteLine($"{seed},{size}-{iter} Catched: {num}");
                        if (bestEntries.Length == 0 || num < bestEntries.Length)
                            bestEntries = modifications.ToArray();
                        prob /= 0.95;
                    }

                    if (iter < 1000 && prob > 0.001)
                    {
                        previousEntries = modifications;

                        ModificationEntry[] newEntries;

                        do
                        {
                            newEntries = modifications.Take(num - 1).Where(entry => rng.NextDouble() >= prob).Append(modifications.Last()).ToArray();
                        } while (newEntries.Length == modifications.Count);

                        RunTestCase(seed, size, num, newEntries, next);
                    }
                    else
                    {
                        Console.WriteLine();
                        foreach (var entry in bestEntries)
                        {
                            Console.WriteLine($"{entry.Container.Name}.{entry.Name} = {entry.Val};");
                        }
                        Console.WriteLine();

                        RunTestCase(seed, size, num, bestEntries, (a, b, c) => { });
                    }
                }

                RunTestCase(seed, size, max_num, null, next);
            });
        }

        public void RunTestCase(int seed, int size, int maxModifications, IReadOnlyList<ModificationEntry> entries, Action<bool, IReadOnlyList<ModificationEntry>, RandomTestContainer> cont)
        {
            var sceneGenerator = new RandomSceneGenerator(new Random(seed));
            var root = sceneGenerator.Generate(size);
            Add(root);
            root.Schedule(() =>
            {
                SetName(root);

                var containerMap = new Dictionary<string, Container>();
                void generateContainerMap(Container c)
                {
                    containerMap.Add(c.Name, c);
                    foreach (var child in c.Children.OfType<Container>())
                        generateContainerMap(child);
                }

                generateContainerMap(root);

                if (entries == null)
                {
                    var modificationGenerator = new RandomModificationGenerator(new Random(seed));
                    for (int j = 0; j < maxModifications; j++)
                    {
                        modificationGenerator.ModifyRandomly(root);

                        if (!Check(root))
                        {
                            cont(false, modificationGenerator.Modifications, root);
                            return;
                        }
                    }

                    cont(true, modificationGenerator.Modifications, root);
                }
                else
                {
                    var newEntries = new List<ModificationEntry>();
                    foreach (var entry in entries)
                    {
                        var newEntry = entry;
                        newEntry.Container = containerMap[entry.Container.Name];

                        newEntry.Execute();
                        newEntries.Add(newEntry);

                        if (!Check(root))
                        {
                            cont(false, newEntries, root);
                            return;
                        }
                    }

                    cont(true, newEntries, root);
                }
            });
        }

        public bool Check(RandomTestContainer root)
        {
            root.UpdateSubTree();
            root.ValidateSubTree();

            var state1 = GetState(root);

            RecalculateState(root);

            var state2 = GetState(root);

            var eq = state1.Count == state2.Count && Enumerable.Range(0, state1.Count).All(i => Precision.AlmostEquals(state1[i], state2[i]));

            if (!eq)
            {
                Console.WriteLine($"{string.Join(",", state1)} vs {string.Join(",", state2)}");
            }
            return eq;
        }

        public class RandomTestContainer : Container
        {
            public readonly int DecendantCount;

            public RandomTestContainer(int size)
            {
                DecendantCount = size;
                Size = new Vector2(1);
                AlwaysPresent = true;
            }

            protected override bool ComputeIsMaskedAway(RectangleF maskingBounds) => false;
        }

        public struct ModificationEntry
        {
            public Container Container;
            public string Name;
            public object Val;

            public void Execute()
            {
                switch (Name)
                {
                    case nameof(Drawable.RelativeSizeAxes):
                        if ((Container.AutoSizeAxes & (Axes)Val) > 0) return;
                        break;
                    case nameof(CompositeDrawable.AutoSizeAxes):
                        if ((Container.RelativeSizeAxes & (Axes)Val) > 0) return;
                        break;
                    case nameof(Drawable.Width):
                        if ((Container.AutoSizeAxes & Axes.X) > 0) return;
                        break;
                    case nameof(Drawable.Height):
                        if ((Container.AutoSizeAxes & Axes.Y) > 0) return;
                        break;
                }
                Container.Set(Name, Val);
            }
        }

        public class RandomModificationGenerator
        {
            private readonly Random rng;

            public RandomModificationGenerator(Random rng)
            {
                this.rng = rng;
            }

            private RandomTestContainer currentContainer;
            private readonly List<ModificationEntry> modifications = new List<ModificationEntry>();
            public IReadOnlyList<ModificationEntry> Modifications => modifications;

            private T modify<T>(string name, T val)
            {
                var entry = new ModificationEntry
                {
                    Container = currentContainer,
                    Name = name,
                    Val = val
                };
                currentEntries.Add(entry);
                entry.Execute();

                return val;
            }

            private readonly List<ModificationEntry> currentEntries = new List<ModificationEntry>();
            public ModificationEntry[] ModifyRandomly(RandomTestContainer root)
            {
                var target = rng.Next(0, root.DecendantCount);
                if (target != 0)
                {
                    target -= 1;
                    foreach (var child in root.Children.OfType<RandomTestContainer>())
                    {
                        if (target < child.DecendantCount)
                        {
                            return ModifyRandomly(child);
                        }

                        target -= child.DecendantCount;
                    }

                    Trace.Assert(false);
                }

                Anchor randomAnchor() => new[] { Anchor.TopLeft, Anchor.Centre, Anchor.BottomRight }[rng.Next(3)];
                float randomPosition() => rng.Next(-3, 4) * 0.5f;
                float randomSize() => rng.Next(1, 6) * 0.5f;
                FillMode randomFillMode() => new[] { FillMode.Fill, FillMode.Fit, FillMode.Stretch }[rng.Next(3)];
                Vector2 randomScale() => new Vector2(rng.Next(1, 4) * 0.5f, rng.Next(1, 4) * 0.5f);
                MarginPadding randomMarginPadding() => new MarginPadding
                {
                    Top = rng.Next(0, 4) * 0.5f,
                    Left = rng.Next(0, 4) * 0.5f,
                    Bottom = rng.Next(0, 4) * 0.5f,
                    Right = rng.Next(0, 4) * 0.5f
                };

                float randomRotation() => rng.Next(-12, 13) * 30;
                Vector2 randomShear() => new Vector2(rng.Next(0, 4) * 0.5f, rng.Next(0, 4) * 0.5f);

                Axes randomAxes() => (Axes)rng.Next(4);

                currentContainer = root;
                currentEntries.Clear();

                var t = rng.Next(16);
                switch (t)
                {
                    case 0:
                    case 1:
                        modify(t == 0 ? nameof(root.Origin) : nameof(root.Anchor), randomAnchor());
                        break;
                    case 2:
                    case 3:
                        modify(t == 2 ? nameof(root.X) : nameof(root.Y), randomPosition());
                        break;
                    case 4:
                    case 5:
                        modify(t == 4 ? nameof(root.Width) : nameof(root.Height), randomSize());
                        break;
                    case 6:
                        modify(nameof(root.FillMode), randomFillMode());
                        break;
                    case 7:
                        modify(nameof(root.Scale), randomScale());
                        break;
                    case 8:
                        modify(nameof(root.Margin), randomMarginPadding());
                        break;
                    case 9:
                        modify(nameof(root.Padding), randomMarginPadding());
                        break;
                    case 10:
                        modify(nameof(root.RelativeSizeAxes), randomAxes());
                        break;
                    case 11:
                        modify(nameof(root.AutoSizeAxes), randomAxes());
                        break;
                    case 12:
                        modify(nameof(root.RelativePositionAxes), randomAxes());
                        break;
                    case 13:
                        modify(nameof(root.BypassAutoSizeAxes), randomAxes());
                        break;
                    case 14:
                        modify(nameof(root.Rotation), randomRotation());
                        break;
                    case 15:
                        modify(nameof(root.Shear), randomShear());
                        break;
                }

                var array = currentEntries.ToArray();
                modifications.AddRange(array);
                return array;
            }
        }

        public void RecalculateState(Drawable root)
        {
            void invalidate(Drawable c)
            {
                c.Invalidate();
                if (!(c is Container container)) return;
                foreach (var child in container.Children)
                    invalidate(child);
            }

            invalidate(root);
            root.UpdateSubTree();
            root.ValidateSubTree();
        }

        public IReadOnlyList<float> GetState(RandomTestContainer root)
        {
            var list = new List<float>();
            void rec(RandomTestContainer c)
            {
                var size = c.DrawSize;
                var position = c.DrawPosition;
                list.Add(size.X);
                list.Add(size.Y);
                list.Add(position.X);
                list.Add(position.Y);
                for (int i = 0; i < 9; i++)
                {
                    //list.Add(c.DrawInfo.Matrix[i / 3, i % 3]);
                }

                foreach (var child in c.Children.OfType<RandomTestContainer>())
                    rec(child);
            }
            rec(root);
            return list;
        }


        public void SetName(RandomTestContainer c)
        {
            if (!(c.Parent is RandomTestContainer p))
                c.Name = "root";
            else
                c.Name = (p.Name == "root" ? "" : p.Name + ".") + "c" + c.ChildID;
            foreach (var child in c.Children.OfType<RandomTestContainer>())
                SetName(child);
        }

        public string GetSceneDescription(RandomTestContainer root)
        {
            List<string> list = new List<string>();
            /*
            void rec(RandomTestContainer c)
            {
                var childNames = new List<string>();
                foreach (var child in c.Children.OfType<RandomTestContainer>())
                {
                    rec(child);
                    childNames.Add(child.Name);
                }

                list.Add($"var {c.Name} = new {nameof(Container)}");
                if (childNames.Count == 0)
                {
                    list.Add("()");
                }
                else if (childNames.Count == 1)
                {
                    list.Add($"{{{nameof(c.Child)} = {childNames[0]}}}");
                }
                else
                {
                    list.Add($" {{{nameof(c.Children)} = new {nameof(Drawable)}[] {{{string.Join(", ", childNames)}}}}}");
                }
                list.Add(";\n");
                list.Add($"{c.Name}.{nameof(c.Name)} = \"{c.Name}\";\n");
            }
            */

            void rec(RandomTestContainer c)
            {
                list.Add($"{c.Name}: [{string.Join(", ", c.Children.Select(x => x.Name))}]\n");
                foreach (var child in c.Children.OfType<RandomTestContainer>())
                    rec(child);
            }

            rec(root);
            return string.Join("", list);
        }

        public class RandomSceneGenerator
        {
            private readonly Random rng;

            public RandomSceneGenerator(Random rng)
            {
                this.rng = rng;
            }

            public RandomTestContainer Generate(int size, string parentName = "")
            {
                var container = new RandomTestContainer(size);
                size -= 1;
                while (size > 0)
                {
                    var childSize = rng.Next(1, size + 1);
                    var child = Generate(childSize, container.Name);
                    container.Add(child);
                    size -= childSize;
                }

                return container;
            }
        }
    }
}
