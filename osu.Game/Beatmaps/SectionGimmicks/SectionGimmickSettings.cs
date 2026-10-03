// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Game.Beatmaps.HitObjectGimmicks;
using osuTK.Graphics;

namespace osu.Game.Beatmaps.SectionGimmicks
{
    public class SectionGimmickSettings
    {
        public bool EnableHPGimmick { get; set; }
        public bool EnableNoMiss { get; set; }
        public bool ForceAllMiss { get; set; }
        public bool FreezeHP { get; set; } = true;
        public bool FreezeAccuracy { get; set; } = true;
        public bool FreezeCombo { get; set; } = true;
        public bool EnableAccuracyRequirement { get; set; }
        public float RequiredAccuracy { get; set; } = float.NaN;
        public bool EnableCountLimits { get; set; }
        public bool EnableNoMissedSliderEnd { get; set; }
        public bool EnableGreatOffsetPenalty { get; set; }

        public int Max300s { get; set; } = -1;
        public int Max100s { get; set; } = -1;
        public int Max50s { get; set; } = -1;
        public int MaxMisses { get; set; } = -1;

        public bool Max300sAffectsSliderEndsAndTicks { get; set; }
        public bool Max100sAffectsSliderEndsAndTicks { get; set; }
        public bool Max50sAffectsSliderEndsAndTicks { get; set; }
        public bool MaxMissesAffectsSliderEndAndTickMisses { get; set; }

        public float HP300 { get; set; } = float.NaN;
        public float HP100 { get; set; } = float.NaN;
        public float HP50 { get; set; } = float.NaN;
        public float HPMiss { get; set; } = float.NaN;

        public float HPStart { get; set; } = float.NaN;
        public float HPCap { get; set; } = float.NaN;

        public bool HP300AffectsSliderEndsAndTicks { get; set; }
        public bool HP100AffectsSliderEndsAndTicks { get; set; }
        public bool HP50AffectsSliderEndsAndTicks { get; set; }
        public bool HPMissAffectsSliderEndAndTickMisses { get; set; }

        public bool NoDrain { get; set; }
        public bool ReverseHP { get; set; }

        public float GreatOffsetThresholdMs { get; set; } = -1;
        public float GreatOffsetPenaltyHP { get; set; } = float.NaN;

        public bool EnableDifficultyOverrides { get; set; }
        public bool AllowUnsafeDifficultyOverrideValues { get; set; }
        public bool DifficultyOverrideStartWithBeatmapValues { get; set; }
        public bool EnableGradualDifficultyChange { get; set; }
        public float GradualDifficultyChangeEndTimeMs { get; set; } = float.NaN;
        public bool KeepDifficultyOverridesAfterSection { get; set; }
        public float SectionCircleSize { get; set; } = float.NaN;
        public bool EnableSectionCircleSizeWindow { get; set; }
        public float SectionCircleSizeStartTimeMs { get; set; } = -1;
        public float SectionCircleSizeEndTimeMs { get; set; } = -1;
        public bool EnableGradualSectionCircleSizeChange { get; set; }
        public float SectionApproachRate { get; set; } = float.NaN;
        public bool EnableSectionApproachRateWindow { get; set; }
        public float SectionApproachRateStartTimeMs { get; set; } = -1;
        public float SectionApproachRateEndTimeMs { get; set; } = -1;
        public bool EnableGradualSectionApproachRateChange { get; set; }
        public float SectionOverallDifficulty { get; set; } = float.NaN;
        public bool EnableSectionOverallDifficultyWindow { get; set; }
        public float SectionOverallDifficultyStartTimeMs { get; set; } = -1;
        public float SectionOverallDifficultyEndTimeMs { get; set; } = -1;
        public bool EnableGradualSectionOverallDifficultyChange { get; set; }
        public bool AllowUnsafeStackLeniencyOverrideValues { get; set; }
        public float SectionStackLeniency { get; set; } = float.NaN;
        public bool AllowUnsafeTickRateOverrideValues { get; set; }
        public double SectionTickRate { get; set; } = double.NaN;

        public bool ForceHidden { get; set; }
        public bool ForceNoApproachCircle { get; set; }
        public bool ForceHardRock { get; set; }
        public bool ForceFlashlight { get; set; }
        public float FlashlightRadius { get; set; } = float.NaN;
        public bool EnableGradualFlashlightRadiusChange { get; set; }
        public bool EnableGradualFlashlightFadeIn { get; set; }
        public float GradualFlashlightRadiusEndTimeMs { get; set; } = float.NaN;
        public bool ForceDoubleTime { get; set; }
        public bool ForceSingleTap { get; set; }
        public bool ForceAlternate { get; set; }

        // Fun mods (non-time altering)
        public bool ForceTransform { get; set; }
        public bool ForceWiggle { get; set; }
        public bool ForceSpinIn { get; set; }
        public bool ForceGrow { get; set; }
        public bool ForceDeflate { get; set; }
        public bool ForceBarrelRoll { get; set; }
        public bool ForceApproachDifferent { get; set; }
        public bool ForceMuted { get; set; }
        public bool ForceNoScope { get; set; }
        public bool ForceMagnetised { get; set; }
        public bool ForceRepel { get; set; }
        public bool ForceFreezeFrame { get; set; }
        public bool ForceBubbles { get; set; }
        public bool ForceSynesthesia { get; set; }
        public bool ForceDepth { get; set; }
        public bool ForceBloom { get; set; }
        public bool ForceTraceable { get; set; }

        // Fun mod adjustable values
        public float WiggleStrength { get; set; } = 1.0f;
        public float GrowStartScale { get; set; } = 0.5f;
        public float DeflateStartScale { get; set; } = 2.0f;
        public float ApproachDifferentScale { get; set; } = 4.0f;
        public int NoScopeHiddenComboCount { get; set; } = 10;
        public float MagnetisedAttractionStrength { get; set; } = 0.5f;
        public float RepelRepulsionStrength { get; set; } = 0.5f;
        public float DepthMaxDepth { get; set; } = 100.0f;
        public int BloomMaxSizeComboCount { get; set; } = 50;
        public float BloomMaxCursorSize { get; set; } = 10.0f;
        public double BarrelRollSpinSpeed { get; set; } = 0.5;
        public int MutedMuteComboCount { get; set; } = 100;

        public string SectionName { get; set; } = string.Empty;
        public Color4 DisplayColor { get; set; } = Color4.White;
    }
}
