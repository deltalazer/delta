// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using DiffPlex;
using DiffPlex.Model;
using osu.Framework.Audio.Track;
using osu.Framework.Graphics.Textures;
using osu.Game.Beatmaps;
using osu.Game.Beatmaps.ControlPoints;
using osu.Game.Beatmaps.Formats;
using osu.Game.Beatmaps.HitObjectGimmicks;
using osu.Game.Beatmaps.SectionGimmicks;
using osu.Game.Extensions;
using osu.Game.IO;
using osu.Game.Rulesets.Objects.Types;
using osu.Game.Skinning;
using Decoder = osu.Game.Beatmaps.Formats.Decoder;

namespace osu.Game.Screens.Edit
{
    /// <summary>
    /// Patches an <see cref="EditorBeatmap"/> based on the difference between two legacy (.osu) states.
    /// </summary>
    public class LegacyEditorBeatmapPatcher
    {
        private readonly EditorBeatmap editorBeatmap;

        public LegacyEditorBeatmapPatcher(EditorBeatmap editorBeatmap)
        {
            this.editorBeatmap = editorBeatmap;
        }

        public void Patch(byte[] currentState, byte[] newState)
        {
            // Diff the beatmaps
            var result = new Differ().CreateLineDiffs(readString(currentState), readString(newState), true, false);
            IBeatmap newBeatmap = null;

            editorBeatmap.BeginChange();
            processHitObjects(result, () => newBeatmap ??= readBeatmap(newState));
            processTimingPoints(() => newBeatmap ??= readBeatmap(newState));
            processBreaks(() => newBeatmap ??= readBeatmap(newState));
            processBookmarks(() => newBeatmap ??= readBeatmap(newState));
            processSectionGimmicks(() => newBeatmap ??= readBeatmap(newState));
            processHitObjectGimmicks(() => newBeatmap ??= readBeatmap(newState));
            processHitObjectLocalData(() => newBeatmap ??= readBeatmap(newState));
            editorBeatmap.EndChange();
        }

        private void processSectionGimmicks(Func<IBeatmap> getNewBeatmap)
        {
            editorBeatmap.SectionGimmicks = cloneGimmicks(getNewBeatmap().SectionGimmicks ?? new BeatmapSectionGimmicks());
        }

        private void processHitObjectGimmicks(Func<IBeatmap> getNewBeatmap)
        {
            editorBeatmap.HitObjectGimmicks = cloneHitObjectGimmicks(getNewBeatmap().HitObjectGimmicks ?? new BeatmapHitObjectGimmicks());
        }

        private void processTimingPoints(Func<IBeatmap> getNewBeatmap)
        {
            ControlPointInfo newControlPoints = EditorBeatmap.ConvertControlPoints(getNewBeatmap().ControlPointInfo);

            // Remove all groups from the current beatmap which don't have a corresponding equal group in the new beatmap.
            foreach (var oldGroup in editorBeatmap.ControlPointInfo.Groups.ToArray())
            {
                var newGroup = newControlPoints.GroupAt(oldGroup.Time);

                if (!oldGroup.Equals(newGroup))
                    editorBeatmap.ControlPointInfo.RemoveGroup(oldGroup);
            }

            // Add all groups from the new beatmap which don't have a corresponding equal group in the old beatmap.
            foreach (var newGroup in newControlPoints.Groups)
            {
                var oldGroup = editorBeatmap.ControlPointInfo.GroupAt(newGroup.Time);

                if (!newGroup.Equals(oldGroup))
                {
                    foreach (var point in newGroup.ControlPoints)
                        editorBeatmap.ControlPointInfo.Add(newGroup.Time, point);
                }
            }
        }

        private void processBreaks(Func<IBeatmap> getNewBeatmap)
        {
            var newBreaks = getNewBeatmap().Breaks.ToArray();

            foreach (var oldBreak in editorBeatmap.Breaks.ToArray())
            {
                if (newBreaks.Any(b => b.Equals(oldBreak)))
                    continue;

                editorBeatmap.Breaks.Remove(oldBreak);
            }

            foreach (var newBreak in newBreaks)
            {
                if (editorBeatmap.Breaks.Any(b => b.Equals(newBreak)))
                    continue;

                editorBeatmap.Breaks.Add(newBreak);
            }
        }

