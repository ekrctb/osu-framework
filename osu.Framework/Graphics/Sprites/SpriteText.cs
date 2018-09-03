﻿// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using OpenTK;
using OpenTK.Graphics;
using osu.Framework.Allocation;
using osu.Framework.Caching;
using osu.Framework.Configuration;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Textures;
using osu.Framework.Graphics.UserInterface;
using osu.Framework.IO.Stores;
using System;
using System.Collections.Generic;
using System.Linq;
using osu.Framework.Extensions.IEnumerableExtensions;

namespace osu.Framework.Graphics.Sprites
{
    /// <summary>
    /// A container for simple text rendering purposes. If more complex text rendering is required, use <see cref="TextFlowContainer"/> instead.
    /// </summary>
    public class SpriteText : FillFlowContainer, IHasCurrentValue<string>, IHasLineBaseHeight, IHasText, IHasFilterTerms
    {
        public IEnumerable<string> FilterTerms => new[] { Text };

        private static readonly char[] default_fixed_width_exceptions = { '.', ':', ',' };

        /// <summary>
        /// An array of characters which should not get a fixed width in a <see cref="FixedWidth"/> instance.
        /// </summary>
        protected virtual char[] FixedWidthExceptionCharacters => default_fixed_width_exceptions;

        /// <summary>
        /// Decide whether we want to make our SpriteText's vertical size to be <see cref="TextSize"/> (the full height) or precisely the size of used characters.
        /// Set to false to allow better centering of individual characters/numerals/etc.
        /// </summary>
        public bool UseFullGlyphHeight = true;

        public override bool IsPresent => base.IsPresent && (!string.IsNullOrEmpty(text) || !textCache.IsValid);

        /// <summary>
        /// True if the text should be wrapped if it gets too wide. Note that \n does NOT cause a line break. If you need explicit line breaks, use <see cref="TextFlowContainer"/> instead.
        /// </summary>
        public bool AllowMultiline
        {
            get => Direction == FillDirection.Full;
            set => Direction = value ? FillDirection.Full : FillDirection.Horizontal;
        }

        private string font;

        /// <summary>
        /// The name of the font to use when looking up textures for the individual characters.
        /// </summary>
        public string Font
        {
            get => font;
            set
            {
                font = value;
                textCache.Invalidate();
            }
        }

        private bool shadow;

        /// <summary>
        /// True if a shadow should be displayed around the text.
        /// </summary>
        public bool Shadow
        {
            get => shadow;
            set
            {
                if (shadow == value)
                    return;
                shadow = value;

                // Trigger a layout refresh because the shadows aren't in the hierarchy yet
                textCache.Invalidate();
            }
        }


        private Color4 shadowColour = new Color4(0f, 0f, 0f, 0.2f);

        /// <summary>
        /// The colour of the shadow displayed around the text. A shadow will only be displayed if the <see cref="Shadow"/> property is set to true.
        /// </summary>
        public Color4 ShadowColour
        {
            get => shadowColour;
            set
            {
                shadowColour = value;
                if (shadow)
                    shadowCache.Invalidate();
            }
        }

        /// <summary>
        /// Gets the base height of the font used by this text. If the font of this text is invalid, 0 is returned.
        /// </summary>
        public float LineBaseHeight
        {
            get
            {
                var baseHeight = store.GetBaseHeight(Font);
                if (baseHeight.HasValue)
                    return baseHeight.Value * TextSize;

                if (string.IsNullOrEmpty(Text))
                    return 0;

                return store.GetBaseHeight(Text[0]).GetValueOrDefault() * TextSize;
            }
        }

        private Cached textCache = new Cached();
        private Cached shadowCache = new Cached();

        private float spaceWidth;

        [Resolved]
        private FontStore store { get; set; }

        public override bool HandleKeyboardInput => false;
        public override bool HandleMouseInput => false;

        /// <summary>
        /// Creates a new sprite text. <see cref="Container{T}.AutoSizeAxes"/> is set to <see cref="Axes.Both"/> by default.
        /// </summary>
        public SpriteText()
        {
            AutoSizeAxes = Axes.Both;
        }

