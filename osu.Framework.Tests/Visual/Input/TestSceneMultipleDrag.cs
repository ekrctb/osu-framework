// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using NUnit.Framework;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Input;
using osu.Framework.Input.Events;
using osu.Framework.Tests.Visual.Input;
using osuTK;
using osuTK.Graphics;
using osuTK.Input;

namespace osu.Framework.Tests.Visual.Drawables
{
    public class TestSceneMultipleDrag : FrameworkTestScene
    {
        public TestSceneMultipleDrag()
        {
            Child = new MultipleDragInputManager
            {
                RelativeSizeAxes = Axes.Both,
                Size = new Vector2(1.0f),
                Children = new Drawable[] {
                    new MultipleDrag {
                        Size = new Vector2(500, 500),
                    },
                }
            };
        }

        public class MultipleDragInputManager : PassThroughInputManager
        {
            protected override MouseButtonEventManager CreateButtonEventManagerFor(MouseButton button)
            {
                return new DraggingEnabledMouseButtonEventManager(button);
            }
        }

        public class DraggingEnabledMouseButtonEventManager : MouseButtonEventManager
        {
            public override bool EnableClick => false;
            public override bool EnableDrag => true;

            public override bool ChangeFocusOnClick => false;

            public DraggingEnabledMouseButtonEventManager(MouseButton button) : base(button)
            {
            }
        }

        public class MultipleDrag : Container
        {
            readonly SpriteText text;

            public MultipleDrag()
            {
                Children = new Drawable[]{
                    new Box
                    {
                        RelativeSizeAxes = Axes.Both,
                        Colour = new Color4(1, 1, 1, 0.2f),
                    },
                    text = new SpriteText(),
                };
            }

            protected override bool OnDragStart(DragStartEvent e) => true;

            protected override void Update()
            {
                text.Text = $"IsDragged = {IsDragged}";
            }
        }
    }
}
