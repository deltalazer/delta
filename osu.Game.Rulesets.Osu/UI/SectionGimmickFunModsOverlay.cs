// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Linq;
using osu.Framework.Allocation;
using osu.Framework.Audio;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Utils;
using osu.Game.Beatmaps;
using osu.Game.Beatmaps.HitObjectGimmicks;
using osu.Game.Beatmaps.SectionGimmicks;
using osu.Game.Configuration;
using osu.Game.Rulesets.Mods;
using osu.Game.Rulesets.Objects.Drawables;
using osu.Game.Rulesets.Osu.Mods;
using osu.Game.Rulesets.Osu.Objects;
using osu.Game.Rulesets.Osu.Objects.Drawables;
using osu.Game.Rulesets.Osu.Scoring;
using osu.Game.Rulesets.Osu.UI.Cursor;
using osu.Game.Rulesets.Osu.Utils;
using osu.Game.Rulesets.Scoring;
using osu.Game.Rulesets.UI;
using osu.Game.Screens.Play;
using osuTK;

namespace osu.Game.Rulesets.Osu.UI
{
    public partial class SectionGimmickFunModsOverlay : CompositeDrawable
    {
        private const float no_scope_min_alpha = 0.0002f;

        private static readonly Vector3 depth_camera_position = new Vector3(OsuPlayfield.BASE_SIZE.X * 0.5f, OsuPlayfield.BASE_SIZE.Y * 0.5f, -200);

        private readonly IBeatmap beatmap;
        private readonly BeatmapSectionGimmicks gimmicks;
        private readonly DrawableRuleset<OsuHitObject> drawableRuleset;
        private readonly IReadOnlyList<Mod> selectedMods;

        private readonly BindableDouble mutedVolumeAdjustment = new BindableDouble(1);

        private OsuModSynesthesia? synesthesiaMod;
        private OsuModBubbles? bubblesMod;

        private bool hasForcedMuted;
        private bool hasForcedBarrelRoll;
        private bool hasForcedNoScope;
        private bool hasForcedBloom;
        private bool hasForcedBubbles;

        private bool selectedTransform;
        private bool selectedWiggle;
        private bool selectedSpinIn;
        private bool selectedGrow;
        private bool selectedDeflate;
        private bool selectedApproachDifferent;
        private bool selectedFreezeFrame;
        private bool selectedSynesthesia;
        private bool selectedBubbles;
        private bool selectedMuted;
        private bool selectedBarrelRoll;
        private bool selectedNoScope;
        private bool selectedBloom;
        private bool selectedMagnetised;
        private bool selectedRepel;
        private bool selectedDepth;

        private bool hasForcedMotionEffects;

        private bool initialDisplayJudgements;
        private readonly HashSet<DrawableHitObject> processedDrawables = new HashSet<DrawableHitObject>();
        private readonly Dictionary<long, HitObjectGimmickSettings> objectSettingsById = new Dictionary<long, HitObjectGimmickSettings>();
        private readonly Dictionary<(double StartTime, int ComboIndexWithOffsets), HitObjectGimmickSettings> objectSettingsByLegacyKey = new Dictionary<(double StartTime, int ComboIndexWithOffsets), HitObjectGimmickSettings>();

        [Resolved(canBeNull: true)]
        private OsuConfigManager? config { get; set; }

        [Resolved(canBeNull: true)]
        private ScoreProcessor? scoreProcessor { get; set; }

        [Resolved(canBeNull: true)]
        private Player? player { get; set; }

        public SectionGimmickFunModsOverlay(IBeatmap beatmap, DrawableRuleset<OsuHitObject> drawableRuleset, IReadOnlyList<Mod> selectedMods)
        {
            this.beatmap = beatmap;
            this.drawableRuleset = drawableRuleset;
            this.selectedMods = selectedMods;

            gimmicks = beatmap.SectionGimmicks;
            var hitObjectGimmicks = beatmap.HitObjectGimmicks ?? new BeatmapHitObjectGimmicks();
            objectSettingsById = HitObjectGimmickBindingUtils.CreateLookupByObjectId(hitObjectGimmicks);
            objectSettingsByLegacyKey = HitObjectGimmickBindingUtils.CreateLookupByLegacyKey(hitObjectGimmicks);

            RelativeSizeAxes = Axes.Both;
        }

