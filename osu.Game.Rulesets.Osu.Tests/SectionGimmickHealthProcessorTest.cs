// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using NUnit.Framework;
using osu.Game.Beatmaps.SectionGimmicks;
using osu.Game.Rulesets.Judgements;
using osu.Game.Rulesets.Osu.Beatmaps;
using osu.Game.Rulesets.Osu.Judgements;
using osu.Game.Rulesets.Osu.Objects;
using osu.Game.Rulesets.Osu.Scoring;
using osu.Game.Rulesets.Scoring;

namespace osu.Game.Rulesets.Osu.Tests
{
    [TestFixture]
    public class SectionGimmickHealthProcessorTest
    {
        [Test]
        public void TestNoMissFailsOnMiss()
        {
            var hit = new HitCircle { StartTime = 1000 };
            var beatmap = new OsuBeatmap();
            beatmap.HitObjects.Add(hit);
            beatmap.SectionGimmicks.Sections.Add(new SectionGimmickSection
            {
                Id = 0,
                StartTime = 0,
                EndTime = -1,
                Settings = new SectionGimmickSettings
                {
                    EnableNoMiss = true
                }
            });

            var hp = new SectionGimmickHealthProcessor(0);
            hp.ApplyBeatmap(beatmap);

            var result = new OsuJudgementResult(hit, hit.CreateJudgement()) { Type = HitResult.Miss };
            hp.ApplyResult(result);

            Assert.That(hp.HasFailed, Is.True);
        }

        [Test]
        public void TestCountLimitFailsOnExceed()
        {
            var hit1 = new HitCircle { StartTime = 1000 };
            var hit2 = new HitCircle { StartTime = 2000 };
            var beatmap = new OsuBeatmap();
            beatmap.HitObjects.Add(hit1);
            beatmap.HitObjects.Add(hit2);
            beatmap.SectionGimmicks.Sections.Add(new SectionGimmickSection
            {
                Id = 0,
                StartTime = 0,
                EndTime = -1,
                Settings = new SectionGimmickSettings
                {
                    EnableCountLimits = true,
                    Max100s = 0
                }
            });

            var hp = new SectionGimmickHealthProcessor(0);
            hp.ApplyBeatmap(beatmap);

            hp.ApplyResult(new OsuJudgementResult(hit1, hit1.CreateJudgement()) { Type = HitResult.Ok });
            hp.ApplyResult(new OsuJudgementResult(hit2, hit2.CreateJudgement()) { Type = HitResult.Ok });

            Assert.That(hp.HasFailed, Is.True);
        }

        [Test]
        public void TestCountLimitFailsOnExceedMissLimit()
        {
            var hit1 = new HitCircle { StartTime = 1000 };
            var hit2 = new HitCircle { StartTime = 2000 };
            var beatmap = new OsuBeatmap();
            beatmap.HitObjects.Add(hit1);
            beatmap.HitObjects.Add(hit2);
            beatmap.SectionGimmicks.Sections.Add(new SectionGimmickSection
            {
                Id = 0,
                StartTime = 0,
                EndTime = -1,
                Settings = new SectionGimmickSettings
                {
                    EnableCountLimits = true,
                    MaxMisses = 0
                }
            });

            var hp = new SectionGimmickHealthProcessor(0);
            hp.ApplyBeatmap(beatmap);

            hp.ApplyResult(new OsuJudgementResult(hit1, hit1.CreateJudgement()) { Type = HitResult.Miss });
            hp.ApplyResult(new OsuJudgementResult(hit2, hit2.CreateJudgement()) { Type = HitResult.Miss });

            Assert.That(hp.HasFailed, Is.True);
        }

