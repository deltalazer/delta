// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Linq;
using NUnit.Framework;
using osu.Game.Beatmaps;
using osu.Game.Beatmaps.HitObjectGimmicks;
using osu.Game.Beatmaps.SectionGimmicks;
using osu.Game.Rulesets.Objects;
using osu.Game.Rulesets.Objects.Types;
using osu.Game.Rulesets.Osu.Beatmaps;
using osu.Game.Rulesets.Osu.Judgements;
using osu.Game.Rulesets.Osu.Objects;
using osu.Game.Rulesets.Osu.Scoring;
using osu.Game.Rulesets.Scoring;
using osuTK;

namespace osu.Game.Rulesets.Osu.Tests
{
    [TestFixture]
    public class SectionGimmickDifficultyOverrideTest
    {
        [Test]
        public void TestSectionDifficultyOverrideAppliesARAndOD()
        {
            var first = new HitCircle { StartTime = 1000 };
            var second = new HitCircle { StartTime = 3000 };

            var beatmap = new OsuBeatmap();
            beatmap.HitObjects.Add(first);
            beatmap.HitObjects.Add(second);

            beatmap.SectionGimmicks.Sections.Add(new SectionGimmickSection
            {
                Id = 0,
                StartTime = 0,
                EndTime = 2000,
                Settings = new SectionGimmickSettings
                {
                    EnableDifficultyOverrides = true,
                    SectionApproachRate = 10,
                    SectionOverallDifficulty = 9,
                }
            });

            var processor = new OsuBeatmapProcessor(beatmap);
            processor.PreProcess();

            foreach (var obj in beatmap.HitObjects)
                obj.ApplyDefaults(beatmap.ControlPointInfo, beatmap.Difficulty);

            processor.PostProcess();

            // AR10 preempt in osu! is 450ms.
            Assert.That(first.TimePreempt, Is.EqualTo(450).Within(0.0001));
            Assert.That(second.TimePreempt, Is.EqualTo(1200).Within(0.0001));

            var od9Windows = new OsuHitWindows();
            od9Windows.SetDifficulty(9);

            var baseOdWindows = new OsuHitWindows();
            baseOdWindows.SetDifficulty(beatmap.Difficulty.OverallDifficulty);

            Assert.That(first.HitWindows.WindowFor(HitResult.Great), Is.EqualTo(od9Windows.WindowFor(HitResult.Great)).Within(0.0001));
            Assert.That(second.HitWindows.WindowFor(HitResult.Great), Is.EqualTo(baseOdWindows.WindowFor(HitResult.Great)).Within(0.0001));
        }

        [Test]
        public void TestSectionDifficultyOverrideAppliesCircleSize()
        {
            var first = new HitCircle { StartTime = 1000 };
            var second = new HitCircle { StartTime = 3000 };

            var beatmap = new OsuBeatmap();
            beatmap.HitObjects.Add(first);
            beatmap.HitObjects.Add(second);

            beatmap.SectionGimmicks.Sections.Add(new SectionGimmickSection
            {
                Id = 0,
                StartTime = 0,
                EndTime = 2000,
                Settings = new SectionGimmickSettings
                {
                    EnableDifficultyOverrides = true,
                    SectionCircleSize = 7,
                }
            });

            var processor = new OsuBeatmapProcessor(beatmap);
            processor.PreProcess();

            foreach (var obj in beatmap.HitObjects)
                obj.ApplyDefaults(beatmap.ControlPointInfo, beatmap.Difficulty);

            processor.PostProcess();

            Assert.That(first.Scale, Is.Not.EqualTo(second.Scale));
            Assert.That(first.Scale, Is.LessThan(second.Scale));
        }

        [Test]
        public void TestSectionDifficultyOverrideAllowsAr11AndOd11()
        {
            var hit = new HitCircle { StartTime = 1000 };

            var beatmap = new OsuBeatmap();
            beatmap.HitObjects.Add(hit);

            beatmap.SectionGimmicks.Sections.Add(new SectionGimmickSection
            {
                Id = 0,
                StartTime = 0,
                EndTime = 2000,
                Settings = new SectionGimmickSettings
                {
                    EnableDifficultyOverrides = true,
                    SectionApproachRate = 11,
                    SectionOverallDifficulty = 11,
                }
            });

            var processor = new OsuBeatmapProcessor(beatmap);
            processor.PreProcess();

            foreach (var obj in beatmap.HitObjects)
                obj.ApplyDefaults(beatmap.ControlPointInfo, beatmap.Difficulty);

            processor.PostProcess();

            // AR11 should be below AR10 preempt 450ms.
            Assert.That(hit.TimePreempt, Is.LessThan(450));

            // OD11 should make great window stricter than OD10.
            var od10 = new OsuHitWindows();
            od10.SetDifficulty(10);
            Assert.That(hit.HitWindows.WindowFor(HitResult.Great), Is.LessThan(od10.WindowFor(HitResult.Great)));
        }

