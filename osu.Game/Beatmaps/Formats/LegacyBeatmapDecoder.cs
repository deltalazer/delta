// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using osu.Framework.Extensions;
using osu.Framework.Logging;
using osu.Game.Audio;
using osu.Game.Beatmaps.ControlPoints;
using osu.Game.Beatmaps.HitObjectGimmicks;
using osu.Game.Beatmaps.SectionGimmicks;
using osu.Game.Beatmaps.Legacy;
using osu.Game.Beatmaps.Timing;
using osu.Game.IO;
using osu.Game.Rulesets;
using osu.Game.Rulesets.Objects;
using osu.Game.Rulesets.Objects.Legacy;
using osu.Game.Rulesets.Objects.Types;
using osu.Game.Screens.Edit;
using osu.Game.Utils;
using osuTK.Graphics;
using Color4 = osuTK.Graphics.Color4;

namespace osu.Game.Beatmaps.Formats
{
    public class LegacyBeatmapDecoder : LegacyDecoder<Beatmap>
    {
        /// <summary>
        /// An offset which needs to be applied to old beatmaps (v4 and lower) to correct timing changes that were applied at a game client level.
        /// </summary>
        public const int EARLY_VERSION_TIMING_OFFSET = 24;

        /// <summary>
        /// A small adjustment to the start time of sample control points to account for rounding/precision errors.
        /// </summary>
        /// <remarks>
        /// Compare: https://github.com/peppy/osu-stable-reference/blob/master/osu!/GameplayElements/HitObjects/HitObject.cs#L319
        /// </remarks>
        public const double CONTROL_POINT_LENIENCY = 5;

        /// <summary>
        /// The maximum allowed number of keys in mania beatmaps.
        /// </summary>
        public const int MAX_MANIA_KEY_COUNT = 18;

        internal static RulesetStore? RulesetStore;

        private Beatmap beatmap = null!;
        private ConvertHitObjectParser parser = null!;

        private LegacySampleBank defaultSampleBank;
        private int defaultSampleVolume = 100;

        public static void Register()
        {
            AddDecoder<Beatmap>(@"osu file format v", m => new LegacyBeatmapDecoder(Parsing.ParseInt(m.Split('v').Last())));
            SetFallbackDecoder<Beatmap>(() => new LegacyBeatmapDecoder());
        }

        /// <summary>
        /// Whether beatmap or runtime offsets should be applied. Defaults on; only disable for testing purposes.
        /// </summary>
        public bool ApplyOffsets = true;

        private readonly int offset;

        public LegacyBeatmapDecoder(int version = LATEST_VERSION)
            : base(version)
        {
            if (RulesetStore == null)
            {
                Logger.Log($"A {nameof(RulesetStore)} was not provided via {nameof(Decoder)}.{nameof(RegisterDependencies)}; falling back to default {nameof(AssemblyRulesetStore)}.");
                RulesetStore = new AssemblyRulesetStore();
            }

            offset = FormatVersion < 5 ? EARLY_VERSION_TIMING_OFFSET : 0;
        }

        protected override Beatmap CreateTemplateObject()
        {
            var templateBeatmap = base.CreateTemplateObject();
            templateBeatmap.ControlPointInfo = new LegacyControlPointInfo();
            return templateBeatmap;
        }

        protected override void ParseStreamInto(LineBufferedReader stream, Beatmap beatmap)
        {
            this.beatmap = beatmap;
            this.beatmap.BeatmapVersion = FormatVersion;
            parser = new ConvertHitObjectParser(getOffsetTime(), FormatVersion);

            ApplyLegacyDefaults(this.beatmap);

            base.ParseStreamInto(stream, beatmap);

            applyDifficultyRestrictions(beatmap.Difficulty, beatmap);

            flushPendingPoints();

            // Objects may be out of order *only* if a user has manually edited an .osu file.
            // Unfortunately there are ranked maps in this state (example: https://osu.ppy.sh/s/594828).
            // OrderBy is used to guarantee that the parsing order of hitobjects with equal start times is maintained (stably-sorted)
            // The parsing order of hitobjects matters in mania difficulty calculation
            this.beatmap.HitObjects = this.beatmap.HitObjects.OrderBy(h => h.StartTime).ToList();

            postProcessBreaks(this.beatmap);

            foreach (var hitObject in this.beatmap.HitObjects)
            {
                applyDefaults(hitObject);
                applySamples(hitObject);
            }
        }

        /// <summary>
        /// Ensures that all <see cref="BeatmapDifficulty"/> settings are within the allowed ranges.
        /// See also: https://github.com/peppy/osu-stable-reference/blob/0e425c0d525ef21353c8293c235cc0621d28338b/osu!/GameplayElements/Beatmaps/Beatmap.cs#L567-L614
        /// </summary>
        private static void applyDifficultyRestrictions(BeatmapDifficulty difficulty, Beatmap beatmap)
        {
            difficulty.DrainRate = Math.Clamp(difficulty.DrainRate, 0, 10);

            // mania uses "circle size" for key count, thus different allowable range
            difficulty.CircleSize = beatmap.BeatmapInfo.Ruleset.OnlineID != 3
                ? Math.Clamp(difficulty.CircleSize, 0, 10)
                : Math.Clamp(difficulty.CircleSize, 1, MAX_MANIA_KEY_COUNT);

            difficulty.OverallDifficulty = Math.Clamp(difficulty.OverallDifficulty, 0, 10);
            difficulty.ApproachRate = Math.Clamp(difficulty.ApproachRate, 0, 10);

            difficulty.SliderMultiplier = Math.Clamp(difficulty.SliderMultiplier, 0.4, 3.6);
            difficulty.SliderTickRate = Math.Clamp(difficulty.SliderTickRate, 0.5, 8);
        }

