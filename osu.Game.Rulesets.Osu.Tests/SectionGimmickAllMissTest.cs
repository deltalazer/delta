// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Linq;
using NUnit.Framework;
using osu.Game.Beatmaps.HitObjectGimmicks;
using osu.Game.Beatmaps.SectionGimmicks;
using osu.Game.Rulesets.Osu.Beatmaps;
using osu.Game.Rulesets.Osu.Judgements;
using osu.Game.Rulesets.Osu.Objects;
using osu.Game.Rulesets.Osu.Scoring;
using osu.Game.Rulesets.Scoring;
using osu.Game.Screens.Edit.Compose;

namespace osu.Game.Rulesets.Osu.Tests
{
    [TestFixture]
    public class SectionGimmickAllMissTest
    {
        [Test]
        public void TestSectionAllMissAppliesToCirclesInsideSectionOnly()
        {
            var inside = new HitCircle { StartTime = 1000 };
            var outside = new HitCircle { StartTime = 3000 };

            var beatmap = createBeatmap(inside, outside);
            addSection(beatmap, endTime: 2000, new SectionGimmickSettings { ForceAllMiss = true });

            process(beatmap);

            Assert.That(inside.ForceAllMiss, Is.True);
            Assert.That(outside.ForceAllMiss, Is.False);
        }

        [Test]
        public void TestSectionAllMissFreezesHPByDefault()
        {
            var circle = new HitCircle { StartTime = 1000 };

            var beatmap = createBeatmap(circle);
            addSection(beatmap, endTime: -1, new SectionGimmickSettings { ForceAllMiss = true });

            process(beatmap);

            Assert.That(circle.FreezeHP, Is.True);
        }

        [Test]
        public void TestSectionUnfrozenHPIsCarriedToHitObject()
        {
            var circle = new HitCircle { StartTime = 1000 };

            var beatmap = createBeatmap(circle);
            addSection(beatmap, endTime: -1, new SectionGimmickSettings
            {
                ForceAllMiss = true,
                FreezeHP = false,
            });

            process(beatmap);

            Assert.That(circle.FreezeHP, Is.False);
        }

        [Test]
        public void TestSliderAndNestedObjectsAreForcedToMiss()
        {
            var slider = new Slider
            {
                StartTime = 1000,
                Path = new osu.Game.Rulesets.Objects.SliderPath(new[]
                {
                    new osu.Game.Rulesets.Objects.PathControlPoint(osuTK.Vector2.Zero),
                    new osu.Game.Rulesets.Objects.PathControlPoint(new osuTK.Vector2(100, 0)),
                }),
            };

            var beatmap = createBeatmap(slider);
            addSection(beatmap, endTime: -1, new SectionGimmickSettings { ForceAllMiss = true });

            process(beatmap);

            Assert.That(slider.ForceAllMiss, Is.True);
            Assert.That(slider.NestedHitObjects.OfType<SliderHeadCircle>().Single().ForceAllMiss, Is.True);
            Assert.That(slider.NestedHitObjects.OfType<OsuHitObject>().All(h => h.ForceAllMiss), Is.True);
        }

        [Test]
        public void TestSpinnerIsForcedToMiss()
        {
            var spinner = new Spinner { StartTime = 1000, Duration = 500 };

            var beatmap = createBeatmap(spinner);
            addSection(beatmap, endTime: -1, new SectionGimmickSettings { ForceAllMiss = true });

            process(beatmap);

            Assert.That(spinner.ForceAllMiss, Is.True);
        }

        [Test]
        public void TestFakeNotesAreLeftAlone()
        {
            var fake = new FakeHitCircle { StartTime = 1000 };

            var beatmap = createBeatmap(fake);
            addSection(beatmap, endTime: -1, new SectionGimmickSettings { ForceAllMiss = true });

            process(beatmap);

            Assert.That(fake.ForceAllMiss, Is.False);
        }