        private void processBookmarks(Func<IBeatmap> getNewBeatmap)
        {
            var newBookmarks = getNewBeatmap().Bookmarks.ToHashSet();

            foreach (int oldBookmark in editorBeatmap.Bookmarks.ToArray())
            {
                if (newBookmarks.Contains(oldBookmark))
                    continue;

                editorBeatmap.Bookmarks.Remove(oldBookmark);
            }

            foreach (int newBookmark in newBookmarks)
            {
                if (editorBeatmap.Bookmarks.Contains(newBookmark))
                    continue;

                int idx = editorBeatmap.Bookmarks.BinarySearch(newBookmark);
                if (idx < 0)
                    editorBeatmap.Bookmarks.Insert(~idx, newBookmark);
            }
        }

        private void processHitObjects(DiffResult result, Func<IBeatmap> getNewBeatmap)
        {
            findChangedIndices(result, LegacyDecoder<Beatmap>.Section.HitObjects, out var removedIndices, out var addedIndices);

            for (int i = removedIndices.Count - 1; i >= 0; i--)
                editorBeatmap.RemoveAt(removedIndices[i]);

            if (addedIndices.Count > 0)
            {
                var newBeatmap = getNewBeatmap();

                foreach (int i in addedIndices)
                    editorBeatmap.Insert(i, newBeatmap.HitObjects[i]);
            }
        }

        private void processHitObjectLocalData(Func<IBeatmap> getNewBeatmap)
        {
            // This method handles data that are stored in control points in the legacy format,
            // but were moved to the hitobjects themselves in lazer.
            // Specifically, the data being referred to here consists of: slider velocity and sample information.

            // For simplicity, this implementation relies on the editor beatmap already having the same hitobjects in sequence as the new beatmap.
            // To guarantee that, `processHitObjects()` must be ran prior to this method for correct operation.
            // This is done to avoid the necessity of reimplementing/reusing parts of LegacyBeatmapDecoder that already treat this data correctly.

            var oldObjects = editorBeatmap.HitObjects;
            var newObjects = getNewBeatmap().HitObjects;

            Debug.Assert(oldObjects.Count == newObjects.Count);

            foreach (var (oldObject, newObject) in oldObjects.Zip(newObjects))
            {
                // if `oldObject` and `newObject` are the same, it means that `oldObject` was inserted into `editorBeatmap` by `processHitObjects()`.
                // in that case, there is nothing to do (and some of the subsequent changes may even prove destructive).
                if (ReferenceEquals(oldObject, newObject))
                    continue;

                if (oldObject is IHasSliderVelocity oldWithVelocity && newObject is IHasSliderVelocity newWithVelocity)
                    oldWithVelocity.SliderVelocityMultiplier = newWithVelocity.SliderVelocityMultiplier;

                oldObject.Samples = newObject.Samples;

                if (oldObject is IHasRepeats oldWithRepeats && newObject is IHasRepeats newWithRepeats)
                {
                    oldWithRepeats.NodeSamples.Clear();
                    oldWithRepeats.NodeSamples.AddRange(newWithRepeats.NodeSamples);
                }

                editorBeatmap.Update(oldObject);
            }
        }

        private void findChangedIndices(DiffResult result, LegacyDecoder<Beatmap>.Section section, out List<int> removedIndices, out List<int> addedIndices)
        {
            removedIndices = new List<int>();
            addedIndices = new List<int>();

            string[] oldArr = result.PiecesOld.ToArray();
            string[] newArr = result.PiecesNew.ToArray();

            // Find the start and end indices of the relevant section headers in both the old and the new beatmap file. Lines changed outside of the modified ranges are ignored.
            int oldSectionStartIndex = Array.IndexOf(oldArr, $"[{section}]");
            if (oldSectionStartIndex == -1)
                return;

            int oldSectionEndIndex = Array.FindIndex(oldArr, oldSectionStartIndex + 1, s => s.StartsWith('['));
            if (oldSectionEndIndex == -1)
                oldSectionEndIndex = oldArr.Length;

            int newSectionStartIndex = Array.IndexOf(newArr, $"[{section}]");
            if (newSectionStartIndex == -1)
                return;

            int newSectionEndIndex = Array.FindIndex(newArr, newSectionStartIndex + 1, s => s.StartsWith('['));
            if (newSectionEndIndex == -1)
                newSectionEndIndex = newArr.Length;

            foreach (var block in result.DiffBlocks)
            {
                // Removed indices
                for (int i = 0; i < block.DeleteCountA; i++)
                {
                    int objectIndex = block.DeleteStartA + i;

                    if (objectIndex <= oldSectionStartIndex || objectIndex >= oldSectionEndIndex)
                        continue;

                    removedIndices.Add(objectIndex - oldSectionStartIndex - 1);
                }

                // Added indices
                for (int i = 0; i < block.InsertCountB; i++)
                {
                    int objectIndex = block.InsertStartB + i;

                    if (objectIndex <= newSectionStartIndex || objectIndex >= newSectionEndIndex)
                        continue;

                    addedIndices.Add(objectIndex - newSectionStartIndex - 1);
                }
            }

            // Sort the indices to ensure that removal + insertion indices don't get jumbled up post-removal or post-insertion.
            // This isn't strictly required, but the differ makes no guarantees about order.
            removedIndices.Sort();
            addedIndices.Sort();
        }

