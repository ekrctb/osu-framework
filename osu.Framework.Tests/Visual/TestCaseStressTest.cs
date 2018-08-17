// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using System.Collections.Generic;
using System.Linq;
using FsCheck;
using JetBrains.Annotations;
using Microsoft.FSharp.Collections;
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
        // Keep (AutoSizeAxes & (Child.RelativeSizeAxes | Child.RelativePositionAxes)) to 0
        public static bool ForbidAutoSizeUndefinedCase = true;
        public static bool NoPadding = true;
        public static bool NoRotation = true;
        public static bool NoShear = true;

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
            public virtual float Scale => 1;
        }

        public void AddCase<TCase>() where TCase : InvalidationFailureCase, new()
        {
            var instance1 = new TCase();
            var instance2 = new TCase();

            for (int i = 0; i < 2; i++)
            {
                var instance = i == 0 ? instance1 : instance2;
                var cell = Cell(0, i);
                cell.Scale = new Vector2(instance.Scale);
                cell.AlwaysPresent = true;
                cell.Child = new Container
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

        public class Case5 : SizeTwoCase
        {
            public override float Scale => 0.02f;

            public Case5()
            {
                Child.RelativeSizeAxes = Axes.X;
                Root.AutoSizeAxes = Axes.Y;
                Child.Rotation = 30;
            }

            public override void DoModification()
            {
                Root.Width = 200;
            }
        }

        public class Case6 : SizeTwoCase
        {
            public override float Scale => 0.02f;

            public Case6()
            {
                Child.RelativeSizeAxes = Axes.Y;
                Child.Shear = new Vector2(-2, 0);
                Root.AutoSizeAxes = Axes.X;
            }

            public override void DoModification()
            {
                Root.Height = 130;
            }
        }

        public class Case7 : SizeTwoCase
        {
            public Case7()
            {
                Root.Width = 2; Child.Scale = new Vector2(0.6666667f, 1);
                Child.Anchor = Anchor.TopRight;
                Child.Margin = new MarginPadding {Top=-1,Left=0.5f,Bottom=0,Right=0};
            }

            public override void DoModification()
            {
                Root.AutoSizeAxes = Axes.X;
            }
        }

        public StressTest()
            : base(1, 2)
        {
            AddStep("Case0", AddCase<Case0>);
            AddStep("Case1", AddCase<Case1>);
            AddStep("Case2", AddCase<Case2>);
            AddStep("Case3", AddCase<Case3>);
            AddStep("Case4", AddCase<Case4>);
            AddStep("Case5", AddCase<Case5>);
            AddStep("Case6", AddCase<Case6>);
            AddStep("Case7", AddCase<Case7>);

            var propSize2 = prop(2, 2);

            AddRepeatStep("quickCheck", () => Check.QuickThrowOnFailure(prop(2, 5)), 100);
            AddRepeatStep("quickCheck(size = 2)", () => Check.QuickThrowOnFailure(propSize2), 100);
        }

        private Property prop(int sizeLo, int sizeUp) =>
            Prop.ForAll(new ArbitraryScene(sizeLo, sizeUp), scene => Prop.ForAll(new ArbitraryModificationList(new ArbitraryModification(scene)), modifications => runTest(scene, modifications)));

        private bool runTest(Scene scene, SceneModification[] modifications)
        {
            var cell = Cell(0, 0);
            var instance = new SceneInstance(scene);
            var container = new Container
            {
                Child = instance.Root,
                Size = new Vector2(250),
                Anchor = Anchor.Centre,
                Origin = Anchor.TopLeft,
            };

            try
            {
                cell.Child = container;
                cell.UpdateSubTree();

                foreach (var entry in modifications)
                {
                    if (!instance.Execute(entry)) continue;

                    if (!instance.CheckStateValidity())
                    {
                        return false;
                    }
                }
            }
            finally
            {
                cell.Remove(container);
                container.Remove(container.Child);
            }

            return true;
        }

        public class ArbitraryModificationList : Arbitrary<SceneModification[]>
        {
            private readonly Arbitrary<SceneModification> elemArbitrary;

            public ArbitraryModificationList(Arbitrary<SceneModification> elemArbitrary)
            {
                this.elemArbitrary = elemArbitrary;
            }

            public override Gen<SceneModification[]> Generator => Gen.ListOf(elemArbitrary.Generator).Select(x => x.ToArray());

            public override IEnumerable<SceneModification[]> Shrinker(SceneModification[] list) =>
                list.Select(elem => list.Where(x => x != elem).ToArray()).Concat(
                    list.SelectMany(elem => elemArbitrary.Shrinker(elem).Select(newElem => list.Select(x => x == elem ? newElem : x).ToArray())));
        }

        public class TestContainer : Container
        {
            public TestContainer()
            {
                Size = new Vector2(1);
                AlwaysPresent = true;
            }

            protected override bool ComputeIsMaskedAway(RectangleF maskingBounds) => false;
        }

        public class SceneModification
        {
            public readonly string NodeName;
            public readonly string PropertyName;
            public readonly object Value;

            public SceneModification(string nodeName, string propertyName, object value)
            {
                NodeName = nodeName;
                PropertyName = propertyName;
                Value = value;
            }

            public override string ToString()
            {
                return $"{NodeName}.{PropertyName} = {formatValue(Value)}";
            }

            private string formatValue(object value)
            {
                switch (value)
                {
                    case MarginPadding x:
                        return $"new MarginPadding {{Top={x.Top},Left={x.Left},Bottom={x.Bottom},Right={x.Right}}}";
                    case Anchor _:
                    case Axes _:
                    case FillMode _:
                        return $"{value.GetType().Name}.{value}";
                    case Vector2 vec:
                        return $"new Vector2({vec.X}, {vec.Y})";
                    default:
                        return value.ToString();
                }
            }
        }

        public class SceneNode
        {
            public readonly SceneNode[] Children;
            public readonly int TreeSize;
            public string Name = "Node";

            public SceneNode(IEnumerable<SceneNode> children)
            {
                Children = children.ToArray();
                TreeSize = 1 + Children.Select(c => c.TreeSize).Sum();
            }

            public override string ToString()
            {
                return $"{Name} {{{string.Join(", ", Children.Select(x => x.ToString()))}}}";
            }
        }

        public class Scene
        {
            private readonly List<SceneNode> nodes;
            public IReadOnlyList<SceneNode> Nodes => nodes;
            public SceneNode Root => Nodes.First();

            public Scene([NotNull] SceneNode root)
            {
                if (root == null) throw new ArgumentNullException(nameof(root));
                nodes = new List<SceneNode>();

                var depthMap = new Dictionary<object, int>();

                void enumNodes(SceneNode node, int depth)
                {
                    depthMap[node] = depth;
                    nodes.Add(node);
                    foreach (var child in node.Children)
                        enumNodes(child, depth + 1);
                }

                enumNodes(root, 0);

                foreach (var group in Nodes.GroupBy(c => depthMap[c]))
                {
                    var depth = group.Key;
                    var array = group.ToArray();
                    string prefix = depth == 0 ? "Root" : string.Join("", Enumerable.Repeat("Grand", depth - 1)) + "Child";
                    int index = 1;
                    foreach (var node in array)
                    {
                        node.Name = array.Length == 1 ? prefix : prefix + index;
                        ++index;
                    }
                }
            }

            public override string ToString()
            {
                return $"Scene({Root})";
            }
        }

        public class ArbitraryScene : Arbitrary<Scene>
        {
            public readonly int SizeLo, SizeUp;

            public ArbitraryScene(int sizeLo, int sizeUp)
            {
                SizeLo = sizeLo;
                SizeUp = sizeUp;
            }

            public override Gen<Scene> Generator => Gen.Choose(SizeLo, SizeUp).SelectMany(gen).Select(root => new Scene(root));

            private static Gen<SceneNode> gen(int treeSize) =>
                genChildren(treeSize - 1).Select(children => new SceneNode(children));

            private static Gen<FSharpList<SceneNode>> genChildren(int treeSize) => treeSize == 0
                ? Gen.Constant(FSharpList<SceneNode>.Empty)
                : Gen.Choose(1, treeSize).SelectMany(childSize => gen(childSize).SelectMany(head =>
                    genChildren(treeSize - childSize).Select(tail => FSharpList<SceneNode>.Cons(head, tail))));
        }

        public class ArbitraryModification : Arbitrary<SceneModification>
        {
            public readonly Scene Scene;

            public ArbitraryModification(Scene scene)
            {
                Scene = scene;
            }

            public override Gen<SceneModification> Generator => gen_for(Scene);

            public override IEnumerable<SceneModification> Shrinker(SceneModification modification)
            {
                return shrink(modification.Value, positiveProperties.Contains(modification.PropertyName))
                    .Select(newValue => new SceneModification(modification.NodeName, modification.PropertyName, newValue));
            }

            private readonly HashSet<string> positiveProperties = new HashSet<string>(new[] { nameof(Width), nameof(Height), nameof(Scale) });

            private IEnumerable<float> shrink_float(float x)
            {
                if (x == 0) yield break;
                yield return 0;
                if (x == 1) yield break;
                yield return 1;
                if (x > 0 || x == -1) yield break;
                yield return -x;
            }

            private IEnumerable<T[]> shrink_tuple<T>(T[] tuple, Func<T, IEnumerable<T>> shrinkElem)
            {
                for (int i = 0; i < tuple.Length; i++)
                {
                    foreach (var newElem in shrinkElem(tuple[i]))
                    {
                        yield return tuple.Take(i).Concat(tuple.Skip(i).Prepend(newElem)).ToArray();
                    }
                }
            }

            private IEnumerable<object> shrink(object value, bool positive = false)
            {
                switch (value)
                {
                    case int x:
                        return Arb.Shrink(x).Cast<object>();
                    case float x:
                        return shrink_float(x).Where(f => f > 0 || !positive).Cast<object>();
                    case Vector2 v:
                        return shrink_tuple(new[] { v.X, v.Y }, x => shrink_float(x).Where(f => f > 0 || !positive)).Select(t => (object)new Vector2(t[0], t[1]));
                    case MarginPadding m:
                        return shrink_tuple(new[] { m.Top, m.Left, m.Bottom, m.Right }, shrink_float).Select(t => (object)new MarginPadding { Top = t[0], Left = t[1], Bottom = t[2], Right = t[3] });
                    case Axes x:
                        return new[] { x & ~Axes.X, x & ~Axes.Y }.Distinct().Where(y => y != x).Cast<object>();
                    default:
                        return Enumerable.Empty<object>();
                }
            }

            private struct Entry
            {
                public readonly string PropertyName;
                public readonly object Value;

                public Entry(string propertyName, object value)
                {
                    PropertyName = propertyName;
                    Value = value;
                }
            }

            private static Gen<SceneModification> gen_for(Scene scene) =>
                Gen.Choose(0, scene.Nodes.Count - 1).SelectMany(nodeIndex =>
                {
                    var node = scene.Nodes[nodeIndex];
                    return for_container.Select(pair => new SceneModification(node.Name, pair.PropertyName, pair.Value));
                });

            private static readonly Gen<float> position = Arb.Generate<NormalFloat>().Select(x => (float)x);
            private static readonly Gen<float> size = Arb.Generate<NormalFloat>().Select(x => (float)x).Where(x => x > 0);
            private static readonly Gen<float> rotation = Arb.Generate<NormalFloat>().Select(x => (float)x * 120);

            private static readonly Gen<Anchor> anchor = Gen.OneOf(new[]
            {
                Anchor.TopLeft,
                Anchor.TopCentre,
                Anchor.TopRight,
                Anchor.CentreLeft,
                Anchor.Centre,
                Anchor.CentreRight,
                Anchor.BottomLeft,
                Anchor.BottomCentre,
                Anchor.BottomRight
            }.Select(Gen.Constant));

            private static readonly Gen<Axes> axes = Gen.OneOf(new[] { Axes.None, Axes.X, Axes.Y, Axes.Both }.Select(Gen.Constant));
            private static readonly Gen<FillMode> fillmode = Gen.OneOf(new[] { FillMode.Fill, FillMode.Fit, FillMode.Stretch }.Select(Gen.Constant));

            private static Gen<Vector2> vec(Gen<float> gen) => Gen.Two(gen).Select(t => new Vector2(t.Item1, t.Item2));

            private static Gen<MarginPadding> marginpadding(Gen<float> gen) => Gen.Four(gen).Select(t => new MarginPadding
            {
                Top = t.Item1,
                Left = t.Item2,
                Bottom = t.Item3,
                Right = t.Item4
            });

            private static Gen<Entry> entry<T>(string propertyName, Gen<T> gen) => gen.Select(x => new Entry(propertyName, x));

            private static readonly Gen<Entry> dummy = Gen.Constant(new Entry("Dummy", 0));
            private static readonly Gen<Entry> for_container = Gen.OneOf(
                entry(nameof(X), position),
                entry(nameof(Y), position),
                entry(nameof(Width), size),
                entry(nameof(Height), size),
                entry(nameof(Margin), marginpadding(position)),
                NoPadding ? dummy : entry(nameof(Padding), marginpadding(position)),
                entry(nameof(Origin), anchor),
                entry(nameof(Anchor), anchor),
                entry(nameof(RelativeSizeAxes), axes),
                entry(nameof(AutoSizeAxes), axes),
                entry(nameof(RelativePositionAxes), axes),
                entry(nameof(BypassAutoSizeAxes), axes),
                entry(nameof(FillMode), fillmode),
                entry(nameof(Scale), vec(size)),
                NoRotation ? dummy : entry(nameof(Rotation), rotation),
                NoShear ? dummy : entry(nameof(Shear), vec(position))
            );
        }

        public class SceneInstance
        {
            private readonly List<TestContainer> nodes;
            public IReadOnlyList<TestContainer> Nodes => nodes;
            public TestContainer Root => Nodes.First();

            private readonly Dictionary<string, TestContainer> nodeMap;
            public TestContainer GetNode(string name) => nodeMap[name];

            public SceneInstance(Scene scene)
            {
                nodes = new List<TestContainer>();
                nodeMap = new Dictionary<string, TestContainer>();

                TestContainer createInstanceTree(SceneNode node)
                {
                    var container = new TestContainer { Name = node.Name };

                    nodes.Add(container);
                    nodeMap.Add(node.Name, container);

                    foreach (var child in node.Children)
                        container.Add(createInstanceTree(child));
                    return container;
                }

                createInstanceTree(scene.Root);
            }

            public bool CanExecute(SceneModification modification)
            {
                var container = GetNode(modification.NodeName);
                var value = modification.Value;

                switch (modification.PropertyName)
                {
                    case nameof(RelativeSizeAxes):
                        if (ForbidAutoSizeUndefinedCase && (container.Parent.AutoSizeAxes & (Axes)value) != 0) return false;
                        return (container.AutoSizeAxes & (Axes)value) == 0;
                    case nameof(RelativePositionAxes):
                        if (ForbidAutoSizeUndefinedCase && (container.Parent.AutoSizeAxes & (Axes)value) != 0) return false;
                        return true;
                    case nameof(AutoSizeAxes):
                        if (ForbidAutoSizeUndefinedCase)
                        {
                            var relativeSizeAxes = container.Children.Select(x => x.RelativeSizeAxes | x.RelativePositionAxes).Prepend(~(Axes)0).Aggregate((x, y) => x & y);
                            if(((Axes)value & relativeSizeAxes) != 0) return false;
                        }
                        return (container.RelativeSizeAxes & (Axes)value) == 0;
                    case nameof(Width):
                        return (container.AutoSizeAxes & Axes.X) == 0;
                    case nameof(Height):
                        return (container.AutoSizeAxes & Axes.Y) == 0;
                    default:
                        return true;
                }
            }

            public bool Execute(SceneModification modification)
            {
                if (modification.PropertyName == "Dummy") return false;
                if (!CanExecute(modification)) return false;

                var container = GetNode(modification.NodeName);
                container.Set(modification.PropertyName, modification.Value);
                return true;
            }

            private float[] getState()
            {
                return Nodes.SelectMany(c =>
                {
                    var size = c.DrawSize;
                    var position = c.DrawPosition;
                    if (Precision.AlmostEquals(size.X, 0) || Precision.AlmostEquals(size.Y, 0)) return new float[] { 0, 0, 0, 0 };
                    return new[] { size.X, size.Y, position.X, position.Y };
                }).ToArray();
            }

            private void invalidate()
            {
                foreach (var node in Nodes)
                    node.Invalidate();
            }

            private void update()
            {
                Root.UpdateSubTree();
                Root.ValidateSubTree();
            }

            public float[] LastState1, LastState2;

            public bool CheckStateValidity()
            {
                if (!Root.IsLoaded) throw new InvalidOperationException("The scene isn't loaded");

                update();

                var state1 = getState();

                invalidate();
                update();

                var state2 = getState();

                LastState1 = state1;
                LastState2 = state2;

                return state1.Length == state2.Length && Enumerable.Range(0, state1.Length).All(i => Precision.AlmostEquals(state1[i], state2[i]));
            }
        }
    }
}