        [Test]
        public void TestSectionDifficultyOverrideAllowsNegativeArBelowMinusTen()
        {
            var hit = new HitCircle { StartTime = 1000 };

            var beatmap = new OsuBeatmap();
            beatmap.HitObjects.Add(hit);

            beatmap.SectionGimmicks.Sections.Add(new SectionGimmickSection
            {
                Id = 0,
                StartTime = 0,
                EndTime = 2000,
                Settings = new SectionGimmickSettings
                {
                    EnableDifficultyOverrides = true,
                    SectionApproachRate = -20,
                }
            });

            var processor = new OsuBeatmapProcessor(beatmap);
            processor.PreProcess();

            foreach (var obj in beatmap.HitObjects)
                obj.ApplyDefaults(beatmap.ControlPointInfo, beatmap.Difficulty);

            processor.PostProcess();

            Assert.That(hit.TimePreempt, Is.GreaterThan(1800));
        }

        [Test]
        public void TestSectionDifficultyOverrideGradualArChange()
        {
            var early = new HitCircle { StartTime = 200 };
            var mid = new HitCircle { StartTime = 700 };
            var late = new HitCircle { StartTime = 1300 };

            var beatmap = new OsuBeatmap();
            beatmap.HitObjects.Add(early);
            beatmap.HitObjects.Add(mid);
            beatmap.HitObjects.Add(late);

            beatmap.SectionGimmicks.Sections.Add(new SectionGimmickSection
            {
                Id = 0,
                StartTime = 0,
                EndTime = 1500,
                Settings = new SectionGimmickSettings
                {
                    EnableDifficultyOverrides = true,
                    EnableGradualDifficultyChange = true,
                    GradualDifficultyChangeEndTimeMs = 1000,
                    SectionApproachRate = 10,
                }
            });

            var processor = new OsuBeatmapProcessor(beatmap);
            processor.PreProcess();

            foreach (var obj in beatmap.HitObjects)
                obj.ApplyDefaults(beatmap.ControlPointInfo, beatmap.Difficulty);

            processor.PostProcess();

            Assert.That(early.TimePreempt, Is.GreaterThan(mid.TimePreempt));
            Assert.That(mid.TimePreempt, Is.GreaterThan(late.TimePreempt));

            // after gradual finish point, should be at target AR10 preempt.
            Assert.That(late.TimePreempt, Is.EqualTo(450).Within(0.0001));
        }

        [Test]
        public void TestSectionDifficultyOverrideKeepAfterSection()
        {
            var inSection = new HitCircle { StartTime = 500 };
            var afterSection = new HitCircle { StartTime = 2000 };

            var beatmap = new OsuBeatmap();
            beatmap.HitObjects.Add(inSection);
            beatmap.HitObjects.Add(afterSection);

            beatmap.SectionGimmicks.Sections.Add(new SectionGimmickSection
            {
                Id = 0,
                StartTime = 0,
                EndTime = 1000,
                Settings = new SectionGimmickSettings
                {
                    EnableDifficultyOverrides = true,
                    KeepDifficultyOverridesAfterSection = true,
                    SectionApproachRate = 10,
                }
            });

            var processor = new OsuBeatmapProcessor(beatmap);
            processor.PreProcess();

            foreach (var obj in beatmap.HitObjects)
                obj.ApplyDefaults(beatmap.ControlPointInfo, beatmap.Difficulty);

            processor.PostProcess();

            Assert.That(inSection.TimePreempt, Is.EqualTo(450).Within(0.0001));
            Assert.That(afterSection.TimePreempt, Is.EqualTo(450).Within(0.0001));
        }

        [Test]
        public void TestSectionDifficultyOverrideRevertsImmediatelyWhenKeepOff()
        {
            var inSection = new HitCircle { StartTime = 500 };
            var rightAfterSection = new HitCircle { StartTime = 1001 };

            var beatmap = new OsuBeatmap();
            beatmap.HitObjects.Add(inSection);
            beatmap.HitObjects.Add(rightAfterSection);

            beatmap.Difficulty.ApproachRate = 5;

            beatmap.SectionGimmicks.Sections.Add(new SectionGimmickSection
            {
                Id = 0,
                StartTime = 0,
                EndTime = 1000,
                Settings = new SectionGimmickSettings
                {
                    EnableDifficultyOverrides = true,
                    KeepDifficultyOverridesAfterSection = false,
                    SectionApproachRate = 10,
                }
            });

            var processor = new OsuBeatmapProcessor(beatmap);
            processor.PreProcess();

            foreach (var obj in beatmap.HitObjects)
                obj.ApplyDefaults(beatmap.ControlPointInfo, beatmap.Difficulty);

            processor.PostProcess();

            // AR10 inside section
            Assert.That(inSection.TimePreempt, Is.EqualTo(450).Within(0.0001));

            // Must immediately revert to base AR5 just outside section when keep is off.
            Assert.That(rightAfterSection.TimePreempt, Is.EqualTo(1200).Within(0.0001));
        }