        private string readString(byte[] state) => Encoding.UTF8.GetString(state);

        private IBeatmap readBeatmap(byte[] state)
        {
            using (var stream = new MemoryStream(state))
            using (var reader = new LineBufferedReader(stream, true))
            {
                var decoded = Decoder.GetDecoder<Beatmap>(reader).Decode(reader);
                decoded.BeatmapInfo.Ruleset = editorBeatmap.BeatmapInfo.Ruleset;
                return new PassThroughWorkingBeatmap(decoded).GetPlayableBeatmap(editorBeatmap.BeatmapInfo.Ruleset);
            }
        }

        private static BeatmapSectionGimmicks cloneGimmicks(BeatmapSectionGimmicks source)
            => new BeatmapSectionGimmicks
            {
                Sections = source.Sections.Select(s =>
                {
                    var settings = s.Settings ?? new SectionGimmickSettings();

                    return new SectionGimmickSection
                    {
                        Id = s.Id,
                        StartTime = s.StartTime,
                        EndTime = s.EndTime,
                        Settings = new SectionGimmickSettings
                        {
                            EnableHPGimmick = settings.EnableHPGimmick,
                            EnableNoMiss = settings.EnableNoMiss,
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
                        }
                    };
                }).ToList(),
            };

        private static BeatmapHitObjectGimmicks cloneHitObjectGimmicks(BeatmapHitObjectGimmicks source)
            => new BeatmapHitObjectGimmicks
            {
                Entries = source.Entries.Select(e =>
                {
                    var settings = e.Settings ?? new HitObjectGimmickSettings();

                    return new HitObjectGimmickEntry
                    {
                        ObjectId = e.ObjectId,
                        StartTime = e.StartTime,
                        ComboIndexWithOffsets = e.ComboIndexWithOffsets,
                        Settings = new HitObjectGimmickSettings
                        {
                            EnableHPGimmick = settings.EnableHPGimmick,
                            EnableNoMiss = settings.EnableNoMiss,
                            EnableCountLimits = settings.EnableCountLimits,
                            EnableGreatOffsetPenalty = settings.EnableGreatOffsetPenalty,
                            Max300s = settings.Max300s,
                            Max100s = settings.Max100s,
                            Max50s = settings.Max50s,
                            MaxMisses = settings.MaxMisses,
                            HP300 = settings.HP300,
                            HP100 = settings.HP100,
                            HP50 = settings.HP50,
                            HPMiss = settings.HPMiss,
                            GreatOffsetThresholdMs = settings.GreatOffsetThresholdMs,
                            GreatOffsetPenaltyHP = settings.GreatOffsetPenaltyHP,
                            EnableDifficultyOverrides = settings.EnableDifficultyOverrides,
                            AllowUnsafeDifficultyOverrideValues = settings.AllowUnsafeDifficultyOverrideValues,
                            SectionCircleSize = settings.SectionCircleSize,
                            SectionApproachRate = settings.SectionApproachRate,
                            SectionOverallDifficulty = settings.SectionOverallDifficulty,
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
                        }
                    };
                }).ToList(),
            };

        private class PassThroughWorkingBeatmap : WorkingBeatmap
        {
            private readonly IBeatmap beatmap;

            public PassThroughWorkingBeatmap(IBeatmap beatmap)
                : base(beatmap.BeatmapInfo, null)
            {
                this.beatmap = beatmap;
            }

            protected override IBeatmap GetBeatmap() => beatmap;

            public override Texture GetBackground() => throw new NotImplementedException();

            protected override Track GetBeatmapTrack() => throw new NotImplementedException();

            protected internal override ISkin GetSkin() => throw new NotImplementedException();

            public override Stream GetStream(string storagePath) => throw new NotImplementedException();
        }
    }
}
