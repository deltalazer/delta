// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Localisation;
using osu.Game.Configuration;
using osu.Game.Beatmaps.SectionGimmicks;
using osu.Game.Beatmaps;
using osu.Game.Graphics;
using osu.Game.Graphics.Sprites;
using osu.Game.Rulesets.Osu.Scoring;
using osu.Game.Rulesets.Scoring;
using osu.Game.Screens.Play;
using osu.Game.Skinning;

namespace osu.Game.Rulesets.Osu.UI
{
    public partial class SectionGimmickDetailsDisplay : CompositeDrawable, ISerialisableDrawable
    {
        public bool UsesFixedAnchor { get; set; } = true;

        [SettingSource("Details layout", "Choose whether details are shown in one line or multiple lines.")]
        public Bindable<DetailsLayoutMode> LayoutMode { get; } = new Bindable<DetailsLayoutMode>(DetailsLayoutMode.SingleLine);

        [Resolved(canBeNull: true)]
        private HealthProcessor? healthProcessor { get; set; }

        [Resolved(canBeNull: true)]
        private IGameplayClock? gameplayClock { get; set; }

        [Resolved(canBeNull: true)]
        private IBindable<WorkingBeatmap>? workingBeatmap { get; set; }

        private OsuSpriteText detailsText = null!;

        [BackgroundDependencyLoader]
        private void load()
        {
            AutoSizeAxes = Axes.Both;

            InternalChild = detailsText = new OsuSpriteText
            {
                Font = OsuFont.GetFont(size: 18, weight: FontWeight.Regular),
                Shadow = true,
            };
        }

        protected override void Update()
        {
            base.Update();

            if (gameplayClock?.CurrentTime < 0)
            {
                detailsText.Text = string.Empty;
                return;
            }

            if (healthProcessor is not SectionGimmickHealthProcessor sectionHealthProcessor)
            {
                detailsText.Text = string.Empty;
                return;
            }

            var active = sectionHealthProcessor.ActiveSection;
            if (active == null)
            {
                detailsText.Text = string.Empty;
                return;
            }

            LocalisableString text = buildDetailsLabelForTest(active, new BindableBool(LayoutMode.Value == DetailsLayoutMode.MultiLine), workingBeatmap?.Value?.Beatmap?.Difficulty);
            if (!EqualityComparer<LocalisableString>.Default.Equals(detailsText.Text, text))
                detailsText.Text = text;

            var settings = active.Settings;
            if (detailsText.Colour != settings.DisplayColor)
                detailsText.Colour = settings.DisplayColor;
        }