        [Test]
        public void TestSectionInheritsDifficultyFromPreviousSection()
        {
            var section0Object = new HitCircle { StartTime = 500 };
            var section1EarlyObject = new HitCircle { StartTime = 1500 };
            var section1LateObject = new HitCircle { StartTime = 2500 };

            var beatmap = new OsuBeatmap();
            beatmap.HitObjects.Add(section0Object);
            beatmap.HitObjects.Add(section1EarlyObject);
            beatmap.HitObjects.Add(section1LateObject);

            // Set a specific base difficulty to make the test deterministic
            beatmap.Difficulty.ApproachRate = 5; // AR5 preempt = 1200ms

            // Section 0: AR=8, keeps overrides after section
            beatmap.SectionGimmicks.Sections.Add(new SectionGimmickSection
            {
                Id = 0,
                StartTime = 0,
                EndTime = 1000,
                Settings = new SectionGimmickSettings
                {
                    EnableDifficultyOverrides = true,
                    KeepDifficultyOverridesAfterSection = true,
                    SectionApproachRate = 8,
                }
            });

            // Section 1: Gradual AR shift to AR=10, should start from AR=8 (inherited)
            beatmap.SectionGimmicks.Sections.Add(new SectionGimmickSection
            {
                Id = 1,
                StartTime = 1000,
                EndTime = 3000,
                Settings = new SectionGimmickSettings
                {
                    EnableDifficultyOverrides = true,
                    EnableGradualDifficultyChange = true,
                    GradualDifficultyChangeEndTimeMs = 2000,
                    SectionApproachRate = 10,
                }
            });

            var processor = new OsuBeatmapProcessor(beatmap);
            processor.PreProcess();

            foreach (var obj in beatmap.HitObjects)
                obj.ApplyDefaults(beatmap.ControlPointInfo, beatmap.Difficulty);

            processor.PostProcess();

            // AR8 preempt is 750ms, AR10 preempt is 450ms
            var ar8Preempt = 750;
            var ar10Preempt = 450;

            // Section 0 object should have AR=8
            Assert.That(section0Object.TimePreempt, Is.EqualTo(ar8Preempt).Within(0.0001));

            // Section 1 early object should be halfway through gradual shift from AR8 to AR10
            // At 1500ms (progress 0.5), should be AR9 = (750 + 450) / 2 = 600ms
            var ar9Preempt = 600;
            Assert.That(section1EarlyObject.TimePreempt, Is.EqualTo(ar9Preempt).Within(0.0001));

            // Section 1 late object should have AR=10 (after gradual shift completes)
            Assert.That(section1LateObject.TimePreempt, Is.EqualTo(ar10Preempt).Within(0.0001));
        }

        [Test]
        public void TestForceHardRockToggleRestoresPosition()
        {
            var circle = new HitCircle { StartTime = 1000, Position = new Vector2(128, 100) };

            var beatmap = new OsuBeatmap();
            beatmap.HitObjects.Add(circle);

            beatmap.SectionGimmicks.Sections.Add(new SectionGimmickSection
            {
                Id = 0,
                StartTime = 0,
                EndTime = 2000,
                Settings = new SectionGimmickSettings
                {
                    ForceHardRock = true,
                }
            });

            var processor = new OsuBeatmapProcessor(beatmap);
            processor.PreProcess();
            foreach (var obj in beatmap.HitObjects)
                obj.ApplyDefaults(beatmap.ControlPointInfo, beatmap.Difficulty);
            processor.PostProcess();

            Assert.That(circle.Y, Is.EqualTo(284).Within(0.0001));

            beatmap.SectionGimmicks.Sections[0].Settings.ForceHardRock = false;
            processor.PostProcess();

            Assert.That(circle.Y, Is.EqualTo(100).Within(0.0001));
        }

        [Test]
        public void TestForceHardRockEditWhileEnabledPersistsWhenDisabled()
        {
            var circle = new HitCircle { StartTime = 1000, Position = new Vector2(128, 100) };

            var beatmap = new OsuBeatmap();
            beatmap.HitObjects.Add(circle);

            beatmap.SectionGimmicks.Sections.Add(new SectionGimmickSection
            {
                Id = 0,
                StartTime = 0,
                EndTime = 2000,
                Settings = new SectionGimmickSettings
                {
                    ForceHardRock = true,
                }
            });

            var processor = new OsuBeatmapProcessor(beatmap);
            processor.PreProcess();
            foreach (var obj in beatmap.HitObjects)
                obj.ApplyDefaults(beatmap.ControlPointInfo, beatmap.Difficulty);
            processor.PostProcess();

            // Simulate editing while HR is enabled (editing in flipped space).
            circle.Y = 260;
            processor.PostProcess();

            beatmap.SectionGimmicks.Sections[0].Settings.ForceHardRock = false;
            processor.PostProcess();

            // Expected normal-space Y after inverse transform: 384 - 260 = 124
            Assert.That(circle.Y, Is.EqualTo(124).Within(0.0001));
        }