        [BackgroundDependencyLoader]
        private void load()
        {
            selectedTransform = hasSelectedMod<OsuModTransform>();
            selectedWiggle = hasSelectedMod<OsuModWiggle>();
            selectedSpinIn = hasSelectedMod<OsuModSpinIn>();
            selectedGrow = hasSelectedMod<OsuModGrow>();
            selectedDeflate = hasSelectedMod<OsuModDeflate>();
            selectedApproachDifferent = hasSelectedMod<OsuModApproachDifferent>();
            selectedFreezeFrame = hasSelectedMod<OsuModFreezeFrame>();
            selectedSynesthesia = hasSelectedMod<OsuModSynesthesia>();
            selectedBubbles = hasSelectedMod<OsuModBubbles>();
            selectedMuted = hasSelectedMod<OsuModMuted>();
            selectedBarrelRoll = hasSelectedMod<OsuModBarrelRoll>();
            selectedNoScope = hasSelectedMod<OsuModNoScope>();
            selectedBloom = hasSelectedMod<OsuModBloom>();
            selectedMagnetised = hasSelectedMod<OsuModMagnetised>();
            selectedRepel = hasSelectedMod<OsuModRepel>();
            selectedDepth = hasSelectedMod<OsuModDepth>();

            hasForcedMuted = hasAnyForced(s => s.ForceMuted) && !selectedMuted;
            hasForcedBarrelRoll = hasAnyForced(s => s.ForceBarrelRoll) && !selectedBarrelRoll;
            hasForcedNoScope = hasAnyForced(s => s.ForceNoScope) && !selectedNoScope;
            hasForcedBloom = hasAnyForced(s => s.ForceBloom) && !selectedBloom;
            hasForcedBubbles = hasAnyForced(s => s.ForceBubbles) && !selectedBubbles;
            hasForcedMotionEffects = (hasAnyForced(s => s.ForceMagnetised) && !selectedMagnetised)
                                     || (hasAnyForced(s => s.ForceRepel) && !selectedRepel)
                                     || (hasAnyForced(s => s.ForceDepth) && !selectedDepth);

            if (hasAnyForced(s => s.ForceSynesthesia) && !selectedSynesthesia)
            {
                synesthesiaMod = new OsuModSynesthesia();
                synesthesiaMod.ApplyToBeatmap(beatmap);
            }

            if (hasForcedBubbles)
            {
                bubblesMod = new OsuModBubbles();
                bubblesMod.ApplyToDrawableRuleset(drawableRuleset);

                if (scoreProcessor != null)
                    bubblesMod.ApplyToScoreProcessor(scoreProcessor);
            }

            if (hasForcedMuted)
                drawableRuleset.Audio.AddAdjustment(AdjustableProperty.Volume, mutedVolumeAdjustment);

            if (hasForcedMotionEffects)
            {
                if (drawableRuleset.Playfield is OsuPlayfield osuPlayfield)
                    osuPlayfield.FollowPoints.Hide();
            }

            initialDisplayJudgements = drawableRuleset.Playfield.DisplayJudgements.Value;
        }

        protected override void Update()
        {
            base.Update();

            applyDrawableModsOnce();
            updateSectionScopedGlobalEffects();
            updateMotionEffects();
        }