        // Kept non-public for tests via reflection.
        private static string buildDetailsLabelForTest(SectionGimmickSection section, BindableBool multiline, IBeatmapDifficultyInfo? difficulty = null)
        {
            var settings = section.Settings;
            var details = new List<string>();

            if (settings.EnableHPGimmick)
            {
                if (!float.IsNaN(settings.HP300))
                    details.Add($"300: {settings.HP300:+0.###;-0.###}");
                if (!float.IsNaN(settings.HP100))
                    details.Add($"100: {settings.HP100:+0.###;-0.###}");
                if (!float.IsNaN(settings.HP50))
                    details.Add($"50: {settings.HP50:+0.###;-0.###}");
                if (!float.IsNaN(settings.HPMiss))
                    details.Add($"Miss: {settings.HPMiss:+0.###;-0.###}");
                if (settings.NoDrain)
                    details.Add("NoDrain");
                if (settings.ReverseHP)
                    details.Add("ReverseHP");
            }

            if (settings.EnableNoMiss)
                details.Add("NoMiss");

            if (settings.EnableAccuracyRequirement)
                details.Add($"ReqAcc: {(Math.Clamp(settings.RequiredAccuracy, 0f, 1f) * 100f):0.##}%");

            if (settings.EnableCountLimits)
            {
                if (settings.Max300s >= 0)
                    details.Add($"Max300: {settings.Max300s}");
                if (settings.Max100s >= 0)
                    details.Add($"Max100: {settings.Max100s}");
                if (settings.Max50s >= 0)
                    details.Add($"Max50: {settings.Max50s}");
                if (settings.MaxMisses >= 0)
                    details.Add($"MaxMiss: {settings.MaxMisses}");
            }

            if (settings.EnableNoMissedSliderEnd)
                details.Add("NoMissedSliderEnd");

            if (settings.EnableGreatOffsetPenalty)
            {
                if (settings.GreatOffsetThresholdMs >= 0)
                    details.Add($"OffsetThreshold: {settings.GreatOffsetThresholdMs}ms");
                double? effectiveOd = null;
                if (settings.EnableDifficultyOverrides && !float.IsNaN(settings.SectionOverallDifficulty))
                    effectiveOd = settings.SectionOverallDifficulty;
                else if (difficulty != null)
                    effectiveOd = difficulty.OverallDifficulty;

                if (effectiveOd != null)
                {
                    var hitWindows = new OsuHitWindows();
                    hitWindows.SetDifficulty(effectiveOd.Value);
                    double perfectWindow = hitWindows.WindowFor(HitResult.Great);
                    details.Add($"Perfect(300)@OD{effectiveOd.Value:0.##}: ±{perfectWindow:0.###}ms");
                }
                if (!float.IsNaN(settings.GreatOffsetPenaltyHP))
                    details.Add($"OffsetPenalty: {settings.GreatOffsetPenaltyHP:+0.###;-0.###}");
            }

            if (settings.EnableDifficultyOverrides)
            {
                if (settings.EnableGradualDifficultyChange && !float.IsNaN(settings.GradualDifficultyChangeEndTimeMs))
                    details.Add($"GradualTo: {settings.GradualDifficultyChangeEndTimeMs:0.###}ms");
                if (settings.KeepDifficultyOverridesAfterSection)
                    details.Add("KeepAfterSection");

                if (!float.IsNaN(settings.SectionCircleSize))
                    details.Add($"CS: {settings.SectionCircleSize:0.###}");
                if (!float.IsNaN(settings.SectionApproachRate))
                    details.Add($"AR: {settings.SectionApproachRate:0.###}");
                if (!float.IsNaN(settings.SectionOverallDifficulty))
                    details.Add($"OD: {settings.SectionOverallDifficulty:0.###}");

                // Display forced mods
                if (settings.ForceHidden)
                    details.Add("HD");
                if (settings.ForceNoApproachCircle)
                    details.Add("NoApproachCircle");
                if (settings.ForceHardRock)
                    details.Add("HR");
                if (settings.ForceFlashlight)
                    details.Add("FL");
                if (settings.ForceDoubleTime)
                    details.Add("DT");
                if (settings.ForceSingleTap)
                    details.Add("SG");
                if (settings.ForceAlternate)
                    details.Add("AL");

                // Fun mods
                if (settings.ForceTransform)
                    details.Add("TR");
                if (settings.ForceWiggle)
                    details.Add("WG");
                if (settings.ForceSpinIn)
                    details.Add("SI");
                if (settings.ForceGrow)
                    details.Add("GR");
                if (settings.ForceDeflate)
                    details.Add("DF");
                if (settings.ForceBarrelRoll)
                    details.Add("BR");
                if (settings.ForceApproachDifferent)
                    details.Add("AD");
                if (settings.ForceMuted)
                    details.Add("MU");
                if (settings.ForceNoScope)
                    details.Add("NS");
                if (settings.ForceTraceable)
                    details.Add("TC");
                if (settings.ForceMagnetised)
                    details.Add("MG");
                if (settings.ForceRepel)
                    details.Add("RP");
                if (settings.ForceFreezeFrame)
                    details.Add("FF");
                if (settings.ForceBubbles)
                    details.Add("BL");
                if (settings.ForceSynesthesia)
                    details.Add("SY");
                if (settings.ForceDepth)
                    details.Add("DP");
                if (settings.ForceBloom)
                    details.Add("BM");
            }

            if (details.Count == 0)
                return string.Empty;

            return string.Join(multiline.Value ? "\n" : " | ", details);
        }

        public enum DetailsLayoutMode
        {
            SingleLine,
            MultiLine,
        }
    }
}