        [Test]
        public void TestForceHardRockAppliesToBoundaryObjects()
        {
            var atStart = new HitCircle { StartTime = 1000, Position = new Vector2(128, 50) };
            var atEnd = new HitCircle { StartTime = 2000, Position = new Vector2(128, 60) };

            var beatmap = new OsuBeatmap();
            beatmap.HitObjects.Add(atStart);
            beatmap.HitObjects.Add(atEnd);

            beatmap.SectionGimmicks.Sections.Add(new SectionGimmickSection
            {
                Id = 0,
                StartTime = 1000,
                EndTime = 2000,
                Settings = new SectionGimmickSettings
                {
                    ForceHardRock = true,
                }
            });

            var processor = new OsuBeatmapProcessor(beatmap);
            processor.PreProcess();
            foreach (var obj in beatmap.HitObjects)
                obj.ApplyDefaults(beatmap.ControlPointInfo, beatmap.Difficulty);
            processor.PostProcess();

            Assert.That(atStart.Y, Is.EqualTo(334).Within(0.0001));
            Assert.That(atEnd.Y, Is.EqualTo(324).Within(0.0001));
        }

        [Test]
        public void TestForceHardRockUsesRunningDifficultyValues()
        {
            var hit = new HitCircle { StartTime = 1000, Position = new Vector2(128, 100) };

            var beatmap = new OsuBeatmap();
            beatmap.HitObjects.Add(hit);

            beatmap.SectionGimmicks.Sections.Add(new SectionGimmickSection
            {
                Id = 0,
                StartTime = 0,
                EndTime = 2000,
                Settings = new SectionGimmickSettings
                {
                    EnableDifficultyOverrides = true,
                    SectionApproachRate = 8,
                    SectionOverallDifficulty = 6,
                    SectionCircleSize = 4,
                    ForceHardRock = true,
                }
            });

            var processor = new OsuBeatmapProcessor(beatmap);
            processor.PreProcess();

            foreach (var obj in beatmap.HitObjects)
                obj.ApplyDefaults(beatmap.ControlPointInfo, beatmap.Difficulty);

            processor.PostProcess();

            // Running values from section overrides: AR8/OD6/CS4
            // HR should apply on top: AR=8*1.4=11.2->10 cap, OD=6*1.4=8.4, CS=4*1.3=5.2
            Assert.That(hit.TimePreempt, Is.EqualTo(450).Within(0.0001));

            var expectedOd = new OsuHitWindows();
            expectedOd.SetDifficulty(8.4);
            Assert.That(hit.HitWindows.WindowFor(HitResult.Great), Is.EqualTo(expectedOd.WindowFor(HitResult.Great)).Within(0.0001));

            var expectedScaleObj = new HitCircle { StartTime = hit.StartTime };
            expectedScaleObj.ApplyDefaults(beatmap.ControlPointInfo, new BeatmapDifficulty
            {
                CircleSize = 5.2f,
                ApproachRate = beatmap.Difficulty.ApproachRate,
                OverallDifficulty = beatmap.Difficulty.OverallDifficulty,
                DrainRate = beatmap.Difficulty.DrainRate,
                SliderMultiplier = beatmap.Difficulty.SliderMultiplier,
                SliderTickRate = beatmap.Difficulty.SliderTickRate,
            });

            Assert.That(hit.Scale, Is.EqualTo(expectedScaleObj.Scale).Within(0.0001));
        }

        [Test]
        public void TestForceHardRockDoesNotLeakOutsideSectionAndDoesNotCompound()
        {
            var inside1 = new HitCircle { StartTime = 500, Position = new Vector2(128, 100) };
            var inside2 = new HitCircle { StartTime = 900, Position = new Vector2(128, 110) };
            var outside = new HitCircle { StartTime = 1500, Position = new Vector2(128, 120) };

            var beatmap = new OsuBeatmap();
            beatmap.HitObjects.Add(inside1);
            beatmap.HitObjects.Add(inside2);
            beatmap.HitObjects.Add(outside);

            beatmap.Difficulty.ApproachRate = 5;
            beatmap.Difficulty.OverallDifficulty = 5;
            beatmap.Difficulty.CircleSize = 5;

            beatmap.SectionGimmicks.Sections.Add(new SectionGimmickSection
            {
                Id = 0,
                StartTime = 0,
                EndTime = 1000,
                Settings = new SectionGimmickSettings
                {
                    ForceHardRock = true,
                }
            });

            var processor = new OsuBeatmapProcessor(beatmap);
            processor.PreProcess();
            foreach (var obj in beatmap.HitObjects)
                obj.ApplyDefaults(beatmap.ControlPointInfo, beatmap.Difficulty);
            processor.PostProcess();

            // Inside section: AR should be HR AR(5*1.4=7)
            var ar7Preempt = 900;
            Assert.That(inside1.TimePreempt, Is.EqualTo(ar7Preempt).Within(0.0001));
            Assert.That(inside2.TimePreempt, Is.EqualTo(ar7Preempt).Within(0.0001));

            // Outside section: should return to base AR5 (not stay HR)
            var ar5Preempt = 1200;
            Assert.That(outside.TimePreempt, Is.EqualTo(ar5Preempt).Within(0.0001));

            // Position should be restored outside section (no HR flip leakage)
            Assert.That(outside.Y, Is.EqualTo(120).Within(0.0001));
        }