        [Test]
        public void TestGreatOffsetPenaltyAppliesToGreatOkAndMeh()
        {
            var hit1 = new HitCircle { StartTime = 1000 };
            var hit2 = new HitCircle { StartTime = 2000 };
            var hit3 = new HitCircle { StartTime = 3000 };
            var beatmap = new OsuBeatmap();
            beatmap.HitObjects.Add(hit1);
            beatmap.HitObjects.Add(hit2);
            beatmap.HitObjects.Add(hit3);
            beatmap.SectionGimmicks.Sections.Add(new SectionGimmickSection
            {
                Id = 0,
                StartTime = 0,
                EndTime = -1,
                Settings = new SectionGimmickSettings
                {
                    EnableGreatOffsetPenalty = true,
                    GreatOffsetThresholdMs = 10,
                    GreatOffsetPenaltyHP = -0.1f,
                }
            });

            var withPenalty = new SectionGimmickHealthProcessor(0);
            withPenalty.ApplyBeatmap(beatmap);
            withPenalty.Health.Value = 0.8;
            withPenalty.ApplyResult(createResultWithOffset(hit1, HitResult.Great, -11));
            withPenalty.ApplyResult(createResultWithOffset(hit2, HitResult.Ok, -11));
            withPenalty.ApplyResult(createResultWithOffset(hit3, HitResult.Meh, -11));

            var withoutPenalty = new SectionGimmickHealthProcessor(0);
            beatmap.SectionGimmicks.Sections[0].Settings.GreatOffsetThresholdMs = 1000;
            withoutPenalty.ApplyBeatmap(beatmap);
            withoutPenalty.Health.Value = 0.8;
            withoutPenalty.ApplyResult(createResultWithOffset(hit1, HitResult.Great, -11));
            withoutPenalty.ApplyResult(createResultWithOffset(hit2, HitResult.Ok, -11));
            withoutPenalty.ApplyResult(createResultWithOffset(hit3, HitResult.Meh, -11));

            Assert.That(withPenalty.Health.Value - withoutPenalty.Health.Value, Is.EqualTo(-0.3).Within(0.0001));
        }

        [Test]
        public void TestGreatOffsetPenaltyDoesNotApplyWhenHPGimmickDefinesJudgement()
        {
            var hit = new HitCircle { StartTime = 1000 };
            var beatmap = new OsuBeatmap();
            beatmap.HitObjects.Add(hit);
            beatmap.SectionGimmicks.Sections.Add(new SectionGimmickSection
            {
                Id = 0,
                StartTime = 0,
                EndTime = -1,
                Settings = new SectionGimmickSettings
                {
                    EnableGreatOffsetPenalty = true,
                    GreatOffsetThresholdMs = 10,
                    GreatOffsetPenaltyHP = -0.1f,
                    EnableHPGimmick = true,
                    NoDrain = true,
                    HP300 = 0,
                    HP100 = -0.2f,
                    HP50 = 0,
                    HPMiss = 0,
                }
            });

            var hp = new SectionGimmickHealthProcessor(0);
            hp.ApplyBeatmap(beatmap);
            hp.Health.Value = 0.8;

            hp.ApplyResult(createResultWithOffset(hit, HitResult.Ok, 20));

            // Only HP gimmick should apply here (+0.2 because non-reverse mode negates configured HP values).
            Assert.That(hp.Health.Value, Is.EqualTo(1.0).Within(0.0001));
        }

        [Test]
        public void TestAccuracyRequirementFailsWhenFirstPostSectionResultProcessedBelowThreshold()
        {
            var hit1 = new HitCircle { StartTime = 1000 };
            var hit2 = new HitCircle { StartTime = 2000 };

            var beatmap = new OsuBeatmap();
            beatmap.HitObjects.Add(hit1);
            beatmap.HitObjects.Add(hit2);
            beatmap.SectionGimmicks.Sections.Add(new SectionGimmickSection
            {
                Id = 0,
                StartTime = 0,
                EndTime = 1500,
                Settings = new SectionGimmickSettings
                {
                    EnableAccuracyRequirement = true,
                    RequiredAccuracy = 0.8f,
                }
            });

            var hp = new SectionGimmickHealthProcessor(0);
            hp.ApplyBeatmap(beatmap);

            hp.ApplyResult(new OsuJudgementResult(hit1, hit1.CreateJudgement()) { Type = HitResult.Ok });
            Assert.That(hp.HasFailed, Is.False);

            hp.ApplyResult(new OsuJudgementResult(hit2, hit2.CreateJudgement()) { Type = HitResult.Great });

            Assert.That(hp.HasFailed, Is.True);
        }

        [Test]
        public void TestAccuracyRequirementPassesWhenSectionAccuracyMeetsThreshold()
        {
            var hit1 = new HitCircle { StartTime = 1000 };
            var hit2 = new HitCircle { StartTime = 2000 };

            var beatmap = new OsuBeatmap();
            beatmap.HitObjects.Add(hit1);
            beatmap.HitObjects.Add(hit2);
            beatmap.SectionGimmicks.Sections.Add(new SectionGimmickSection
            {
                Id = 0,
                StartTime = 0,
                EndTime = 2000,
                Settings = new SectionGimmickSettings
                {
                    EnableAccuracyRequirement = true,
                    RequiredAccuracy = 0.75f,
                }
            });

            var hp = new SectionGimmickHealthProcessor(0);
            hp.ApplyBeatmap(beatmap);

            hp.ApplyResult(new OsuJudgementResult(hit1, hit1.CreateJudgement()) { Type = HitResult.Great });
            hp.ApplyResult(new OsuJudgementResult(hit2, hit2.CreateJudgement()) { Type = HitResult.Ok });

            Assert.That(hp.HasFailed, Is.False);
        }