        private void applyDrawableModsOnce()
        {
            if (!drawableRuleset.Playfield.HitObjectContainer.AliveEntries.Any())
                return;

            foreach (var entry in drawableRuleset.Playfield.HitObjectContainer.AliveEntries)
            {
                DrawableHitObject drawable = entry.Value;

                if (!processedDrawables.Add(drawable))
                    continue;

                SectionGimmickSettings? settings = resolveSettingsForHitObject(drawable.HitObject);
                if (settings == null)
                    continue;

                if (settings.ForceTransform && !selectedTransform)
                    applyModToDrawable(new OsuModTransform(), drawable);

                if (settings.ForceWiggle && !selectedWiggle)
                {
                    var mod = new OsuModWiggle();
                    mod.Strength.Value = Math.Clamp(settings.WiggleStrength, 0.1f, 2f);
                    applyModToDrawable(mod, drawable);
                }

                if (settings.ForceSpinIn && !selectedSpinIn)
                    applyModToDrawable(new OsuModSpinIn(), drawable);

                if (settings.ForceGrow && !selectedGrow)
                {
                    var mod = new OsuModGrow();
                    mod.StartScale.Value = Math.Clamp(settings.GrowStartScale, 0f, 0.99f);
                    applyModToDrawable(mod, drawable);
                }

                if (settings.ForceDeflate && !selectedDeflate)
                {
                    var mod = new OsuModDeflate();
                    mod.StartScale.Value = Math.Clamp(settings.DeflateStartScale, 1f, 25f);
                    applyModToDrawable(mod, drawable);
                }

                if (settings.ForceApproachDifferent && !selectedApproachDifferent)
                {
                    var mod = new OsuModApproachDifferent();
                    mod.Scale.Value = Math.Clamp(settings.ApproachDifferentScale, 1.5f, 10f);
                    applyModToDrawable(mod, drawable);
                }

                if (settings.ForceSynesthesia && synesthesiaMod != null)
                    synesthesiaMod.ApplyToDrawableHitObject(drawable);

                if (settings.ForceBubbles && bubblesMod != null)
                    bubblesMod.ApplyToDrawableHitObject(drawable);

                if (settings.ForceFreezeFrame && !selectedFreezeFrame)
                    applyCustomFreezeFrame(drawable);
            }
        }

        private void updateSectionScopedGlobalEffects()
        {
            SectionGimmickSettings? currentSettings = resolveSettingsAtTime(Time.Current);

            updateBarrelRoll(currentSettings);
            updateMuted(currentSettings);
            updateNoScope(currentSettings);
            updateBloom(currentSettings);
            updateBubblesJudgementVisibility(currentSettings);
        }

        private void updateBarrelRoll(SectionGimmickSettings? settings)
        {
            if (!hasForcedBarrelRoll)
                return;

            bool active = settings?.ForceBarrelRoll == true;

            if (active)
            {
                double spinSpeed = Math.Clamp(settings!.BarrelRollSpinSpeed, 0.02, 12);
                float rotation = 360f * (float)(drawableRuleset.Playfield.Time.Current / 60000d * spinSpeed);

                drawableRuleset.PlayfieldAdjustmentContainer.Rotation = rotation;

                Vector2 playfieldSize = drawableRuleset.Playfield.DrawSize;
                if (playfieldSize.X > 0 && playfieldSize.Y > 0)
                {
                    float minSide = MathF.Min(playfieldSize.X, playfieldSize.Y);
                    float maxSide = MathF.Max(playfieldSize.X, playfieldSize.Y);
                    drawableRuleset.PlayfieldAdjustmentContainer.Scale = new Vector2(minSide / maxSide);
                }

                if (drawableRuleset.Playfield is OsuPlayfield osuPlayfield && osuPlayfield.Cursor != null)
                    osuPlayfield.Cursor.ActiveCursor.Rotation = -rotation;
            }
            else
            {
                drawableRuleset.PlayfieldAdjustmentContainer.Rotation = 0;
                drawableRuleset.PlayfieldAdjustmentContainer.Scale = Vector2.One;

                if (drawableRuleset.Playfield is OsuPlayfield osuPlayfield && osuPlayfield.Cursor != null)
                    osuPlayfield.Cursor.ActiveCursor.Rotation = 0;
            }
        }