        [Test]
        public void TestHitObjectDifficultyOverrideAppliesWithoutSections()
        {
            var target = new HitCircle { StartTime = 500 };
            var other = new HitCircle { StartTime = 500, ComboOffset = 1, NewCombo = true };

            var beatmap = new OsuBeatmap();
            beatmap.HitObjects.Add(target);
            beatmap.HitObjects.Add(other);

            var processor = new OsuBeatmapProcessor(beatmap);
            processor.PreProcess();

            foreach (var obj in beatmap.HitObjects)
                obj.ApplyDefaults(beatmap.ControlPointInfo, beatmap.Difficulty);

            target.UpdateComboInformation(null);
            other.UpdateComboInformation(target);

            target.GimmickObjectId = 1001;
            other.GimmickObjectId = 2002;

            beatmap.HitObjectGimmicks.Entries.Add(new HitObjectGimmickEntry
            {
                ObjectId = target.GimmickObjectId,
                StartTime = target.StartTime,
                ComboIndexWithOffsets = target.ComboIndexWithOffsets,
                Settings = new HitObjectGimmickSettings
                {
                    EnableDifficultyOverrides = true,
                    SectionApproachRate = 10,
                    SectionOverallDifficulty = 9,
                    SectionCircleSize = 7,
                },
            });

            processor.PostProcess();

            Assert.That(target.TimePreempt, Is.EqualTo(450).Within(0.0001));
            Assert.That(other.TimePreempt, Is.EqualTo(1200).Within(0.0001));

            var od9 = new OsuHitWindows();
            od9.SetDifficulty(9);

            var baseOd = new OsuHitWindows();
            baseOd.SetDifficulty(beatmap.Difficulty.OverallDifficulty);

            Assert.That(target.HitWindows.WindowFor(HitResult.Great), Is.EqualTo(od9.WindowFor(HitResult.Great)).Within(0.0001));
            Assert.That(other.HitWindows.WindowFor(HitResult.Great), Is.EqualTo(baseOd.WindowFor(HitResult.Great)).Within(0.0001));
            Assert.That(target.Scale, Is.LessThan(other.Scale));
        }

        [Test]
        public void TestHitObjectDifficultyOverrideAppliesOnTopOfSectionOverrides()
        {
            var target = new HitCircle { StartTime = 500 };
            var other = new HitCircle { StartTime = 700, ComboOffset = 1, NewCombo = true };

            var beatmap = new OsuBeatmap();
            beatmap.HitObjects.Add(target);
            beatmap.HitObjects.Add(other);

            beatmap.SectionGimmicks.Sections.Add(new SectionGimmickSection
            {
                Id = 0,
                StartTime = 0,
                EndTime = 1000,
                Settings = new SectionGimmickSettings
                {
                    EnableDifficultyOverrides = true,
                    SectionApproachRate = 8,
                    SectionOverallDifficulty = 8,
                    SectionCircleSize = 5,
                }
            });

            var processor = new OsuBeatmapProcessor(beatmap);
            processor.PreProcess();

            foreach (var obj in beatmap.HitObjects)
                obj.ApplyDefaults(beatmap.ControlPointInfo, beatmap.Difficulty);

            target.UpdateComboInformation(null);
            other.UpdateComboInformation(target);

            target.GimmickObjectId = 3003;
            other.GimmickObjectId = 4004;

            beatmap.HitObjectGimmicks.Entries.Add(new HitObjectGimmickEntry
            {
                ObjectId = target.GimmickObjectId,
                StartTime = target.StartTime,
                ComboIndexWithOffsets = target.ComboIndexWithOffsets,
                Settings = new HitObjectGimmickSettings
                {
                    EnableDifficultyOverrides = true,
                    SectionApproachRate = 10,
                },
            });

            processor.PostProcess();

            // target should use object override AR10
            Assert.That(target.TimePreempt, Is.EqualTo(450).Within(0.0001));
            // other should use section override AR8
            Assert.That(other.TimePreempt, Is.EqualTo(750).Within(0.0001));
        }