        /// <summary>
        /// Processes the beatmap such that a new combo is started the first hitobject following each break.
        /// </summary>
        private static void postProcessBreaks(Beatmap beatmap)
        {
            int currentBreak = 0;
            bool forceNewCombo = false;

            foreach (var h in beatmap.HitObjects.OfType<ConvertHitObject>())
            {
                while (currentBreak < beatmap.Breaks.Count && beatmap.Breaks[currentBreak].EndTime < h.StartTime)
                {
                    forceNewCombo = true;
                    currentBreak++;
                }

                h.NewCombo |= forceNewCombo;
                forceNewCombo = false;
            }
        }

        private void applyDefaults(HitObject hitObject)
        {
            DifficultyControlPoint difficultyControlPoint = (beatmap.ControlPointInfo as LegacyControlPointInfo)?.DifficultyPointAt(hitObject.StartTime) ?? DifficultyControlPoint.DEFAULT;

            if (hitObject is IHasGenerateTicks hasGenerateTicks)
                hasGenerateTicks.GenerateTicks = difficultyControlPoint.GenerateTicks;

            if (hitObject is IHasSliderVelocity hasSliderVelocity)
                hasSliderVelocity.SliderVelocityMultiplier = difficultyControlPoint.SliderVelocity;

            hitObject.ApplyDefaults(beatmap.ControlPointInfo, beatmap.Difficulty);
        }

        private void applySamples(HitObject hitObject)
        {
            if (hitObject is IHasRepeats hasRepeats)
            {
                SampleControlPoint sampleControlPoint = (beatmap.ControlPointInfo as LegacyControlPointInfo)?.SamplePointAt(hitObject.StartTime + CONTROL_POINT_LENIENCY + 1)
                                                        ?? SampleControlPoint.DEFAULT;
                hitObject.Samples = hitObject.Samples.Select(sampleControlPoint.ApplyTo).ToList();

                for (int i = 0; i < hasRepeats.NodeSamples.Count; i++)
                {
                    double time = hitObject.StartTime + i * hasRepeats.Duration / hasRepeats.SpanCount() + CONTROL_POINT_LENIENCY;
                    var nodeSamplePoint = (beatmap.ControlPointInfo as LegacyControlPointInfo)?.SamplePointAt(time) ?? SampleControlPoint.DEFAULT;

                    hasRepeats.NodeSamples[i] = hasRepeats.NodeSamples[i].Select(nodeSamplePoint.ApplyTo).ToList();
                }
            }
            else
            {
                SampleControlPoint sampleControlPoint = (beatmap.ControlPointInfo as LegacyControlPointInfo)?.SamplePointAt(hitObject.GetEndTime() + CONTROL_POINT_LENIENCY)
                                                        ?? SampleControlPoint.DEFAULT;
                hitObject.Samples = hitObject.Samples.Select(sampleControlPoint.ApplyTo).ToList();
            }
        }

        /// <summary>
        /// Some `BeatmapInfo` members have default values that differ from the default values used by stable.
        /// In addition, legacy beatmaps will sometimes not contain some configuration keys, in which case
        /// the legacy default values should be used.
        /// This method's intention is to restore those legacy defaults.
        /// See also: https://osu.ppy.sh/wiki/en/Client/File_formats/Osu_%28file_format%29
        /// </summary>
        internal static void ApplyLegacyDefaults(Beatmap beatmap)
        {
            beatmap.WidescreenStoryboard = false;
            // in a perfect world this would throw if osu! ruleset couldn't be found,
            // but unfortunately there are "legitimate" cases where it's not there (i.e. ruleset test projects),
            // so attempt to trudge on with whatever it is that's in `BeatmapInfo` if the lookup fails.
            beatmap.BeatmapInfo.Ruleset = RulesetStore?.GetRuleset(0) ?? beatmap.BeatmapInfo.Ruleset;
        }

        protected override void ParseLine(Beatmap beatmap, Section section, string line)
        {
            switch (section)
            {
                case Section.General:
                    handleGeneral(line);
                    return;

                case Section.Editor:
                    handleEditor(line);
                    return;

                case Section.Metadata:
                    handleMetadata(line);
                    return;

                case Section.Difficulty:
                    handleDifficulty(line);
                    return;

                case Section.Events:
                    handleEvent(line);
                    return;

                case Section.TimingPoints:
                    handleTimingPoint(line);
                    return;

                case Section.HitObjects:
                    handleHitObject(line);
                    return;

                case Section.BeatmapSectionGimmicks:
                    handleSectionGimmick(line);
                    return;

                case Section.BeatmapHitObjectGimmicks:
                    handleHitObjectGimmick(line);
                    return;
            }

            base.ParseLine(beatmap, section, line);
        }