        [Test]
        public void TestObjectGimmickOverridesSectionFreezeHP()
        {
            var circle = new HitCircle { StartTime = 1000 };

            var beatmap = createBeatmap(circle);
            addSection(beatmap, endTime: -1, new SectionGimmickSettings
            {
                ForceAllMiss = true,
                FreezeHP = false,
            });

            beatmap.HitObjectGimmicks.Entries.Add(new HitObjectGimmickEntry
            {
                StartTime = 1000,
                ComboIndexWithOffsets = circle.ComboIndexWithOffsets,
                Settings = new HitObjectGimmickSettings
                {
                    ForceAllMiss = true,
                    FreezeHP = true,
                }
            });

            process(beatmap);

            Assert.That(circle.ForceAllMiss, Is.True);
            Assert.That(circle.FreezeHP, Is.True);
        }

        [Test]
        public void TestObjectGimmickAllMissWithoutSection()
        {
            var circle = new HitCircle { StartTime = 1000 };

            var beatmap = createBeatmap(circle);
            beatmap.HitObjectGimmicks.Entries.Add(new HitObjectGimmickEntry
            {
                StartTime = 1000,
                ComboIndexWithOffsets = circle.ComboIndexWithOffsets,
                Settings = new HitObjectGimmickSettings
                {
                    ForceAllMiss = true,
                    FreezeHP = false,
                }
            });

            process(beatmap);

            Assert.That(circle.ForceAllMiss, Is.True);
            Assert.That(circle.FreezeHP, Is.False);
        }

        [Test]
        public void TestFrozenHPLeavesHealthUntouchedOnMiss()
        {
            var circle = new HitCircle { StartTime = 1000, ForceAllMiss = true, FreezeHP = true };

            var beatmap = createBeatmap(circle);

            var hp = new SectionGimmickHealthProcessor(0);
            hp.ApplyBeatmap(beatmap);

            double before = hp.Health.Value;
            hp.ApplyResult(new OsuJudgementResult(circle, circle.CreateJudgement()) { Type = HitResult.Miss });

            Assert.That(hp.Health.Value, Is.EqualTo(before).Within(0.0001));
        }

        [Test]
        public void TestUnfrozenHPDrainsHealthOnMiss()
        {
            var circle = new HitCircle { StartTime = 1000, ForceAllMiss = true, FreezeHP = false };

            var beatmap = createBeatmap(circle);

            var hp = new SectionGimmickHealthProcessor(0);
            hp.ApplyBeatmap(beatmap);

            double before = hp.Health.Value;
            hp.ApplyResult(new OsuJudgementResult(circle, circle.CreateJudgement()) { Type = HitResult.Miss });

            Assert.That(hp.Health.Value, Is.LessThan(before));
        }

        [Test]
        public void TestFrozenHPAlsoSuppressesSectionHPGimmickPenalty()
        {
            var circle = new HitCircle { StartTime = 1000, ForceAllMiss = true, FreezeHP = true };

            var beatmap = createBeatmap(circle);
            addSection(beatmap, endTime: -1, new SectionGimmickSettings
            {
                EnableHPGimmick = true,
                NoDrain = true,
                HP300 = 0f,
                HP100 = 0f,
                HP50 = 0f,
                HPMiss = 0.5f,
            });

            var hp = new SectionGimmickHealthProcessor(0);
            hp.ApplyBeatmap(beatmap);

            double before = hp.Health.Value;
            hp.ApplyResult(new OsuJudgementResult(circle, circle.CreateJudgement()) { Type = HitResult.Miss });

            Assert.That(hp.Health.Value, Is.EqualTo(before).Within(0.0001));
        }