        private const float default_text_size = 20;

        private float textSize = default_text_size;

        /// <summary>
        /// The size of the text in local space. This means that if TextSize is set to 16, a single line will have a height of 16.
        /// </summary>
        public float TextSize
        {
            get => textSize;
            set
            {
                if (textSize == value)
                    return;
                textSize = value;

                textCache.Invalidate();
            }
        }

        [BackgroundDependencyLoader]
        private void load()
        {
            spaceWidth = GetTextureForCharacter('.')?.DisplayWidth * 2 ?? default_text_size;
            validateLayout();
        }

        private Bindable<string> current;

        /// <summary>
        /// Implements the <see cref="IHasCurrentValue{T}"/> interface.
        /// </summary>
        public Bindable<string> Current
        {
            get => current;
            set
            {
                if (current != null)
                    current.ValueChanged -= setText;
                if (value != null)
                {
                    value.ValueChanged += setText;
                    value.TriggerChange();
                }

                current = value;
            }
        }

        private void setText(string newText)
        {
            if (text == newText)
                return;

            text = newText ?? string.Empty;
            textCache.Invalidate();
        }

        private string text = string.Empty;

        /// <summary>
        /// Gets or sets the text to be displayed.
        /// </summary>
        public string Text
        {
            get => text;
            set
            {
                if (current != null)
                    throw new InvalidOperationException($@"property {nameof(Text)} cannot be set manually if {nameof(Current)} set");

                setText(value);
            }
        }

        private float? constantWidth;
        /// <summary>
        /// True if all characters should be spaced apart the same distance.
        /// </summary>
        public bool FixedWidth;

        protected override void Update()
        {
            base.Update();
            validateLayout();
        }

        private void validateLayout()
        {
            if (!textCache.IsValid)
            {
                computeLayout();
                textCache.Validate();
            }

            if (!shadowCache.IsValid)
            {
                computeShadowColour();
                shadowCache.Validate();
            }
        }

        public override bool Invalidate(Invalidation invalidation = Invalidation.All, Drawable source = null, bool shallPropagate = true)
        {
            if ((invalidation & Invalidation.Colour) > 0 && Shadow)
                shadowCache.Invalidate();

            return base.Invalidate(invalidation, source, shallPropagate);
        }

        private string lastText;
        private string lastFont;

        private void computeLayout()
        {
            //we can't keep existing drawables if our shadow has changed, as the shadow is applied in the add-loop.
            //this could potentially be optimised if necessary.
            bool allowKeepingExistingDrawables = font == lastFont;

            lastFont = font;

            //keep sprites which haven't changed since last layout.
            List<Drawable> keepDrawables = new List<Drawable>();

            if (allowKeepingExistingDrawables)
            {
                if (lastText == text)
                {
                    Children.ForEach(c => c.Scale = new Vector2(TextSize));
                    return;
                }

                int length = Math.Min(lastText?.Length ?? 0, text.Length);
                keepDrawables.AddRange(Children.TakeWhile((n, i) => i < length && lastText[i] == text[i]));
                RemoveRange(keepDrawables); //doesn't dispose
            }

            Clear();

            if (text.Length == 0)
            {
                lastText = string.Empty;

                // We're going to become not present, so parents need to be signalled to recompute size/layout
                Invalidate(InvalidationFromParentSize | Invalidation.Colour);

                return;
            }

            if (FixedWidth && !constantWidth.HasValue)
                constantWidth = GetTextureForCharacter('D').DisplayWidth;

            foreach (var k in keepDrawables)
            {
                k.Scale = new Vector2(TextSize);
                Add(k);
            }

            for (int index = keepDrawables.Count; index < text.Length; index++)
            {
                char c = text[index];

                bool fixedWidth = FixedWidth && !FixedWidthExceptionCharacters.Contains(c);

                Drawable characterDrawable;

                if (char.IsWhiteSpace(c))
                {
                    float width = fixedWidth ? constantWidth.GetValueOrDefault() : spaceWidth;

                    switch ((int)c)
                    {
                        case 0x3000: //double-width space
                            width *= 2;
                            break;
                    }

                    characterDrawable = new CharacterContainer
                    {
                        Size = new Vector2(width),
                        Scale = new Vector2(TextSize),
                        Colour = Color4.Transparent,
                    };
                }
                else
                {
                    characterDrawable = CreateCharacterDrawable(c);

                    if (fixedWidth)
                    {
                        characterDrawable.Anchor = Anchor.TopCentre;
                        characterDrawable.Origin = Anchor.TopCentre;
                    }

                    Drawable shadowDrawable = null;

                    if (shadow)
                    {
                        shadowDrawable = CreateCharacterDrawable(c);
                        shadowDrawable.Position = new Vector2(0, 0.06f);
                        shadowDrawable.Anchor = characterDrawable.Anchor;
                        shadowDrawable.Origin = characterDrawable.Origin;
                        shadowDrawable.Depth = float.MaxValue;
                    }

                    characterDrawable = new CharacterContainer(characterDrawable, shadowDrawable)
                    {
                        Size = new Vector2(fixedWidth ? constantWidth.GetValueOrDefault() : characterDrawable.DrawSize.X, UseFullGlyphHeight ? 1 : characterDrawable.DrawSize.Y),
                        Scale = new Vector2(TextSize),
                    };
                }

                Add(characterDrawable);
            }

            if (shadow)
                shadowCache.Invalidate();

            lastText = text;
        }