        private void updateMuted(SectionGimmickSettings? settings)
        {
            if (!hasForcedMuted)
                return;

            if (settings?.ForceMuted == true)
            {
                int comboTarget = Math.Clamp(settings.MutedMuteComboCount, 0, 500);
                int combo = scoreProcessor?.Combo.Value ?? 0;

                double dimFactor = comboTarget == 0 ? 1 : (double)combo / comboTarget;
                dimFactor = Math.Clamp(dimFactor, 0, 1);

                mutedVolumeAdjustment.Value = 1 - dimFactor;
            }
            else
            {
                mutedVolumeAdjustment.Value = 1;
            }
        }

        private void updateNoScope(SectionGimmickSettings? settings)
        {
            if (!hasForcedNoScope)
                return;

            if (drawableRuleset.Playfield is not OsuPlayfield osuPlayfield || osuPlayfield.Cursor == null)
                return;

            float alpha = 1;

            if (settings?.ForceNoScope == true)
            {
                int comboTarget = Math.Clamp(settings.NoScopeHiddenComboCount, 0, 50);
                int combo = scoreProcessor?.Combo.Value ?? 0;

                alpha = comboTarget == 0
                    ? no_scope_min_alpha
                    : Math.Max(no_scope_min_alpha, 1 - (float)combo / comboTarget);

                if (player?.IsBreakTime.Value == true)
                    alpha = 1;
            }

            osuPlayfield.Cursor.Alpha = alpha;
            osuPlayfield.Smoke.Alpha = alpha;
        }

        private void updateBloom(SectionGimmickSettings? settings)
        {
            if (!hasForcedBloom)
                return;

            if (drawableRuleset.Playfield is not OsuPlayfield osuPlayfield || osuPlayfield.Cursor?.ActiveCursor is not OsuCursor osuCursor)
                return;

            float scale = 1;

            if (settings?.ForceBloom == true)
            {
                int combo = scoreProcessor?.Combo.Value ?? 0;
                int maxSizeCombo = Math.Clamp(settings.BloomMaxSizeComboCount, 5, 100);
                float maxSize = Math.Clamp(settings.BloomMaxCursorSize, 5f, 15f);

                scale = Math.Clamp(maxSize * ((float)combo / maxSizeCombo), 1, maxSize);

                if (player?.IsBreakTime.Value == true)
                    scale = 1;
            }

            osuCursor.ModScaleAdjust.Value = scale;
        }

        private void updateBubblesJudgementVisibility(SectionGimmickSettings? settings)
        {
            if (!hasForcedBubbles)
                return;

            drawableRuleset.Playfield.DisplayJudgements.Value = settings?.ForceBubbles == true ? false : initialDisplayJudgements;
        }

        private void updateMotionEffects()
        {
            if (!hasForcedMotionEffects)
                return;

            if (drawableRuleset.Playfield.Cursor == null)
                return;

            Vector2 cursorPos = drawableRuleset.Playfield.Cursor.ActiveCursor.DrawPosition;
            double time = drawableRuleset.Playfield.Time.Current;

            foreach (var entry in drawableRuleset.Playfield.HitObjectContainer.AliveEntries)
            {
                DrawableHitObject drawable = entry.Value;
                SectionGimmickSettings? settings = resolveSettingsForHitObject(drawable.HitObject);
                if (settings == null)
                    continue;

                if (settings.ForceDepth && !selectedDepth)
                {
                    applyDepth(time, drawable, Math.Clamp(settings.DepthMaxDepth, 50f, 200f));
                    continue;
                }

                if (settings.ForceMagnetised && !selectedMagnetised)
                {
                    applyMagnetised(drawable, cursorPos, Math.Clamp(settings.MagnetisedAttractionStrength, 0.05f, 1f));
                    continue;
                }

                if (settings.ForceRepel && !selectedRepel)
                    applyRepel(drawable, cursorPos, Math.Clamp(settings.RepelRepulsionStrength, 0.05f, 1f));
            }
        }