        [Test]
        public void TestFrozenHPHoldsDrainAcrossWholeSection()
        {
            var circle = new HitCircle { StartTime = 1000 };

            var beatmap = createBeatmap(circle);
            addSection(beatmap, endTime: 5000, new SectionGimmickSettings { ForceAllMiss = true });

            process(beatmap);

            var hp = new SectionGimmickHealthProcessor(0);
            hp.ApplyBeatmap(beatmap);

            Assert.That(hp.IsHealthKeptAt(0), Is.True);
            Assert.That(hp.IsHealthKeptAt(3000), Is.True);
            Assert.That(hp.IsHealthKeptAt(5000), Is.True);
            Assert.That(hp.IsHealthKeptAt(6000), Is.False);
        }

        [Test]
        public void TestUnfrozenHPDoesNotHoldDrain()
        {
            var circle = new HitCircle { StartTime = 1000 };

            var beatmap = createBeatmap(circle);
            addSection(beatmap, endTime: 5000, new SectionGimmickSettings
            {
                ForceAllMiss = true,
                FreezeHP = false,
            });

            process(beatmap);

            var hp = new SectionGimmickHealthProcessor(0);
            hp.ApplyBeatmap(beatmap);

            Assert.That(hp.IsHealthKeptAt(3000), Is.False);
        }

        [Test]
        public void TestDrainIsNotHeldWithoutAllMiss()
        {
            var circle = new HitCircle { StartTime = 1000 };

            var beatmap = createBeatmap(circle);
            addSection(beatmap, endTime: 5000, new SectionGimmickSettings());

            process(beatmap);

            var hp = new SectionGimmickHealthProcessor(0);
            hp.ApplyBeatmap(beatmap);

            Assert.That(hp.IsHealthKeptAt(3000), Is.False);
        }

        [Test]
        public void TestObjectGimmickFrozenHPHoldsDrainOverObjectDuration()
        {
            var slider = new Slider
            {
                StartTime = 1000,
                Path = new osu.Game.Rulesets.Objects.SliderPath(new[]
                {
                    new osu.Game.Rulesets.Objects.PathControlPoint(osuTK.Vector2.Zero),
                    new osu.Game.Rulesets.Objects.PathControlPoint(new osuTK.Vector2(100, 0)),
                }),
            };

            var beatmap = createBeatmap(slider);
            beatmap.HitObjectGimmicks.Entries.Add(new HitObjectGimmickEntry
            {
                StartTime = 1000,
                ComboIndexWithOffsets = slider.ComboIndexWithOffsets,
                Settings = new HitObjectGimmickSettings { ForceAllMiss = true }
            });

            process(beatmap);

            var hp = new SectionGimmickHealthProcessor(0);
            hp.ApplyBeatmap(beatmap);

            Assert.That(hp.IsHealthKeptAt(slider.StartTime), Is.True);
            Assert.That(hp.IsHealthKeptAt(slider.EndTime), Is.True);
            Assert.That(hp.IsHealthKeptAt(slider.EndTime + 1000), Is.False);
        }

        [Test]
        public void TestFrozenAccuracyExcludesForcedMissFromAccuracy()
        {
            var kept = new HitCircle { StartTime = 1000, ForceAllMiss = true, FreezeAccuracy = true };
            var normal = new HitCircle { StartTime = 2000 };

            var beatmap = createBeatmap(kept, normal);

            var score = new OsuScoreProcessor();
            score.ApplyBeatmap(beatmap);

            score.ApplyResult(new OsuJudgementResult(kept, kept.CreateJudgement()) { Type = HitResult.Miss });
            score.ApplyResult(new OsuJudgementResult(normal, normal.CreateJudgement()) { Type = HitResult.Great });

            Assert.That(score.Accuracy.Value, Is.EqualTo(1).Within(0.0001));
        }

        [Test]
        public void TestUnfrozenAccuracyCountsForcedMissAgainstAccuracy()
        {
            var lost = new HitCircle { StartTime = 1000, ForceAllMiss = true, FreezeAccuracy = false };
            var normal = new HitCircle { StartTime = 2000 };

            var beatmap = createBeatmap(lost, normal);

            var score = new OsuScoreProcessor();
            score.ApplyBeatmap(beatmap);

            score.ApplyResult(new OsuJudgementResult(lost, lost.CreateJudgement()) { Type = HitResult.Miss });
            score.ApplyResult(new OsuJudgementResult(normal, normal.CreateJudgement()) { Type = HitResult.Great });

            Assert.That(score.Accuracy.Value, Is.EqualTo(0.5).Within(0.0001));
        }

