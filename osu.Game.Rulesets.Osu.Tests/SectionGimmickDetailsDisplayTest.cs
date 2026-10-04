// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Reflection;
using NUnit.Framework;
using osu.Framework.Bindables;
using osu.Game.Beatmaps;
using osu.Game.Beatmaps.SectionGimmicks;
using osu.Game.Rulesets.Osu.UI;

namespace osu.Game.Rulesets.Osu.Tests
{
    [TestFixture]
    public class SectionGimmickDetailsDisplayTest
    {
        [Test]
        public void TestBuildDetailsLabelSingleLineNoHpPrefixes()
        {
            var section = new SectionGimmickSection
            {
                Id = 1,
                StartTime = 0,
                EndTime = 1000,
                Settings = new SectionGimmickSettings
                {
                    EnableHPGimmick = true,
                    HP300 = 0,
                    HP100 = -0.2f,
                    HP50 = 0.1f,
                    HPMiss = -0.5f,
                }
            };

            string label = invokeBuildLabel(section, false);

            Assert.That(label, Does.Contain("300: +0"));
            Assert.That(label, Does.Contain("100: -0.2"));
            Assert.That(label, Does.Contain("50: +0.1"));
            Assert.That(label, Does.Contain("Miss: -0.5"));

            Assert.That(label, Does.Not.Contain("HP300"));
            Assert.That(label, Does.Not.Contain("HP100"));
            Assert.That(label, Does.Not.Contain("HP50"));
            Assert.That(label, Does.Not.Contain("HPMiss"));

            Assert.That(label, Does.Contain(" | "));
        }

        [Test]
        public void TestBuildDetailsLabelMultiLineMode()
        {
            var section = new SectionGimmickSection
            {
                Id = 1,
                StartTime = 0,
                EndTime = 1000,
                Settings = new SectionGimmickSettings
                {
                    EnableHPGimmick = true,
                    HP300 = 0,
                    HP100 = -0.2f,
                    HP50 = 0.1f,
                }
            };

            string label = invokeBuildLabel(section, true);

            Assert.That(label, Does.Contain("300: +0\n"));
            Assert.That(label, Does.Contain("100: -0.2\n"));
            Assert.That(label, Does.Not.Contain(" | "));
        }

        [Test]
        public void TestBuildDetailsLabelShowsMaxMissAndPerfectWindowReference()
        {
            var section = new SectionGimmickSection
            {
                Id = 1,
                StartTime = 0,
                EndTime = 1000,
                Settings = new SectionGimmickSettings
                {
                    EnableCountLimits = true,
                    MaxMisses = 2,
                    EnableGreatOffsetPenalty = true,
                    GreatOffsetThresholdMs = 20,
                }
            };

            string label = invokeBuildLabel(section, false, new BeatmapDifficulty { OverallDifficulty = 8 });

            Assert.That(label, Does.Contain("MaxMiss: 2"));
            Assert.That(label, Does.Contain("Perfect(300)@OD8"));
            Assert.That(label, Does.Contain("±31.5ms"));
        }

        [Test]
        public void TestBuildDetailsLabelShowsNoApproachCircleMarker()
        {
            var section = new SectionGimmickSection
            {
                Id = 2,
                StartTime = 1000,
                EndTime = 2000,
                Settings = new SectionGimmickSettings
                {
                    EnableDifficultyOverrides = true,
                    SectionApproachRate = 9,
                    ForceNoApproachCircle = true,
                }
            };

            string label = invokeBuildLabel(section, false);
            Assert.That(label, Does.Contain("NoApproachCircle"));
        }

        [Test]
        public void TestBuildDetailsLabelShowsSingleTapAndAlternateMarkers()
        {
            var sectionSingleTap = new SectionGimmickSection
            {
                Id = 3,
                StartTime = 0,
                EndTime = 1000,
                Settings = new SectionGimmickSettings
                {
                    EnableDifficultyOverrides = true,
                    SectionApproachRate = 8,
                    ForceSingleTap = true,
                }
            };

            string singleTapLabel = invokeBuildLabel(sectionSingleTap, false);
            Assert.That(singleTapLabel, Does.Contain("SG"));

            var sectionAlternate = new SectionGimmickSection
            {
                Id = 4,
                StartTime = 0,
                EndTime = 1000,
                Settings = new SectionGimmickSettings
                {
                    EnableDifficultyOverrides = true,
                    SectionApproachRate = 8,
                    ForceAlternate = true,
                }
            };

            string alternateLabel = invokeBuildLabel(sectionAlternate, false);
            Assert.That(alternateLabel, Does.Contain("AL"));
        }

        [Test]
        public void TestBuildDetailsLabelShowsTraceableMarker()
        {
            var section = new SectionGimmickSection
            {
                Id = 7,
                StartTime = 0,
                EndTime = 1000,
                Settings = new SectionGimmickSettings
                {
                    EnableDifficultyOverrides = true,
                    SectionApproachRate = 8,
                    ForceTraceable = true,
                }
            };

            string label = invokeBuildLabel(section, false);
            Assert.That(label, Does.Contain("TC"));
        }

        [Test]
        public void TestBuildDetailsLabelShowsAccuracyRequirementPercent()
        {
            var section = new SectionGimmickSection
            {
                Id = 8,
                StartTime = 0,
                EndTime = 1000,
                Settings = new SectionGimmickSettings
                {
                    EnableAccuracyRequirement = true,
                    RequiredAccuracy = 0.9753f,
                }
            };

            string label = invokeBuildLabel(section, false);
            Assert.That(label, Does.Contain("ReqAcc: 97.53%"));
        }

        private static string invokeBuildLabel(SectionGimmickSection section, bool multiline, IBeatmapDifficultyInfo? difficulty = null)
        {
            var method = typeof(SectionGimmickDetailsDisplay).GetMethod("buildDetailsLabelForTest", BindingFlags.NonPublic | BindingFlags.Static);
            Assert.That(method, Is.Not.Null);

            var bindable = new BindableBool(multiline);
            return (string)method!.Invoke(null, new object?[] { section, bindable, difficulty })!;
        }
    }
}
