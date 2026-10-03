// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osuTK.Graphics;

namespace osu.Game.Rulesets.Osu.Objects.Drawables
{
    public partial class DrawableFakeSlider : DrawableSlider
    {
        public new FakeSlider HitObject => (FakeSlider)base.HitObject;

        public DrawableFakeSlider()
            : this(null)
        {
        }

        public DrawableFakeSlider(FakeSlider? h)
            : base(h)
        {
        }

        protected override void Update()
        {
            base.Update();
            updateFakeRevealTint();
        }

        protected override void OnFree()
        {
            Colour = Color4.White;
            base.OnFree();
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
