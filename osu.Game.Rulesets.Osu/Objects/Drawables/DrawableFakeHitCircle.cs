// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Game.Rulesets.Objects.Drawables;
using osu.Game.Rulesets.Osu.Judgements;
using osu.Game.Rulesets.Osu.Scoring;
using osu.Game.Rulesets.Osu.UI;
using osu.Game.Rulesets.Scoring;
using osuTK.Graphics;

namespace osu.Game.Rulesets.Osu.Objects.Drawables
{
    public partial class DrawableFakeHitCircle : DrawableHitCircle
    {
        private bool displayPunishJudgement;
        private bool autoHitTriggered;

        public new FakeHitCircle HitObject => (FakeHitCircle)base.HitObject;

        public DrawableFakeHitCircle()
            : this(null)
        {
        }

        public DrawableFakeHitCircle(FakeHitCircle? h)
            : base(h)
        {
        }

        public override bool DisplayResult => displayPunishJudgement;

        protected override void Update()
        {
            base.Update();

            if (!autoHitTriggered
                && HitObject.FakeAutoHitOnApproachClose
                && Time.Current >= HitObject.StartTime
                && !Judged)
            {
                autoHitTriggered = true;
                displayPunishJudgement = false;
                ApplyResult(static (r, _) => ((OsuHitCircleJudgementResult)r).Type = HitResult.IgnoreHit, 0);

                if (HitObject.FakeAutoHitPlayHitsound)
                    base.PlaySamples();
            }

            updateFakeRevealTint();
        }

        protected override void CheckForResult(bool userTriggered, double timeOffset)
        {
            if (autoHitTriggered)
                return;

            if (!userTriggered)
            {
                if (!HitObject.HitWindows.CanBeHit(timeOffset))
                {
                    displayPunishJudgement = false;

                    // Fake circles never punish on timeout; play miss-like animation without miss symbol.
                    ApplyResult(static (r, _) => ((OsuHitCircleJudgementResult)r).Type = HitResult.IgnoreMiss, 0);
                }

                return;
            }

            var result = ResultFor(timeOffset);
            var clickAction = CheckHittable?.Invoke(this, Time.Current, result);

            if (clickAction == ClickAction.Shake)
                Shake();

            if (result == HitResult.None || clickAction != ClickAction.Hit)
                return;

            if (FakeHitObjectPunishmentHelper.ShouldPunishAsMiss(HitObject.FakePunishMode))
            {
                displayPunishJudgement = true;
                ApplyResult(static (r, _) => ((OsuHitCircleJudgementResult)r).Type = HitResult.Miss, 0);
                UpdateState(ArmedState.Hit, true);
            }
            else
            {
                displayPunishJudgement = false;
                ApplyResult(static (r, _) => ((OsuHitCircleJudgementResult)r).Type = HitResult.IgnoreHit, 0);
            }
        }

        protected override void OnFree()
        {
            autoHitTriggered = false;
            displayPunishJudgement = false;
            Colour = Color4.White;
            base.OnFree();
        }

        public override void PlaySamples()
        {
            if (HitObject.FakePlayHitsound)
                base.PlaySamples();
        }

        private void updateFakeRevealTint()
        {
            if (!HitObject.FakeRevealEnabled)
            {
                Colour = Color4.White;
                return;
            }

            double startTime = HitObject.StartTime;
            double sequenceStart = startTime - HitObject.FakeRevealLeadInStartMs;
            double fadeInEnd = sequenceStart + HitObject.FakeRevealLeadInLengthMs;
            double fadeOutStart = startTime - HitObject.FakeRevealFadeOutStartMs;
            double fadeOutEnd = fadeOutStart + HitObject.FakeRevealFadeOutLengthMs;
            double time = Time.Current;

            float revealAlpha;

            if (time < sequenceStart || time > fadeOutEnd)
                revealAlpha = 0;
            else if (time <= fadeInEnd && HitObject.FakeRevealLeadInLengthMs > 0)
                revealAlpha = (float)Math.Clamp((time - sequenceStart) / HitObject.FakeRevealLeadInLengthMs, 0, 1);
            else if (time < fadeOutStart)
                revealAlpha = 1;
            else if (HitObject.FakeRevealFadeOutLengthMs > 0)
                revealAlpha = 1f - (float)Math.Clamp((time - fadeOutStart) / HitObject.FakeRevealFadeOutLengthMs, 0, 1);
            else
                revealAlpha = 0;

            float blend = Math.Clamp(HitObject.FakeRevealStrength * revealAlpha, 0, 1);
            var revealColour = new Color4(HitObject.FakeRevealRed, HitObject.FakeRevealGreen, HitObject.FakeRevealBlue, 1);
            Colour = new Color4(
                Color4.White.R + (revealColour.R - Color4.White.R) * blend,
                Color4.White.G + (revealColour.G - Color4.White.G) * blend,
                Color4.White.B + (revealColour.B - Color4.White.B) * blend,
                1);
        }
    }
}