        private void handleGeneral(string line)
        {
            var pair = SplitKeyVal(line);

            var metadata = beatmap.BeatmapInfo.Metadata;

            switch (pair.Key)
            {
                case @"AudioFilename":
                    metadata.AudioFile = pair.Value.ToStandardisedPath();
                    break;

                case @"AudioLeadIn":
                    beatmap.AudioLeadIn = Parsing.ParseInt(pair.Value);
                    break;

                case @"PreviewTime":
                    int time = Parsing.ParseInt(pair.Value);
                    metadata.PreviewTime = time == -1 ? time : getOffsetTime(time);
                    break;

                case @"SampleSet":
                    defaultSampleBank = Enum.Parse<LegacySampleBank>(pair.Value);
                    break;

                case @"SampleVolume":
                    defaultSampleVolume = Parsing.ParseInt(pair.Value);
                    break;

                case @"StackLeniency":
                    beatmap.StackLeniency = Parsing.ParseFloat(pair.Value);
                    break;

                case @"Mode":
                    beatmap.BeatmapInfo.Ruleset = RulesetStore?.GetRuleset(Parsing.ParseInt(pair.Value)) ?? throw new ArgumentException("Ruleset is not available locally.");
                    break;

                case @"LetterboxInBreaks":
                    beatmap.LetterboxInBreaks = Parsing.ParseInt(pair.Value) == 1;
                    break;

                case @"SpecialStyle":
                    beatmap.SpecialStyle = Parsing.ParseInt(pair.Value) == 1;
                    break;

                case @"WidescreenStoryboard":
                    beatmap.WidescreenStoryboard = Parsing.ParseInt(pair.Value) == 1;
                    break;

                case @"EpilepsyWarning":
                    beatmap.EpilepsyWarning = Parsing.ParseInt(pair.Value) == 1;
                    break;

                case @"SamplesMatchPlaybackRate":
                    beatmap.SamplesMatchPlaybackRate = Parsing.ParseInt(pair.Value) == 1;
                    break;

                case @"Countdown":
                    beatmap.Countdown = Enum.Parse<CountdownType>(pair.Value);
                    break;

                case @"CountdownOffset":
                    beatmap.CountdownOffset = Parsing.ParseInt(pair.Value);
                    break;
            }
        }

        private void handleEditor(string line)
        {
            var pair = SplitKeyVal(line);

            switch (pair.Key)
            {
                case @"Bookmarks":
                    beatmap.Bookmarks = pair.Value.Split(',').Select(v =>
                    {
                        bool result = int.TryParse(v, out int val);
                        return new { result, val };
                    }).Where(p => p.result).Select(p => p.val).ToArray();
                    break;

                case @"DistanceSpacing":
                    beatmap.DistanceSpacing = Math.Max(0, Parsing.ParseDouble(pair.Value));
                    break;

                case @"BeatDivisor":
                    beatmap.BeatmapInfo.BeatDivisor = Math.Clamp(Parsing.ParseInt(pair.Value), BindableBeatDivisor.MINIMUM_DIVISOR, BindableBeatDivisor.MAXIMUM_DIVISOR);
                    break;

                case @"GridSize":
                    beatmap.GridSize = Parsing.ParseInt(pair.Value);
                    break;

                case @"TimelineZoom":
                    beatmap.TimelineZoom = Math.Max(0, Parsing.ParseDouble(pair.Value));
                    break;
            }
        }

        private void handleMetadata(string line)
        {
            var pair = SplitKeyVal(line);

            var metadata = beatmap.BeatmapInfo.Metadata;

            switch (pair.Key)
            {
                case @"Title":
                    metadata.Title = pair.Value;
                    break;

                case @"TitleUnicode":
                    metadata.TitleUnicode = pair.Value;
                    break;

                case @"Artist":
                    metadata.Artist = pair.Value;
                    break;

                case @"ArtistUnicode":
                    metadata.ArtistUnicode = pair.Value;
                    break;

                case @"Creator":
                    metadata.Author.Username = pair.Value;
                    break;

                case @"Version":
                    beatmap.BeatmapInfo.DifficultyName = pair.Value;
                    break;

                case @"Source":
                    metadata.Source = pair.Value;
                    break;

                case @"Tags":
                    metadata.Tags = pair.Value;
                    break;

                case @"BeatmapID":
                    beatmap.BeatmapInfo.OnlineID = Parsing.ParseInt(pair.Value);
                    break;

                case @"BeatmapSetID":
                    beatmap.BeatmapInfo.BeatmapSet = new BeatmapSetInfo { OnlineID = Parsing.ParseInt(pair.Value) };
                    break;
            }
        }

        private void handleDifficulty(string line)
        {
            var pair = SplitKeyVal(line);

            var difficulty = beatmap.Difficulty;

            switch (pair.Key)
            {
                case @"HPDrainRate":
                    difficulty.DrainRate = Parsing.ParseFloat(pair.Value);
                    break;

                case @"CircleSize":
                    difficulty.CircleSize = Parsing.ParseFloat(pair.Value);
                    break;

                case @"OverallDifficulty":
                    difficulty.OverallDifficulty = Parsing.ParseFloat(pair.Value);
                    if (!hasApproachRate)
                        difficulty.ApproachRate = difficulty.OverallDifficulty;
                    break;

                case @"ApproachRate":
                    difficulty.ApproachRate = Parsing.ParseFloat(pair.Value);
                    hasApproachRate = true;
                    break;

                case @"SliderMultiplier":
                    difficulty.SliderMultiplier = Parsing.ParseDouble(pair.Value);
                    break;

                case @"SliderTickRate":
                    difficulty.SliderTickRate = Parsing.ParseDouble(pair.Value);
                    break;
            }
        }

