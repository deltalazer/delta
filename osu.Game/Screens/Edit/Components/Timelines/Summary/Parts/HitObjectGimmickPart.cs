// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Linq;
using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Game.Graphics;
using osuTK.Graphics;

namespace osu.Game.Screens.Edit.Components.Timelines.Summary.Parts
{
    public partial class HitObjectGimmickPart : TimelinePart
    {
        private string? lastSignature;

        [Resolved]
        private OsuColour colours { get; set; } = null!;

        protected override void Update()
        {
            base.Update();

            string signature = buildSignature();

            if (signature == lastSignature)
                return;

            lastSignature = signature;
            recreate();
        }

        private void recreate()
        {
            Clear();

            var entries = EditorBeatmap.HitObjectGimmicks.Entries.Where(e => e.Settings?.ForceNoApproachCircle == true)
                                        .OrderBy(e => e.StartTime)
                                        .ThenBy(e => e.ComboIndexWithOffsets)
                                        .ToList();

            if (entries.Count == 0)
                return;

            foreach (var entry in entries)
                addLine(entry.StartTime, colours.Blue2, 0.95f, 4f);
        }

        private void addLine(double time, Color4 colour, float alpha, float width)
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
                        Alpha = 0.55f,
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

        private string buildSignature()
        {
            var entries = EditorBeatmap.HitObjectGimmicks.Entries.Where(e => e.Settings?.ForceNoApproachCircle == true)
                                        .OrderBy(e => e.StartTime)
                                        .ThenBy(e => e.ComboIndexWithOffsets)
                                        .ToList();

            if (entries.Count == 0)
                return string.Empty;

            return string.Join("|", entries.Select(e => $"{e.ObjectId?.ToString() ?? "legacy"}:{e.StartTime:F3}:{e.ComboIndexWithOffsets}"));
        }
    }
}
