// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Globalization;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Threading;
using osu.Game.Beatmaps.SectionGimmicks;
using osu.Game.Overlays;
using osu.Game.Graphics;
using osu.Game.Graphics.Sprites;
using osu.Game.Graphics.UserInterfaceV2;
using osu.Game.Overlays.Notifications;
using osu.Game.Rulesets.Edit;
using osu.Game.Beatmaps.HitObjectGimmicks;
using osuTK;
using osuTK.Graphics;

namespace osu.Game.Rulesets.Osu.Edit
{
    public partial class HitObjectGimmickToolboxGroup : EditorToolboxGroup
    {
        [Resolved]
        private osu.Game.Screens.Edit.EditorBeatmap editorBeatmap { get; set; } = null!;

        [Resolved(canBeNull: true)]
        private INotificationOverlay? notifications { get; set; }

        private HitObjectGimmickEditorModel model = null!;

        private FormCheckBox enableHpGimmick = null!;
        private FormCheckBox fakeNote = null!;
        private FormEnumDropdown<FakePunishMode> fakePunishMode = null!;
        private FormCheckBox fakePlayHitsound = null!;
        private FormCheckBox fakeAutoHitOnApproachClose = null!;
        private FormCheckBox fakeAutoHitPlayHitsound = null!;
        private FormCheckBox fakeRevealEnabled = null!;
        private FormNumberBox fakeRevealRed = null!;
        private FormNumberBox fakeRevealGreen = null!;
        private FormNumberBox fakeRevealBlue = null!;
        private FormSliderBar<float> fakeRevealStrength = null!;
        private FormNumberBox fakeRevealLeadInStartMs = null!;
        private FormNumberBox fakeRevealLeadInLengthMs = null!;
        private FormNumberBox fakeRevealFadeOutStartMs = null!;
        private FormNumberBox fakeRevealFadeOutLengthMs = null!;

        private FillFlowContainer fakeNoteFields = null!;
        private FillFlowContainer fakeRevealFields = null!;
        private static HitObjectGimmickSettings? fakeTintClipboard;
        private FormNumberBox hp300 = null!;
        private FormNumberBox hp100 = null!;
        private FormNumberBox hp50 = null!;
        private FormNumberBox hpMiss = null!;

        private FormCheckBox enableNoMiss = null!;

        private FormCheckBox forceAllMiss = null!;
        private FillFlowContainer allMissFields = null!;
        private FormCheckBox freezeHP = null!;
        private FormCheckBox freezeAccuracy = null!;
        private FormCheckBox freezeCombo = null!;

        private FormCheckBox enableCountLimits = null!;
        private FormNumberBox max300 = null!;
        private FormNumberBox max100 = null!;
        private FormNumberBox max50 = null!;
        private FormNumberBox maxMiss = null!;

        private FormCheckBox enableGreatOffsetPenalty = null!;
        private FormNumberBox greatOffsetThreshold = null!;
        private FormNumberBox greatOffsetPenaltyHp = null!;

        private FormCheckBox enableDifficultyOverrides = null!;
        private FormCheckBox allowUnsafeDifficultyOverrideValues = null!;
        private FormSliderBar<float> sectionCircleSize = null!;
        private FormSliderBar<float> sectionApproachRate = null!;
        private FormSliderBar<float> sectionOverallDifficulty = null!;
        private FormCheckBox allowUnsafeStackLeniencyOverrideValues = null!;
        private FormSliderBar<float> sectionStackLeniency = null!;
        private FormCheckBox allowUnsafeTickRateOverrideValues = null!;
        private FormSliderBar<double> sectionTickRate = null!;

        private FormCheckBox forceHidden = null!;
        private FormCheckBox forceHardRock = null!;
        private FormCheckBox forceFlashlight = null!;
        private FormNumberBox flashlightRadius = null!;
        private FormCheckBox forceNoApproachCircle = null!;

        private FillFlowContainer hpFields = null!;
        private FillFlowContainer countLimitFields = null!;
        private FillFlowContainer offsetPenaltyFields = null!;
        private FillFlowContainer difficultyOverrideFields = null!;
        private bool previousDifficultyOverridesEnabled;

        private OsuSpriteText selectionStatus = null!;

        private bool updatingControls;
        private readonly ScheduledDelegate[] fadeSchedules = new ScheduledDelegate[7];
        private bool selectionUpdateScheduled;

        public HitObjectGimmickToolboxGroup()
            : base("hit object gimmicks")
        {
        }

