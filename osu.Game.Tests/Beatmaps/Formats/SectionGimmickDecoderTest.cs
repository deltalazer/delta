// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Globalization;
using System.IO;
using System.Threading;
using NUnit.Framework;
using osu.Game.Beatmaps;
using osu.Game.Beatmaps.Formats;
using osu.Game.IO;
using osuTK.Graphics;

namespace osu.Game.Tests.Beatmaps.Formats
{
    [TestFixture]
    public class SectionGimmickDecoderTest
    {
        [Test]
        public void TestDecodeSectionGimmicksSection()
        {
            const string content = """
osu file format v128

[General]
AudioFilename: test.mp3
Mode: 0

[Metadata]
Title: Test
Artist: Test
Creator: Test
Version: Test

[Difficulty]
HPDrainRate: 5
CircleSize: 4
OverallDifficulty: 8
ApproachRate: 8
SliderMultiplier: 1.4
SliderTickRate: 1

[TimingPoints]
0,500,4,2,0,100,1,0

[HitObjects]
256,192,1000,1,0,0:0:0:0:

[BeatmapSectionGimmicks]
0,0,1500,EnableHPGimmick=True|NoDrain=True|HP300=-0.02|HP100=-0.05|HP50=0.08|HPMiss=-0.12|EnableNoMiss=True
""";

            using var ms = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(content));
            using var reader = new LineBufferedReader(ms);
            var decoder = Decoder.GetDecoder<Beatmap>(reader);
            var beatmap = decoder.Decode(reader);

            Assert.That(beatmap.SectionGimmicks.Sections.Count, Is.EqualTo(1));
            var section = beatmap.SectionGimmicks.Sections[0];
            Assert.That(section.Id, Is.EqualTo(0));
            Assert.That(section.Settings.EnableHPGimmick, Is.True);
            Assert.That(section.Settings.EnableNoMiss, Is.True);
            Assert.That(section.Settings.NoDrain, Is.True);
            Assert.That(section.Settings.HP100, Is.EqualTo(-0.05f));
        }

