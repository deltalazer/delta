// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Linq;
using NUnit.Framework;
using osu.Game.Beatmaps.HitObjectGimmicks;
using osu.Game.Beatmaps.SectionGimmicks;
using osu.Game.Rulesets.Osu.Edit;
using osu.Game.Rulesets.Osu.Objects;
using osu.Game.Screens.Edit;

namespace osu.Game.Rulesets.Osu.Tests
{
    [TestFixture]
    public class HitObjectGimmickEditorModelTest
    {
        [Test]
        public void TestSetSelectionForceNoApproachCircleAddsAndRemovesEntries()
        {
            var beatmap = new osu.Game.Rulesets.Osu.Beatmaps.OsuBeatmap();
            var first = new HitCircle { StartTime = 500 };
            var second = new HitCircle { StartTime = 1000, NewCombo = true, ComboOffset = 1 };

            beatmap.HitObjects.Add(first);
            beatmap.HitObjects.Add(second);

            beatmap.BeatmapInfo.Ruleset = new osu.Game.Rulesets.Osu.OsuRuleset().RulesetInfo;
            var editorBeatmap = new EditorBeatmap(beatmap, beatmapInfo: beatmap.BeatmapInfo);

            first.UpdateComboInformation(null);
            second.UpdateComboInformation(first);

            editorBeatmap.SelectedHitObjects.Add(first);

            var model = new HitObjectGimmickEditorModel(editorBeatmap);

            model.SetSelectionForceNoApproachCircle(true);

            Assert.That(editorBeatmap.HitObjectGimmicks.Entries.Count, Is.EqualTo(1));
            var entry = editorBeatmap.HitObjectGimmicks.Entries[0];
            Assert.That(entry.StartTime, Is.EqualTo(first.StartTime));
            Assert.That(entry.ComboIndexWithOffsets, Is.EqualTo(first.ComboIndexWithOffsets));
            Assert.That(entry.ObjectId, Is.EqualTo(first.GimmickObjectId));
            Assert.That(entry.Settings.ForceNoApproachCircle, Is.True);

            model.SetSelectionForceNoApproachCircle(false);

            Assert.That(editorBeatmap.HitObjectGimmicks.Entries.Count, Is.EqualTo(0));
        }

        [Test]
        public void TestSelectionStateReflectsAppliedEntries()
        {
            var beatmap = new osu.Game.Rulesets.Osu.Beatmaps.OsuBeatmap();
            var first = new HitCircle { StartTime = 500 };
            var second = new HitCircle { StartTime = 500, NewCombo = true, ComboOffset = 1 };

            beatmap.HitObjects.Add(first);
            beatmap.HitObjects.Add(second);

            beatmap.BeatmapInfo.Ruleset = new osu.Game.Rulesets.Osu.OsuRuleset().RulesetInfo;
            var editorBeatmap = new EditorBeatmap(beatmap, beatmapInfo: beatmap.BeatmapInfo);

            first.UpdateComboInformation(null);
            second.UpdateComboInformation(first);

            editorBeatmap.HitObjectGimmicks = new BeatmapHitObjectGimmicks
            {
                Entries =
                {
                    new HitObjectGimmickEntry
                    {
                        StartTime = first.StartTime,
                        ComboIndexWithOffsets = first.ComboIndexWithOffsets,
                        Settings = new HitObjectGimmickSettings
                        {
                            ForceNoApproachCircle = true,
                        }
                    }
                }
            };

            editorBeatmap.SelectedHitObjects.Add(first);

            var model = new HitObjectGimmickEditorModel(editorBeatmap);

            Assert.That(model.HasSelection, Is.True);
            Assert.That(model.IsSelectionNoApproachCircleForced, Is.True);

            editorBeatmap.SelectedHitObjects.Add(second);
            Assert.That(model.IsSelectionNoApproachCircleForced, Is.False);
        }

