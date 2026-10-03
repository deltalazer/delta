// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Linq;
using osu.Framework.Bindables;
using osu.Game.Beatmaps.SectionGimmicks;

namespace osu.Game.Screens.Edit.Compose
{
    public class SectionGimmickEditorModel
    {
        private readonly EditorBeatmap editorBeatmap;

        private SectionGimmickSettings? copiedSettings;

        public readonly BindableList<SectionGimmickSection> Sections = new BindableList<SectionGimmickSection>();

        public readonly BindableInt SelectedSectionId = new BindableInt(-1);

        public bool HasCopiedSettings => copiedSettings != null;

        public SectionGimmickEditorModel(EditorBeatmap editorBeatmap)
        {
            this.editorBeatmap = editorBeatmap;

            if (this.editorBeatmap.SectionGimmicks == null)
                this.editorBeatmap.SectionGimmicks = new BeatmapSectionGimmicks();

            syncFromBeatmap();
        }

        public void AddSection(double time)
        {
            mutate(sections =>
            {
                int newId = sections.Count > 0 ? sections.Max(s => s.Id) + 1 : 0;

                double startTime = time;

                // Avoid duplicate start times for deterministic section resolution.
                while (sections.Any(s => Math.Abs(s.StartTime - startTime) < 0.0001))
                    startTime += 1;

                var ordered = sections.OrderBy(s => s.StartTime).ToList();

                // Keep new section anchored to user timeline position.
                // If the previous section spans past this point (or is open-ended), cap it here.
                var previous = ordered.LastOrDefault(s => s.StartTime < startTime);

                if (previous != null && (previous.EndTime < 0 || previous.EndTime > startTime))
                    previous.EndTime = startTime;

                var next = ordered.FirstOrDefault(s => s.StartTime > startTime);

                var newSettings = new SectionGimmickSettings();

                // If previous section has "Keep overrides after section" enabled,
                // inherit difficulty override values from the previous section
                if (sections.Count > 0)
                {
                    var source = previous;

                    if (source?.Settings.KeepDifficultyOverridesAfterSection == true &&
                        source.Settings.EnableDifficultyOverrides)
                    {
                        newSettings.EnableDifficultyOverrides = true;
                        newSettings.SectionCircleSize = source.Settings.SectionCircleSize;
                        newSettings.SectionApproachRate = source.Settings.SectionApproachRate;
                        newSettings.SectionOverallDifficulty = source.Settings.SectionOverallDifficulty;
                        newSettings.AllowUnsafeStackLeniencyOverrideValues = source.Settings.AllowUnsafeStackLeniencyOverrideValues;
                        newSettings.SectionStackLeniency = source.Settings.SectionStackLeniency;
                        newSettings.AllowUnsafeTickRateOverrideValues = source.Settings.AllowUnsafeTickRateOverrideValues;
                        newSettings.SectionTickRate = source.Settings.SectionTickRate;
                    }
                }

                sections.Add(new SectionGimmickSection
                {
                    Id = newId,
                    StartTime = startTime,
                    EndTime = next?.StartTime ?? -1,
                    Settings = newSettings,
                });

                return newId;
            });
        }

        public void RemoveSelectedSection()
        {
            int selectedId = SelectedSectionId.Value;

            if (selectedId < 0)
                return;

            mutate(sections =>
            {
                var orderedBefore = sections.OrderBy(s => s.StartTime).ToList();
                int removedIndex = orderedBefore.FindIndex(s => s.Id == selectedId);

                if (removedIndex < 0)
                    return sections.FirstOrDefault()?.Id ?? -1;

                sections.RemoveAll(s => s.Id == selectedId);

                var orderedAfter = sections.OrderBy(s => s.StartTime).ToList();

                // Re-number section IDs to keep them contiguous after deletion.
                for (int i = 0; i < orderedAfter.Count; i++)
                    orderedAfter[i].Id = i;

                sections.Clear();
                sections.AddRange(orderedAfter);

                if (orderedAfter.Count == 0)
                    return -1;

                // Select the previous section (or first remaining if deleted section was first).
                int newSelectedIndex = Math.Clamp(removedIndex - 1, 0, orderedAfter.Count - 1);
                return orderedAfter[newSelectedIndex].Id;
            });
        }

