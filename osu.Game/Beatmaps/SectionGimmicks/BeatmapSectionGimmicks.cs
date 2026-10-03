// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;

namespace osu.Game.Beatmaps.SectionGimmicks
{
    public class BeatmapSectionGimmicks
    {
        public List<SectionGimmickSection> Sections { get; set; } = new List<SectionGimmickSection>();

        public SectionGimmickSection? FindSectionAt(double time)
        {
            SectionGimmickSection? best = null;

            foreach (var section in Sections)
            {
                if (!section.Contains(time))
                    continue;

                if (best == null || section.StartTime > best.StartTime)
                    best = section;
            }

            return best;
        }
    }
}