        [Test]
        public void TestSetSelectionFloatSettingClampsOutOfRangeValues()
        {
            var beatmap = new osu.Game.Rulesets.Osu.Beatmaps.OsuBeatmap();
            var first = new HitCircle { StartTime = 500 };
            beatmap.HitObjects.Add(first);

            beatmap.BeatmapInfo.Ruleset = new osu.Game.Rulesets.Osu.OsuRuleset().RulesetInfo;
            var editorBeatmap = new EditorBeatmap(beatmap, beatmapInfo: beatmap.BeatmapInfo);
            first.UpdateComboInformation(null);
            editorBeatmap.SelectedHitObjects.Add(first);

            var model = new HitObjectGimmickEditorModel(editorBeatmap);

            model.SetSelectionFloatSetting((s, value) => s.SectionCircleSize = value, 500);
            model.SetSelectionFloatSetting((s, value) => s.SectionApproachRate = value, -500);
            model.SetSelectionFloatSetting((s, value) => s.SectionOverallDifficulty = value, 500);
            model.SetSelectionFloatSetting((s, value) => s.HP300 = value, 500);

            var entry = editorBeatmap.HitObjectGimmicks.Entries[0];

            Assert.That(entry.Settings.SectionCircleSize, Is.EqualTo(11));
            Assert.That(entry.Settings.SectionApproachRate, Is.EqualTo(-20));
            Assert.That(entry.Settings.SectionOverallDifficulty, Is.EqualTo(11));
            Assert.That(entry.Settings.HP300, Is.EqualTo(2));
        }

        [Test]
        public void TestSetSelectionFloatSettingAllowsUnsafeDifficultyOverrideValues()
        {
            var beatmap = new osu.Game.Rulesets.Osu.Beatmaps.OsuBeatmap();
            var first = new HitCircle { StartTime = 500 };
            beatmap.HitObjects.Add(first);

            beatmap.BeatmapInfo.Ruleset = new osu.Game.Rulesets.Osu.OsuRuleset().RulesetInfo;
            var editorBeatmap = new EditorBeatmap(beatmap, beatmapInfo: beatmap.BeatmapInfo);
            first.UpdateComboInformation(null);
            editorBeatmap.SelectedHitObjects.Add(first);

            var model = new HitObjectGimmickEditorModel(editorBeatmap);

            model.SetSelectionBoolSetting((s, value) => s.AllowUnsafeDifficultyOverrideValues = value, true);
            model.SetSelectionFloatSetting((s, value) => s.SectionCircleSize = value, 500);
            model.SetSelectionFloatSetting((s, value) => s.SectionApproachRate = value, 500);

            var entry = editorBeatmap.HitObjectGimmicks.Entries[0];

            Assert.That(entry.Settings.AllowUnsafeDifficultyOverrideValues, Is.True);
            Assert.That(entry.Settings.SectionCircleSize, Is.EqualTo(500));
            Assert.That(entry.Settings.SectionApproachRate, Is.EqualTo(500));
        }

        [Test]
        public void TestSetSelectionTickRateSupportsUnsafeNegativeValues()
        {
            var beatmap = new osu.Game.Rulesets.Osu.Beatmaps.OsuBeatmap();
            var first = new HitCircle { StartTime = 500 };
            beatmap.HitObjects.Add(first);

            beatmap.BeatmapInfo.Ruleset = new osu.Game.Rulesets.Osu.OsuRuleset().RulesetInfo;
            var editorBeatmap = new EditorBeatmap(beatmap, beatmapInfo: beatmap.BeatmapInfo);
            first.UpdateComboInformation(null);
            editorBeatmap.SelectedHitObjects.Add(first);

            var model = new HitObjectGimmickEditorModel(editorBeatmap);

            model.SetSelectionDoubleSetting((s, value) => s.SectionTickRate = value, -2);
            Assert.That(editorBeatmap.HitObjectGimmicks.Entries[0].Settings.SectionTickRate, Is.EqualTo(0));

            model.SetSelectionBoolSetting((s, value) => s.AllowUnsafeTickRateOverrideValues = value, true);
            model.SetSelectionDoubleSetting((s, value) => s.SectionTickRate = value, -2);
            Assert.That(editorBeatmap.HitObjectGimmicks.Entries[0].Settings.SectionTickRate, Is.EqualTo(-2));
        }

        [Test]
        public void TestMovingHitObjectRetainsObjectBoundGimmickEntry()
        {
            var beatmap = new osu.Game.Rulesets.Osu.Beatmaps.OsuBeatmap();
            var first = new HitCircle { StartTime = 500 };
            beatmap.HitObjects.Add(first);

            beatmap.BeatmapInfo.Ruleset = new osu.Game.Rulesets.Osu.OsuRuleset().RulesetInfo;
            var editorBeatmap = new EditorBeatmap(beatmap, beatmapInfo: beatmap.BeatmapInfo);
            first.UpdateComboInformation(null);
            editorBeatmap.SelectedHitObjects.Add(first);

            var model = new HitObjectGimmickEditorModel(editorBeatmap);
            model.SetSelectionForceNoApproachCircle(true);

            var entryBefore = editorBeatmap.HitObjectGimmicks.Entries.Single();
            var objectId = first.GimmickObjectId;

            editorBeatmap.BeginChange();
            first.StartTime = 1500;
            editorBeatmap.Update(first);
            editorBeatmap.EndChange();

            // Post-process should keep the same entry object binding and update metadata.
            var entryAfter = editorBeatmap.HitObjectGimmicks.Entries.Single(e => e.ObjectId == objectId);
            Assert.That(entryAfter.Settings.ForceNoApproachCircle, Is.True);
            Assert.That(entryAfter.StartTime, Is.EqualTo(first.StartTime));
            Assert.That(entryAfter.ObjectId, Is.EqualTo(entryBefore.ObjectId));
        }