        private void applyMagnetised(DrawableHitObject drawable, Vector2 cursorPos, float attractionStrength)
        {
            switch (drawable)
            {
                case DrawableHitCircle circle:
                    easeTo(drawableRuleset.Playfield, circle, cursorPos, attractionStrength);
                    break;

                case DrawableSlider slider:
                    if (!slider.HeadCircle.Result.HasResult)
                        easeTo(drawableRuleset.Playfield, slider, cursorPos, attractionStrength);
                    else
                        easeTo(drawableRuleset.Playfield, slider, cursorPos - slider.Ball.DrawPosition, attractionStrength);
                    break;
            }
        }

        private void applyRepel(DrawableHitObject drawable, Vector2 cursorPos, float repulsionStrength)
        {
            Vector2 destination = Vector2.Clamp(2 * drawable.Position - cursorPos, Vector2.Zero, OsuPlayfield.BASE_SIZE);

            if (drawable.HitObject is Slider sliderHitObject)
            {
                var possibleMovementBounds = OsuHitObjectGenerationUtils.CalculatePossibleMovementBounds(sliderHitObject);
                destination = Vector2.Clamp(
                    destination,
                    new Vector2(possibleMovementBounds.Left, possibleMovementBounds.Top),
                    new Vector2(possibleMovementBounds.Right, possibleMovementBounds.Bottom));
            }

            switch (drawable)
            {
                case DrawableHitCircle circle:
                    easeToRepel(drawableRuleset.Playfield, circle, destination, cursorPos, repulsionStrength);
                    break;

                case DrawableSlider slider:
                    if (!slider.HeadCircle.Result.HasResult)
                        easeToRepel(drawableRuleset.Playfield, slider, destination, cursorPos, repulsionStrength);
                    else
                        easeToRepel(drawableRuleset.Playfield, slider, destination - slider.Ball.DrawPosition, cursorPos, repulsionStrength);
                    break;
            }
        }

        private static void easeTo(Playfield playfield, DrawableHitObject hitObject, Vector2 destination, float attractionStrength)
        {
            double dampLength = Interpolation.Lerp(3000, 40, attractionStrength);

            float x = (float)Interpolation.DampContinuously(hitObject.X, destination.X, dampLength, playfield.Clock.ElapsedFrameTime);
            float y = (float)Interpolation.DampContinuously(hitObject.Y, destination.Y, dampLength, playfield.Clock.ElapsedFrameTime);

            hitObject.Position = new Vector2(x, y);
        }

        private static void easeToRepel(Playfield playfield, DrawableHitObject hitObject, Vector2 destination, Vector2 cursorPos, float repulsionStrength)
        {
            double dampLength = Vector2.Distance(hitObject.Position, cursorPos) / (0.04 * repulsionStrength + 0.04);

            float x = (float)Interpolation.DampContinuously(hitObject.X, destination.X, dampLength, playfield.Clock.ElapsedFrameTime);
            float y = (float)Interpolation.DampContinuously(hitObject.Y, destination.Y, dampLength, playfield.Clock.ElapsedFrameTime);

            hitObject.Position = new Vector2(x, y);
        }

        private static void applyDepth(double time, DrawableHitObject drawable, float maxDepth)
        {
            switch (drawable)
            {
                case DrawableHitCircle circle:
                    processDepthHitObject(time, circle, maxDepth);
                    break;

                case DrawableSlider slider:
                    processDepthSlider(time, slider, maxDepth);
                    break;
            }
        }

        private static void processDepthHitObject(double time, DrawableOsuHitObject drawable, float maxDepth)
        {
            var hitObject = drawable.HitObject;

            double speed = maxDepth / hitObject.TimePreempt;
            double appearTime = hitObject.StartTime - hitObject.TimePreempt;
            float z = maxDepth - (float)((Math.Max(time, appearTime) - appearTime) * speed);

            float scale = depthScaleFor(z);
            drawable.Position = depthToPlayfieldPosition(scale, hitObject.StackedPosition);
            drawable.Scale = new Vector2(scale);
        }