        [Test]
        public void TestSectionFreezeAccuracyIsCarriedToHitObject()
        {
            var circle = new HitCircle { StartTime = 1000 };

            var beatmap = createBeatmap(circle);
            addSection(beatmap, endTime: -1, new SectionGimmickSettings
            {
                ForceAllMiss = true,
                FreezeAccuracy = false,
            });

            process(beatmap);

            Assert.That(circle.FreezeAccuracy, Is.False);
        }

        [Test]
        public void TestAllMissFreezesAccuracyByDefault()
        {
            var circle = new HitCircle { StartTime = 1000 };

            var beatmap = createBeatmap(circle);
            addSection(beatmap, endTime: -1, new SectionGimmickSettings { ForceAllMiss = true });

            process(beatmap);

            Assert.That(circle.FreezeAccuracy, Is.True);
        }

        [Test]
        public void TestFrozenPPKeepsComboThroughForcedMiss()
        {
            var first = new HitCircle { StartTime = 1000 };
            var forced = new HitCircle { StartTime = 2000, ForceAllMiss = true, FreezeCombo = true };
            var third = new HitCircle { StartTime = 3000 };

            var beatmap = createBeatmap(first, forced, third);

            var score = new OsuScoreProcessor();
            score.ApplyBeatmap(beatmap);

            score.ApplyResult(new OsuJudgementResult(first, first.CreateJudgement()) { Type = HitResult.Great });
            score.ApplyResult(new OsuJudgementResult(forced, forced.CreateJudgement()) { Type = HitResult.Miss });
            score.ApplyResult(new OsuJudgementResult(third, third.CreateJudgement()) { Type = HitResult.Great });

            Assert.That(score.Combo.Value, Is.EqualTo(2));
            Assert.That(score.HighestCombo.Value, Is.EqualTo(2));
        }

        [Test]
        public void TestUnfrozenPPBreaksComboOnForcedMiss()
        {
            var first = new HitCircle { StartTime = 1000 };
            var forced = new HitCircle { StartTime = 2000, ForceAllMiss = true, FreezeCombo = false };

            var beatmap = createBeatmap(first, forced);

            var score = new OsuScoreProcessor();
            score.ApplyBeatmap(beatmap);

            score.ApplyResult(new OsuJudgementResult(first, first.CreateJudgement()) { Type = HitResult.Great });
            score.ApplyResult(new OsuJudgementResult(forced, forced.CreateJudgement()) { Type = HitResult.Miss });

            Assert.That(score.Combo.Value, Is.EqualTo(0));
        }

        [Test]
        public void TestRevertingFrozenPPMissRestoresCombo()
        {
            var first = new HitCircle { StartTime = 1000 };
            var forced = new HitCircle { StartTime = 2000, ForceAllMiss = true, FreezeCombo = true };

            var beatmap = createBeatmap(first, forced);

            var score = new OsuScoreProcessor();
            score.ApplyBeatmap(beatmap);

            score.ApplyResult(new OsuJudgementResult(first, first.CreateJudgement()) { Type = HitResult.Great });

            var forcedResult = new OsuJudgementResult(forced, forced.CreateJudgement()) { Type = HitResult.Miss };
            score.ApplyResult(forcedResult);
            Assert.That(score.Combo.Value, Is.EqualTo(1));

            score.RevertResult(forcedResult);
            Assert.That(score.Combo.Value, Is.EqualTo(1));
        }