        [Test]
        public void TestUnsafeDifficultyOverridesAllowValuesPastNormalLimits()
        {
            var beatmap = new osu.Game.Rulesets.Osu.Beatmaps.OsuBeatmap();
            var first = new HitCircle { StartTime = 500 };
            beatmap.HitObjects.Add(first);

            beatmap.BeatmapInfo.Ruleset = new osu.Game.Rulesets.Osu.OsuRuleset().RulesetInfo;
            var editorBeatmap = new EditorBeatmap(beatmap, beatmapInfo: beatmap.BeatmapInfo);
            first.UpdateComboInformation(null);
            editorBeatmap.SelectedHitObjects.Add(first);

            var model = new HitObjectGimmickEditorModel(editorBeatmap);

            model.SetSelectionBoolSetting((s, value) => s.AllowUnsafeDifficultyOverrideValues = value, true);
            model.SetSelectionFloatSetting((s, value) => s.SectionCircleSize = value, 999);
            model.SetSelectionFloatSetting((s, value) => s.SectionApproachRate = value, 999);
            model.SetSelectionFloatSetting((s, value) => s.SectionOverallDifficulty = value, 999);

            var entry = editorBeatmap.HitObjectGimmicks.Entries.Single();
            Assert.That(entry.Settings.SectionCircleSize, Is.EqualTo(999));
            Assert.That(entry.Settings.SectionApproachRate, Is.EqualTo(999));
            Assert.That(entry.Settings.SectionOverallDifficulty, Is.EqualTo(999));
        }

        [Test]
        public void TestUnsafeDifficultyOverridesAllowNegativeValuesPastNormalLimits()
        {
            var beatmap = new osu.Game.Rulesets.Osu.Beatmaps.OsuBeatmap();
            var first = new HitCircle { StartTime = 500 };
            beatmap.HitObjects.Add(first);

            beatmap.BeatmapInfo.Ruleset = new osu.Game.Rulesets.Osu.OsuRuleset().RulesetInfo;
            var editorBeatmap = new EditorBeatmap(beatmap, beatmapInfo: beatmap.BeatmapInfo);
            first.UpdateComboInformation(null);
            editorBeatmap.SelectedHitObjects.Add(first);

            var model = new HitObjectGimmickEditorModel(editorBeatmap);

            model.SetSelectionBoolSetting((s, value) => s.AllowUnsafeDifficultyOverrideValues = value, true);
            model.SetSelectionFloatSetting((s, value) => s.SectionCircleSize = value, -123);
            model.SetSelectionFloatSetting((s, value) => s.SectionApproachRate = value, -123);
            model.SetSelectionFloatSetting((s, value) => s.SectionOverallDifficulty = value, -123);

            var entry = editorBeatmap.HitObjectGimmicks.Entries.Single();
            Assert.That(entry.Settings.SectionCircleSize, Is.EqualTo(-123));
            Assert.That(entry.Settings.SectionApproachRate, Is.EqualTo(-123));
            Assert.That(entry.Settings.SectionOverallDifficulty, Is.EqualTo(-123));
        }

        [Test]
        public void TestSafeDifficultyOverridesClampValuesToNormalLimits()
        {
            var settings = new HitObjectGimmickSettings
            {
                AllowUnsafeDifficultyOverrideValues = false,
                SectionCircleSize = 999,
                SectionApproachRate = 999,
                SectionOverallDifficulty = 999,
            };

            SectionGimmickValueClamper.ClampHitObjectSettingsInPlace(settings);

            Assert.That(settings.SectionCircleSize, Is.EqualTo(11));
            Assert.That(settings.SectionApproachRate, Is.EqualTo(11));
            Assert.That(settings.SectionOverallDifficulty, Is.EqualTo(11));
        }

    }
}