        private static void processDepthSlider(double time, DrawableSlider drawableSlider, float maxDepth)
        {
            var hitObject = drawableSlider.HitObject;

            double baseSpeed = maxDepth / hitObject.TimePreempt;
            double appearTime = hitObject.StartTime - hitObject.TimePreempt;

            float sliderMinDepth = depthForScale(1.5f);
            float zEnd = maxDepth - (float)((Math.Max(hitObject.StartTime + hitObject.Duration, appearTime) - appearTime) * baseSpeed);

            if (zEnd > sliderMinDepth)
            {
                processDepthHitObject(time, drawableSlider, maxDepth);
                return;
            }

            double offsetAfterStartTime = hitObject.Duration + 500;
            double slowSpeed = Math.Min(-sliderMinDepth / offsetAfterStartTime, baseSpeed);

            double decelerationTime = hitObject.TimePreempt * 0.2;
            float decelerationDistance = (float)(decelerationTime * (baseSpeed + slowSpeed) * 0.5);

            float z;

            if (time < hitObject.StartTime - decelerationTime)
            {
                float fullDistance = decelerationDistance + (float)(baseSpeed * (hitObject.TimePreempt - decelerationTime));
                z = fullDistance - (float)((Math.Max(time, appearTime) - appearTime) * baseSpeed);
            }
            else if (time < hitObject.StartTime)
            {
                double timeOffset = time - (hitObject.StartTime - decelerationTime);
                double deceleration = (slowSpeed - baseSpeed) / decelerationTime;
                z = decelerationDistance - (float)(baseSpeed * timeOffset + deceleration * timeOffset * timeOffset * 0.5);
            }
            else
            {
                double endTime = hitObject.StartTime + offsetAfterStartTime;
                z = -(float)((Math.Min(time, endTime) - hitObject.StartTime) * slowSpeed);
            }

            float scale = depthScaleFor(z);
            drawableSlider.Position = depthToPlayfieldPosition(scale, hitObject.StackedPosition);
            drawableSlider.Scale = new Vector2(scale);
        }

        private static float depthScaleFor(float depth) => -depth_camera_position.Z / Math.Max(1f, depth - depth_camera_position.Z);

        private static float depthForScale(float scale) => -depth_camera_position.Z / scale + depth_camera_position.Z;

        private static Vector2 depthToPlayfieldPosition(float scale, Vector2 positionAtZeroDepth)
            => (positionAtZeroDepth - depth_camera_position.Xy) * scale + depth_camera_position.Xy;

        private static void applyModToDrawable(Mod mod, DrawableHitObject drawable)
        {
            if (mod is IApplicableToDrawableHitObject applicable)
                applicable.ApplyToDrawableHitObject(drawable);
        }

        private void applyCustomFreezeFrame(DrawableHitObject drawable)
        {
            if (drawable is not DrawableHitCircle drawableHitCircle)
                return;

            var hitCircle = drawableHitCircle.HitObject;
            float originalPreempt = (float)(beatmap.HitObjects.OfType<OsuHitObject>().FirstOrDefault()?.TimePreempt ?? hitCircle.TimePreempt);

            drawable.ApplyCustomUpdateState += (drawableObject, _) =>
            {
                if (drawableObject is not DrawableHitCircle circle)
                    return;

                var approachCircle = circle.ApproachCircle;
                approachCircle.ClearTransforms(targetMember: nameof(approachCircle.Scale));
                approachCircle.ScaleTo(4 * (float)(circle.HitObject.TimePreempt / originalPreempt));

                using (approachCircle.BeginAbsoluteSequence(circle.HitObject.StartTime - circle.HitObject.TimePreempt))
                    approachCircle.ScaleTo(1, circle.HitObject.TimePreempt).Then().Expire();
            };
        }