        [Test]
        public void TestSectionFreezeComboIsCarriedToHitObject()
        {
            var circle = new HitCircle { StartTime = 1000 };

            var beatmap = createBeatmap(circle);
            addSection(beatmap, endTime: -1, new SectionGimmickSettings
            {
                ForceAllMiss = true,
                FreezeCombo = false,
            });

            process(beatmap);

            Assert.That(circle.FreezeCombo, Is.False);
        }

        [Test]
        public void TestAllMissFreezesPPByDefault()
        {
            var circle = new HitCircle { StartTime = 1000 };

            var beatmap = createBeatmap(circle);
            addSection(beatmap, endTime: -1, new SectionGimmickSettings { ForceAllMiss = true });

            process(beatmap);

            Assert.That(circle.FreezeCombo, Is.True);
        }

        [Test]
        public void TestForcedMissDoesNotTripNoMissRequirement()
        {
            var circle = new HitCircle { StartTime = 1000, ForceAllMiss = true };

            var beatmap = createBeatmap(circle);
            addSection(beatmap, endTime: -1, new SectionGimmickSettings
            {
                ForceAllMiss = true,
                EnableNoMiss = true,
            });

            var hp = new SectionGimmickHealthProcessor(0);
            hp.ApplyBeatmap(beatmap);

            hp.ApplyResult(new OsuJudgementResult(circle, circle.CreateJudgement()) { Type = HitResult.Miss });

            Assert.That(hp.HasFailed, Is.False);
        }

        [Test]
        public void TestGenuineMissStillTripsNoMissRequirement()
        {
            var forced = new HitCircle { StartTime = 1000, ForceAllMiss = true };
            var genuine = new HitCircle { StartTime = 2000 };

            var beatmap = createBeatmap(forced, genuine);
            addSection(beatmap, endTime: -1, new SectionGimmickSettings { EnableNoMiss = true });

            var hp = new SectionGimmickHealthProcessor(0);
            hp.ApplyBeatmap(beatmap);

            hp.ApplyResult(new OsuJudgementResult(genuine, genuine.CreateJudgement()) { Type = HitResult.Miss });

            Assert.That(hp.HasFailed, Is.True);
        }

        [Test]
        public void TestSectionSettingsCloneKeepsAllMiss()
        {
            var source = new BeatmapSectionGimmicks
            {
                Sections =
                {
                    new SectionGimmickSection
                    {
                        Id = 0,
                        StartTime = 0,
                        EndTime = 5000,
                        Settings = new SectionGimmickSettings
                        {
                            ForceAllMiss = true,
                            FreezeHP = false,
                            FreezeAccuracy = false,
                        }
                    }
                }
            };

            var settings = SectionGimmickEditorModel.CloneGimmicks(source).Sections.Single().Settings;

            Assert.That(settings.ForceAllMiss, Is.True);
            Assert.That(settings.FreezeHP, Is.False);
            Assert.That(settings.FreezeAccuracy, Is.False);
        }

        [Test]
        public void TestHitObjectSettingsCloneKeepsAllMiss()
        {
            var source = new HitObjectGimmickSettings
            {
                ForceAllMiss = true,
                FreezeHP = false,
                FreezeAccuracy = false,
            };

            var clone = HitObjectGimmickBindingUtils.CloneSettings(source);

            Assert.That(clone.ForceAllMiss, Is.True);
            Assert.That(clone.FreezeHP, Is.False);
            Assert.That(clone.FreezeAccuracy, Is.False);
        }

        private static OsuBeatmap createBeatmap(params OsuHitObject[] hitObjects)
        {
            var beatmap = new OsuBeatmap();

            foreach (var hitObject in hitObjects)
                beatmap.HitObjects.Add(hitObject);

            return beatmap;
        }

        private static void addSection(OsuBeatmap beatmap, double endTime, SectionGimmickSettings settings)
            => beatmap.SectionGimmicks.Sections.Add(new SectionGimmickSection
            {
                Id = 0,
                StartTime = 0,
                EndTime = endTime,
                Settings = settings,
            });

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
