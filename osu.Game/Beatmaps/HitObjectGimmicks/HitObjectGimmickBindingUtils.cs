// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using osu.Game.Rulesets.Objects;
using osu.Game.Rulesets.Objects.Types;

namespace osu.Game.Beatmaps.HitObjectGimmicks
{
    public static class HitObjectGimmickBindingUtils
    {
        private static long nextObjectId;

        public static long GenerateNewObjectId()
            => Interlocked.Increment(ref nextObjectId);

        public static void EnsureObjectIds(IEnumerable<HitObject> hitObjects)
        {
            var objectList = hitObjects.ToList();
            var usedObjectIds = new HashSet<long>();

            foreach (var hitObject in objectList)
            {
                if (hitObject.GimmickObjectId.HasValue && usedObjectIds.Add(hitObject.GimmickObjectId.Value))
                    continue;

                hitObject.GimmickObjectId = generateUniqueObjectId(usedObjectIds);
            }
        }

        public static void SynchroniseEntriesWithHitObjects(IBeatmap beatmap)
        {
            var hitObjects = beatmap.HitObjects.ToList();

            var gimmicks = beatmap.HitObjectGimmicks;

            if (gimmicks == null)
            {
                EnsureObjectIds(hitObjects);
                return;
            }

            if (gimmicks.Entries.Count == 0)
            {
                EnsureObjectIds(hitObjects);
                return;
            }

            if (hitObjects.Count == 0)
                return;

            var usedObjectIds = new HashSet<long>(hitObjects.Where(h => h.GimmickObjectId.HasValue).Select(h => h.GimmickObjectId!.Value));

            var byObjectId = hitObjects
                             .Where(h => h.GimmickObjectId.HasValue)
                             .GroupBy(h => h.GimmickObjectId!.Value)
                             .ToDictionary(g => g.Key, g => new Queue<HitObject>(g));

            var byLegacyKey = hitObjects
                             .OfType<IHasComboInformation>()
                             .GroupBy(h => (((HitObject)h).StartTime, h.ComboIndexWithOffsets))
                             .ToDictionary(g => g.Key, g => new Queue<HitObject>(g.Cast<HitObject>()));

            var byStartTime = hitObjects
                             .OfType<IHasComboInformation>()
                             .Select(h => (hitObject: (HitObject)h, combo: h.ComboIndexWithOffsets))
                             .GroupBy(v => v.hitObject.StartTime)
                             .ToDictionary(g => g.Key, g => new Queue<HitObject>(g.OrderBy(v => v.combo).Select(v => v.hitObject)));

            var entries = gimmicks.Entries;
            var claimedHitObjects = new HashSet<HitObject>();
            var boundEntries = new List<(HitObjectGimmickEntry entry, HitObject hitObject)>();

            // Iterate backwards so newest entries win if duplicates exist in file.
            for (int i = entries.Count - 1; i >= 0; i--)
            {
                var entry = entries[i];

                if (entry == null)
                    continue;

                HitObject? matched = null;

                if (entry.ObjectId.HasValue
                    && byObjectId.TryGetValue(entry.ObjectId.Value, out Queue<HitObject>? objectIdCandidates)
                    && tryDequeueUnclaimed(objectIdCandidates, claimedHitObjects, out matched))
                {
                    // matched by existing object id.
                }

                if (matched == null
                    && byLegacyKey.TryGetValue((entry.StartTime, entry.ComboIndexWithOffsets), out Queue<HitObject>? legacyCandidates)
                    && tryDequeueUnclaimed(legacyCandidates, claimedHitObjects, out matched))
                {
                    // matched by legacy key.
                }

                if (matched == null
                    && byStartTime.TryGetValue(entry.StartTime, out Queue<HitObject>? timeCandidates)
                    && tryDequeueUnclaimed(timeCandidates, claimedHitObjects, out matched))
                {
                    // matched by start-time fallback.
                }

                if (matched == null)
                    continue;

                if (!matched.GimmickObjectId.HasValue)
                {
                    if (entry.ObjectId.HasValue)
                    {
                        bool objectIdUsedByAnotherObject = hitObjects.Any(h => !ReferenceEquals(h, matched) && h.GimmickObjectId == entry.ObjectId);

                        matched.GimmickObjectId = objectIdUsedByAnotherObject
                            ? generateUniqueObjectId(usedObjectIds)
                            : entry.ObjectId;
                    }
                    else
                        matched.GimmickObjectId = generateUniqueObjectId(usedObjectIds);
                }
                else if (entry.ObjectId.HasValue && matched.GimmickObjectId.Value != entry.ObjectId.Value)
                {
                    // Preserve persisted id when possible, falling back to the matched object's id if conflicting.
                    bool persistedIdInUseByOtherObject = hitObjects.Any(h => !ReferenceEquals(h, matched) && h.GimmickObjectId == entry.ObjectId);

                    if (!persistedIdInUseByOtherObject)
                        matched.GimmickObjectId = entry.ObjectId;
                    else
                        entry.ObjectId = matched.GimmickObjectId;
                }

                boundEntries.Add((entry, matched));
            }

            var validEntries = boundEntries.Select(b => b.entry).ToHashSet();
            entries.RemoveAll(e => e == null || !validEntries.Contains(e));

            EnsureObjectIds(hitObjects);

            foreach (var (entry, hitObject) in boundEntries)
            {
                entry.ObjectId = hitObject.GimmickObjectId;
                entry.StartTime = hitObject.StartTime;

                if (hitObject is IHasComboInformation comboInformation)
                    entry.ComboIndexWithOffsets = comboInformation.ComboIndexWithOffsets;
            }

            // Collapse duplicate entries for the same object id, keeping the latest one.
            var latestByObjectId = new Dictionary<long, HitObjectGimmickEntry>();

            foreach (var entry in entries)
            {
                if (entry.ObjectId.HasValue)
                    latestByObjectId[entry.ObjectId.Value] = entry;
            }

            entries.RemoveAll(e => e.ObjectId.HasValue
                                   && latestByObjectId.TryGetValue(e.ObjectId.Value, out var latest)
                                   && !ReferenceEquals(e, latest));

            // Legacy-key dedupe for entries still lacking an object id.
            var latestByLegacyKey = new Dictionary<(double StartTime, int ComboIndexWithOffsets), HitObjectGimmickEntry>();

            foreach (var entry in entries)
            {
                if (!entry.ObjectId.HasValue)
                    latestByLegacyKey[(entry.StartTime, entry.ComboIndexWithOffsets)] = entry;
            }

            entries.RemoveAll(e => !e.ObjectId.HasValue
                                   && latestByLegacyKey.TryGetValue((e.StartTime, e.ComboIndexWithOffsets), out var latest)
                                   && !ReferenceEquals(e, latest));
        }