        [BackgroundDependencyLoader]
        private void load()
        {
            model = new HitObjectGimmickEditorModel(editorBeatmap);

            Child = new FillFlowContainer
            {
                RelativeSizeAxes = Axes.X,
                AutoSizeAxes = Axes.Y,
                Direction = FillDirection.Vertical,
                Spacing = new Vector2(5),
                Children = new Drawable[]
                {
                    selectionStatus = new OsuSpriteText
                    {
                        Text = "No object selected",
                        Font = OsuFont.GetFont(size: 12, weight: FontWeight.SemiBold),
                        Colour = Color4.Gray,
                    },

                    fakeNote = new FormCheckBox { Caption = "Fake Note" },
                    fakeNoteFields = createContainer(
                        fakePunishMode = new FormEnumDropdown<FakePunishMode>
                        {
                            Caption = "Fake Punish Mode",
                            Current = { Value = FakePunishMode.None },
                        },
                        fakePlayHitsound = new FormCheckBox { Caption = "Fake Play Hitsound" },
                        fakeAutoHitOnApproachClose = new FormCheckBox { Caption = "Fake Auto Hit on Approach Close" },
                        fakeAutoHitPlayHitsound = new FormCheckBox { Caption = "Fake Auto Hit Play Hitsound" },
                        fakeRevealEnabled = new FormCheckBox { Caption = "Fake Reveal Tint" },
                        fakeRevealFields = createContainer(
                            fakeRevealStrength = new FormSliderBar<float>
                            {
                                Caption = "Reveal Strength (0-1)",
                                Current = new BindableFloat
                                {
                                    MinValue = 0,
                                    MaxValue = 1,
                                    Precision = 0.01f,
                                    Value = HitObjectGimmickSettings.DEFAULT_FAKE_REVEAL_STRENGTH,
                                },
                                TransferValueOnCommit = true,
                                CommitEmptyAsNaN = false,
                                TabbableContentContainer = this,
                            },
                            fakeRevealRed = new FormNumberBox(allowDecimals: true) { Caption = "Reveal Color R (0-1)", TabbableContentContainer = this },
                            fakeRevealGreen = new FormNumberBox(allowDecimals: true) { Caption = "Reveal Color G (0-1)", TabbableContentContainer = this },
                            fakeRevealBlue = new FormNumberBox(allowDecimals: true) { Caption = "Reveal Color B (0-1)", TabbableContentContainer = this },
                            fakeRevealLeadInStartMs = new FormNumberBox(allowDecimals: true) { Caption = "Reveal Lead-In Start (ms)", TabbableContentContainer = this },
                            fakeRevealLeadInLengthMs = new FormNumberBox(allowDecimals: true) { Caption = "Reveal Lead-In Length (ms)", TabbableContentContainer = this },
                            fakeRevealFadeOutStartMs = new FormNumberBox(allowDecimals: true) { Caption = "Reveal Fade-Out Start (ms)", TabbableContentContainer = this },
                            fakeRevealFadeOutLengthMs = new FormNumberBox(allowDecimals: true) { Caption = "Reveal Fade-Out Length (ms)", TabbableContentContainer = this },
                            new FormButton
                            {
                                Caption = "Fake Tint",
                                ButtonText = "Copy",
                                Action = copyFakeTintSettings,
                            },
                            new FormButton
                            {
                                Caption = "Fake Tint",
                                ButtonText = "Paste",
                                Action = pasteFakeTintSettings,
                            })),

                    enableHpGimmick = new FormCheckBox { Caption = "HP Gimmick" },
                    hpFields = createContainer(
                        hp300 = new FormNumberBox(allowDecimals: true) { Caption = "HP300" },
                        hp100 = new FormNumberBox(allowDecimals: true) { Caption = "HP100" },
                        hp50 = new FormNumberBox(allowDecimals: true) { Caption = "HP50" },
                        hpMiss = new FormNumberBox(allowDecimals: true) { Caption = "HPMiss" }),

                    enableNoMiss = new FormCheckBox { Caption = "No Miss" },

                    forceAllMiss = new FormCheckBox { Caption = "All Miss" },
                    allMissFields = createContainer(
                        freezeHP = new FormCheckBox { Caption = "Freeze HP" },
                        freezeAccuracy = new FormCheckBox { Caption = "Freeze accuracy" },
                        freezeCombo = new FormCheckBox { Caption = "Freeze Combo" }),

                    enableCountLimits = new FormCheckBox { Caption = "Count Limits" },
                    countLimitFields = createContainer(
                        max300 = new FormNumberBox { Caption = "Max300s (-1 disabled)" },
                        max100 = new FormNumberBox { Caption = "Max100s (-1 disabled)" },
                        max50 = new FormNumberBox { Caption = "Max50s (-1 disabled)" },
                        maxMiss = new FormNumberBox { Caption = "MaxMisses (-1 disabled)" }),

                    enableGreatOffsetPenalty = new FormCheckBox { Caption = "Great Offset Penalty" },
                    offsetPenaltyFields = createContainer(
                        greatOffsetThreshold = new FormNumberBox(allowDecimals: true) { Caption = "GreatOffsetThresholdMs" },
                        greatOffsetPenaltyHp = new FormNumberBox(allowDecimals: true) { Caption = "GreatOffsetPenaltyHP" }),

                    enableDifficultyOverrides = new FormCheckBox { Caption = "Difficulty Overrides (CS/AR/OD)" },
                    difficultyOverrideFields = createContainer(
                        allowUnsafeDifficultyOverrideValues = new FormCheckBox { Caption = "Allow values past limits (unsafe)" },
                        sectionCircleSize = new FormSliderBar<float>
                        {
                            Caption = "CS (0-11)",
                            Current = new BindableFloat
                            {
                                // Keep an extended edit range so unsafe mode can input values past normal limits.
                                // Safe mode still clamps via SectionGimmickValueClamper.
                                MinValue = -1000,
                                MaxValue = 1000,
                                Precision = 0.1f,
                            },
                            TransferValueOnCommit = true,
                            CommitEmptyAsNaN = false,
                            TabbableContentContainer = this,
                        },
                        sectionApproachRate = new FormSliderBar<float>
                        {
                            Caption = "AR (<= 11)",
                            Current = new BindableFloat
                            {
                                MinValue = -1000,
                                MaxValue = 1000,
                                Precision = 0.1f,
                            },
                            TransferValueOnCommit = true,
                            CommitEmptyAsNaN = false,
                            TabbableContentContainer = this,
                        },
                        sectionOverallDifficulty = new FormSliderBar<float>
                        {
                            Caption = "OD (0-11)",
                            Current = new BindableFloat
                            {
                                MinValue = -1000,
                                MaxValue = 1000,
                                Precision = 0.1f,
                            },
                            TransferValueOnCommit = true,
                            CommitEmptyAsNaN = false,
                            TabbableContentContainer = this,
                        },
                        allowUnsafeStackLeniencyOverrideValues = new FormCheckBox { Caption = "Allow stack leniency values past limits (unsafe)" },
                        sectionStackLeniency = new FormSliderBar<float>
                        {
                            Caption = "Stack Leniency (0-1)",
                            Current = new BindableFloat
                            {
                                MinValue = -5,
                                MaxValue = 5,
                                Precision = 0.01f,
                            },
                            TransferValueOnCommit = true,
                            CommitEmptyAsNaN = false,
                            TabbableContentContainer = this,
                        },
                        allowUnsafeTickRateOverrideValues = new FormCheckBox { Caption = "Allow tick rate values past limits (unsafe)" },
                        sectionTickRate = new FormSliderBar<double>
                        {
                            Caption = "Tick Rate (>= 0)",
                            Current = new BindableDouble
                            {
                                MinValue = -20,
                                MaxValue = 20,
                                Precision = 0.01,
                            },
                            TransferValueOnCommit = true,
                            CommitEmptyAsNaN = false,
                            TabbableContentContainer = this,
                        }),

                    forceHidden = new FormCheckBox { Caption = "Force Hidden (HD)" },
                    forceHardRock = new FormCheckBox { Caption = "Force Hard Rock (HR)" },
                    forceFlashlight = new FormCheckBox { Caption = "Force Flashlight (FL)" },
                    flashlightRadius = new FormNumberBox(allowDecimals: true) { Caption = "FL radius (20-400)" },
                    forceNoApproachCircle = new FormCheckBox { Caption = "Force No Approach Circle" },
                }
            };

            bindControlEvents();
            updateDifficultyOverrideDefaults();

            editorBeatmap.SelectedHitObjects.BindCollectionChanged((_, _) => scheduleSelectionUpdate(), true);
            editorBeatmap.HitObjectUpdated += _ => scheduleSelectionUpdate();
            editorBeatmap.HitObjectAdded += _ => scheduleSelectionUpdate();
            editorBeatmap.HitObjectRemoved += _ => scheduleSelectionUpdate();
            editorBeatmap.BeatmapReprocessed += scheduleSelectionUpdate;
        }

