// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Game.Beatmaps.HitObjectGimmicks;
using osu.Game.Rulesets.Objects;
using osu.Game.Rulesets.Osu.Objects;

namespace osu.Game.Rulesets.Osu.Scoring
{
    internal static class FakeHitObjectPunishmentHelper
    {
        public static bool IsFakeHitObject(HitObject hitObject)
            => hitObject is FakeHitCircle
               || hitObject is FakeSlider
               || (hitObject is SliderHeadCircle head && head.Slider is FakeSlider)
               || (hitObject is SliderEndCircle end && end.ParentSlider is FakeSlider)
               || (hitObject is SliderTick tick && tick.Slider is FakeSlider);

        public static bool IsPrimaryFakeHitObject(HitObject hitObject)
            => hitObject is FakeHitCircle || hitObject is FakeSlider;

        public static FakePunishMode ResolvePunishMode(HitObject hitObject)
            => hitObject switch
            {
                FakeHitCircle fakeCircle => fakeCircle.FakePunishMode,
                FakeSlider fakeSlider => fakeSlider.FakePunishMode,
                SliderHeadCircle { Slider: FakeSlider fakeSlider } => fakeSlider.FakePunishMode,
                SliderEndCircle { ParentSlider: FakeSlider fakeSlider } => fakeSlider.FakePunishMode,
                SliderTick { Slider: FakeSlider fakeSlider } => fakeSlider.FakePunishMode,
                _ => FakePunishMode.None,
            };

        public static bool ShouldPunishAsMiss(FakePunishMode mode)
            => mode == FakePunishMode.Miss;
    }
}