        [Test]
        public void TestDeletedHitObjectGimmickEntriesAreRemovedAndDuplicateObjectEntriesCollapsed()
        {
            var first = new HitCircle { StartTime = 500 };
            var second = new HitCircle { StartTime = 700, NewCombo = true, ComboOffset = 1 };

            var beatmap = new OsuBeatmap();
            beatmap.HitObjects.Add(first);
            beatmap.HitObjects.Add(second);

            var processor = new OsuBeatmapProcessor(beatmap);
            processor.PreProcess();

            foreach (var obj in beatmap.HitObjects)
                obj.ApplyDefaults(beatmap.ControlPointInfo, beatmap.Difficulty);

            first.UpdateComboInformation(null);
            second.UpdateComboInformation(first);

            first.GimmickObjectId = 111;
            second.GimmickObjectId = 222;

            beatmap.HitObjectGimmicks.Entries.Add(new HitObjectGimmickEntry
            {
                ObjectId = 111,
                StartTime = first.StartTime,
                ComboIndexWithOffsets = first.ComboIndexWithOffsets,
                Settings = new HitObjectGimmickSettings { ForceNoApproachCircle = false }
            });

            beatmap.HitObjectGimmicks.Entries.Add(new HitObjectGimmickEntry
            {
                ObjectId = 111,
                StartTime = first.StartTime,
                ComboIndexWithOffsets = first.ComboIndexWithOffsets,
                Settings = new HitObjectGimmickSettings { ForceNoApproachCircle = true }
            });

            beatmap.HitObjectGimmicks.Entries.Add(new HitObjectGimmickEntry
            {
                ObjectId = 999,
                StartTime = 1200,
                ComboIndexWithOffsets = 0,
                Settings = new HitObjectGimmickSettings { ForceHardRock = true }
            });

            processor.PostProcess();

            Assert.That(beatmap.HitObjectGimmicks.Entries.Count(e => e.ObjectId == 111), Is.EqualTo(1));
            Assert.That(beatmap.HitObjectGimmicks.Entries.Single(e => e.ObjectId == 111).Settings.ForceNoApproachCircle, Is.True);
            Assert.That(beatmap.HitObjectGimmicks.Entries.All(e => e.ObjectId != 999), Is.True);
        }

        [Test]
        public void TestEntryObjectIdRemapsToCurrentObjectIdAfterReloadLikeMismatch()
        {
            var first = new HitCircle { StartTime = 500 };
            var second = new HitCircle { StartTime = 700, NewCombo = true, ComboOffset = 1 };

            var beatmap = new OsuBeatmap();
            beatmap.HitObjects.Add(first);
            beatmap.HitObjects.Add(second);

            var processor = new OsuBeatmapProcessor(beatmap);
            processor.PreProcess();

            foreach (var obj in beatmap.HitObjects)
                obj.ApplyDefaults(beatmap.ControlPointInfo, beatmap.Difficulty);

            first.UpdateComboInformation(null);
            second.UpdateComboInformation(first);

            // Simulate reopen where object got a different runtime-generated id.
            first.GimmickObjectId = 5001;

            beatmap.HitObjectGimmicks.Entries.Add(new HitObjectGimmickEntry
            {
                ObjectId = 7777,
                StartTime = first.StartTime,
                ComboIndexWithOffsets = first.ComboIndexWithOffsets,
                Settings = new HitObjectGimmickSettings { ForceHidden = true }
            });

            processor.PostProcess();

            var entry = beatmap.HitObjectGimmicks.Entries.Single();
            Assert.That(entry.ObjectId, Is.EqualTo(first.GimmickObjectId));
            Assert.That(entry.Settings.ForceHidden, Is.True);
        }

        [Test]
        public void TestSectionTickRateOverrideAppliesToSliderTickGeneration()
        {
            var slider = new Slider
            {
                StartTime = 1000,
                Position = new Vector2(100, 100),
                Path = new SliderPath(PathType.LINEAR, new[]
                {
                    Vector2.Zero,
                    new Vector2(300, 0),
                }, 300),
                RepeatCount = 0,
            };

            var beatmap = new OsuBeatmap();
            beatmap.Difficulty.SliderTickRate = 1;
            beatmap.HitObjects.Add(slider);

            beatmap.SectionGimmicks.Sections.Add(new SectionGimmickSection
            {
                Id = 0,
                StartTime = 0,
                EndTime = 2000,
                Settings = new SectionGimmickSettings
                {
                    EnableDifficultyOverrides = true,
                    SectionTickRate = 4,
                }
            });

            var processor = new OsuBeatmapProcessor(beatmap);
            processor.PreProcess();

            foreach (var obj in beatmap.HitObjects)
                obj.ApplyDefaults(beatmap.ControlPointInfo, beatmap.Difficulty);

            processor.PostProcess();

            Assert.That(slider.NestedHitObjects.OfType<SliderTick>().Count(), Is.GreaterThan(0));
        }

        [Test]
        public void TestUnsafeNegativeTickRateOverrideIsAppliedWhenEnabled()
        {
            var slider = new Slider
            {
                StartTime = 1000,
                Position = new Vector2(100, 100),
                Path = new SliderPath(PathType.LINEAR, new[]
                {
                    Vector2.Zero,
                    new Vector2(300, 0),
                }, 300),
                RepeatCount = 0,
            };

            var beatmap = new OsuBeatmap();
            beatmap.Difficulty.SliderTickRate = 1;
            beatmap.HitObjects.Add(slider);

            beatmap.SectionGimmicks.Sections.Add(new SectionGimmickSection
            {
                Id = 0,
                StartTime = 0,
                EndTime = 2000,
                Settings = new SectionGimmickSettings
                {
                    EnableDifficultyOverrides = true,
                    AllowUnsafeTickRateOverrideValues = true,
                    SectionTickRate = -2,
                }
            });

            var processor = new OsuBeatmapProcessor(beatmap);
            processor.PreProcess();

            foreach (var obj in beatmap.HitObjects)
                obj.ApplyDefaults(beatmap.ControlPointInfo, beatmap.Difficulty);

            processor.PostProcess();

            Assert.That(slider.NestedHitObjects.OfType<SliderTick>().Count(), Is.EqualTo(0));
        }

