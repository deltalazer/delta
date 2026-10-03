// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Linq;
using osu.Game.Beatmaps;
using osu.Game.Beatmaps.HitObjectGimmicks;
using osu.Game.Beatmaps.SectionGimmicks;
using osu.Game.Rulesets.Judgements;
using osu.Game.Rulesets.Objects;
using osu.Game.Rulesets.Osu.Objects;
using osu.Game.Rulesets.Scoring;

namespace osu.Game.Rulesets.Osu.Scoring
{
    public partial class SectionGimmickHealthProcessor : OsuHealthProcessor
    {
        private readonly SectionGimmickCountTracker countTracker = new SectionGimmickCountTracker();
        private BeatmapSectionGimmicks gimmicks = new BeatmapSectionGimmicks();
        private BeatmapHitObjectGimmicks hitObjectGimmicks = new BeatmapHitObjectGimmicks();
        private Dictionary<long, HitObjectGimmickSettings> objectSettingsById = new Dictionary<long, HitObjectGimmickSettings>();
        private Dictionary<(double StartTime, int ComboIndexWithOffsets), HitObjectGimmickSettings> objectSettingsLookup = new Dictionary<(double StartTime, int ComboIndexWithOffsets), HitObjectGimmickSettings>();
        private readonly List<(double Start, double End)> keptHealthRanges = new List<(double, double)>();
        private SectionGimmickSection? activeSection;
        private double activeSectionAccuracyBaseScore;
        private double activeSectionAccuracyMaxBaseScore;
        private bool activeSectionAccuracyRequirementEvaluated;

        public SectionGimmickSection? ActiveSection => activeSection;

        public SectionGimmickHealthProcessor(double drainStartTime)
            : base(drainStartTime)
        {
        }

        public override void ApplyBeatmap(IBeatmap beatmap)
        {
            gimmicks = beatmap.SectionGimmicks ?? new BeatmapSectionGimmicks();
            hitObjectGimmicks = beatmap.HitObjectGimmicks ?? new BeatmapHitObjectGimmicks();
            objectSettingsById = createObjectSettingsLookupByObjectId(hitObjectGimmicks);
            objectSettingsLookup = createObjectSettingsLookup(hitObjectGimmicks);
            SectionGimmicksValidator.Validate(gimmicks);

            keptHealthRanges.Clear();
            keptHealthRanges.AddRange(beatmap.HitObjects
                                             .Where(h => h is { ForceAllMiss: true, FreezeHP: true })
                                             .Select(h => (h.StartTime, h.GetEndTime())));

            base.ApplyBeatmap(beatmap);
        }

        public bool IsHealthKeptAt(double time)
        {
            if (SectionGimmickSectionResolver.Resolve(gimmicks, time)?.Settings is { ForceAllMiss: true, FreezeHP: true })
                return true;

            foreach ((double start, double end) in keptHealthRanges)
            {
                if (time >= start && time <= end)
                    return true;
            }

            return false;
        }

        protected override void Update()
        {
            base.Update();

            var settings = resolveSection(Time.Current)?.Settings;

            if (settings != null && settings.EnableHPGimmick && !float.IsNaN(settings.HPCap))
                Health.Value = Math.Min(Health.Value, settings.HPCap);

            bool cancelDrain = settings != null && ((settings.EnableHPGimmick && settings.NoDrain) || settings.EnableGreatOffsetPenalty);

            cancelDrain |= IsHealthKeptAt(Time.Current);

            if (cancelDrain)
            {
                // cancel out frame drain while this mode is active.
                // keep this section-scoped
                Health.Value += DrainRate * Time.Elapsed;
            }
        }

        protected override void ApplyResultInternal(JudgementResult result)
        {
            if (activeSection != null && activeSection.EndTime >= 0 && result.HitObject.StartTime > activeSection.EndTime)
            {
                if (evaluateActiveSectionAccuracyRequirementIfNeeded(result.HitObject.StartTime))
                    return;
            }

            var section = resolveSection(result.HitObject.StartTime);
            var objectSettings = resolveObjectSettings(result.HitObject);
            SectionGimmickSettings? settings = null;

            if (section != null || objectSettings != null)
            {
                settings = mergeSettings(section?.Settings, objectSettings);

                if (settings.EnableNoMiss && result.Type == HitResult.Miss && !result.HitObject.ForceAllMiss)
                {
                    TriggerFailure();
                    return;
                }

                if (settings.EnableAccuracyRequirement)
                    updateAccuracyRequirementTracking(result);

                if (settings.EnableNoMissedSliderEnd &&
                    (result.HitObject is SliderEndCircle || result.HitObject is SliderRepeat || result.HitObject is SliderTick) &&
                    (result.Type == HitResult.IgnoreMiss || result.Type == HitResult.LargeTickMiss || result.Type == HitResult.SmallTickMiss))
                {
                    TriggerFailure();
                    return;
                }

                countTracker.Record(result, settings);
                if (countTracker.Exceeds(settings, result))
                {
                    TriggerFailure();
                    return;
                }

                if (evaluateActiveSectionAccuracyRequirementIfNeeded(result.HitObject.StartTime))
                    return;
            }

            base.ApplyResultInternal(result);

            // Apply additional offset penalty after base judgement application.
            if (settings != null)
            {
                if (settings.EnableGreatOffsetPenalty && shouldApplyOffsetPenalty(settings, result))
                {
                    if (Math.Abs(result.TimeOffset) > settings.GreatOffsetThresholdMs)
                        Health.Value += settings.GreatOffsetPenaltyHP;
                }
            }
        }