        private SectionGimmickSettings? resolveSettingsAtTime(double time)
            => SectionGimmickSectionResolver.Resolve(gimmicks, time)?.Settings;

        private SectionGimmickSettings? resolveSettingsForHitObject(osu.Game.Rulesets.Objects.HitObject hitObject)
        {
            if (hitObject is not OsuHitObject osuHitObject)
                return null;

            if (HitObjectGimmickBindingUtils.TryGetSettings(osuHitObject, objectSettingsById, objectSettingsByLegacyKey, out var objectSettings))
                return mapToSectionSettings(objectSettings);

            return resolveSettingsAtTime(osuHitObject.StartTime);
        }

        private static SectionGimmickSettings mapToSectionSettings(HitObjectGimmickSettings source)
            => new SectionGimmickSettings
            {
                EnableHPGimmick = source.EnableHPGimmick,
                EnableNoMiss = source.EnableNoMiss,
                EnableCountLimits = source.EnableCountLimits,
                EnableGreatOffsetPenalty = source.EnableGreatOffsetPenalty,

                Max300s = source.Max300s,
                Max100s = source.Max100s,
                Max50s = source.Max50s,
                MaxMisses = source.MaxMisses,

                HP300 = source.HP300,
                HP100 = source.HP100,
                HP50 = source.HP50,
                HPMiss = source.HPMiss,

                GreatOffsetThresholdMs = source.GreatOffsetThresholdMs,
                GreatOffsetPenaltyHP = source.GreatOffsetPenaltyHP,

                EnableDifficultyOverrides = source.EnableDifficultyOverrides,
                AllowUnsafeDifficultyOverrideValues = source.AllowUnsafeDifficultyOverrideValues,
                SectionCircleSize = source.SectionCircleSize,
                SectionApproachRate = source.SectionApproachRate,
                SectionOverallDifficulty = source.SectionOverallDifficulty,
                AllowUnsafeStackLeniencyOverrideValues = source.AllowUnsafeStackLeniencyOverrideValues,
                SectionStackLeniency = source.SectionStackLeniency,
                AllowUnsafeTickRateOverrideValues = source.AllowUnsafeTickRateOverrideValues,
                SectionTickRate = source.SectionTickRate,

                ForceHidden = source.ForceHidden,
                ForceNoApproachCircle = source.ForceNoApproachCircle,
                ForceHardRock = source.ForceHardRock,
                ForceFlashlight = source.ForceFlashlight,
                SpinnerDirection = source.SpinnerDirection,
                SpinnerWrongDirection = source.SpinnerWrongDirection,
                SpinnerIndicator = source.SpinnerIndicator,
                ForceTraceable = source.ForceTraceable,
                FlashlightRadius = source.FlashlightRadius,
            };

        private bool hasAnyForced(Func<SectionGimmickSettings, bool> predicate)
            => gimmicks.Sections.Any(s => predicate(s.Settings));

        private bool hasSelectedMod<TMod>()
            where TMod : Mod
            => selectedMods.Any(m => m is TMod);

        public static bool HasAnyForcedFunMods(IBeatmap beatmap)
        {
            return beatmap.SectionGimmicks.Sections.Any(s =>
                s.Settings.ForceTransform
                || s.Settings.ForceWiggle
                || s.Settings.ForceSpinIn
                || s.Settings.ForceGrow
                || s.Settings.ForceDeflate
                || s.Settings.ForceBarrelRoll
                || s.Settings.ForceApproachDifferent
                || s.Settings.ForceMuted
                || s.Settings.ForceNoScope
                || s.Settings.ForceTraceable
                || s.Settings.ForceMagnetised
                || s.Settings.ForceRepel
                || s.Settings.ForceFreezeFrame
                || s.Settings.ForceBubbles
                || s.Settings.ForceSynesthesia
                || s.Settings.ForceDepth
                || s.Settings.ForceBloom);
        }
    }
}