        private void bindControlEvents()
        {
            enableHpGimmick.Current.BindValueChanged(v => setBool(v.NewValue, (s, value) => s.EnableHPGimmick = value));
            fakeNote.Current.BindValueChanged(v => setBool(v.NewValue, (s, value) => s.IsFakeNote = value));
            fakePunishMode.Current.BindValueChanged(v =>
            {
                if (updatingControls)
                    return;

                if (!model.HasSelection)
                    return;

                model.SetSelectionFakePunishMode(v.NewValue);
                scheduleSelectionUpdate();
            });
            fakePlayHitsound.Current.BindValueChanged(v => setBool(v.NewValue, (s, value) => s.FakePlayHitsound = value));
            fakeAutoHitOnApproachClose.Current.BindValueChanged(v => setBool(v.NewValue, (s, value) => s.FakeAutoHitOnApproachClose = value));
            fakeAutoHitPlayHitsound.Current.BindValueChanged(v => setBool(v.NewValue, (s, value) => s.FakeAutoHitPlayHitsound = value));
            fakeRevealEnabled.Current.BindValueChanged(v => setBool(v.NewValue, (s, value) => s.FakeRevealEnabled = value));
            bindSlider(fakeRevealStrength, (s, value) => s.FakeRevealStrength = value, v => Math.Clamp(v, 0f, 1f));
            bindFloat(fakeRevealRed, (s, value) => s.FakeRevealRed = value, v => Math.Clamp(v, 0f, 1f));
            bindFloat(fakeRevealGreen, (s, value) => s.FakeRevealGreen = value, v => Math.Clamp(v, 0f, 1f));
            bindFloat(fakeRevealBlue, (s, value) => s.FakeRevealBlue = value, v => Math.Clamp(v, 0f, 1f));
            bindFloat(fakeRevealLeadInStartMs, (s, value) => s.FakeRevealLeadInStartMs = value, v => Math.Max(0f, v));
            bindFloat(fakeRevealLeadInLengthMs, (s, value) => s.FakeRevealLeadInLengthMs = value, v => Math.Max(0f, v));
            bindFloat(fakeRevealFadeOutStartMs, (s, value) => s.FakeRevealFadeOutStartMs = value, v => Math.Max(0f, v));
            bindFloat(fakeRevealFadeOutLengthMs, (s, value) => s.FakeRevealFadeOutLengthMs = value, v => Math.Max(0f, v));

            enableNoMiss.Current.BindValueChanged(v =>
            {
                setBool(v.NewValue, (s, value) => s.EnableNoMiss = value);

                if (v.NewValue && !updatingControls)
                    forceAllMiss.Current.Value = false;
            });
            forceAllMiss.Current.BindValueChanged(v =>
            {
                setBool(v.NewValue, (s, value) => s.ForceAllMiss = value);

                if (v.NewValue && !updatingControls)
                    enableNoMiss.Current.Value = false;
            });
            freezeHP.Current.BindValueChanged(v => setBool(v.NewValue, (s, value) => s.FreezeHP = value));
            freezeAccuracy.Current.BindValueChanged(v => setBool(v.NewValue, (s, value) => s.FreezeAccuracy = value));
            freezeCombo.Current.BindValueChanged(v => setBool(v.NewValue, (s, value) => s.FreezeCombo = value));
            enableCountLimits.Current.BindValueChanged(v => setBool(v.NewValue, (s, value) => s.EnableCountLimits = value));
            enableGreatOffsetPenalty.Current.BindValueChanged(v => setBool(v.NewValue, (s, value) => s.EnableGreatOffsetPenalty = value));
            enableDifficultyOverrides.Current.BindValueChanged(v =>
            {
                bool wasEnabled = previousDifficultyOverridesEnabled;

                setBool(v.NewValue, (s, value) => s.EnableDifficultyOverrides = value);

                if (!updatingControls)
                    previousDifficultyOverridesEnabled = v.NewValue;

                if (updatingControls || !v.NewValue || wasEnabled)
                    return;

                setDefaultSelectionDifficultyOverrideValues();
            });
            allowUnsafeDifficultyOverrideValues.Current.BindValueChanged(v =>
            {
                if (!updatingControls && v.NewValue)
                    postUnsafeDifficultyWarning();

                setBool(v.NewValue, (s, value) => s.AllowUnsafeDifficultyOverrideValues = value);
            });
            allowUnsafeStackLeniencyOverrideValues.Current.BindValueChanged(v =>
            {
                if (!updatingControls && v.NewValue)
                    postUnsafeDifficultyWarning();

                setBool(v.NewValue, (s, value) => s.AllowUnsafeStackLeniencyOverrideValues = value);
            });
            allowUnsafeTickRateOverrideValues.Current.BindValueChanged(v =>
            {
                if (!updatingControls && v.NewValue)
                    postUnsafeDifficultyWarning();

                setBool(v.NewValue, (s, value) => s.AllowUnsafeTickRateOverrideValues = value);
            });

            forceHidden.Current.BindValueChanged(v => setBool(v.NewValue, (s, value) => s.ForceHidden = value));
            forceHardRock.Current.BindValueChanged(v => setBool(v.NewValue, (s, value) => s.ForceHardRock = value));
            forceFlashlight.Current.BindValueChanged(v => setBool(v.NewValue, (s, value) => s.ForceFlashlight = value));
            bindFloatOnCommitOnly(flashlightRadius, (s, value) => s.FlashlightRadius = value, v => Math.Clamp(v, 20f, 400f));
            forceNoApproachCircle.Current.BindValueChanged(v => setBool(v.NewValue, (s, value) => s.ForceNoApproachCircle = value));

            bindFloat(hp300, (s, value) => s.HP300 = value, v => Math.Clamp(v, -2f, 2f));
            bindFloat(hp100, (s, value) => s.HP100 = value, v => Math.Clamp(v, -2f, 2f));
            bindFloat(hp50, (s, value) => s.HP50 = value, v => Math.Clamp(v, -2f, 2f));
            bindFloat(hpMiss, (s, value) => s.HPMiss = value, v => Math.Clamp(v, -2f, 2f));

            bindInt(max300, (s, value) => s.Max300s = value, v => Math.Max(-1, v));
            bindInt(max100, (s, value) => s.Max100s = value, v => Math.Max(-1, v));
            bindInt(max50, (s, value) => s.Max50s = value, v => Math.Max(-1, v));
            bindInt(maxMiss, (s, value) => s.MaxMisses = value, v => Math.Max(-1, v));

            bindFloat(greatOffsetThreshold, (s, value) => s.GreatOffsetThresholdMs = value, v => Math.Max(0f, v));
            bindFloat(greatOffsetPenaltyHp, (s, value) => s.GreatOffsetPenaltyHP = value, v => Math.Min(0f, v));

            bindSlider(sectionCircleSize, (s, value) => s.SectionCircleSize = value, v => isUnsafeDifficultyOverrideEnabled() ? v : SectionGimmickValueClamper.ClampCircleSize(v));
            bindSlider(sectionApproachRate, (s, value) => s.SectionApproachRate = value, v => isUnsafeDifficultyOverrideEnabled() ? v : SectionGimmickValueClamper.ClampApproachRate(v));
            bindSlider(sectionOverallDifficulty, (s, value) => s.SectionOverallDifficulty = value, v => isUnsafeDifficultyOverrideEnabled() ? v : SectionGimmickValueClamper.ClampOverallDifficulty(v));
            bindSlider(sectionStackLeniency, (s, value) => s.SectionStackLeniency = value, v => isUnsafeStackLeniencyOverrideEnabled() ? v : SectionGimmickValueClamper.ClampStackLeniency(v));
            bindSlider(sectionTickRate, (s, value) => s.SectionTickRate = value, v => isUnsafeTickRateOverrideEnabled() ? v : SectionGimmickValueClamper.ClampTickRate(v));

            sectionCircleSize.Current.BindValueChanged(_ => updateDifficultyOverrideDefaults());
            sectionApproachRate.Current.BindValueChanged(_ => updateDifficultyOverrideDefaults());
            sectionOverallDifficulty.Current.BindValueChanged(_ => updateDifficultyOverrideDefaults());
            sectionStackLeniency.Current.BindValueChanged(_ => updateDifficultyOverrideDefaults());
            sectionTickRate.Current.BindValueChanged(_ => updateDifficultyOverrideDefaults());
        }

