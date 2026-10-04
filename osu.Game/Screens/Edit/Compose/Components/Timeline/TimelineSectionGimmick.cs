// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Allocation;
using osu.Framework.Extensions.Color4Extensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Game.Beatmaps.SectionGimmicks;
using osu.Game.Graphics;
using osu.Game.Graphics.Sprites;
using osuTK.Graphics;

namespace osu.Game.Screens.Edit.Compose.Components.Timeline
{
    public partial class TimelineSectionGimmick : CompositeDrawable
    {
        public SectionGimmickSection Section { get; }

        public Action<int>? OnSelected { get; init; }

        private Box background = null!;
        private OsuSpriteText label = null!;

        private Color4 baseColour;

        private bool isSelected;

        public bool IsSelected => isSelected;

        public TimelineSectionGimmick(SectionGimmickSection section)
        {
            Section = section;
        }

        [BackgroundDependencyLoader]
        private void load(OsuColour colours)
        {
            RelativePositionAxes = Axes.X;
            RelativeSizeAxes = Axes.Y;
            Origin = Anchor.TopLeft;
            Width = (float)Math.Max(1, Section.EndTime < 0 ? 1 : Section.EndTime - Section.StartTime);
            X = (float)Section.StartTime;

            InternalChildren = new Drawable[]
            {
                background = new Box
                {
                    RelativeSizeAxes = Axes.Both,
                    Colour = baseColour = colours.Pink2,
                    Alpha = 0.45f,
                },
                new Container
                {
                    RelativeSizeAxes = Axes.Both,
                    Padding = new MarginPadding { Horizontal = 6, Vertical = 1 },
                    Child = label = new OsuSpriteText
                    {
                        Anchor = Anchor.CentreLeft,
                        Origin = Anchor.CentreLeft,
                        Text = $"Section {Section.Id}",
                        Font = OsuFont.GetFont(size: 12, weight: FontWeight.SemiBold),
                    }
                }
            };

            updateSelectedState();
        }

        protected override bool OnClick(osu.Framework.Input.Events.ClickEvent e)
        {
            OnSelected?.Invoke(Section.Id);
            return true;
        }

        protected override void Update()
        {
            base.Update();
            label.Alpha = DrawWidth >= 65 ? 1 : 0;
        }

        public void SetSelected(bool selected)
        {
            if (isSelected == selected)
                return;

            isSelected = selected;
            updateSelectedState();
        }

        private void updateSelectedState()
        {
            if (background == null)
                return;

            background.FadeColour(isSelected ? baseColour.Lighten(0.5f) : baseColour, 200, Easing.OutQuint);
            background.FadeTo(isSelected ? 0.9f : 0.45f, 200, Easing.OutQuint);
        }
    }
}