        private static Dictionary<(double StartTime, int ComboIndexWithOffsets), HitObjectGimmickSettings> createObjectSettingsLookup(BeatmapHitObjectGimmicks gimmicks)
            => HitObjectGimmickBindingUtils.CreateLookupByLegacyKey(gimmicks);

        private static Dictionary<long, HitObjectGimmickSettings> createObjectSettingsLookupByObjectId(BeatmapHitObjectGimmicks gimmicks)
            => HitObjectGimmickBindingUtils.CreateLookupByObjectId(gimmicks);

        protected override double GetHealthIncreaseFor(JudgementResult result)
        {
            if (result.HitObject is { ForceAllMiss: true, FreezeHP: true })
                return 0;

            var section = resolveSection(result.HitObject.StartTime);
            var objectSettings = resolveObjectSettings(result.HitObject);
            if (section == null && objectSettings == null)
                return base.GetHealthIncreaseFor(result);

            var settings = mergeSettings(section?.Settings, objectSettings);
            if (!settings.EnableHPGimmick)
                return base.GetHealthIncreaseFor(result);

            float hpValue = mapResultForHp(result, settings) switch
            {
                HitResult.Great => settings.HP300,
                HitResult.Ok => settings.HP100,
                HitResult.Meh => settings.HP50,
                HitResult.Miss => settings.HPMiss,
                _ => float.NaN
            };

            if (float.IsNaN(hpValue))
                return base.GetHealthIncreaseFor(result);

            // When ReverseHP is false, positive HP values should drain (subtract from health)
            // When ReverseHP is true, positive HP values should heal (add to health)
            double delta = settings.ReverseHP ? hpValue : -hpValue;

            if (!float.IsNaN(settings.HPCap) && delta > 0)
                delta = Math.Min(delta, settings.HPCap - Health.Value);

            return delta;
        }

        private static HitResult mapResultForHp(JudgementResult result, SectionGimmickSettings settings)
        {
            switch (result.Type)
            {
                case HitResult.Great:
                case HitResult.Ok:
                case HitResult.Meh:
                case HitResult.Miss:
                    return result.Type;

                case HitResult.LargeTickHit:
                case HitResult.SmallTickHit:
                case HitResult.SliderTailHit:
                    if (settings.HP300AffectsSliderEndsAndTicks)
                        return HitResult.Great;
                    if (settings.HP100AffectsSliderEndsAndTicks)
                        return HitResult.Ok;
                    if (settings.HP50AffectsSliderEndsAndTicks)
                        return HitResult.Meh;
                    return HitResult.None;

                case HitResult.LargeTickMiss:
                case HitResult.SmallTickMiss:
                    return settings.HPMissAffectsSliderEndAndTickMisses ? HitResult.Miss : HitResult.None;

                case HitResult.IgnoreMiss:
                    return settings.HPMissAffectsSliderEndAndTickMisses
                           && (result.HitObject is SliderEndCircle || result.HitObject is SliderRepeat)
                        ? HitResult.Miss
                        : HitResult.None;

                default:
                    return HitResult.None;
            }
        }

        private SectionGimmickSection? resolveSection(double time)
        {
            var section = SectionGimmickSectionResolver.Resolve(gimmicks, time);

            if (section == null)
            {
                activeSection = null;
                return null;
            }

            if (activeSection?.Id != section.Id)
            {
                activeSection = section;
                countTracker.EnterSection(section.Id);
                activeSectionAccuracyBaseScore = 0;
                activeSectionAccuracyMaxBaseScore = 0;
                activeSectionAccuracyRequirementEvaluated = false;

                var settings = section.Settings;

                if (settings.EnableHPGimmick && !float.IsNaN(settings.HPStart))
                    Health.Value = settings.HPStart;

                if ((settings.EnableHPGimmick && settings.NoDrain && !settings.ReverseHP) && settings.EnableGreatOffsetPenalty)
                {
                    // both enabled: no-op special case, handled by per-hit logic.
                }
            }

            return section;
        }