        private void bindSlider(FormSliderBar<float> source, Action<osu.Game.Beatmaps.HitObjectGimmicks.HitObjectGimmickSettings, float> setter, Func<float, float> clamp)
            => source.Current.BindValueChanged(v => setSlider(source, setter, v.NewValue, clamp));

        private void bindSlider(FormSliderBar<double> source, Action<osu.Game.Beatmaps.HitObjectGimmicks.HitObjectGimmickSettings, double> setter, Func<double, double> clamp)
            => source.Current.BindValueChanged(v => setSlider(source, setter, v.NewValue, clamp));

        private void bindFloat(FormNumberBox source, Action<osu.Game.Beatmaps.HitObjectGimmicks.HitObjectGimmickSettings, float> setter, Func<float, float> clamp)
            => source.OnCommit += (_, _) => setFloat(source, setter, clamp);

        private void bindFloatOnCommitOnly(FormNumberBox source, Action<osu.Game.Beatmaps.HitObjectGimmicks.HitObjectGimmickSettings, float> setter, Func<float, float> clamp)
            => source.OnCommit += (_, _) => setFloat(source, setter, clamp);

        private void bindInt(FormNumberBox source, Action<osu.Game.Beatmaps.HitObjectGimmicks.HitObjectGimmickSettings, int> setter, Func<int, int> clamp)
            => source.OnCommit += (_, _) => setInt(source, setter, clamp);