        public void SetSelectedStartTime(double startTime)
        {
            mutateSelectedSection(section => section.StartTime = startTime);
        }

        public void SetSelectedEndTime(double endTime)
        {
            mutateSelectedSection(section => section.EndTime = endTime);
        }

        public void SetSelectedSetting(Action<SectionGimmickSettings> settingMutation)
        {
            mutateSelectedSection(section =>
            {
                settingMutation(section.Settings);
                SectionGimmickValueClamper.ClampSectionSettingsInPlace(section.Settings);
            });
        }

        public void CopySelectedSettings()
        {
            var selected = Sections.FirstOrDefault(s => s.Id == SelectedSectionId.Value);

            if (selected == null)
                return;

            copiedSettings = cloneSettings(selected.Settings);
        }

        public void PasteSettingsTo(IEnumerable<int> targetSectionIds)
        {
            if (copiedSettings == null)
                return;

            int[] targets = targetSectionIds.Distinct().ToArray();

            if (targets.Length == 0 && SelectedSectionId.Value >= 0)
                targets = [SelectedSectionId.Value];

            if (targets.Length == 0)
                return;

            mutate(sections =>
            {
                foreach (int id in targets)
                {
                    int index = sections.FindIndex(s => s.Id == id);

                    if (index < 0)
                        continue;

                    sections[index].Settings = cloneSettings(copiedSettings);
                }

                return targets[0];
            });
        }

        public BeatmapSectionGimmicks CreateClonedCurrentGimmicks()
            => cloneGimmicks(editorBeatmap.SectionGimmicks ?? new BeatmapSectionGimmicks());

        public static BeatmapSectionGimmicks CloneGimmicks(BeatmapSectionGimmicks source)
            => cloneGimmicks(source);

        private void mutateSelectedSection(Action<SectionGimmickSection> sectionMutation)
        {
            int selectedId = SelectedSectionId.Value;

            if (selectedId < 0)
                return;

            mutate(sections =>
            {
                var selected = sections.FirstOrDefault(s => s.Id == selectedId);

                if (selected == null)
                    return selectedId;

                sectionMutation(selected);
                return selectedId;
            });
        }

        private void mutate(Func<List<SectionGimmickSection>, int?> mutation)
        {
            editorBeatmap.BeginChange();

            try
            {
                var sections = cloneSections((editorBeatmap.SectionGimmicks ?? new BeatmapSectionGimmicks()).Sections);
                int? preferredSelection = mutation(sections);

                editorBeatmap.SectionGimmicks = new BeatmapSectionGimmicks
                {
                    Sections = sections.OrderBy(s => s.StartTime).ToList(),
                };

                // Force immediate reprocess so section gimmick changes are previewed instantly
                // in editor gameplay without requiring a reload.
                editorBeatmap.UpdateAllHitObjects();

                syncFromBeatmap(preferredSelection);
            }
            finally
            {
                editorBeatmap.EndChange();
            }
        }

        private void syncFromBeatmap(int? preferredSelection = null)
        {
            int selectionId = preferredSelection ?? SelectedSectionId.Value;

            Sections.Clear();
            var source = editorBeatmap.SectionGimmicks ?? new BeatmapSectionGimmicks();
            Sections.AddRange(cloneSections(source.Sections.OrderBy(s => s.StartTime)));

            if (Sections.All(s => s.Id != selectionId))
                selectionId = Sections.FirstOrDefault()?.Id ?? -1;

            SelectedSectionId.Value = selectionId;
        }

        private static BeatmapSectionGimmicks cloneGimmicks(BeatmapSectionGimmicks source)
            => new BeatmapSectionGimmicks
            {
                Sections = cloneSections(source.Sections),
            };