        private void handleEvent(string line)
        {
            string[] split = line.Split(',');

            // Until we have full storyboard encoder coverage, let's track any lines which aren't handled
            // and store them to a temporary location such that they aren't lost on editor save / export.
            bool lineSupportedByEncoder = false;

            if (Enum.TryParse(split[0], out LegacyEventType type))
            {
                switch (type)
                {
                    case LegacyEventType.Sprite:
                        // Generally, the background is the first thing defined in a beatmap file.
                        // In some older beatmaps, it is not present and replaced by a storyboard-level background instead.
                        // Allow the first sprite (by file order) to act as the background in such cases.
                        if (string.IsNullOrEmpty(beatmap.BeatmapInfo.Metadata.BackgroundFile))
                        {
                            beatmap.BeatmapInfo.Metadata.BackgroundFile = CleanFilename(split[3]);
                            lineSupportedByEncoder = true;
                        }

                        break;

                    case LegacyEventType.Video:
                        string filename = CleanFilename(split[2]);

                        // Some very old beatmaps had incorrect type specifications for their backgrounds (ie. using 1 for VIDEO
                        // instead of 0 for BACKGROUND). To handle this gracefully, check the file extension against known supported
                        // video extensions and handle similar to a background if it doesn't match.
                        if (!SupportedExtensions.VIDEO_EXTENSIONS.Contains(Path.GetExtension(filename).ToLowerInvariant()))
                        {
                            beatmap.BeatmapInfo.Metadata.BackgroundFile = filename;
                            lineSupportedByEncoder = true;
                        }

                        break;

                    case LegacyEventType.Background:
                        beatmap.BeatmapInfo.Metadata.BackgroundFile = CleanFilename(split[2]);
                        lineSupportedByEncoder = true;
                        break;

                    case LegacyEventType.Break:
                        double start = getOffsetTime(Parsing.ParseDouble(split[1]));
                        double end = Math.Max(start, getOffsetTime(Parsing.ParseDouble(split[2])));

                        beatmap.Breaks.Add(new BreakPeriod(start, end));
                        lineSupportedByEncoder = true;
                        break;
                }
            }

            if (!lineSupportedByEncoder)
                beatmap.UnhandledEventLines.Add(line);
        }

        private void handleTimingPoint(string line)
        {
            string[] split = line.Split(',');

            double time = getOffsetTime(Parsing.ParseDouble(split[0].Trim()));

            // beatLength is allowed to be NaN to handle an edge case in which some beatmaps use NaN slider velocity to disable slider tick generation (see LegacyDifficultyControlPoint).
            double beatLength = Parsing.ParseDouble(split[1].Trim(), allowNaN: true);

            // If beatLength is NaN, speedMultiplier should still be 1 because all comparisons against NaN are false.
            double speedMultiplier = beatLength < 0 ? 100.0 / -beatLength : 1;

            TimeSignature timeSignature = TimeSignature.SimpleQuadruple;
            if (split.Length >= 3)
                timeSignature = split[2][0] == '0' ? TimeSignature.SimpleQuadruple : new TimeSignature(Parsing.ParseInt(split[2]));

            LegacySampleBank sampleSet = defaultSampleBank;
            if (split.Length >= 4)
                sampleSet = (LegacySampleBank)Parsing.ParseInt(split[3]);

            int customSampleBank = 0;
            if (split.Length >= 5)
                customSampleBank = Parsing.ParseInt(split[4]);

            int sampleVolume = defaultSampleVolume;
            if (split.Length >= 6)
                sampleVolume = Parsing.ParseInt(split[5]);

            bool timingChange = true;
            if (split.Length >= 7)
                timingChange = split[6][0] == '1';

            bool kiaiMode = false;
            bool omitFirstBarSignature = false;

            if (split.Length >= 8)
            {
                LegacyEffectFlags effectFlags = (LegacyEffectFlags)Parsing.ParseInt(split[7]);
                kiaiMode = effectFlags.HasFlag(LegacyEffectFlags.Kiai);
                omitFirstBarSignature = effectFlags.HasFlag(LegacyEffectFlags.OmitFirstBarLine);
            }

            string stringSampleSet = sampleSet.ToString().ToLowerInvariant();
            if (stringSampleSet == @"none")
                stringSampleSet = HitSampleInfo.BANK_NORMAL;

            if (timingChange)
            {
                if (double.IsNaN(beatLength))
                    throw new InvalidDataException("Beat length cannot be NaN in a timing control point");

                var controlPoint = CreateTimingControlPoint();

                controlPoint.BeatLength = beatLength;
                controlPoint.TimeSignature = timeSignature;
                controlPoint.OmitFirstBarLine = omitFirstBarSignature;

                addControlPoint(time, controlPoint, true);
            }

            int onlineRulesetID = beatmap.BeatmapInfo.Ruleset.OnlineID;

            addControlPoint(time, new DifficultyControlPoint
            {
                GenerateTicks = !double.IsNaN(beatLength),
                SliderVelocity = speedMultiplier,
            }, timingChange);

            var effectPoint = new EffectControlPoint
            {
                KiaiMode = kiaiMode,
            };

            // osu!taiko and osu!mania use effect points rather than difficulty points for scroll speed adjustments.
            if (onlineRulesetID == 1 || onlineRulesetID == 3)
                effectPoint.ScrollSpeed = speedMultiplier;

            addControlPoint(time, effectPoint, timingChange);

            addControlPoint(time, new LegacySampleControlPoint
            {
                SampleBank = stringSampleSet,
                SampleVolume = sampleVolume,
                CustomSampleBank = customSampleBank,
            }, timingChange);
        }

        private readonly List<ControlPoint> pendingControlPoints = new List<ControlPoint>();
        private readonly HashSet<Type> pendingControlPointTypes = new HashSet<Type>();
        private double pendingControlPointsTime;
        private bool hasApproachRate;

