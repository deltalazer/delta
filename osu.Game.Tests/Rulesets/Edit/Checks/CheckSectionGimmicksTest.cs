// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Linq;
using NUnit.Framework;
using osu.Framework.Audio.Track;
using osu.Framework.Graphics.Textures;
using osu.Game.Beatmaps;
using osu.Game.Beatmaps.SectionGimmicks;
using osu.Game.Rulesets.Edit;
using osu.Game.Rulesets.Edit.Checks;
using osu.Game.Skinning;

namespace osu.Game.Tests.Rulesets.Edit.Checks
{
    [TestFixture]
    public class CheckSectionGimmicksTest
    {
        [Test]
        public void TestNoIssueForValidSectionGimmicks()
        {
            var beatmap = new Beatmap();
            beatmap.SectionGimmicks.Sections.Add(new SectionGimmickSection
            {
                Id = 0,
                StartTime = 0,
                EndTime = 1000,
                Settings = new SectionGimmickSettings
                {
                    EnableNoMiss = true,
                }
            });

            var issues = new CheckSectionGimmicks().Run(createContext(beatmap)).ToList();

            Assert.That(issues, Is.Empty);
        }

        [Test]
        public void TestIssueForInvalidSectionGimmicks()
        {
            var beatmap = new Beatmap();
            beatmap.SectionGimmicks.Sections.Add(new SectionGimmickSection
            {
                Id = 0,
                StartTime = 500,
                EndTime = 100,
                Settings = new SectionGimmickSettings(),
            });

            var issues = new CheckSectionGimmicks().Run(createContext(beatmap)).ToList();

            Assert.That(issues.Count, Is.EqualTo(1));
            Assert.That(issues[0].Template, Is.TypeOf<CheckSectionGimmicks.IssueTemplateInvalidSectionGimmicks>());
            Assert.That(issues[0].ToString(), Does.Contain("invalid range").IgnoreCase);
        }

        [Test]
        public void TestNoIssueForExtendedDifficultyOverrideLimits()
        {
            var beatmap = new Beatmap();
            beatmap.SectionGimmicks.Sections.Add(new SectionGimmickSection
            {
                Id = 0,
                StartTime = 0,
                EndTime = 1000,
                Settings = new SectionGimmickSettings
                {
                    EnableDifficultyOverrides = true,
                    SectionCircleSize = 11,
                    SectionApproachRate = 11,
                    SectionOverallDifficulty = 11,
                }
            });

            var issues = new CheckSectionGimmicks().Run(createContext(beatmap)).ToList();

            Assert.That(issues, Is.Empty);
        }

        [Test]
        public void TestNoIssueForNegativeArDifficultyOverride()
        {
            var beatmap = new Beatmap();
            beatmap.SectionGimmicks.Sections.Add(new SectionGimmickSection
            {
                Id = 0,
                StartTime = 0,
                EndTime = 1000,
                Settings = new SectionGimmickSettings
                {
                    EnableDifficultyOverrides = true,
                    SectionApproachRate = -20,
                }
            });

            var issues = new CheckSectionGimmicks().Run(createContext(beatmap)).ToList();

            Assert.That(issues, Is.Empty);
        }

        [Test]
        public void TestIssueForGradualDifficultyWithoutOverrides()
        {
            var beatmap = new Beatmap();
            beatmap.SectionGimmicks.Sections.Add(new SectionGimmickSection
            {
                Id = 0,
                StartTime = 0,
                EndTime = 1000,
                Settings = new SectionGimmickSettings
                {
                    EnableGradualDifficultyChange = true,
                    GradualDifficultyChangeEndTimeMs = 900,
                }
            });

            var issues = new CheckSectionGimmicks().Run(createContext(beatmap)).ToList();

            Assert.That(issues.Count, Is.EqualTo(1));
            Assert.That(issues[0].ToString(), Does.Contain("requires difficulty overrides").IgnoreCase);
        }

        [Test]
        public void TestIssueForBothSingleTapAndAlternateForced()
        {
            var beatmap = new Beatmap();
            beatmap.SectionGimmicks.Sections.Add(new SectionGimmickSection
            {
                Id = 0,
                StartTime = 0,
                EndTime = 1000,
                Settings = new SectionGimmickSettings
                {
                    ForceSingleTap = true,
                    ForceAlternate = true,
                }
            });

            var issues = new CheckSectionGimmicks().Run(createContext(beatmap)).ToList();

            Assert.That(issues.Count, Is.EqualTo(1));
            Assert.That(issues[0].ToString(), Does.Contain("cannot both be enabled").IgnoreCase);
        }

        [Test]
        public void TestIssueForInvalidHpStartRange()
        {
            var beatmap = new Beatmap();
            beatmap.SectionGimmicks.Sections.Add(new SectionGimmickSection
            {
                Id = 0,
                StartTime = 0,
                EndTime = 1000,
                Settings = new SectionGimmickSettings
                {
                    HPStart = 1.2f,
                }
            });

            var issues = new CheckSectionGimmicks().Run(createContext(beatmap)).ToList();

            Assert.That(issues.Count, Is.EqualTo(1));
            Assert.That(issues[0].ToString(), Does.Contain("HPStart").IgnoreCase);
        }

