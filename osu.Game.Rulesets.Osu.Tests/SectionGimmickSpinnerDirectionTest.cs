// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Linq;
using NUnit.Framework;
using osu.Game.Beatmaps;
using osu.Game.Beatmaps.HitObjectGimmicks;
using osu.Game.Beatmaps.SectionGimmicks;
using osu.Game.Rulesets.Osu.Beatmaps;
using osu.Game.Rulesets.Osu.Edit;
using osu.Game.Rulesets.Osu.Objects;

namespace osu.Game.Rulesets.Osu.Tests
{
    [TestFixture]
    public class SectionGimmickSpinnerDirectionTest
    {
        [Test]
        public void TestSectionDirectionIsCarriedToSpinner()
        {
            var spinner = new Spinner { StartTime = 1000, EndTime = 3000 };

            var beatmap = createBeatmap(spinner);
            addSection(beatmap, new SectionGimmickSettings
            {
                SpinnerDirection = ForcedSpinnerDirection.Clockwise,
                SpinnerIndicator = SpinnerDirectionIndicator.Arrow,
            });

            process(beatmap);

            Assert.That(spinner.SpinnerDirection, Is.EqualTo(ForcedSpinnerDirection.Clockwise));
            Assert.That(spinner.SpinnerIndicator, Is.EqualTo(SpinnerDirectionIndicator.Arrow));
        }

        [Test]
        public void TestSectionWrongDirectionBehaviourIsCarried()
        {
            var spinner = new Spinner { StartTime = 1000, EndTime = 3000 };

            var beatmap = createBeatmap(spinner);
            addSection(beatmap, new SectionGimmickSettings
            {
                SpinnerDirection = ForcedSpinnerDirection.CounterClockwise,
                SpinnerWrongDirection = SpinnerWrongDirectionBehaviour.InstantMiss,
            });

            process(beatmap);

            Assert.That(spinner.SpinnerDirection, Is.EqualTo(ForcedSpinnerDirection.CounterClockwise));
            Assert.That(spinner.SpinnerWrongDirection, Is.EqualTo(SpinnerWrongDirectionBehaviour.InstantMiss));
        }

        [Test]
        public void TestObjectSettingsOverrideSection()
        {
            var spinner = new Spinner { StartTime = 1000, EndTime = 3000 };

            var beatmap = createBeatmap(spinner);
            addSection(beatmap, new SectionGimmickSettings { SpinnerDirection = ForcedSpinnerDirection.Clockwise });
            addObjectSettings(beatmap, spinner, new HitObjectGimmickSettings { SpinnerDirection = ForcedSpinnerDirection.CounterClockwise });

            process(beatmap);

            Assert.That(spinner.SpinnerDirection, Is.EqualTo(ForcedSpinnerDirection.CounterClockwise));
        }

        [Test]
        public void TestDefaultsWhenNoGimmickPresent()
        {
            var spinner = new Spinner { StartTime = 1000, EndTime = 3000 };

            process(createBeatmap(spinner));

            Assert.That(spinner.SpinnerDirection, Is.EqualTo(ForcedSpinnerDirection.Any));
            Assert.That(spinner.SpinnerWrongDirection, Is.EqualTo(SpinnerWrongDirectionBehaviour.NoProgress));
            Assert.That(spinner.SpinnerIndicator, Is.EqualTo(SpinnerDirectionIndicator.None));
        }

        [Test]
        public void TestEditorCloneKeepsSpinnerSettingsTogether()
        {
            var source = new BeatmapHitObjectGimmicks();

            source.Entries.Add(new HitObjectGimmickEntry
            {
                ObjectId = 1,
                StartTime = 1000,
                Settings = new HitObjectGimmickSettings
                {
                    SpinnerDirection = ForcedSpinnerDirection.Clockwise,
                    SpinnerWrongDirection = SpinnerWrongDirectionBehaviour.InstantMiss,
                    SpinnerIndicator = SpinnerDirectionIndicator.Arrow,
                },
            });

            var clone = HitObjectGimmickEditorModel.CloneHitObjectGimmicks(source);
            var settings = clone.Entries.Single().Settings;

            Assert.That(settings.SpinnerDirection, Is.EqualTo(ForcedSpinnerDirection.Clockwise));
            Assert.That(settings.SpinnerWrongDirection, Is.EqualTo(SpinnerWrongDirectionBehaviour.InstantMiss));
            Assert.That(settings.SpinnerIndicator, Is.EqualTo(SpinnerDirectionIndicator.Arrow));
        }

        private static OsuBeatmap createBeatmap(params OsuHitObject[] hitObjects)
        {
            var beatmap = new OsuBeatmap();

            foreach (var hitObject in hitObjects)
                beatmap.HitObjects.Add(hitObject);

            return beatmap;
        }

        private static void addSection(OsuBeatmap beatmap, SectionGimmickSettings settings)
            => beatmap.SectionGimmicks.Sections.Add(new SectionGimmickSection
            {
                Id = 0,
                StartTime = 0,
                EndTime = -1,
                Settings = settings,
            });

        private static void addObjectSettings(OsuBeatmap beatmap, OsuHitObject hitObject, HitObjectGimmickSettings settings)
        {
            HitObjectGimmickBindingUtils.EnsureObjectIds(new[] { hitObject });

            beatmap.HitObjectGimmicks.Entries.Add(new HitObjectGimmickEntry
            {
                ObjectId = hitObject.GimmickObjectId,
                StartTime = hitObject.StartTime,
                Settings = settings,
            });
        }

        private static void process(OsuBeatmap beatmap)
        {
            var processor = new OsuBeatmapProcessor(beatmap);
            processor.PreProcess();

            foreach (var obj in beatmap.HitObjects)
                obj.ApplyDefaults(beatmap.ControlPointInfo, beatmap.Difficulty);

            processor.PostProcess();
        }
    }
}