        private static bool shouldApplyOffsetPenalty(SectionGimmickSettings settings, JudgementResult result)
        {
            bool baseApplicable = result.Type is HitResult.Great or HitResult.Ok or HitResult.Meh;
            if (!baseApplicable)
                return false;

            // If HP gimmick already provides explicit per-judgement punishment for this result,
            // avoid double-penalising that same judgement type.
            if (!settings.EnableHPGimmick)
                return true;

            float configured = result.Type switch
            {
                HitResult.Great => settings.HP300,
                HitResult.Ok => settings.HP100,
                HitResult.Meh => settings.HP50,
                _ => float.NaN,
            };

            if (float.IsNaN(configured))
                return true;

            double resultingHealthDelta = settings.ReverseHP ? configured : -configured;
            bool hpGimmickAlreadyPunishes = resultingHealthDelta < 0;
            return !hpGimmickAlreadyPunishes;
        }

        private HitObjectGimmickSettings? resolveObjectSettings(osu.Game.Rulesets.Objects.HitObject hitObject)
        {
            if (hitObject is not OsuHitObject osuHitObject)
                return null;

            return HitObjectGimmickBindingUtils.TryGetSettings(osuHitObject, objectSettingsById, objectSettingsLookup, out var settings)
                ? settings
                : null;
        }

        private static SectionGimmickSettings mergeSettings(SectionGimmickSettings? sectionSettings, HitObjectGimmickSettings? objectSettings)
        {
            var result = new SectionGimmickSettings();

            if (sectionSettings != null)
            {
                result.EnableHPGimmick = sectionSettings.EnableHPGimmick;
                result.EnableNoMiss = sectionSettings.EnableNoMiss;
                result.EnableAccuracyRequirement = sectionSettings.EnableAccuracyRequirement;
                result.RequiredAccuracy = sectionSettings.RequiredAccuracy;
                result.EnableCountLimits = sectionSettings.EnableCountLimits;
                result.EnableNoMissedSliderEnd = sectionSettings.EnableNoMissedSliderEnd;
                result.EnableGreatOffsetPenalty = sectionSettings.EnableGreatOffsetPenalty;

                result.Max300s = sectionSettings.Max300s;
                result.Max100s = sectionSettings.Max100s;
                result.Max50s = sectionSettings.Max50s;
                result.MaxMisses = sectionSettings.MaxMisses;

                result.HP300 = sectionSettings.HP300;
                result.HP100 = sectionSettings.HP100;
                result.HP50 = sectionSettings.HP50;
                result.HPMiss = sectionSettings.HPMiss;
                result.HPStart = sectionSettings.HPStart;
                result.HPCap = sectionSettings.HPCap;
                result.HP300AffectsSliderEndsAndTicks = sectionSettings.HP300AffectsSliderEndsAndTicks;
                result.HP100AffectsSliderEndsAndTicks = sectionSettings.HP100AffectsSliderEndsAndTicks;
                result.HP50AffectsSliderEndsAndTicks = sectionSettings.HP50AffectsSliderEndsAndTicks;
                result.HPMissAffectsSliderEndAndTickMisses = sectionSettings.HPMissAffectsSliderEndAndTickMisses;
                result.Max300sAffectsSliderEndsAndTicks = sectionSettings.Max300sAffectsSliderEndsAndTicks;
                result.Max100sAffectsSliderEndsAndTicks = sectionSettings.Max100sAffectsSliderEndsAndTicks;
                result.Max50sAffectsSliderEndsAndTicks = sectionSettings.Max50sAffectsSliderEndsAndTicks;
                result.MaxMissesAffectsSliderEndAndTickMisses = sectionSettings.MaxMissesAffectsSliderEndAndTickMisses;
                result.NoDrain = sectionSettings.NoDrain;
                result.ReverseHP = sectionSettings.ReverseHP;

                result.GreatOffsetThresholdMs = sectionSettings.GreatOffsetThresholdMs;
                result.GreatOffsetPenaltyHP = sectionSettings.GreatOffsetPenaltyHP;
            }

            if (objectSettings != null)
            {
                result.EnableHPGimmick = result.EnableHPGimmick || objectSettings.EnableHPGimmick;
                result.EnableNoMiss = result.EnableNoMiss || objectSettings.EnableNoMiss;
                result.EnableCountLimits = result.EnableCountLimits || objectSettings.EnableCountLimits;
                result.EnableGreatOffsetPenalty = result.EnableGreatOffsetPenalty || objectSettings.EnableGreatOffsetPenalty;

                if (objectSettings.Max300s >= 0) result.Max300s = objectSettings.Max300s;
                if (objectSettings.Max100s >= 0) result.Max100s = objectSettings.Max100s;
                if (objectSettings.Max50s >= 0) result.Max50s = objectSettings.Max50s;
                if (objectSettings.MaxMisses >= 0) result.MaxMisses = objectSettings.MaxMisses;

                if (!float.IsNaN(objectSettings.HP300)) result.HP300 = objectSettings.HP300;
                if (!float.IsNaN(objectSettings.HP100)) result.HP100 = objectSettings.HP100;
                if (!float.IsNaN(objectSettings.HP50)) result.HP50 = objectSettings.HP50;
                if (!float.IsNaN(objectSettings.HPMiss)) result.HPMiss = objectSettings.HPMiss;

                if (objectSettings.GreatOffsetThresholdMs >= 0) result.GreatOffsetThresholdMs = objectSettings.GreatOffsetThresholdMs;
                if (!float.IsNaN(objectSettings.GreatOffsetPenaltyHP)) result.GreatOffsetPenaltyHP = objectSettings.GreatOffsetPenaltyHP;
            }

            return result;
        }