        private void scheduleSelectionUpdate()
        {
            if (selectionUpdateScheduled)
                return;

            selectionUpdateScheduled = true;

            Scheduler.AddOnce(() =>
            {
                selectionUpdateScheduled = false;

                // Commit any in-progress text edits before selection state is refreshed.
                // This ensures clicking the playfield (which deselects) persists typed values,
                // matching the behavior of pressing enter/clicking off the textbox.
                GetContainingFocusManager()?.ChangeFocus(null);

                updateFromSelection();
            });
        }

        private void updateFromSelection()
        {
            updatingControls = true;

            var state = model.GetSelectionState();
            bool hasSelection = state.HasSelection;

            selectionStatus.Text = hasSelection
                ? $"Selected objects: {state.SelectionCount}"
                : "No object selected";
            selectionStatus.Colour = hasSelection ? Color4.White : Color4.Gray;

            // Values may be updated while there is no selection (resetting to defaults).
            // Ensure controls are writable during this update pass, then apply final enabled state below.
            setEnabledState(true,
                enableHpGimmick,
                hp300, hp100, hp50, hpMiss,
                fakeNote, fakePunishMode, fakePlayHitsound, fakeAutoHitOnApproachClose, fakeAutoHitPlayHitsound, fakeRevealEnabled, fakeRevealStrength,
                fakeRevealRed, fakeRevealGreen, fakeRevealBlue,
                fakeRevealLeadInStartMs, fakeRevealLeadInLengthMs, fakeRevealFadeOutStartMs, fakeRevealFadeOutLengthMs,
                enableNoMiss,
                forceAllMiss, freezeHP, freezeAccuracy, freezeCombo,
                enableCountLimits, max300, max100, max50, maxMiss,
                enableGreatOffsetPenalty, greatOffsetThreshold, greatOffsetPenaltyHp,
                enableDifficultyOverrides, allowUnsafeDifficultyOverrideValues, sectionCircleSize, sectionApproachRate, sectionOverallDifficulty,
                allowUnsafeStackLeniencyOverrideValues, sectionStackLeniency, allowUnsafeTickRateOverrideValues, sectionTickRate,
                forceHidden, forceHardRock, forceFlashlight, flashlightRadius, forceNoApproachCircle);

            enableHpGimmick.Current.Value = hasSelection && state.EnableHPGimmick;
            fakeNote.Current.Value = hasSelection && state.IsFakeNote;
            fakePunishMode.Current.Value = hasSelection ? state.FakePunishMode : FakePunishMode.None;
            enableNoMiss.Current.Value = hasSelection && state.EnableNoMiss;
            forceAllMiss.Current.Value = hasSelection && (state.RepresentativeSettings?.ForceAllMiss ?? false);
            freezeHP.Current.Value = state.RepresentativeSettings?.FreezeHP ?? true;
            freezeAccuracy.Current.Value = state.RepresentativeSettings?.FreezeAccuracy ?? true;
            freezeCombo.Current.Value = state.RepresentativeSettings?.FreezeCombo ?? true;
            enableCountLimits.Current.Value = hasSelection && state.EnableCountLimits;
            enableGreatOffsetPenalty.Current.Value = hasSelection && state.EnableGreatOffsetPenalty;
            enableDifficultyOverrides.Current.Value = hasSelection && state.EnableDifficultyOverrides;
            previousDifficultyOverridesEnabled = enableDifficultyOverrides.Current.Value;
            allowUnsafeDifficultyOverrideValues.Current.Value = hasSelection && state.AllowUnsafeDifficultyOverrideValues;

            var representative = state.RepresentativeSettings;
            allowUnsafeStackLeniencyOverrideValues.Current.Value = hasSelection && (representative?.AllowUnsafeStackLeniencyOverrideValues ?? false);
            allowUnsafeTickRateOverrideValues.Current.Value = hasSelection && (representative?.AllowUnsafeTickRateOverrideValues ?? false);

            forceHidden.Current.Value = hasSelection && state.ForceHidden;
            forceHardRock.Current.Value = hasSelection && state.ForceHardRock;
            forceFlashlight.Current.Value = hasSelection && state.ForceFlashlight;
            forceNoApproachCircle.Current.Value = hasSelection && state.ForceNoApproachCircle;

            hp300.Current.Value = formatFloat(representative?.HP300 ?? float.NaN);
            hp100.Current.Value = formatFloat(representative?.HP100 ?? float.NaN);
            hp50.Current.Value = formatFloat(representative?.HP50 ?? float.NaN);
            hpMiss.Current.Value = formatFloat(representative?.HPMiss ?? float.NaN);

            max300.Current.Value = formatInt(representative?.Max300s ?? -1);
            max100.Current.Value = formatInt(representative?.Max100s ?? -1);
            max50.Current.Value = formatInt(representative?.Max50s ?? -1);
            maxMiss.Current.Value = formatInt(representative?.MaxMisses ?? -1);

            greatOffsetThreshold.Current.Value = formatFloat(representative?.GreatOffsetThresholdMs ?? -1);
            greatOffsetPenaltyHp.Current.Value = formatFloat(representative?.GreatOffsetPenaltyHP ?? float.NaN);

            fakePlayHitsound.Current.Value = hasSelection && (representative?.FakePlayHitsound ?? false);
            fakeAutoHitOnApproachClose.Current.Value = hasSelection && state.FakeAutoHitOnApproachClose;
            fakeAutoHitPlayHitsound.Current.Value = hasSelection && state.FakeAutoHitPlayHitsound;
            fakeRevealEnabled.Current.Value = !hasSelection || (representative?.FakeRevealEnabled ?? true);
            fakeRevealStrength.Current.Value = hasSelection ? (representative?.FakeRevealStrength ?? HitObjectGimmickSettings.DEFAULT_FAKE_REVEAL_STRENGTH) : HitObjectGimmickSettings.DEFAULT_FAKE_REVEAL_STRENGTH;
            fakeRevealRed.Current.Value = formatFloat(hasSelection ? (representative?.FakeRevealRed ?? 1f) : 1f);
            fakeRevealGreen.Current.Value = formatFloat(hasSelection ? (representative?.FakeRevealGreen ?? 0.3019608f) : 0.3019608f);
            fakeRevealBlue.Current.Value = formatFloat(hasSelection ? (representative?.FakeRevealBlue ?? 0.3019608f) : 0.3019608f);
            fakeRevealLeadInStartMs.Current.Value = formatFloat(hasSelection ? (representative?.FakeRevealLeadInStartMs ?? HitObjectGimmickSettings.DEFAULT_FAKE_REVEAL_LEAD_IN_START_MS) : HitObjectGimmickSettings.DEFAULT_FAKE_REVEAL_LEAD_IN_START_MS);
            fakeRevealLeadInLengthMs.Current.Value = formatFloat(hasSelection ? (representative?.FakeRevealLeadInLengthMs ?? HitObjectGimmickSettings.DEFAULT_FAKE_REVEAL_LEAD_IN_LENGTH_MS) : HitObjectGimmickSettings.DEFAULT_FAKE_REVEAL_LEAD_IN_LENGTH_MS);
            fakeRevealFadeOutStartMs.Current.Value = formatFloat(hasSelection ? (representative?.FakeRevealFadeOutStartMs ?? HitObjectGimmickSettings.DEFAULT_FAKE_REVEAL_FADE_OUT_START_MS) : HitObjectGimmickSettings.DEFAULT_FAKE_REVEAL_FADE_OUT_START_MS);
            fakeRevealFadeOutLengthMs.Current.Value = formatFloat(hasSelection ? (representative?.FakeRevealFadeOutLengthMs ?? HitObjectGimmickSettings.DEFAULT_FAKE_REVEAL_FADE_OUT_LENGTH_MS) : HitObjectGimmickSettings.DEFAULT_FAKE_REVEAL_FADE_OUT_LENGTH_MS);

            sectionCircleSize.Current.Value = float.IsNaN(representative?.SectionCircleSize ?? float.NaN) ? 0f : representative!.SectionCircleSize;
            sectionApproachRate.Current.Value = float.IsNaN(representative?.SectionApproachRate ?? float.NaN) ? 0f : representative!.SectionApproachRate;
            sectionOverallDifficulty.Current.Value = float.IsNaN(representative?.SectionOverallDifficulty ?? float.NaN) ? 0f : representative!.SectionOverallDifficulty;
            sectionStackLeniency.Current.Value = float.IsNaN(representative?.SectionStackLeniency ?? float.NaN) ? 0f : representative!.SectionStackLeniency;
            sectionTickRate.Current.Value = double.IsNaN(representative?.SectionTickRate ?? double.NaN) ? 0 : representative!.SectionTickRate;
            flashlightRadius.Current.Value = formatFloat(representative?.FlashlightRadius ?? float.NaN);

            if (float.IsNaN(representative?.SectionCircleSize ?? float.NaN))
                sectionCircleSize.Current.Value = float.NaN;

            if (float.IsNaN(representative?.SectionApproachRate ?? float.NaN))
                sectionApproachRate.Current.Value = float.NaN;

            if (float.IsNaN(representative?.SectionOverallDifficulty ?? float.NaN))
                sectionOverallDifficulty.Current.Value = float.NaN;

            if (float.IsNaN(representative?.SectionStackLeniency ?? float.NaN))
                sectionStackLeniency.Current.Value = float.NaN;

            if (double.IsNaN(representative?.SectionTickRate ?? double.NaN))
                sectionTickRate.Current.Value = double.NaN;

            if (!hasSelection)
            {
                sectionCircleSize.Current.Value = float.NaN;
                sectionApproachRate.Current.Value = float.NaN;
                sectionOverallDifficulty.Current.Value = float.NaN;
                sectionStackLeniency.Current.Value = float.NaN;
                sectionTickRate.Current.Value = double.NaN;
            }

            scheduleFade(hpFields, enableHpGimmick.Current.Value, 0);
            hpFields.AlwaysPresent = enableHpGimmick.Current.Value;

            scheduleFade(allMissFields, forceAllMiss.Current.Value, 6);
            allMissFields.AlwaysPresent = forceAllMiss.Current.Value;

            scheduleFade(fakeNoteFields, fakeNote.Current.Value, 4);
            fakeNoteFields.AlwaysPresent = fakeNote.Current.Value;

            scheduleFade(fakeRevealFields, fakeNote.Current.Value && fakeRevealEnabled.Current.Value, 5);
            fakeRevealFields.AlwaysPresent = fakeNote.Current.Value && fakeRevealEnabled.Current.Value;

            scheduleFade(countLimitFields, enableCountLimits.Current.Value, 1);
            countLimitFields.AlwaysPresent = enableCountLimits.Current.Value;

            scheduleFade(offsetPenaltyFields, enableGreatOffsetPenalty.Current.Value, 2);
            offsetPenaltyFields.AlwaysPresent = enableGreatOffsetPenalty.Current.Value;

            scheduleFade(difficultyOverrideFields, enableDifficultyOverrides.Current.Value, 3);
            difficultyOverrideFields.AlwaysPresent = enableDifficultyOverrides.Current.Value;

            if (IsLoaded)
            {
                bool enabled = hasSelection;
                setEnabledState(enabled,
                    enableHpGimmick,
                    hp300, hp100, hp50, hpMiss,
                    fakeNote, fakePunishMode, fakePlayHitsound, fakeAutoHitOnApproachClose, fakeAutoHitPlayHitsound, fakeRevealEnabled, fakeRevealStrength,
                    fakeRevealRed, fakeRevealGreen, fakeRevealBlue,
                    fakeRevealLeadInStartMs, fakeRevealLeadInLengthMs, fakeRevealFadeOutStartMs, fakeRevealFadeOutLengthMs,
                    enableNoMiss,
                    forceAllMiss, freezeHP, freezeAccuracy, freezeCombo, freezeCombo,
                    enableCountLimits, max300, max100, max50, maxMiss,
                    enableGreatOffsetPenalty, greatOffsetThreshold, greatOffsetPenaltyHp,
                    enableDifficultyOverrides, allowUnsafeDifficultyOverrideValues, sectionCircleSize, sectionApproachRate, sectionOverallDifficulty,
                    allowUnsafeStackLeniencyOverrideValues, sectionStackLeniency, allowUnsafeTickRateOverrideValues, sectionTickRate,
                    forceHidden, forceHardRock, forceFlashlight, flashlightRadius, forceNoApproachCircle);

                setEnabledState(enabled && fakeNote.Current.Value,
                    fakePunishMode, fakePlayHitsound, fakeAutoHitOnApproachClose, fakeAutoHitPlayHitsound, fakeRevealEnabled,
                    fakeRevealStrength, fakeRevealRed, fakeRevealGreen, fakeRevealBlue,
                    fakeRevealLeadInStartMs, fakeRevealLeadInLengthMs, fakeRevealFadeOutStartMs, fakeRevealFadeOutLengthMs);

                setEnabledState(enabled && fakeNote.Current.Value && fakeAutoHitOnApproachClose.Current.Value,
                    fakeAutoHitPlayHitsound);

                setEnabledState(enabled && fakeNote.Current.Value && fakeRevealEnabled.Current.Value,
                    fakeRevealStrength, fakeRevealRed, fakeRevealGreen, fakeRevealBlue,
                    fakeRevealLeadInStartMs, fakeRevealLeadInLengthMs, fakeRevealFadeOutStartMs, fakeRevealFadeOutLengthMs);
            }

            updatingControls = false;
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();
            updateFromSelection();
        }