        [Test]
        public void TestForceFlashlightPresenceDetection()
        {
            var beatmap = new OsuBeatmap();
            beatmap.HitObjects.Add(new HitCircle { StartTime = 1000 });

            beatmap.SectionGimmicks.Sections.Add(new SectionGimmickSection
            {
                Id = 0,
                StartTime = 0,
                EndTime = 1500,
                Settings = new SectionGimmickSettings
                {
                    ForceFlashlight = true,
                }
            });

            Assert.That(osu.Game.Rulesets.Osu.UI.SectionGimmickFlashlightOverlay.HasAnyForcedFlashlightSection(beatmap), Is.True);

            beatmap.SectionGimmicks.Sections[0].Settings.ForceFlashlight = false;
            Assert.That(osu.Game.Rulesets.Osu.UI.SectionGimmickFlashlightOverlay.HasAnyForcedFlashlightSection(beatmap), Is.False);
        }

        [Test]
        public void TestForceNoApproachCircleFlagsAppliedBySection()
        {
            var inside = new HitCircle { StartTime = 500 };
            var outside = new HitCircle { StartTime = 1500 };

            var beatmap = new OsuBeatmap();
            beatmap.HitObjects.Add(inside);
            beatmap.HitObjects.Add(outside);

            beatmap.SectionGimmicks.Sections.Add(new SectionGimmickSection
            {
                Id = 0,
                StartTime = 0,
                EndTime = 1000,
                Settings = new SectionGimmickSettings
                {
                    ForceNoApproachCircle = true,
                }
            });

            var processor = new OsuBeatmapProcessor(beatmap);
            processor.PreProcess();

            foreach (var obj in beatmap.HitObjects)
                obj.ApplyDefaults(beatmap.ControlPointInfo, beatmap.Difficulty);

            processor.PostProcess();

            Assert.That(inside.ForceNoApproachCircle, Is.True);
            Assert.That(outside.ForceNoApproachCircle, Is.False);
        }

        [Test]
        public void TestForceNoApproachCircleFlagsAppliedBySelectedObjectGimmick()
        {
            var target = new HitCircle { StartTime = 500 };
            var other = new HitCircle { StartTime = 500, ComboOffset = 1, NewCombo = true };

            var beatmap = new OsuBeatmap();
            beatmap.HitObjects.Add(target);
            beatmap.HitObjects.Add(other);

            var processor = new OsuBeatmapProcessor(beatmap);
            processor.PreProcess();

            foreach (var obj in beatmap.HitObjects)
                obj.ApplyDefaults(beatmap.ControlPointInfo, beatmap.Difficulty);

            target.UpdateComboInformation(null);
            other.UpdateComboInformation(target);

            target.GimmickObjectId = 5005;
            other.GimmickObjectId = 6006;

            beatmap.HitObjectGimmicks.Entries.Add(new HitObjectGimmickEntry
            {
                ObjectId = target.GimmickObjectId,
                StartTime = target.StartTime,
                ComboIndexWithOffsets = target.ComboIndexWithOffsets,
                Settings = new HitObjectGimmickSettings
                {
                    ForceNoApproachCircle = true,
                },
            });

            processor.PostProcess();

            Assert.That(target.ForceNoApproachCircle, Is.True);
            Assert.That(other.ForceNoApproachCircle, Is.False);
        }

        [Test]
        public void TestFakeSliderConversionPreservesPathAndRepeats()
        {
            var fakeSliderSource = new Slider
            {
                StartTime = 1000,
                Position = new Vector2(128, 192),
                Path = new SliderPath(PathType.LINEAR, new[]
                {
                    Vector2.Zero,
                    new Vector2(200, 0),
                }, 200),
                RepeatCount = 2,
            };

            var beatmap = new OsuBeatmap();
            beatmap.HitObjects.Add(fakeSliderSource);

            var processor = new OsuBeatmapProcessor(beatmap);
            processor.PreProcess();

            foreach (var obj in beatmap.HitObjects)
                obj.ApplyDefaults(beatmap.ControlPointInfo, beatmap.Difficulty);

            fakeSliderSource.UpdateComboInformation(null);
            fakeSliderSource.GimmickObjectId = 9001;

            beatmap.HitObjectGimmicks.Entries.Add(new HitObjectGimmickEntry
            {
                ObjectId = fakeSliderSource.GimmickObjectId,
                StartTime = fakeSliderSource.StartTime,
                ComboIndexWithOffsets = fakeSliderSource.ComboIndexWithOffsets,
                Settings = new HitObjectGimmickSettings
                {
                    IsFakeNote = true,
                },
            });

            processor.PostProcess();

            Assert.That(beatmap.HitObjects[0], Is.TypeOf<FakeSlider>());

            var fakeSlider = (FakeSlider)beatmap.HitObjects[0];
            Assert.That(fakeSlider.RepeatCount, Is.EqualTo(2));
            Assert.That(fakeSlider.Path.ControlPoints.Count, Is.EqualTo(2));
            Assert.That(fakeSlider.NestedHitObjects.OfType<SliderTick>().Count(), Is.GreaterThan(0));
            Assert.That(fakeSlider.NestedHitObjects.OfType<SliderTailCircle>().Count(), Is.EqualTo(1));
            Assert.That(fakeSlider.NestedHitObjects.OfType<SliderRepeat>().Count(), Is.EqualTo(2));
        }