        private void updateAccuracyRequirementTracking(JudgementResult result)
        {
            if (result.Type == HitResult.None)
                return;

            if (affectsSectionAccuracyDenominator(result))
                activeSectionAccuracyMaxBaseScore += getSectionAccuracyMaxBaseScoreForResult(result);

            if (result.Type.AffectsAccuracy())
                activeSectionAccuracyBaseScore += getSectionBaseScoreForResult(result.Type);
        }

        private static bool hasSectionEnded(SectionGimmickSection section, double hitObjectTime)
            => section.EndTime >= 0 && hitObjectTime >= section.EndTime;

        private bool evaluateActiveSectionAccuracyRequirementIfNeeded(double time)
        {
            if (activeSection == null || activeSectionAccuracyRequirementEvaluated)
                return false;

            if (!activeSection.Settings.EnableAccuracyRequirement)
            {
                activeSectionAccuracyRequirementEvaluated = true;
                return false;
            }

            if (!hasSectionEnded(activeSection, time))
                return false;

            activeSectionAccuracyRequirementEvaluated = true;

            double currentSectionAccuracy = activeSectionAccuracyMaxBaseScore > 0
                ? activeSectionAccuracyBaseScore / activeSectionAccuracyMaxBaseScore
                : 1;

            if (!meetsRequiredAccuracy(currentSectionAccuracy, activeSection.Settings.RequiredAccuracy))
            {
                TriggerFailure();
                return true;
            }

            return false;
        }

        private static bool affectsSectionAccuracyDenominator(JudgementResult result)
            => result.HitObject is not { ForceAllMiss: true, FreezeAccuracy: true }
               && (result.Judgement.MaxResult.AffectsAccuracy()
                   || (result.HitObject is FakeHitCircle or FakeSlider
                       && result.Type == HitResult.Miss
                       && result.Judgement.MaxResult == HitResult.IgnoreHit));

        private static int getSectionAccuracyMaxBaseScoreForResult(JudgementResult result)
        {
            if (result.HitObject is FakeHitCircle or FakeSlider
                && result.Type == HitResult.Miss
                && result.Judgement.MaxResult == HitResult.IgnoreHit)
            {
                return getSectionBaseScoreForResult(HitResult.Great);
            }

            return getSectionBaseScoreForResult(result.Judgement.MaxResult);
        }

        private static int getSectionBaseScoreForResult(HitResult result)
            => result switch
            {
                HitResult.SmallTickHit => 10,
                HitResult.LargeTickHit => 30,
                HitResult.SliderTailHit => 150,
                HitResult.Great => 300,
                HitResult.Ok => 100,
                HitResult.Meh => 50,
                HitResult.Good => 200,
                HitResult.Perfect => 300,
                _ => 0,
            };

        private static bool meetsRequiredAccuracy(double currentAccuracyFraction, float requiredAccuracyFraction)
        {
            double currentPercent = Math.Round(Math.Clamp(currentAccuracyFraction, 0, 1) * 100, 2, MidpointRounding.AwayFromZero);
            double requiredPercent = Math.Round(Math.Clamp(requiredAccuracyFraction, 0, 1) * 100, 2, MidpointRounding.AwayFromZero);
            return currentPercent + 0.0000001 >= requiredPercent;
        }
    }
}