        [Test]
        public void TestIssueForInvalidHpCapRange()
        {
            var beatmap = new Beatmap();
            beatmap.SectionGimmicks.Sections.Add(new SectionGimmickSection
            {
                Id = 0,
                StartTime = 0,
                EndTime = 1000,
                Settings = new SectionGimmickSettings
                {
                    HPCap = -0.1f,
                }
            });

            var issues = new CheckSectionGimmicks().Run(createContext(beatmap)).ToList();

            Assert.That(issues.Count, Is.EqualTo(1));
            Assert.That(issues[0].ToString(), Does.Contain("HPCap").IgnoreCase);
        }

        [Test]
        public void TestNoIssueForOutOfRangeDifficultyWhenUnsafeEnabled()
        {
            var beatmap = new Beatmap();
            beatmap.SectionGimmicks.Sections.Add(new SectionGimmickSection
            {
                Id = 0,
                StartTime = 0,
                EndTime = 1000,
                Settings = new SectionGimmickSettings
                {
                    EnableDifficultyOverrides = true,
                    AllowUnsafeDifficultyOverrideValues = true,
                    SectionCircleSize = 500,
                }
            });

            var issues = new CheckSectionGimmicks().Run(createContext(beatmap)).ToList();

            Assert.That(issues, Is.Empty);
        }

        [Test]
        public void TestIssueForInvalidFlashlightRadius()
        {
            var beatmap = new Beatmap();
            beatmap.SectionGimmicks.Sections.Add(new SectionGimmickSection
            {
                Id = 0,
                StartTime = 0,
                EndTime = 1000,
                Settings = new SectionGimmickSettings
                {
                    ForceFlashlight = true,
                    FlashlightRadius = 999,
                }
            });

            var issues = new CheckSectionGimmicks().Run(createContext(beatmap)).ToList();

            Assert.That(issues.Count, Is.EqualTo(1));
            Assert.That(issues[0].ToString(), Does.Contain("FlashlightRadius").IgnoreCase);
        }

        [Test]
        public void TestIssueForGradualFlashlightRadiusWithoutForceFlashlight()
        {
            var beatmap = new Beatmap();
            beatmap.SectionGimmicks.Sections.Add(new SectionGimmickSection
            {
                Id = 0,
                StartTime = 0,
                EndTime = 1000,
                Settings = new SectionGimmickSettings
                {
                    EnableGradualFlashlightRadiusChange = true,
                    FlashlightRadius = 120,
                }
            });

            var issues = new CheckSectionGimmicks().Run(createContext(beatmap)).ToList();

            Assert.That(issues.Count, Is.EqualTo(1));
            Assert.That(issues[0].ToString(), Does.Contain("gradual flashlight radius").IgnoreCase);
        }

        [Test]
        public void TestNoIssueForGradualFlashlightFadeInWithoutGradualRadius()
        {
            var beatmap = new Beatmap();
            beatmap.SectionGimmicks.Sections.Add(new SectionGimmickSection
            {
                Id = 0,
                StartTime = 0,
                EndTime = 1000,
                Settings = new SectionGimmickSettings
                {
                    ForceFlashlight = true,
                    FlashlightRadius = 120,
                    EnableGradualFlashlightFadeIn = true,
                }
            });

            var issues = new CheckSectionGimmicks().Run(createContext(beatmap)).ToList();

            Assert.That(issues, Is.Empty);
        }

        [Test]
        public void TestNoIssueForGradualFlashlightRadiusWithFadeIn()
        {
            var beatmap = new Beatmap();
            beatmap.SectionGimmicks.Sections.Add(new SectionGimmickSection
            {
                Id = 0,
                StartTime = 0,
                EndTime = 1000,
                Settings = new SectionGimmickSettings
                {
                    ForceFlashlight = true,
                    FlashlightRadius = 120,
                    EnableGradualFlashlightRadiusChange = true,
                    EnableGradualFlashlightFadeIn = true,
                }
            });

            var issues = new CheckSectionGimmicks().Run(createContext(beatmap)).ToList();

            Assert.That(issues, Is.Empty);
        }

        private static BeatmapVerifierContext createContext(IBeatmap beatmap)
        {
            var working = new TestWorkingBeatmap(beatmap.BeatmapInfo, beatmap);
            return new BeatmapVerifierContext(beatmap, working);
        }

        private class TestWorkingBeatmap : WorkingBeatmap
        {
            private readonly IBeatmap beatmap;

            public TestWorkingBeatmap(BeatmapInfo beatmapInfo, IBeatmap beatmap)
                : base(beatmapInfo, null)
            {
                this.beatmap = beatmap;
            }

            protected override IBeatmap GetBeatmap() => beatmap;

            public override Texture GetBackground() => throw new NotImplementedException();

            protected override Track GetBeatmapTrack() => throw new NotImplementedException();

            protected internal override ISkin GetSkin() => throw new NotImplementedException();

            public override System.IO.Stream GetStream(string storagePath) => throw new NotImplementedException();
        }
    }
}
