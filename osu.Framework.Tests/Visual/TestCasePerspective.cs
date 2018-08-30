// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using osu.Framework.Extensions.MatrixExtensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Testing;
using OpenTK;

namespace osu.Framework.Tests.Visual
{
    public class TestCasePerspective : TestCase
    {
        private readonly TestBrowser browser;
        public TestCasePerspective()
        {
            var perspectiveContainer = new PerspectiveContainer
            {
                Anchor = Anchor.Centre,
                Origin = Anchor.Centre,
                RelativeSizeAxes = Axes.Both
            };
            browser = new TestBrowser();
            //perspectiveContainer.ChildrenEnumerable = Enumerable.Range(0, 100).Select(i => new Box
            //{
            //    Anchor = Anchor.Centre,
            //    Position = new Vector2(RNG.NextSingle() * 2 - 1, RNG.NextSingle() * 2 - 1) * 100,
            //    Colour = new Color4(RNG.NextSingle(), RNG.NextSingle(), RNG.NextSingle(), .5f),
            //    Size = new Vector2(10),
            //});
            perspectiveContainer.Add(new Container {
                Size = new Vector2(1000),
                Scale = new Vector2(.5f),
                Anchor = Anchor.Centre,
                Origin = Anchor.Centre,
                Child = browser
            });

            Add(new Container
            {
                Child = perspectiveContainer,
                Anchor = Anchor.Centre,
                Origin = Anchor.Centre,
            });
            AddSliderStep("A", -2, 2, 0f, x =>
            {
                perspectiveContainer.A = x / 100;
                perspectiveContainer.Invalidate();
            });
            AddSliderStep("B", -2, 2, 0f, x =>
            {
                perspectiveContainer.B = x / 100;
                perspectiveContainer.Invalidate();
            });
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();
            browser.LoadTest(typeof(TestCaseSizing));
        }

        public class PerspectiveContainer : Container
        {
            public float A, B;
            public override DrawInfo DrawInfo
            {
                get
                {
                    DrawInfo di = Parent?.DrawInfo ?? new DrawInfo(null);

                    Vector2 translation = DrawPosition + AnchorPosition + Parent.ChildOffset;
                    Vector2 origin = OriginPosition;

                    MatrixExtensions.TranslateFromLeft(ref di.Matrix, translation);

                    di.Matrix = new Matrix3(1, 0, A, 0, 1, B, 0, 0, 1) * di.Matrix;

                    MatrixExtensions.TranslateFromLeft(ref di.Matrix, -origin);

                    di.MatrixInverse = di.Matrix.Inverted();
                    return di;
                }
            }
        }
    }
}
