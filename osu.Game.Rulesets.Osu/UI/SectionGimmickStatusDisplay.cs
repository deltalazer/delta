// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Localisation;
using osu.Game.Beatmaps.SectionGimmicks;
using osu.Game.Graphics;
using osu.Game.Graphics.Sprites;
using osu.Game.Rulesets.Osu.Scoring;
using osu.Game.Rulesets.Scoring;
using osu.Game.Screens.Play;
using osu.Game.Skinning;

namespace osu.Game.Rulesets.Osu.UI
{
    public partial class SectionGimmickStatusDisplay : CompositeDrawable, ISerialisableDrawable
    {
        public bool UsesFixedAnchor { get; set; } = true;

        [Resolved(canBeNull: true)]
        private HealthProcessor? healthProcessor { get; set; }

        [Resolved(canBeNull: true)]
        private IGameplayClock? gameplayClock { get; set; }

        private OsuSpriteText statusText = null!;

        [BackgroundDependencyLoader]
        private void load()
        {
            AutoSizeAxes = Axes.Both;

            InternalChild = statusText = new OsuSpriteText
            {
                Font = OsuFont.GetFont(size: 22, weight: FontWeight.Bold),
                Shadow = true,
            };
        }

        protected override void Update()
        {
            base.Update();

            if (gameplayClock?.CurrentTime < 0)
            {
                statusText.Text = string.Empty;
                return;
            }

            if (healthProcessor is not SectionGimmickHealthProcessor sectionHealthProcessor)
            {
                statusText.Text = string.Empty;
                return;
            }

            var active = sectionHealthProcessor.ActiveSection;
            if (active == null)
            {
                statusText.Text = string.Empty;
                return;
            }

            LocalisableString text = buildLabel(active);
            if (!EqualityComparer<LocalisableString>.Default.Equals(statusText.Text, text))
                statusText.Text = text;

            var settings = active.Settings;
            if (statusText.Colour != settings.DisplayColor)
                statusText.Colour = settings.DisplayColor;
        }

        private static string buildLabel(SectionGimmickSection section)
        {
            var settings = section.Settings;

            string sectionName = string.IsNullOrEmpty(settings.SectionName)
                ? $"Section {section.Id}"
                : settings.SectionName;

            var tags = new List<string>();

            if (settings.ForceHidden)
                tags.Add("HD");
            if (settings.ForceNoApproachCircle)
                tags.Add("NoAC");
            if (settings.ForceHardRock)
                tags.Add("HR");
            if (settings.ForceFlashlight)
                tags.Add("FL");
            if (settings.ForceDoubleTime)
                tags.Add("DT");
            if (settings.ForceSingleTap)
                tags.Add("SG");
            if (settings.ForceAlternate)
                tags.Add("AL");

            // Fun mods
            if (settings.ForceTransform)
                tags.Add("TR");
            if (settings.ForceWiggle)
                tags.Add("WG");
            if (settings.ForceSpinIn)
                tags.Add("SI");
            if (settings.ForceGrow)
                tags.Add("GR");
            if (settings.ForceDeflate)
                tags.Add("DF");
            if (settings.ForceBarrelRoll)
                tags.Add("BR");
            if (settings.ForceApproachDifferent)
                tags.Add("AD");
            if (settings.ForceMuted)
                tags.Add("MU");
            if (settings.ForceNoScope)
                tags.Add("NS");
            if (settings.ForceTraceable)
                tags.Add("TC");
            if (settings.ForceMagnetised)
                tags.Add("MG");
            if (settings.ForceRepel)
                tags.Add("RP");
            if (settings.ForceFreezeFrame)
                tags.Add("FF");
            if (settings.ForceBubbles)
                tags.Add("BL");
            if (settings.ForceSynesthesia)
                tags.Add("SY");
            if (settings.ForceDepth)
                tags.Add("DP");
            if (settings.ForceBloom)
                tags.Add("BM");

            if (tags.Count > 0)
                return $"{sectionName} ({string.Join(", ", tags)})";

            return sectionName;
        }
    }
}