        private void scheduleFade(FillFlowContainer container, bool visible, int slot)
        {
            float target = visible ? 1 : 0;

            if (Math.Abs(container.Alpha - target) < 0.0001f)
                return;

            fadeSchedules[slot]?.Cancel();
            fadeSchedules[slot] = Scheduler.AddDelayed(() => container.FadeTo(target, 150), 0);
        }

        private void setBool(bool value, Action<osu.Game.Beatmaps.HitObjectGimmicks.HitObjectGimmickSettings, bool> setter)
        {
            if (updatingControls)
                return;

            if (!model.HasSelection)
                return;

            model.SetSelectionBoolSetting(setter, value);
            scheduleSelectionUpdate();
        }

        private void setFloat(FormNumberBox source, Action<osu.Game.Beatmaps.HitObjectGimmicks.HitObjectGimmickSettings, float> setter, Func<float, float> clamp)
        {
            if (updatingControls)
                return;

            if (!model.HasSelection)
                return;

            if (!float.TryParse(source.Current.Value, NumberStyles.Float, CultureInfo.InvariantCulture, out float value))
                return;

            float clamped = clamp(value);
            string formatted = formatFloat(clamped);

            if (source.Current.Value != formatted)
            {
                source.Current.Value = formatted;
                return;
            }

            model.SetSelectionFloatSetting(setter, clamped);
            scheduleSelectionUpdate();
        }