        [Test]
        public void TestDecodeSectionGimmicksWithInvariantDecimalWhenCurrentCultureUsesComma()
        {
            const string content = """
osu file format v128

[General]
AudioFilename: test.mp3
Mode: 0

[Metadata]
Title: Test
Artist: Test
Creator: Test
Version: Test

[Difficulty]
HPDrainRate: 5
CircleSize: 4
OverallDifficulty: 8
ApproachRate: 8
SliderMultiplier: 1.4
SliderTickRate: 1

[TimingPoints]
0,500,4,2,0,100,1,0

[HitObjects]
256,192,1000,1,0,0:0:0:0:

[BeatmapSectionGimmicks]
1,1234.5,2345.5,EnableGreatOffsetPenalty=True|GreatOffsetThresholdMs=24.5|GreatOffsetPenaltyHP=-0.12
""";

            CultureInfo previousCulture = Thread.CurrentThread.CurrentCulture;
            try
            {
                Thread.CurrentThread.CurrentCulture = new CultureInfo("fr-FR");

                using var ms = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(content));
                using var reader = new LineBufferedReader(ms);
                var decoder = Decoder.GetDecoder<Beatmap>(reader);
                var beatmap = decoder.Decode(reader);

                Assert.That(beatmap.SectionGimmicks.Sections.Count, Is.EqualTo(1));
                var section = beatmap.SectionGimmicks.Sections[0];

                Assert.That(section.StartTime, Is.EqualTo(1234.5));
                Assert.That(section.EndTime, Is.EqualTo(2345.5));
            }
            finally
            {
                Thread.CurrentThread.CurrentCulture = previousCulture;
            }
        }

        [Test]
        public void TestDecodeSectionGimmicksMaxMisses()
        {
            const string content = """
osu file format v128

[General]
AudioFilename: test.mp3
Mode: 0

[Metadata]
Title: Test
Artist: Test
Creator: Test
Version: Test

[Difficulty]
HPDrainRate: 5
CircleSize: 4
OverallDifficulty: 8
ApproachRate: 8
SliderMultiplier: 1.4
SliderTickRate: 1

[TimingPoints]
0,500,4,2,0,100,1,0

[HitObjects]
256,192,1000,1,0,0:0:0:0:

[BeatmapSectionGimmicks]
1,0,2000,EnableCountLimits=True|MaxMisses=2
""";

            using var ms = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(content));
            using var reader = new LineBufferedReader(ms);
            var decoder = Decoder.GetDecoder<Beatmap>(reader);
            var beatmap = decoder.Decode(reader);

            Assert.That(beatmap.SectionGimmicks.Sections.Count, Is.EqualTo(1));
            var section = beatmap.SectionGimmicks.Sections[0];

            Assert.That(section.Settings.EnableCountLimits, Is.True);
            Assert.That(section.Settings.MaxMisses, Is.EqualTo(2));
        }

        [Test]
        public void TestDecodeSectionGimmicksCustomSectionNameAndDisplayColor()
        {
            const string content = """
osu file format v128

[General]
AudioFilename: test.mp3
Mode: 0

[Metadata]
Title: Test
Artist: Test
Creator: Test
Version: Test

[Difficulty]
HPDrainRate: 5
CircleSize: 4
OverallDifficulty: 8
ApproachRate: 8
SliderMultiplier: 1.4
SliderTickRate: 1

[TimingPoints]
0,500,4,2,0,100,1,0

[HitObjects]
256,192,1000,1,0,0:0:0:0:

[BeatmapSectionGimmicks]
0,0,1500,EnableHPGimmick=True|SectionName=Intro Rush|DisplayColor=4294901760
""";

            using var ms = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(content));
            using var reader = new LineBufferedReader(ms);
            var decoder = Decoder.GetDecoder<Beatmap>(reader);
            var beatmap = decoder.Decode(reader);

            Assert.That(beatmap.SectionGimmicks.Sections.Count, Is.EqualTo(1));
            var section = beatmap.SectionGimmicks.Sections[0];

            Assert.That(section.Settings.SectionName, Is.EqualTo("Intro Rush"));

            // 4294901760 -> A=0, R=0, G=255, B=255 in parser's ARGB mapping.
            Assert.That(section.Settings.DisplayColor, Is.EqualTo(new Color4(0f, 1f, 1f, 0f)));
        }

        [Test]
        public void TestDecodeSectionGimmicksDifficultyOverrides()
        {
            const string content = """
osu file format v128

[General]
AudioFilename: test.mp3
Mode: 0

[Metadata]
Title: Test
Artist: Test
Creator: Test
Version: Test

[Difficulty]
HPDrainRate: 5
CircleSize: 4
OverallDifficulty: 8
ApproachRate: 8
SliderMultiplier: 1.4
SliderTickRate: 1

[TimingPoints]
0,500,4,2,0,100,1,0

[HitObjects]
256,192,1000,1,0,0:0:0:0:

[BeatmapSectionGimmicks]
0,0,1500,EnableDifficultyOverrides=True|SectionApproachRate=9.5|SectionOverallDifficulty=7
""";

            using var ms = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(content));
            using var reader = new LineBufferedReader(ms);
            var decoder = Decoder.GetDecoder<Beatmap>(reader);
            var beatmap = decoder.Decode(reader);

            Assert.That(beatmap.SectionGimmicks.Sections.Count, Is.EqualTo(1));
            var section = beatmap.SectionGimmicks.Sections[0];

            Assert.That(section.Settings.EnableDifficultyOverrides, Is.True);
            Assert.That(section.Settings.SectionApproachRate, Is.EqualTo(9.5f));
            Assert.That(section.Settings.SectionOverallDifficulty, Is.EqualTo(7f));
        }

        [Test]
        public void TestDecodeSectionGimmicksDifficultyOverridesWithCircleSize()
        {
            const string content = """
osu file format v128

[General]
AudioFilename: test.mp3
Mode: 0

[Metadata]
Title: Test
Artist: Test
Creator: Test
Version: Test

[Difficulty]
HPDrainRate: 5
CircleSize: 4
OverallDifficulty: 8
ApproachRate: 8
SliderMultiplier: 1.4
SliderTickRate: 1

[TimingPoints]
0,500,4,2,0,100,1,0

[HitObjects]
256,192,1000,1,0,0:0:0:0:

[BeatmapSectionGimmicks]
0,0,1500,EnableDifficultyOverrides=True|SectionCircleSize=7
""";

            using var ms = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(content));
            using var reader = new LineBufferedReader(ms);
            var decoder = Decoder.GetDecoder<Beatmap>(reader);
            var beatmap = decoder.Decode(reader);

            Assert.That(beatmap.SectionGimmicks.Sections.Count, Is.EqualTo(1));
            var section = beatmap.SectionGimmicks.Sections[0];

            Assert.That(section.Settings.EnableDifficultyOverrides, Is.True);
            Assert.That(section.Settings.SectionCircleSize, Is.EqualTo(7f));
        }

        [Test]
        public void TestDecodeSectionGimmicksGradualDifficultyOverrides()
        {
            const string content = """
osu file format v128

[General]
AudioFilename: test.mp3
Mode: 0

[Metadata]
Title: Test
Artist: Test
Creator: Test
Version: Test

[Difficulty]
HPDrainRate: 5
CircleSize: 4
OverallDifficulty: 8
ApproachRate: 8
SliderMultiplier: 1.4
SliderTickRate: 1

[TimingPoints]
0,500,4,2,0,100,1,0

[HitObjects]
256,192,1000,1,0,0:0:0:0:

[BeatmapSectionGimmicks]
0,0,1500,EnableDifficultyOverrides=True|EnableGradualDifficultyChange=True|GradualDifficultyChangeEndTimeMs=1200|KeepDifficultyOverridesAfterSection=True|SectionApproachRate=10
""";

            using var ms = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(content));
            using var reader = new LineBufferedReader(ms);
            var decoder = Decoder.GetDecoder<Beatmap>(reader);
            var beatmap = decoder.Decode(reader);

            Assert.That(beatmap.SectionGimmicks.Sections.Count, Is.EqualTo(1));
            var section = beatmap.SectionGimmicks.Sections[0];

            Assert.That(section.Settings.EnableDifficultyOverrides, Is.True);
            Assert.That(section.Settings.EnableGradualDifficultyChange, Is.True);
            Assert.That(section.Settings.GradualDifficultyChangeEndTimeMs, Is.EqualTo(1200f));
            Assert.That(section.Settings.KeepDifficultyOverridesAfterSection, Is.True);
            Assert.That(section.Settings.SectionApproachRate, Is.EqualTo(10f));
        }

        [Test]
        public void TestDecodeSectionGimmicksForceNoApproachCircle()
        {
            const string content = """
osu file format v128

[General]
AudioFilename: test.mp3
Mode: 0

[Metadata]
Title: Test
Artist: Test
Creator: Test
Version: Test

[Difficulty]
HPDrainRate: 5
CircleSize: 4
OverallDifficulty: 8
ApproachRate: 8
SliderMultiplier: 1.4
SliderTickRate: 1

[TimingPoints]
0,500,4,2,0,100,1,0

[HitObjects]
256,192,1000,1,0,0:0:0:0:

[BeatmapSectionGimmicks]
0,0,1500,EnableDifficultyOverrides=True|SectionApproachRate=9|ForceNoApproachCircle=True|ForceSingleTap=True
""";

            using var ms = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(content));
            using var reader = new LineBufferedReader(ms);
            var decoder = Decoder.GetDecoder<Beatmap>(reader);
            var beatmap = decoder.Decode(reader);

            Assert.That(beatmap.SectionGimmicks.Sections.Count, Is.EqualTo(1));
            var section = beatmap.SectionGimmicks.Sections[0];

            Assert.That(section.Settings.EnableDifficultyOverrides, Is.True);
            Assert.That(section.Settings.ForceNoApproachCircle, Is.True);
            Assert.That(section.Settings.ForceSingleTap, Is.True);
        }

        [Test]
        public void TestDecodeHitObjectGimmicksForceNoApproachCircle()
        {
            const string content = """
osu file format v128

[General]
AudioFilename: test.mp3
Mode: 0

[Metadata]
Title: Test
Artist: Test
Creator: Test
Version: Test

[Difficulty]
HPDrainRate: 5
CircleSize: 4
OverallDifficulty: 8
ApproachRate: 8
SliderMultiplier: 1.4
SliderTickRate: 1

[TimingPoints]
0,500,4,2,0,100,1,0

[HitObjects]
256,192,1000,1,0,0:0:0:0:

[BeatmapHitObjectGimmicks]
1000,0,ForceNoApproachCircle=True
""";

            using var ms = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(content));
            using var reader = new LineBufferedReader(ms);
            var decoder = Decoder.GetDecoder<Beatmap>(reader);
            var beatmap = decoder.Decode(reader);

            Assert.That(beatmap.HitObjectGimmicks.Entries.Count, Is.EqualTo(1));
            var entry = beatmap.HitObjectGimmicks.Entries[0];

            Assert.That(entry.StartTime, Is.EqualTo(1000));
            Assert.That(entry.ComboIndexWithOffsets, Is.EqualTo(0));
            Assert.That(entry.Settings.ForceNoApproachCircle, Is.True);
        }
    }
}