        private static List<SectionGimmickSection> cloneSections(IEnumerable<SectionGimmickSection>? sections)
            => sections?.Select(cloneSection).ToList() ?? new List<SectionGimmickSection>();

        private static SectionGimmickSection cloneSection(SectionGimmickSection section)
            => new SectionGimmickSection
            {
                Id = section.Id,
                StartTime = section.StartTime,
                EndTime = section.EndTime,
                Settings = cloneSettings(section.Settings ?? new SectionGimmickSettings()),
            };

        private static SectionGimmickSettings cloneSettings(SectionGimmickSettings settings)
            => new SectionGimmickSettings
            {
                EnableHPGimmick = settings.EnableHPGimmick,
                EnableNoMiss = settings.EnableNoMiss,
                ForceAllMiss = settings.ForceAllMiss,
                FreezeHP = settings.FreezeHP,
                FreezeAccuracy = settings.FreezeAccuracy,
                FreezeCombo = settings.FreezeCombo,
                EnableAccuracyRequirement = settings.EnableAccuracyRequirement,
                RequiredAccuracy = settings.RequiredAccuracy,
                EnableCountLimits = settings.EnableCountLimits,
                EnableNoMissedSliderEnd = settings.EnableNoMissedSliderEnd,
                EnableGreatOffsetPenalty = settings.EnableGreatOffsetPenalty,
                Max300s = settings.Max300s,
                Max100s = settings.Max100s,
                Max50s = settings.Max50s,
                MaxMisses = settings.MaxMisses,
                Max300sAffectsSliderEndsAndTicks = settings.Max300sAffectsSliderEndsAndTicks,
                Max100sAffectsSliderEndsAndTicks = settings.Max100sAffectsSliderEndsAndTicks,
                Max50sAffectsSliderEndsAndTicks = settings.Max50sAffectsSliderEndsAndTicks,
                MaxMissesAffectsSliderEndAndTickMisses = settings.MaxMissesAffectsSliderEndAndTickMisses,
                HP300 = settings.HP300,
                HP100 = settings.HP100,
                HP50 = settings.HP50,
                HPMiss = settings.HPMiss,
                HPStart = settings.HPStart,
                HPCap = settings.HPCap,
                HP300AffectsSliderEndsAndTicks = settings.HP300AffectsSliderEndsAndTicks,
                HP100AffectsSliderEndsAndTicks = settings.HP100AffectsSliderEndsAndTicks,
                HP50AffectsSliderEndsAndTicks = settings.HP50AffectsSliderEndsAndTicks,
                HPMissAffectsSliderEndAndTickMisses = settings.HPMissAffectsSliderEndAndTickMisses,
                NoDrain = settings.NoDrain,
                ReverseHP = settings.ReverseHP,
                GreatOffsetThresholdMs = settings.GreatOffsetThresholdMs,
                GreatOffsetPenaltyHP = settings.GreatOffsetPenaltyHP,
                EnableDifficultyOverrides = settings.EnableDifficultyOverrides,
                AllowUnsafeDifficultyOverrideValues = settings.AllowUnsafeDifficultyOverrideValues,
                DifficultyOverrideStartWithBeatmapValues = settings.DifficultyOverrideStartWithBeatmapValues,
                EnableGradualDifficultyChange = settings.EnableGradualDifficultyChange,
                GradualDifficultyChangeEndTimeMs = settings.GradualDifficultyChangeEndTimeMs,
                KeepDifficultyOverridesAfterSection = settings.KeepDifficultyOverridesAfterSection,
                SectionCircleSize = settings.SectionCircleSize,
                EnableSectionCircleSizeWindow = settings.EnableSectionCircleSizeWindow,
                SectionCircleSizeStartTimeMs = settings.SectionCircleSizeStartTimeMs,
                SectionCircleSizeEndTimeMs = settings.SectionCircleSizeEndTimeMs,
                EnableGradualSectionCircleSizeChange = settings.EnableGradualSectionCircleSizeChange,
                SectionApproachRate = settings.SectionApproachRate,
                EnableSectionApproachRateWindow = settings.EnableSectionApproachRateWindow,
                SectionApproachRateStartTimeMs = settings.SectionApproachRateStartTimeMs,
                SectionApproachRateEndTimeMs = settings.SectionApproachRateEndTimeMs,
                EnableGradualSectionApproachRateChange = settings.EnableGradualSectionApproachRateChange,
                SectionOverallDifficulty = settings.SectionOverallDifficulty,
                EnableSectionOverallDifficultyWindow = settings.EnableSectionOverallDifficultyWindow,
                SectionOverallDifficultyStartTimeMs = settings.SectionOverallDifficultyStartTimeMs,
                SectionOverallDifficultyEndTimeMs = settings.SectionOverallDifficultyEndTimeMs,
                EnableGradualSectionOverallDifficultyChange = settings.EnableGradualSectionOverallDifficultyChange,
                AllowUnsafeStackLeniencyOverrideValues = settings.AllowUnsafeStackLeniencyOverrideValues,
                SectionStackLeniency = settings.SectionStackLeniency,
                AllowUnsafeTickRateOverrideValues = settings.AllowUnsafeTickRateOverrideValues,
                SectionTickRate = settings.SectionTickRate,
                ForceHidden = settings.ForceHidden,
                ForceNoApproachCircle = settings.ForceNoApproachCircle,
                ForceHardRock = settings.ForceHardRock,
                ForceFlashlight = settings.ForceFlashlight,
                ForceTraceable = settings.ForceTraceable,
                FlashlightRadius = settings.FlashlightRadius,
                EnableGradualFlashlightRadiusChange = settings.EnableGradualFlashlightRadiusChange,
                EnableGradualFlashlightFadeIn = settings.EnableGradualFlashlightFadeIn,
                GradualFlashlightRadiusEndTimeMs = settings.GradualFlashlightRadiusEndTimeMs,
                ForceDoubleTime = settings.ForceDoubleTime,
                ForceSingleTap = settings.ForceSingleTap,
                ForceAlternate = settings.ForceAlternate,
                ForceTransform = settings.ForceTransform,
                ForceWiggle = settings.ForceWiggle,
                ForceSpinIn = settings.ForceSpinIn,
                ForceGrow = settings.ForceGrow,
                ForceDeflate = settings.ForceDeflate,
                ForceBarrelRoll = settings.ForceBarrelRoll,
                ForceApproachDifferent = settings.ForceApproachDifferent,
                ForceMuted = settings.ForceMuted,
                ForceNoScope = settings.ForceNoScope,
                ForceMagnetised = settings.ForceMagnetised,
                ForceRepel = settings.ForceRepel,
                ForceFreezeFrame = settings.ForceFreezeFrame,
                ForceBubbles = settings.ForceBubbles,
                ForceSynesthesia = settings.ForceSynesthesia,
                ForceDepth = settings.ForceDepth,
                ForceBloom = settings.ForceBloom,
                WiggleStrength = settings.WiggleStrength,
                GrowStartScale = settings.GrowStartScale,
                DeflateStartScale = settings.DeflateStartScale,
                ApproachDifferentScale = settings.ApproachDifferentScale,
                NoScopeHiddenComboCount = settings.NoScopeHiddenComboCount,
                MagnetisedAttractionStrength = settings.MagnetisedAttractionStrength,
                RepelRepulsionStrength = settings.RepelRepulsionStrength,
                DepthMaxDepth = settings.DepthMaxDepth,
                BloomMaxSizeComboCount = settings.BloomMaxSizeComboCount,
                BloomMaxCursorSize = settings.BloomMaxCursorSize,
                BarrelRollSpinSpeed = settings.BarrelRollSpinSpeed,
                MutedMuteComboCount = settings.MutedMuteComboCount,
                SectionName = settings.SectionName,
                DisplayColor = settings.DisplayColor,
            };
    }
}