        private void computeShadowColour()
        {
            if (!shadow)
                return;

            //adjust shadow alpha based on highest component intensity to avoid muddy display of darker text.
            //squared result for quadratic fall-off seems to give the best result.
            var avgColour = (Color4)DrawInfo.Colour.AverageColour;
            float shadowAlpha = (float)Math.Pow(Math.Max(Math.Max(avgColour.R, avgColour.G), avgColour.B), 2);

            foreach (var character in Children.Cast<CharacterContainer>())
            {
                if (character.Shadow == null)
                    continue;

                character.Shadow.Alpha = shadowAlpha;
                character.Shadow.Colour = shadowColour;
            }
        }

        /// <summary>
        /// Creates a <see cref="Drawable"/> to use if the current font does not have a texture for a character.
        /// </summary>
        /// <returns>The <see cref="Drawable"/> to use if the current font does not have a texture for a character.</returns>
        protected virtual Drawable CreateFallbackCharacterDrawable() => new Box
        {
            Origin = Anchor.Centre,
            Anchor = Anchor.Centre,
            Scale = new Vector2(0.7f)
        };

        /// <summary>
        /// Creates a <see cref="Drawable"/> to use for a given character.
        /// </summary>
        /// <param name="c">The character the drawable should be created for.</param>
        /// <returns>The <see cref="Drawable"/> created for the given character.</returns>
        protected virtual Drawable CreateCharacterDrawable(char c)
        {
            var tex = GetTextureForCharacter(c);
            if (tex != null)
                return new Sprite { Texture = tex };

            return CreateFallbackCharacterDrawable();
        }

        /// <summary>
        /// Gets the texture for the given character.
        /// </summary>
        /// <param name="c">The character to get the texture for.</param>
        /// <returns>The texture for the given character.</returns>
        protected Texture GetTextureForCharacter(char c)
        {
            if (store == null)
                return null;

            return store.Get(getTextureName(c)) ?? store.Get(getTextureName(c, false));
        }

        private string getTextureName(char c, bool useFont = true) => !useFont || string.IsNullOrEmpty(Font) ? c.ToString() : $@"{Font}/{c}";

        public override string ToString()
        {
            return $@"""{Text}"" " + base.ToString();
        }

        private class CharacterContainer : CompositeDrawable
        {
            public readonly Drawable Shadow;

            public CharacterContainer(Drawable character = null, Drawable shadow = null)
            {
                Shadow = shadow;

                if (character != null)
                    AddInternal(character);

                if (shadow != null)
                    AddInternal(shadow);
            }
        }
    }
}