        [Test]
        public void TestAccuracyRequirementFailsOnFirstHitAfterSectionEnd()
        {
            var hitInSection = new HitCircle { StartTime = 1000 };
            var hitAfterSection = new HitCircle { StartTime = 3000 };

            var beatmap = new OsuBeatmap();
            beatmap.HitObjects.Add(hitInSection);
            beatmap.HitObjects.Add(hitAfterSection);
            beatmap.SectionGimmicks.Sections.Add(new SectionGimmickSection
            {
                Id = 0,
                StartTime = 0,
                EndTime = 2000,
                Settings = new SectionGimmickSettings
                {
                    EnableAccuracyRequirement = true,
                    RequiredAccuracy = 0.5f,
                }
            });

            var hp = new SectionGimmickHealthProcessor(0);
            hp.ApplyBeatmap(beatmap);

            hp.ApplyResult(new OsuJudgementResult(hitInSection, hitInSection.CreateJudgement()) { Type = HitResult.Miss });
            Assert.That(hp.HasFailed, Is.False, "Should not fail before section end is reached.");

            hp.ApplyResult(new OsuJudgementResult(hitAfterSection, hitAfterSection.CreateJudgement()) { Type = HitResult.Great });
            Assert.That(hp.HasFailed, Is.True, "Should fail on the first hit processed after the section end with insufficient section accuracy.");
        }

        [Test]
        public void TestAccuracyRequirementEqualBoundaryPassesAtTwoDecimalPrecision()
        {
            var hit1 = new HitCircle { StartTime = 1000 };
            var hit2 = new HitCircle { StartTime = 1400 };
            var hitAfterSection = new HitCircle { StartTime = 2500 };

            var beatmap = new OsuBeatmap();
            beatmap.HitObjects.Add(hit1);
            beatmap.HitObjects.Add(hit2);
            beatmap.HitObjects.Add(hitAfterSection);
            beatmap.SectionGimmicks.Sections.Add(new SectionGimmickSection
            {
                Id = 0,
                StartTime = 0,
                EndTime = 2000,
                Settings = new SectionGimmickSettings
                {
                    EnableAccuracyRequirement = true,
                    RequiredAccuracy = 0.6667f, // 66.67%
                }
            });

            var hp = new SectionGimmickHealthProcessor(0);
            hp.ApplyBeatmap(beatmap);

            hp.ApplyResult(new OsuJudgementResult(hit1, hit1.CreateJudgement()) { Type = HitResult.Great });
            hp.ApplyResult(new OsuJudgementResult(hit2, hit2.CreateJudgement()) { Type = HitResult.Ok });
            hp.ApplyResult(new OsuJudgementResult(hitAfterSection, hitAfterSection.CreateJudgement()) { Type = HitResult.Great });

            Assert.That(hp.HasFailed, Is.False, "Accuracy at the two-decimal boundary should count as pass (>= required). ");
        }

        private static OsuJudgementResult createResultWithOffset(HitCircle hit, HitResult type, double offsetMs)
        {
            var result = new OsuJudgementResult(hit, hit.CreateJudgement())
            {
                Type = type
            };

            // TimeOffset setter is internal, so use RawTime via reflection in test assembly.
            typeof(JudgementResult).GetProperty("RawTime", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                                  ?.SetValue(result, hit.StartTime + offsetMs);

            return result;
        }

        [Test]
        public void TestReverseHPGreatUsesConfiguredHP300()
        {
            var hit = new HitCircle { StartTime = 1000 };
            var beatmap = new OsuBeatmap();
            beatmap.HitObjects.Add(hit);
            beatmap.SectionGimmicks.Sections.Add(new SectionGimmickSection
            {
                Id = 0,
                StartTime = 0,
                EndTime = -1,
                Settings = new SectionGimmickSettings
                {
                    EnableHPGimmick = true,
                    NoDrain = true,
                    ReverseHP = true,
                    HP100 = 0.1f,
                    HP50 = 0.1f,
                    HPMiss = 0.1f,
                    HP300 = -0.2f,
                }
            });

            var hp = new SectionGimmickHealthProcessor(0);
            hp.ApplyBeatmap(beatmap);
            hp.Health.Value = 0.5;

            hp.ApplyResult(new OsuJudgementResult(hit, hit.CreateJudgement()) { Type = HitResult.Great });

            Assert.That(hp.Health.Value, Is.EqualTo(0.3).Within(0.0001));
        }

    }
}