        private void addControlPoint(double time, ControlPoint point, bool timingChange)
        {
            if (time != pendingControlPointsTime)
                flushPendingPoints();

            if (timingChange)
                pendingControlPoints.Insert(0, point);
            else
                pendingControlPoints.Add(point);

            pendingControlPointsTime = time;
        }

        private void flushPendingPoints()
        {
            // Changes from non-timing-points are added to the end of the list (see addControlPoint()) and should override any changes from timing-points (added to the start of the list).
            for (int i = pendingControlPoints.Count - 1; i >= 0; i--)
            {
                var type = pendingControlPoints[i].GetType();
                if (!pendingControlPointTypes.Add(type))
                    continue;

                beatmap.ControlPointInfo.Add(pendingControlPointsTime, pendingControlPoints[i]);
            }

            pendingControlPoints.Clear();
            pendingControlPointTypes.Clear();
        }

        private void handleHitObject(string line)
        {
            var obj = parser.Parse(line);
            obj.ApplyDefaults(beatmap.ControlPointInfo, beatmap.Difficulty);

            beatmap.HitObjects.Add(obj);
        }

        private void handleSectionGimmick(string line)
        {
            string[] split = line.Split(',', 4);
            if (split.Length < 3)
                return;

            if (!int.TryParse(split[0], out int id))
                return;

            if (!double.TryParse(split[1], NumberStyles.Float, CultureInfo.InvariantCulture, out double startTime))
                return;

            if (!double.TryParse(split[2], NumberStyles.Float, CultureInfo.InvariantCulture, out double endTime))
                return;

            var section = new SectionGimmickSection
            {
                Id = id,
                StartTime = startTime,
                EndTime = endTime,
                Settings = new SectionGimmickSettings()
            };

            if (split.Length == 4 && !string.IsNullOrEmpty(split[3]))
            {
                foreach (string kv in split[3].Split('|'))
                {
                    if (string.IsNullOrEmpty(kv))
                        continue;

                    var pair = SplitKeyVal(kv, '=');
                    string key = pair.Key;
                    string value = pair.Value;

                    switch (key)
                    {
                        case "EnableHPGimmick": section.Settings.EnableHPGimmick = parseBool(value); break;
                        case "EnableNoMiss": section.Settings.EnableNoMiss = parseBool(value); break;
                        case "EnableAccuracyRequirement": section.Settings.EnableAccuracyRequirement = parseBool(value); break;
                        case "RequiredAccuracy": section.Settings.RequiredAccuracy = Parsing.ParseFloat(value); break;
                        case "EnableCountLimits": section.Settings.EnableCountLimits = parseBool(value); break;
                        case "EnableNoMissedSliderEnd": section.Settings.EnableNoMissedSliderEnd = parseBool(value); break;
                        case "EnableGreatOffsetPenalty": section.Settings.EnableGreatOffsetPenalty = parseBool(value); break;
                        case "Max300s": section.Settings.Max300s = Parsing.ParseInt(value); break;
                        case "Max100s": section.Settings.Max100s = Parsing.ParseInt(value); break;
                        case "Max50s": section.Settings.Max50s = Parsing.ParseInt(value); break;
                        case "MaxMisses": section.Settings.MaxMisses = Parsing.ParseInt(value); break;
                        case "Max300sAffectsSliderEndsAndTicks": section.Settings.Max300sAffectsSliderEndsAndTicks = parseBool(value); break;
                        case "Max100sAffectsSliderEndsAndTicks": section.Settings.Max100sAffectsSliderEndsAndTicks = parseBool(value); break;
                        case "Max50sAffectsSliderEndsAndTicks": section.Settings.Max50sAffectsSliderEndsAndTicks = parseBool(value); break;
                        case "MaxMissesAffectsSliderEndAndTickMisses": section.Settings.MaxMissesAffectsSliderEndAndTickMisses = parseBool(value); break;
                        case "HP300": section.Settings.HP300 = Parsing.ParseFloat(value); break;
                        case "HP100": section.Settings.HP100 = Parsing.ParseFloat(value); break;
                        case "HP50": section.Settings.HP50 = Parsing.ParseFloat(value); break;
                        case "HPMiss": section.Settings.HPMiss = Parsing.ParseFloat(value); break;
                        case "HPStart": section.Settings.HPStart = Parsing.ParseFloat(value); break;
                        case "HPCap": section.Settings.HPCap = Parsing.ParseFloat(value); break;
                        case "HP300AffectsSliderEndsAndTicks": section.Settings.HP300AffectsSliderEndsAndTicks = parseBool(value); break;
                        case "HP100AffectsSliderEndsAndTicks": section.Settings.HP100AffectsSliderEndsAndTicks = parseBool(value); break;
                        case "HP50AffectsSliderEndsAndTicks": section.Settings.HP50AffectsSliderEndsAndTicks = parseBool(value); break;
                        case "HPMissAffectsSliderEndAndTickMisses": section.Settings.HPMissAffectsSliderEndAndTickMisses = parseBool(value); break;
                        case "NoDrain": section.Settings.NoDrain = parseBool(value); break;
                        case "ReverseHP": section.Settings.ReverseHP = parseBool(value); break;
                        case "GreatOffsetThresholdMs": section.Settings.GreatOffsetThresholdMs = Parsing.ParseFloat(value); break;
                        case "GreatOffsetPenaltyHP": section.Settings.GreatOffsetPenaltyHP = Parsing.ParseFloat(value); break;
                        case "EnableDifficultyOverrides": section.Settings.EnableDifficultyOverrides = parseBool(value); break;
                        case "AllowUnsafeDifficultyOverrideValues": section.Settings.AllowUnsafeDifficultyOverrideValues = parseBool(value); break;
                        case "DifficultyOverrideStartWithBeatmapValues": section.Settings.DifficultyOverrideStartWithBeatmapValues = parseBool(value); break;
                        case "EnableGradualDifficultyChange": section.Settings.EnableGradualDifficultyChange = parseBool(value); break;
                        case "GradualDifficultyChangeEndTimeMs": section.Settings.GradualDifficultyChangeEndTimeMs = Parsing.ParseFloat(value); break;
                        case "KeepDifficultyOverridesAfterSection": section.Settings.KeepDifficultyOverridesAfterSection = parseBool(value); break;
                        case "SectionCircleSize": section.Settings.SectionCircleSize = Parsing.ParseFloat(value); break;
                        case "EnableSectionCircleSizeWindow": section.Settings.EnableSectionCircleSizeWindow = parseBool(value); break;
                        case "SectionCircleSizeStartTimeMs": section.Settings.SectionCircleSizeStartTimeMs = Parsing.ParseFloat(value); break;
                        case "SectionCircleSizeEndTimeMs": section.Settings.SectionCircleSizeEndTimeMs = Parsing.ParseFloat(value); break;
                        case "EnableGradualSectionCircleSizeChange": section.Settings.EnableGradualSectionCircleSizeChange = parseBool(value); break;
                        case "SectionApproachRate": section.Settings.SectionApproachRate = Parsing.ParseFloat(value); break;
                        case "EnableSectionApproachRateWindow": section.Settings.EnableSectionApproachRateWindow = parseBool(value); break;
                        case "SectionApproachRateStartTimeMs": section.Settings.SectionApproachRateStartTimeMs = Parsing.ParseFloat(value); break;
                        case "SectionApproachRateEndTimeMs": section.Settings.SectionApproachRateEndTimeMs = Parsing.ParseFloat(value); break;
                        case "EnableGradualSectionApproachRateChange": section.Settings.EnableGradualSectionApproachRateChange = parseBool(value); break;
                        case "SectionOverallDifficulty": section.Settings.SectionOverallDifficulty = Parsing.ParseFloat(value); break;
                        case "EnableSectionOverallDifficultyWindow": section.Settings.EnableSectionOverallDifficultyWindow = parseBool(value); break;
                        case "SectionOverallDifficultyStartTimeMs": section.Settings.SectionOverallDifficultyStartTimeMs = Parsing.ParseFloat(value); break;
                        case "SectionOverallDifficultyEndTimeMs": section.Settings.SectionOverallDifficultyEndTimeMs = Parsing.ParseFloat(value); break;
                        case "EnableGradualSectionOverallDifficultyChange": section.Settings.EnableGradualSectionOverallDifficultyChange = parseBool(value); break;
                        case "AllowUnsafeStackLeniencyOverrideValues": section.Settings.AllowUnsafeStackLeniencyOverrideValues = parseBool(value); break;
                        case "SectionStackLeniency": section.Settings.SectionStackLeniency = Parsing.ParseFloat(value); break;
                        case "AllowUnsafeTickRateOverrideValues": section.Settings.AllowUnsafeTickRateOverrideValues = parseBool(value); break;
                        case "SectionTickRate": section.Settings.SectionTickRate = Parsing.ParseDouble(value); break;
                        case "ForceHidden": section.Settings.ForceHidden = parseBool(value); break;
                        case "ForceNoApproachCircle": section.Settings.ForceNoApproachCircle = parseBool(value); break;
                        case "ForceHardRock": section.Settings.ForceHardRock = parseBool(value); break;
                        case "ForceFlashlight": section.Settings.ForceFlashlight = parseBool(value); break;
                        case "ForceTraceable": section.Settings.ForceTraceable = parseBool(value); break;
                        case "FlashlightRadius": section.Settings.FlashlightRadius = Parsing.ParseFloat(value); break;
                        case "EnableGradualFlashlightRadiusChange": section.Settings.EnableGradualFlashlightRadiusChange = parseBool(value); break;
                        case "EnableGradualFlashlightFadeIn": section.Settings.EnableGradualFlashlightFadeIn = parseBool(value); break;
                        case "GradualFlashlightRadiusEndTimeMs": section.Settings.GradualFlashlightRadiusEndTimeMs = Parsing.ParseFloat(value); break;
                        case "ForceDoubleTime": section.Settings.ForceDoubleTime = parseBool(value); break;
                        case "ForceSingleTap": section.Settings.ForceSingleTap = parseBool(value); break;
                        case "ForceAlternate": section.Settings.ForceAlternate = parseBool(value); break;
                        case "ForceTransform": section.Settings.ForceTransform = parseBool(value); break;
                        case "ForceWiggle": section.Settings.ForceWiggle = parseBool(value); break;
                        case "ForceSpinIn": section.Settings.ForceSpinIn = parseBool(value); break;
                        case "ForceGrow": section.Settings.ForceGrow = parseBool(value); break;
                        case "ForceDeflate": section.Settings.ForceDeflate = parseBool(value); break;
                        case "ForceBarrelRoll": section.Settings.ForceBarrelRoll = parseBool(value); break;
                        case "ForceApproachDifferent": section.Settings.ForceApproachDifferent = parseBool(value); break;
                        case "ForceMuted": section.Settings.ForceMuted = parseBool(value); break;
                        case "ForceNoScope": section.Settings.ForceNoScope = parseBool(value); break;
                        case "ForceMagnetised": section.Settings.ForceMagnetised = parseBool(value); break;
                        case "ForceRepel": section.Settings.ForceRepel = parseBool(value); break;
                        case "ForceFreezeFrame": section.Settings.ForceFreezeFrame = parseBool(value); break;
                        case "ForceBubbles": section.Settings.ForceBubbles = parseBool(value); break;
                        case "ForceSynesthesia": section.Settings.ForceSynesthesia = parseBool(value); break;
                        case "ForceDepth": section.Settings.ForceDepth = parseBool(value); break;
                        case "ForceBloom": section.Settings.ForceBloom = parseBool(value); break;
                        case "WiggleStrength": if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out float wiggleStrength)) section.Settings.WiggleStrength = wiggleStrength; break;
                        case "GrowStartScale": if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out float growStartScale)) section.Settings.GrowStartScale = growStartScale; break;
                        case "DeflateStartScale": if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out float deflateStartScale)) section.Settings.DeflateStartScale = deflateStartScale; break;
                        case "ApproachDifferentScale": if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out float approachDifferentScale)) section.Settings.ApproachDifferentScale = approachDifferentScale; break;
                        case "NoScopeHiddenComboCount": if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int noScopeHiddenComboCount)) section.Settings.NoScopeHiddenComboCount = noScopeHiddenComboCount; break;
                        case "MagnetisedAttractionStrength": if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out float magnetisedAttractionStrength)) section.Settings.MagnetisedAttractionStrength = magnetisedAttractionStrength; break;
                        case "RepelRepulsionStrength": if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out float repelRepulsionStrength)) section.Settings.RepelRepulsionStrength = repelRepulsionStrength; break;
                        case "DepthMaxDepth": if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out float depthMaxDepth)) section.Settings.DepthMaxDepth = depthMaxDepth; break;
                        case "BloomMaxSizeComboCount": if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int bloomMaxSizeComboCount)) section.Settings.BloomMaxSizeComboCount = bloomMaxSizeComboCount; break;
                        case "BloomMaxCursorSize": if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out float bloomMaxCursorSize)) section.Settings.BloomMaxCursorSize = bloomMaxCursorSize; break;
                        case "BarrelRollSpinSpeed": if (double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out double barrelRollSpinSpeed)) section.Settings.BarrelRollSpinSpeed = barrelRollSpinSpeed; break;
                        case "MutedMuteComboCount": if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int mutedMuteComboCount)) section.Settings.MutedMuteComboCount = mutedMuteComboCount; break;
                        case "SectionName": section.Settings.SectionName = value; break;
                        case "DisplayColor":
                            if (uint.TryParse(value, out uint colorArgb))
                            {
                                float a = (colorArgb & 0xFF) / 255f;
                                float r = ((colorArgb >> 8) & 0xFF) / 255f;
                                float g = ((colorArgb >> 16) & 0xFF) / 255f;
                                float b = ((colorArgb >> 24) & 0xFF) / 255f;
                                section.Settings.DisplayColor = new Color4(r, g, b, a);
                            }
                            break;
                    }
                }
            }

            beatmap.SectionGimmicks.Sections.Add(section);

            static bool parseBool(string boolValue)
                => boolValue == "1" || boolValue.Equals("true", StringComparison.OrdinalIgnoreCase);
        }

        private void handleHitObjectGimmick(string line)
        {
            string[] split = line.Split(',', 3);
            if (split.Length < 2)
                return;

            if (!double.TryParse(split[0], NumberStyles.Float, CultureInfo.InvariantCulture, out double startTime))
                return;

            if (!int.TryParse(split[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out int comboIndexWithOffsets))
                return;

            var entry = new HitObjectGimmickEntry
            {
                StartTime = startTime,
                ComboIndexWithOffsets = comboIndexWithOffsets,
                Settings = new HitObjectGimmickSettings(),
            };

            if (split.Length == 3 && !string.IsNullOrEmpty(split[2]))
            {
                foreach (string kv in split[2].Split('|'))
                {
                    if (string.IsNullOrEmpty(kv))
                        continue;

                    var pair = SplitKeyVal(kv, '=');
                    string key = pair.Key;
                    string value = pair.Value;

                    switch (key)
                    {
                        case "EnableHPGimmick":
                            entry.Settings.EnableHPGimmick = parseBool(value);
                            break;
                        case "IsFakeNote":
                            entry.Settings.IsFakeNote = parseBool(value);
                            break;
                        case "FakePunishMode":
                            if (!Enum.TryParse(value, true, out FakePunishMode fakePunishMode))
                                fakePunishMode = FakePunishMode.None;

                            if (fakePunishMode != FakePunishMode.None && fakePunishMode != FakePunishMode.Miss)
                                fakePunishMode = FakePunishMode.Miss;

                            entry.Settings.FakePunishMode = fakePunishMode;
                            break;
                        case "FakePlayHitsound":
                            entry.Settings.FakePlayHitsound = parseBool(value);
                            break;
                        case "FakeAutoHitOnApproachClose":
                            entry.Settings.FakeAutoHitOnApproachClose = parseBool(value);
                            break;
                        case "FakeAutoHitPlayHitsound":
                            entry.Settings.FakeAutoHitPlayHitsound = parseBool(value);
                            break;
                        case "FakeRevealEnabled":
                            entry.Settings.FakeRevealEnabled = parseBool(value);
                            break;
                        case "FakeRevealRed":
                            entry.Settings.FakeRevealRed = Parsing.ParseFloat(value);
                            break;
                        case "FakeRevealGreen":
                            entry.Settings.FakeRevealGreen = Parsing.ParseFloat(value);
                            break;
                        case "FakeRevealBlue":
                            entry.Settings.FakeRevealBlue = Parsing.ParseFloat(value);
                            break;
                        case "FakeRevealStrength":
                            entry.Settings.FakeRevealStrength = Parsing.ParseFloat(value);
                            break;
                        case "FakeRevealLeadInStartMs":
                            entry.Settings.FakeRevealLeadInStartMs = Parsing.ParseFloat(value);
                            break;
                        case "FakeRevealLeadInLengthMs":
                            entry.Settings.FakeRevealLeadInLengthMs = Parsing.ParseFloat(value);
                            break;
                        case "FakeRevealFadeOutStartMs":
                            entry.Settings.FakeRevealFadeOutStartMs = Parsing.ParseFloat(value);
                            break;
                        case "FakeRevealFadeOutLengthMs":
                            entry.Settings.FakeRevealFadeOutLengthMs = Parsing.ParseFloat(value);
                            break;
                        case "ObjectId":
                            if (long.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out long objectId))
                                entry.ObjectId = objectId;
                            break;
                        case "EnableNoMiss":
                            entry.Settings.EnableNoMiss = parseBool(value);
                            break;
                        case "EnableCountLimits":
                            entry.Settings.EnableCountLimits = parseBool(value);
                            break;
                        case "EnableGreatOffsetPenalty":
                            entry.Settings.EnableGreatOffsetPenalty = parseBool(value);
                            break;
                        case "Max300s":
                            entry.Settings.Max300s = Parsing.ParseInt(value);
                            break;
                        case "Max100s":
                            entry.Settings.Max100s = Parsing.ParseInt(value);
                            break;
                        case "Max50s":
                            entry.Settings.Max50s = Parsing.ParseInt(value);
                            break;
                        case "MaxMisses":
                            entry.Settings.MaxMisses = Parsing.ParseInt(value);
                            break;
                        case "HP300":
                            entry.Settings.HP300 = Parsing.ParseFloat(value);
                            break;
                        case "HP100":
                            entry.Settings.HP100 = Parsing.ParseFloat(value);
                            break;
                        case "HP50":
                            entry.Settings.HP50 = Parsing.ParseFloat(value);
                            break;
                        case "HPMiss":
                            entry.Settings.HPMiss = Parsing.ParseFloat(value);
                            break;
                        case "GreatOffsetThresholdMs":
                            entry.Settings.GreatOffsetThresholdMs = Parsing.ParseFloat(value);
                            break;
                        case "GreatOffsetPenaltyHP":
                            entry.Settings.GreatOffsetPenaltyHP = Parsing.ParseFloat(value);
                            break;
                        case "EnableDifficultyOverrides":
                            entry.Settings.EnableDifficultyOverrides = parseBool(value);
                            break;
                        case "AllowUnsafeDifficultyOverrideValues":
                            entry.Settings.AllowUnsafeDifficultyOverrideValues = parseBool(value);
                            break;
                        case "SectionCircleSize":
                            entry.Settings.SectionCircleSize = Parsing.ParseFloat(value);
                            break;
                        case "SectionApproachRate":
                            entry.Settings.SectionApproachRate = Parsing.ParseFloat(value);
                            break;
                        case "SectionOverallDifficulty":
                            entry.Settings.SectionOverallDifficulty = Parsing.ParseFloat(value);
                            break;
                        case "AllowUnsafeStackLeniencyOverrideValues":
                            entry.Settings.AllowUnsafeStackLeniencyOverrideValues = parseBool(value);
                            break;
                        case "SectionStackLeniency":
                            entry.Settings.SectionStackLeniency = Parsing.ParseFloat(value);
                            break;
                        case "AllowUnsafeTickRateOverrideValues":
                            entry.Settings.AllowUnsafeTickRateOverrideValues = parseBool(value);
                            break;
                        case "SectionTickRate":
                            entry.Settings.SectionTickRate = Parsing.ParseDouble(value);
                            break;
                        case "ForceHidden":
                            entry.Settings.ForceHidden = parseBool(value);
                            break;
                        case "ForceNoApproachCircle":
                            entry.Settings.ForceNoApproachCircle = parseBool(value);
                            break;
                        case "ForceHardRock":
                            entry.Settings.ForceHardRock = parseBool(value);
                            break;
                        case "ForceFlashlight":
                            entry.Settings.ForceFlashlight = parseBool(value);
                            break;
                        case "ForceTraceable":
                            entry.Settings.ForceTraceable = parseBool(value);
                            break;
                        case "FlashlightRadius":
                            entry.Settings.FlashlightRadius = Parsing.ParseFloat(value);
                            break;
                    }
                }
            }

            beatmap.HitObjectGimmicks.Entries.Add(entry);

            SectionGimmickValueClamper.ClampHitObjectSettingsInPlace(entry.Settings);

            static bool parseBool(string boolValue)
                => boolValue == "1" || boolValue.Equals("true", StringComparison.OrdinalIgnoreCase);
        }

        private int getOffsetTime(int time) => time + (ApplyOffsets ? offset : 0);

        private double getOffsetTime() => ApplyOffsets ? offset : 0;

        private double getOffsetTime(double time) => time + (ApplyOffsets ? offset : 0);

        protected virtual TimingControlPoint CreateTimingControlPoint() => new TimingControlPoint();
    }
}
