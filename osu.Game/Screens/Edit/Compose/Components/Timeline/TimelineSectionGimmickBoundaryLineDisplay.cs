// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Linq;
using osu.Framework.Allocation;
using osu.Framework.Extensions.Color4Extensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Game.Graphics;
using osu.Game.Screens.Edit.Components.Timelines.Summary.Parts;
using osuTK.Graphics;

namespace osu.Game.Screens.Edit.Compose.Components.Timeline
{
    /// <summary>
    /// Draws explicit section boundary lines (start and end) exactly like timeline marker lines.
    /// This is intentionally simple and high-contrast to guarantee visibility.
    /// </summary>
    public partial class TimelineSectionGimmickBoundaryLineDisplay : TimelinePart
    {
        private string? lastSignature;

        [Resolved]
        private OsuColour colours { get; set; } = null!;

        protected override void Update()
        {
            base.Update();

            string signature = buildSignature();

            if (signature != lastSignature)
                recreate();

            lastSignature = signature;
        }

        private void recreate()
        {
            Clear();

            var sections = EditorBeatmap.SectionGimmicks.Sections;

            if (sections.Count == 0)
                return;

            var usedEndTimes = new HashSet<double>();

            foreach (var section in sections.OrderBy(s => s.StartTime))
            {
                addBoundaryLine(section.StartTime, colours.Yellow, 4f, 1f);

                if (section.EndTime >= 0 && usedEndTimes.Add(section.EndTime))
                    addBoundaryLine(section.EndTime, colours.Orange1, 3f, 0.9f);
            }
        }

        private string buildSignature()
        {
            var sections = EditorBeatmap.SectionGimmicks.Sections;

            if (sections.Count == 0)
                return string.Empty;

            return string.Join("|", sections.OrderBy(s => s.StartTime)
                                              .Select(s => $"{s.Id}:{s.StartTime:F3}:{s.EndTime:F3}:{s.Settings.DisplayColor.ToHex()}:{s.Settings.SectionName}"));
        }

        private void addBoundaryLine(double time, Color4 colour, float width, float alpha)
        {
            Add(new Container
            {
                RelativePositionAxes = Axes.X,
                RelativeSizeAxes = Axes.Y,
                X = (float)time,
                Width = 1,
                Children = new Drawable[]
                {
                    new Box
                    {
                        RelativeSizeAxes = Axes.Y,
                        Width = width + 2,
                        Anchor = Anchor.Centre,
                        Origin = Anchor.Centre,
                        Colour = Color4.Black,
                        Alpha = Math.Min(0.7f, alpha * 0.7f),
                    },
                    new Box
                    {
                        RelativeSizeAxes = Axes.Y,
                        Width = width,
                        Anchor = Anchor.Centre,
                        Origin = Anchor.Centre,
                        Colour = colour,
                        Alpha = alpha,
                    },
                }
            });
        }
    }
}