        private static long generateUniqueObjectId(HashSet<long> usedObjectIds)
        {
            long id = 1;

            while (!usedObjectIds.Add(id))
                id++;

            advanceSharedCounterTo(id);

            return id;
        }

        private static void advanceSharedCounterTo(long value)
        {
            long current;

            while ((current = Interlocked.Read(ref nextObjectId)) < value)
            {
                if (Interlocked.CompareExchange(ref nextObjectId, value, current) == current)
                    break;
            }
        }

        private static bool tryDequeueUnclaimed(Queue<HitObject>? candidates, HashSet<HitObject> claimed, out HitObject? hitObject)
        {
            while (candidates != null && candidates.Count > 0)
            {
                var candidate = candidates.Dequeue();

                if (claimed.Add(candidate))
                {
                    hitObject = candidate;
                    return true;
                }
            }

            hitObject = null;
            return false;
        }

        public static Dictionary<long, HitObjectGimmickSettings> CreateLookupByObjectId(BeatmapHitObjectGimmicks gimmicks)
        {
            var lookup = new Dictionary<long, HitObjectGimmickSettings>();

            foreach (var entry in gimmicks.Entries)
            {
                if (!entry.ObjectId.HasValue)
                    continue;

                lookup[entry.ObjectId.Value] = entry.Settings ?? new HitObjectGimmickSettings();
            }

            return lookup;
        }

        public static Dictionary<(double StartTime, int ComboIndexWithOffsets), HitObjectGimmickSettings> CreateLookupByLegacyKey(BeatmapHitObjectGimmicks gimmicks)
        {
            var lookup = new Dictionary<(double StartTime, int ComboIndexWithOffsets), HitObjectGimmickSettings>();

            foreach (var entry in gimmicks.Entries)
                lookup[(entry.StartTime, entry.ComboIndexWithOffsets)] = entry.Settings ?? new HitObjectGimmickSettings();

            return lookup;
        }

        public static bool TryGetSettings(HitObject hitObject,
                                          Dictionary<long, HitObjectGimmickSettings> objectIdLookup,
                                          Dictionary<(double StartTime, int ComboIndexWithOffsets), HitObjectGimmickSettings> legacyLookup,
                                          out HitObjectGimmickSettings settings)
        {
            if (hitObject.GimmickObjectId.HasValue
                && objectIdLookup.TryGetValue(hitObject.GimmickObjectId.Value, out settings!))
            {
                return true;
            }

            if (hitObject is IHasComboInformation comboInformation
                && legacyLookup.TryGetValue((hitObject.StartTime, comboInformation.ComboIndexWithOffsets), out settings!))
            {
                return true;
            }

            settings = null!;
            return false;
        }

        public static HitObjectGimmickSettings CloneSettings(HitObjectGimmickSettings source)
            => new HitObjectGimmickSettings
            {
                IsFakeNote = source.IsFakeNote,
                FakePunishMode = source.FakePunishMode,
                FakePlayHitsound = source.FakePlayHitsound,
                FakeAutoHitOnApproachClose = source.FakeAutoHitOnApproachClose,
                FakeAutoHitPlayHitsound = source.FakeAutoHitPlayHitsound,
                FakeRevealEnabled = source.FakeRevealEnabled,
                FakeRevealRed = source.FakeRevealRed,
                FakeRevealGreen = source.FakeRevealGreen,
                FakeRevealBlue = source.FakeRevealBlue,
                FakeRevealStrength = source.FakeRevealStrength,
                FakeRevealLeadInStartMs = source.FakeRevealLeadInStartMs,
                FakeRevealLeadInLengthMs = source.FakeRevealLeadInLengthMs,
                FakeRevealFadeOutStartMs = source.FakeRevealFadeOutStartMs,
                FakeRevealFadeOutLengthMs = source.FakeRevealFadeOutLengthMs,

                EnableHPGimmick = source.EnableHPGimmick,
                EnableNoMiss = source.EnableNoMiss,
                ForceAllMiss = source.ForceAllMiss,
                FreezeHP = source.FreezeHP,
                FreezeAccuracy = source.FreezeAccuracy,
                FreezeCombo = source.FreezeCombo,
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

        public static BeatmapHitObjectGimmicks CloneGimmicks(BeatmapHitObjectGimmicks source)
            => new BeatmapHitObjectGimmicks
            {
                Entries = source.Entries.Select(e => new HitObjectGimmickEntry
                {
                    ObjectId = e.ObjectId,
                    StartTime = e.StartTime,
                    ComboIndexWithOffsets = e.ComboIndexWithOffsets,
                    Settings = CloneSettings(e.Settings ?? new HitObjectGimmickSettings()),
                }).ToList(),
            };
    }
}
