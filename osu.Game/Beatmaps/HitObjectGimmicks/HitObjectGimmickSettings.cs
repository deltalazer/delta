// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

namespace osu.Game.Beatmaps.HitObjectGimmicks
{
    public enum FakePunishMode
    {
        None,
        Miss,
    }

    public class HitObjectGimmickSettings
    {
        public const float DEFAULT_FAKE_REVEAL_STRENGTH = 0.55f;
        public const float DEFAULT_FAKE_REVEAL_LEAD_IN_START_MS = 900;
        public const float DEFAULT_FAKE_REVEAL_LEAD_IN_LENGTH_MS = 200;
        public const float DEFAULT_FAKE_REVEAL_FADE_OUT_START_MS = 300;
        public const float DEFAULT_FAKE_REVEAL_FADE_OUT_LENGTH_MS = 250;

        public bool IsFakeNote { get; set; }
        public FakePunishMode FakePunishMode { get; set; } = FakePunishMode.None;
        public bool FakePlayHitsound { get; set; }
        public bool FakeAutoHitOnApproachClose { get; set; }
        public bool FakeAutoHitPlayHitsound { get; set; }
        public bool FakeRevealEnabled { get; set; } = true;
        public float FakeRevealRed { get; set; } = 1f;
        public float FakeRevealGreen { get; set; } = 0.3019608f;
        public float FakeRevealBlue { get; set; } = 0.3019608f;
        public float FakeRevealStrength { get; set; } = DEFAULT_FAKE_REVEAL_STRENGTH;
        public float FakeRevealLeadInStartMs { get; set; } = DEFAULT_FAKE_REVEAL_LEAD_IN_START_MS;
        public float FakeRevealLeadInLengthMs { get; set; } = DEFAULT_FAKE_REVEAL_LEAD_IN_LENGTH_MS;
        public float FakeRevealFadeOutStartMs { get; set; } = DEFAULT_FAKE_REVEAL_FADE_OUT_START_MS;
        public float FakeRevealFadeOutLengthMs { get; set; } = DEFAULT_FAKE_REVEAL_FADE_OUT_LENGTH_MS;

        public bool EnableHPGimmick { get; set; }
        public bool EnableNoMiss { get; set; }
        public bool EnableCountLimits { get; set; }
        public bool EnableGreatOffsetPenalty { get; set; }

        public bool ForceAllMiss { get; set; }
        public bool FreezeHP { get; set; } = true;
        public bool FreezeAccuracy { get; set; } = true;
        public bool FreezeCombo { get; set; } = true;

        public int Max300s { get; set; } = -1;
        public int Max100s { get; set; } = -1;
        public int Max50s { get; set; } = -1;
        public int MaxMisses { get; set; } = -1;

        public float HP300 { get; set; } = float.NaN;
        public float HP100 { get; set; } = float.NaN;
        public float HP50 { get; set; } = float.NaN;
        public float HPMiss { get; set; } = float.NaN;

        public float GreatOffsetThresholdMs { get; set; } = -1;
        public float GreatOffsetPenaltyHP { get; set; } = float.NaN;

        public bool EnableDifficultyOverrides { get; set; }
        public bool AllowUnsafeDifficultyOverrideValues { get; set; }
        public float SectionCircleSize { get; set; } = float.NaN;
        public float SectionApproachRate { get; set; } = float.NaN;
        public float SectionOverallDifficulty { get; set; } = float.NaN;
        public bool AllowUnsafeStackLeniencyOverrideValues { get; set; }
        public float SectionStackLeniency { get; set; } = float.NaN;
        public bool AllowUnsafeTickRateOverrideValues { get; set; }
        public double SectionTickRate { get; set; } = double.NaN;

        public bool ForceHidden { get; set; }
        public bool ForceNoApproachCircle { get; set; }
        public bool ForceHardRock { get; set; }
        public bool ForceFlashlight { get; set; }
        public ForcedSpinnerDirection SpinnerDirection { get; set; }
        public SpinnerWrongDirectionBehaviour SpinnerWrongDirection { get; set; }
        public SpinnerDirectionIndicator SpinnerIndicator { get; set; }
        public bool ForceTraceable { get; set; }
        public float FlashlightRadius { get; set; } = float.NaN;
    }
}