        [Test]
        public void TestFakeSliderNestedHitObjectsUseFakeJudgement()
        {
            var fakeSlider = new FakeSlider
            {
                StartTime = 1000,
                Position = new Vector2(128, 192),
                Path = new SliderPath(PathType.LINEAR, new[]
                {
                    Vector2.Zero,
                    new Vector2(220, 0),
                }, 220),
                RepeatCount = 1,
            };

            var beatmap = new OsuBeatmap();
            beatmap.HitObjects.Add(fakeSlider);

            var processor = new OsuBeatmapProcessor(beatmap);
            processor.PreProcess();

            foreach (var obj in beatmap.HitObjects)
                obj.ApplyDefaults(beatmap.ControlPointInfo, beatmap.Difficulty);

            processor.PostProcess();

            Assert.That(fakeSlider.HeadCircle.CreateJudgement(), Is.TypeOf<FakeCircleJudgement>());

            var tail = fakeSlider.NestedHitObjects.OfType<SliderTailCircle>().Single();
            Assert.That(tail.CreateJudgement(), Is.TypeOf<FakeCircleJudgement>());

            var repeat = fakeSlider.NestedHitObjects.OfType<SliderRepeat>().Single();
            Assert.That(repeat.CreateJudgement(), Is.TypeOf<FakeCircleJudgement>());

            var tick = fakeSlider.NestedHitObjects.OfType<SliderTick>().First();
            Assert.That(tick.CreateJudgement(), Is.TypeOf<FakeCircleJudgement>());
        }

        [Test]
        public void TestFakePunishModeLegacyHpDrainMapsToMiss()
        {
            var beatmap = new OsuBeatmap
            {
                HitObjects =
                {
                    new HitCircle
                    {
                        StartTime = 1000,
                        Position = new Vector2(128, 128),
                        GimmickObjectId = 7777,
                    }
                },
                HitObjectGimmicks = new BeatmapHitObjectGimmicks
                {
                    Entries =
                    {
                        new HitObjectGimmickEntry
                        {
                            ObjectId = 7777,
                            StartTime = 1000,
                            ComboIndexWithOffsets = 1,
                            Settings = new HitObjectGimmickSettings
                            {
                                IsFakeNote = true,
                                FakePunishMode = (FakePunishMode)2,
                            }
                        }
                    }
                }
            };

            var processor = new OsuBeatmapProcessor(beatmap);
            processor.PreProcess();
            processor.PostProcess();

            var fake = beatmap.HitObjects.OfType<FakeHitCircle>().Single();
            Assert.That(fake.FakePunishMode, Is.EqualTo(FakePunishMode.Miss));
        }

        [Test]
        public void TestFakeAutoHitFlagsAreCopiedToFakeSlider()
        {
            var source = new Slider
            {
                StartTime = 1000,
                Position = new Vector2(128, 192),
                Path = new SliderPath(PathType.LINEAR, new[]
                {
                    Vector2.Zero,
                    new Vector2(180, 0),
                }, 180),
                RepeatCount = 1,
            };

            var beatmap = new OsuBeatmap();
            beatmap.HitObjects.Add(source);

            var processor = new OsuBeatmapProcessor(beatmap);
            processor.PreProcess();

            foreach (var obj in beatmap.HitObjects)
                obj.ApplyDefaults(beatmap.ControlPointInfo, beatmap.Difficulty);

            source.UpdateComboInformation(null);
            source.GimmickObjectId = 5050;

            beatmap.HitObjectGimmicks.Entries.Add(new HitObjectGimmickEntry
            {
                ObjectId = source.GimmickObjectId,
                StartTime = source.StartTime,
                ComboIndexWithOffsets = source.ComboIndexWithOffsets,
                Settings = new HitObjectGimmickSettings
                {
                    IsFakeNote = true,
                    FakeAutoHitOnApproachClose = true,
                    FakeAutoHitPlayHitsound = true,
                },
            });

            processor.PostProcess();

            var fakeSlider = (FakeSlider)beatmap.HitObjects[0];
            Assert.That(fakeSlider.FakeAutoHitOnApproachClose, Is.True);
            Assert.That(fakeSlider.FakeAutoHitPlayHitsound, Is.True);
        }
    }
}