        private void setInt(FormNumberBox source, Action<osu.Game.Beatmaps.HitObjectGimmicks.HitObjectGimmickSettings, int> setter, Func<int, int> clamp)
        {
            if (updatingControls)
                return;

            if (!model.HasSelection)
                return;

            if (!int.TryParse(source.Current.Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int value))
                return;

            int clamped = clamp(value);
            string formatted = formatInt(clamped);

            if (source.Current.Value != formatted)
            {
                source.Current.Value = formatted;
                return;
            }

            model.SetSelectionIntSetting(setter, clamped);
            scheduleSelectionUpdate();
        }

        private void setSlider(FormSliderBar<float> source,
                               Action<osu.Game.Beatmaps.HitObjectGimmicks.HitObjectGimmickSettings, float> setter,
                               float value,
                               Func<float, float> clamp)
        {
            if (updatingControls)
                return;

            if (!model.HasSelection)
                return;

            float clamped = clamp(value);

            if (Math.Abs(value - clamped) > 0.0001f)
            {
                source.Current.Value = clamped;
                return;
            }

            model.SetSelectionFloatSetting(setter, clamped);
            scheduleSelectionUpdate();
        }

        private void setSlider(FormSliderBar<double> source,
                               Action<osu.Game.Beatmaps.HitObjectGimmicks.HitObjectGimmickSettings, double> setter,
                               double value,
                               Func<double, double> clamp)
        {
            if (updatingControls)
                return;

            if (!model.HasSelection)
                return;

            double clamped = clamp(value);

            if (Math.Abs(value - clamped) > 0.0001)
            {
                source.Current.Value = clamped;
                return;
            }

            model.SetSelectionDoubleSetting(setter, clamped);
            scheduleSelectionUpdate();
        }

        private static FillFlowContainer createContainer(params Drawable[] children)
            => new FillFlowContainer
            {
                RelativeSizeAxes = Axes.X,
                AutoSizeAxes = Axes.Y,
                Direction = FillDirection.Vertical,
                Padding = new MarginPadding { Left = 15 },
                Spacing = new Vector2(5),
                Children = children,
            };

        private static void setEnabledState(bool enabled, params Drawable[] items)
        {
            foreach (var item in items)
            {
                switch (item)
                {
                    case FormCheckBox c:
                        c.Current.Disabled = !enabled;
                        break;

                    case FormSliderBar<float> s:
                        s.Current.Disabled = !enabled;
                        break;

                    case FormSliderBar<double> s:
                        s.Current.Disabled = !enabled;
                        break;

                    case FormEnumDropdown<FakePunishMode> d:
                        d.Current.Disabled = !enabled;
                        break;


                    case FormTextBox t:
                        t.ReadOnly = !enabled;
                        break;

                    case FormButton b:
                        b.Enabled.Value = enabled;
                        break;
                }
            }
        }

