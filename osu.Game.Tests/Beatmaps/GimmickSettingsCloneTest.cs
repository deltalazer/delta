// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using osu.Game.Beatmaps.HitObjectGimmicks;
using osu.Game.Beatmaps.SectionGimmicks;
using osu.Game.Screens.Edit.Compose;
using osuTK.Graphics;

namespace osu.Game.Tests.Beatmaps
{
    [TestFixture]
    public class GimmickSettingsCloneTest
    {
        [Test]
        public void TestHitObjectSettingsCloneKeepsEveryProperty()
        {
            var source = new HitObjectGimmickSettings();
            populate(source);

            var clone = HitObjectGimmickBindingUtils.CloneSettings(source);

            assertAllPropertiesMatch(source, clone);
        }

        [Test]
        public void TestSectionSettingsCloneKeepsEveryProperty()
        {
            var settings = new SectionGimmickSettings();
            populate(settings);

            var source = new BeatmapSectionGimmicks();
            source.Sections.Add(new SectionGimmickSection { Id = 1, StartTime = 0, EndTime = 1000, Settings = settings });

            var clone = SectionGimmickEditorModel.CloneGimmicks(source);

            assertAllPropertiesMatch(settings, clone.Sections.Single().Settings);
        }

        private static void populate(object settings)
        {
            foreach (var property in writableProperties(settings.GetType()))
                property.SetValue(settings, nonDefaultValue(property, property.GetValue(settings)));
        }

        private static void assertAllPropertiesMatch(object source, object clone)
        {
            var properties = writableProperties(source.GetType()).ToList();

            Assert.That(properties, Is.Not.Empty);

            foreach (var property in properties)
            {
                Assert.That(property.GetValue(clone), Is.EqualTo(property.GetValue(source)),
                    $"{source.GetType().Name}.{property.Name} was not carried across by the clone.");
            }
        }

        private static PropertyInfo[] writableProperties(Type type)
            => type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                   .Where(p => p.CanRead && p.CanWrite)
                   .ToArray();

        private static object nonDefaultValue(PropertyInfo property, object? current)
        {
            var type = Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType;

            if (type.IsEnum)
            {
                object[] values = Enum.GetValues(type).Cast<object>().ToArray();
                long currentValue = current == null ? 0 : Convert.ToInt64(current);

                return values.FirstOrDefault(v => Convert.ToInt64(v) != currentValue) ?? values[0];
            }

            if (type == typeof(bool))
                return !(bool)(current ?? false);

            if (type == typeof(int))
                return (int)(current ?? 0) + 7;

            if (type == typeof(float))
                return float.IsNaN((float)(current ?? 0f)) ? 12.5f : (float)(current ?? 0f) + 12.5f;

            if (type == typeof(double))
                return double.IsNaN((double)(current ?? 0d)) ? 12.5d : (double)(current ?? 0d) + 12.5d;

            if (type == typeof(Color4))
                return new Color4(0.25f, 0.5f, 0.75f, 1f);

            if (type == typeof(string))
                return "gimmick-clone-probe";

            throw new NotSupportedException($"{property.DeclaringType?.Name}.{property.Name} has unhandled type {type.Name}; extend this test so the clone stays covered.");
        }
    }
}