        private static string formatFloat(float value)
            => float.IsNaN(value) ? string.Empty : value.ToString(CultureInfo.InvariantCulture);

        private static string formatInt(int value)
            => value.ToString(CultureInfo.InvariantCulture);

        private static string formatDouble(double value)
            => double.IsNaN(value) ? string.Empty : value.ToString(CultureInfo.InvariantCulture);

        private void copyFakeTintSettings()
        {
            if (!model.HasSelection)
                return;

            var settings = model.GetSelectionRepresentativeSettings();
            if (settings == null)
                return;

            fakeTintClipboard = new HitObjectGimmickSettings
            {
                FakeRevealEnabled = settings.FakeRevealEnabled,
                FakeRevealRed = settings.FakeRevealRed,
                FakeRevealGreen = settings.FakeRevealGreen,
                FakeRevealBlue = settings.FakeRevealBlue,
                FakeRevealStrength = settings.FakeRevealStrength,
                FakeRevealLeadInStartMs = settings.FakeRevealLeadInStartMs,
                FakeRevealLeadInLengthMs = settings.FakeRevealLeadInLengthMs,
                FakeRevealFadeOutStartMs = settings.FakeRevealFadeOutStartMs,
                FakeRevealFadeOutLengthMs = settings.FakeRevealFadeOutLengthMs,
            };

            notifications?.Post(new SimpleNotification
            {
                Text = "copied fake tint settings",
            });
        }

        private void pasteFakeTintSettings()
        {
            if (!model.HasSelection || fakeTintClipboard == null)
                return;

            if (!fakeNote.Current.Value)
            {
                notifications?.Post(new SimpleNotification
                {
                    Text = "enable fake note before pasting fake tint settings",
                });
                return;
            }

            model.SetSelectionBoolSetting((s, value) => s.FakeRevealEnabled = value, fakeTintClipboard.FakeRevealEnabled);
            model.SetSelectionFloatSetting((s, value) => s.FakeRevealRed = value, fakeTintClipboard.FakeRevealRed);
            model.SetSelectionFloatSetting((s, value) => s.FakeRevealGreen = value, fakeTintClipboard.FakeRevealGreen);
            model.SetSelectionFloatSetting((s, value) => s.FakeRevealBlue = value, fakeTintClipboard.FakeRevealBlue);
            model.SetSelectionFloatSetting((s, value) => s.FakeRevealStrength = value, fakeTintClipboard.FakeRevealStrength);
            model.SetSelectionFloatSetting((s, value) => s.FakeRevealLeadInStartMs = value, fakeTintClipboard.FakeRevealLeadInStartMs);
            model.SetSelectionFloatSetting((s, value) => s.FakeRevealLeadInLengthMs = value, fakeTintClipboard.FakeRevealLeadInLengthMs);
            model.SetSelectionFloatSetting((s, value) => s.FakeRevealFadeOutStartMs = value, fakeTintClipboard.FakeRevealFadeOutStartMs);
            model.SetSelectionFloatSetting((s, value) => s.FakeRevealFadeOutLengthMs = value, fakeTintClipboard.FakeRevealFadeOutLengthMs);

            scheduleSelectionUpdate();

            notifications?.Post(new SimpleNotification
            {
                Text = "pasted fake tint settings",
            });
        }

        private bool isUnsafeDifficultyOverrideEnabled()
            => allowUnsafeDifficultyOverrideValues.Current.Value;

        private bool isUnsafeStackLeniencyOverrideEnabled()
            => allowUnsafeStackLeniencyOverrideValues.Current.Value;

        private bool isUnsafeTickRateOverrideEnabled()
            => allowUnsafeTickRateOverrideValues.Current.Value;

        private void updateDifficultyOverrideDefaults()
        {
            if (updatingControls)
                return;

            // Avoid mutating defaults while controls are disabled (can happen during initial load / selection sync).
            if (sectionCircleSize.Current.Disabled
                || sectionApproachRate.Current.Disabled
                || sectionOverallDifficulty.Current.Disabled
                || sectionStackLeniency.Current.Disabled
                || sectionTickRate.Current.Disabled)
                return;

            trySetDefault(sectionCircleSize.Current, editorBeatmap.Difficulty.CircleSize);
            trySetDefault(sectionApproachRate.Current, editorBeatmap.Difficulty.ApproachRate);
            trySetDefault(sectionOverallDifficulty.Current, editorBeatmap.Difficulty.OverallDifficulty);
            trySetDefault(sectionStackLeniency.Current, editorBeatmap.StackLeniency);
            trySetDefault(sectionTickRate.Current, editorBeatmap.Difficulty.SliderTickRate);

            static void trySetDefault<T>(Bindable<T> bindable, T value)
            {
                if (bindable.Disabled)
                    return;

                try
                {
                    bindable.Default = value;
                }
                catch (InvalidOperationException)
                {
                    // Can happen transiently during control state updates.
                }
            }
        }

        private void setDefaultSelectionDifficultyOverrideValues()
        {
            if (!model.HasSelection)
                return;

            var current = model.GetSelectionRepresentativeSettings();
            bool changed = false;

            if (float.IsNaN(current?.SectionStackLeniency ?? float.NaN))
            {
                model.SetSelectionFloatSetting((s, value) => s.SectionStackLeniency = value, editorBeatmap.StackLeniency);
                changed = true;
            }

            if (double.IsNaN(current?.SectionTickRate ?? double.NaN))
            {
                model.SetSelectionDoubleSetting((s, value) => s.SectionTickRate = value, editorBeatmap.Difficulty.SliderTickRate);
                changed = true;
            }

            if (changed)
                scheduleSelectionUpdate();
        }

        private void postUnsafeDifficultyWarning()
            => notifications?.Post(new SimpleNotification
            {
                Text = "unsafe difficulty overrides enabled - values past normal limits can break gameplay or crash",
            });
    }
}
