// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Globalization;
using System.Linq;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Localisation;
using osu.Framework.Threading;
using osu.Game.Beatmaps.SectionGimmicks;
using osu.Game.Graphics;
using osu.Game.Graphics.Sprites;
using osu.Game.Graphics.UserInterfaceV2;
using osu.Game.Overlays;
using osu.Game.Overlays.Notifications;
using osu.Game.Rulesets.Edit;
using osu.Game.Rulesets.Objects;
using osu.Game.Rulesets.Osu.Scoring;
using osu.Game.Screens.Edit;
using osu.Game.Screens.Edit.Compose;
using osuTK;
using osuTK.Graphics;

namespace osu.Game.Rulesets.Osu.Edit
{
    public partial class SectionGimmickToolboxGroup : EditorToolboxGroup
    {
        private readonly BindableInt selectedSectionId = new BindableInt(-1);

        [Resolved]
        private SectionGimmickEditorModel model { get; set; } = null!;

        [Resolved]
        private EditorClock clock { get; set; } = null!;

        [Resolved]
        private EditorBeatmap editorBeatmap { get; set; } = null!;

        [Resolved(canBeNull: true)]
        private INotificationOverlay? notifications { get; set; }

        [Resolved(canBeNull: true)]
        private Editor? editor { get; set; }

        private RoundedButton addSectionButton = null!;
        private RoundedButton removeSectionButton = null!;
        private CompositeDrawable sectionActionButtons = null!;
        private RoundedButton copySettingsButton = null!;
        private RoundedButton pasteSettingsButton = null!;
        private RoundedButton applyScopeButton = null!;
        private CompositeDrawable settingsActionButtons = null!;

        private SectionSelectionDropdown sectionDropdown = null!;

        private FormTextBox sectionNameBox = null!;
        private FormNumberBox startTimeBox = null!;
        private FormNumberBox endTimeBox = null!;

        private RoundedButton setStartHereButton = null!;
        private RoundedButton setEndHereButton = null!;
        private RoundedButton setGradualFinishTimeButton = null!;

        private FillFlowContainer selectedSectionFlow = null!;

        private FormCheckBox enableHpGimmick = null!;
        private FillFlowContainer hpGroupFields = null!;
        private FormCheckBox noDrain = null!;
        private FormCheckBox reverseHp = null!;
        private FormNumberBox hpStart = null!;
        private FormNumberBox hpCap = null!;
        private FormNumberBox hp300 = null!;
        private FormCheckBox hp300AffectsSliderEndsAndTicks = null!;
        private FormNumberBox hp100 = null!;
        private FormCheckBox hp100AffectsSliderEndsAndTicks = null!;
        private FormNumberBox hp50 = null!;
        private FormCheckBox hp50AffectsSliderEndsAndTicks = null!;
        private FormNumberBox hpMiss = null!;
        private FormCheckBox hpMissAffectsSliderEndAndTickMisses = null!;
        private FormCheckBox showHpSliderRouting = null!;
        private FillFlowContainer hpSliderRoutingFields = null!;

        private FormCheckBox enableNoMiss = null!;
        private FormCheckBox enableAccuracyRequirement = null!;
        private FormNumberBox requiredAccuracy = null!;

        private FormCheckBox enableCountLimits = null!;
        private FillFlowContainer countLimitFields = null!;
        private FormNumberBox max300s = null!;
        private FormCheckBox max300sAffectsSliderEndsAndTicks = null!;
        private FormNumberBox max100s = null!;
        private FormCheckBox max100sAffectsSliderEndsAndTicks = null!;
        private FormNumberBox max50s = null!;
        private FormCheckBox max50sAffectsSliderEndsAndTicks = null!;
        private FormNumberBox maxMisses = null!;
        private FormCheckBox maxMissesAffectsSliderEndAndTickMisses = null!;
        private FormCheckBox showCountSliderRouting = null!;
        private FillFlowContainer countSliderRoutingFields = null!;

        private FormCheckBox enableNoMissedSliderEnd = null!;

        private FormCheckBox enableGreatOffsetPenalty = null!;
        private FillFlowContainer greatOffsetFields = null!;
        private FormNumberBox greatOffsetThreshold = null!;
        private FormNumberBox greatOffsetPenaltyHp = null!;

        private FormCheckBox enableDifficultyOverrides = null!;
        private FillFlowContainer difficultyOverrideFields = null!;
        private FormCheckBox allowUnsafeDifficultyOverrideValues = null!;
        private FormCheckBox difficultyOverrideStartWithBeatmapValues = null!;
        private FormCheckBox enableGradualDifficultyChange = null!;
        private FormNumberBox gradualDifficultyChangeEndTime = null!;
        private FormCheckBox keepDifficultyOverridesAfterSection = null!;
        private RoundedButton inheritFromPreviousButton = null!;
        private FormSliderBar<float> sectionCircleSize = null!;
        private FormCheckBox enableSectionCircleSizeWindow = null!;
        private FormNumberBox sectionCircleSizeStartTime = null!;
        private FormNumberBox sectionCircleSizeEndTime = null!;
        private RoundedButton setSectionCircleSizeStartNowButton = null!;
        private RoundedButton setSectionCircleSizeEndNowButton = null!;
        private FormCheckBox enableGradualSectionCircleSizeChange = null!;
        private FillFlowContainer sectionCircleSizeWindowFields = null!;
        private FormSliderBar<float> sectionApproachRate = null!;
        private FormCheckBox enableSectionApproachRateWindow = null!;
        private FormNumberBox sectionApproachRateStartTime = null!;
        private FormNumberBox sectionApproachRateEndTime = null!;
        private RoundedButton setSectionApproachRateStartNowButton = null!;
        private RoundedButton setSectionApproachRateEndNowButton = null!;
        private FormCheckBox enableGradualSectionApproachRateChange = null!;
        private FillFlowContainer sectionApproachRateWindowFields = null!;
        private FormSliderBar<float> sectionOverallDifficulty = null!;
        private FormCheckBox enableSectionOverallDifficultyWindow = null!;
        private FormNumberBox sectionOverallDifficultyStartTime = null!;
        private FormNumberBox sectionOverallDifficultyEndTime = null!;
        private RoundedButton setSectionOverallDifficultyStartNowButton = null!;
        private RoundedButton setSectionOverallDifficultyEndNowButton = null!;
        private FormCheckBox enableGradualSectionOverallDifficultyChange = null!;
        private FillFlowContainer sectionOverallDifficultyWindowFields = null!;
        private FormCheckBox allowUnsafeStackLeniencyOverrideValues = null!;
        private FormSliderBar<float> sectionStackLeniency = null!;
        private FormCheckBox allowUnsafeTickRateOverrideValues = null!;
        private FormSliderBar<double> sectionTickRate = null!;

        private FormCheckBox forceHidden = null!;
        private FormCheckBox forceNoApproachCircle = null!;
        private FormCheckBox forceHardRock = null!;
        private FormCheckBox forceFlashlight = null!;
        private FormNumberBox flashlightRadius = null!;
        private FormCheckBox enableGradualFlashlightRadiusChange = null!;
        private FormCheckBox enableGradualFlashlightFadeIn = null!;
        private FormNumberBox gradualFlashlightRadiusEndTime = null!;
        private RoundedButton setFlGradualFinishTimeButton = null!;
        private FormCheckBox forceDoubleTime = null!;
        private FormCheckBox showForceMods = null!;
        private FillFlowContainer forceModsFields = null!;

        private FormCheckBox showFunMods = null!;
        private FillFlowContainer funModsFields = null!;
        private FormCheckBox forceTransform = null!;
        private FormCheckBox forceWiggle = null!;
        private FormNumberBox wiggleStrength = null!;
        private FormCheckBox forceSpinIn = null!;
        private FormCheckBox forceGrow = null!;
        private FormNumberBox growStartScale = null!;
        private FormCheckBox forceDeflate = null!;
        private FormNumberBox deflateStartScale = null!;
        private FormCheckBox forceBarrelRoll = null!;
        private FormNumberBox barrelRollSpinSpeed = null!;
        private FormCheckBox forceApproachDifferent = null!;
        private FormNumberBox approachDifferentScale = null!;
        private FormCheckBox forceMuted = null!;
        private FormNumberBox mutedMuteComboCount = null!;
        private FormCheckBox forceNoScope = null!;
        private FormCheckBox forceTraceable = null!;
        private FormNumberBox noScopeHiddenComboCount = null!;
        private FormCheckBox forceMagnetised = null!;
        private FormNumberBox magnetisedAttractionStrength = null!;
        private FormCheckBox forceRepel = null!;
        private FormNumberBox repelRepulsionStrength = null!;
        private FormCheckBox forceFreezeFrame = null!;
        private FormCheckBox forceBubbles = null!;
        private FormCheckBox forceSynesthesia = null!;
        private FormCheckBox forceDepth = null!;
        private FormNumberBox depthMaxDepth = null!;
        private FormCheckBox forceBloom = null!;
        private FormNumberBox bloomMaxSizeComboCount = null!;
        private FormNumberBox bloomMaxCursorSize = null!;

        private FormEnumDropdown<SectionGimmickApplyScope> applyScopeDropdown = null!;

        private OsuSpriteText validationStatus = null!;

        private bool updatingControls;
        private readonly BindableList<HitObject> selectedHitObjects = new BindableList<HitObject>();
        private readonly ScheduledDelegate[] fadeSchedules = new ScheduledDelegate[15];

        public SectionGimmickToolboxGroup()
            : base("Section gimmicks")
        {
        }

        [BackgroundDependencyLoader]
        private void load()
        {
            selectedSectionId.BindTo(model.SelectedSectionId);

            Child = new FillFlowContainer
            {
                RelativeSizeAxes = Axes.X,
                AutoSizeAxes = Axes.Y,
                Spacing = new Vector2(5),
                Direction = FillDirection.Vertical,
                Children = new Drawable[]
                {
                    sectionDropdown = new SectionSelectionDropdown
                    {
                        RelativeSizeAxes = Axes.X,
                        Caption = "Sections",
                    },
                    sectionActionButtons = new GridContainer
                    {
                        RelativeSizeAxes = Axes.X,
                        AutoSizeAxes = Axes.Y,
                        RowDimensions = new[] { new Dimension(GridSizeMode.AutoSize) },
                        ColumnDimensions = new[] { new Dimension(), new Dimension() },
                        Content = new[]
                        {
                            new Drawable[]
                            {
                            addSectionButton = new RoundedButton
                            {
                                Text = "Add",
                                RelativeSizeAxes = Axes.X,
                                Action = () => model.AddSection(clock.CurrentTime),
                            },
                            removeSectionButton = new RoundedButton
                            {
                                Text = "Remove",
                                RelativeSizeAxes = Axes.X,
                                Action = () => model.RemoveSelectedSection(),
                            },
                            }
                        }
                    },
                    selectedSectionFlow = new FillFlowContainer
                    {
                        RelativeSizeAxes = Axes.X,
                        AutoSizeAxes = Axes.Y,
                        Direction = FillDirection.Vertical,
                        Spacing = new Vector2(5),
                        Children = new Drawable[]
                        {
                            sectionNameBox = new FormTextBox
                            {
                                Caption = "Name",
                                PlaceholderText = "e.g., Kiai time",
                                TabbableContentContainer = this,
                            },
                            new GridContainer
                            {
                                RelativeSizeAxes = Axes.X,
                                AutoSizeAxes = Axes.Y,
                                RowDimensions = new[] { new Dimension(GridSizeMode.AutoSize) },
                                ColumnDimensions = new[] { new Dimension(), new Dimension() },
                                Content = new[]
                                {
                                    new Drawable[]
                                    {
                                        startTimeBox = new FormNumberBox(allowDecimals: true)
                                        {
                                            Caption = "Start (ms)",
                                            Current = { Value = "0" },
                                            TabbableContentContainer = this,
                                        },
                                        setStartHereButton = new RoundedButton
                                        {
                                            Text = "Use current time",
                                            RelativeSizeAxes = Axes.X,
                                            Action = () => model.SetSelectedStartTime(clock.CurrentTime),
                                        },
                                    }
                                }
                            },
                            new GridContainer
                            {
                                RelativeSizeAxes = Axes.X,
                                AutoSizeAxes = Axes.Y,
                                RowDimensions = new[] { new Dimension(GridSizeMode.AutoSize) },
                                ColumnDimensions = new[] { new Dimension(), new Dimension() },
                                Content = new[]
                                {
                                    new Drawable[]
                                    {
                                        endTimeBox = new FormNumberBox(allowDecimals: true)
                                        {
                                            Caption = "End (ms, -1 map end)",
                                            Current = { Value = "-1" },
                                            TabbableContentContainer = this,
                                        },
                                        setEndHereButton = new RoundedButton
                                        {
                                            Text = "Use current time",
                                            RelativeSizeAxes = Axes.X,
                                            Action = () => model.SetSelectedEndTime(clock.CurrentTime),
                                        },
                                    }
                                }
                            },

                            settingsActionButtons = new GridContainer
                            {
                                RelativeSizeAxes = Axes.X,
                                AutoSizeAxes = Axes.Y,
                                RowDimensions = new[] { new Dimension(GridSizeMode.AutoSize) },
                                ColumnDimensions = new[] { new Dimension(), new Dimension() },
                                Content = new[]
                                {
                                    new Drawable[]
                                    {
                                        copySettingsButton = new RoundedButton
                                        {
                                            Text = "Copy",
                                            RelativeSizeAxes = Axes.X,
                                            Action = () => model.CopySelectedSettings(),
                                        },
                                        pasteSettingsButton = new RoundedButton
                                        {
                                            Text = "Paste",
                                            RelativeSizeAxes = Axes.X,
                                            Action = () => model.PasteSettingsTo(Array.Empty<int>()),
                                        },
                                    }
                                }
                            },

                            applyScopeDropdown = new FormEnumDropdown<SectionGimmickApplyScope>
                            {
                                Caption = "Apply scope",
                                Current = { Value = SectionGimmickApplyScope.ThisDifficulty },
                            },
                            applyScopeButton = new RoundedButton
                            {
                                Text = "Apply",
                                RelativeSizeAxes = Axes.X,
                                Action = applyCurrentSettingsByScope,
                            },

                            enableHpGimmick = new FormCheckBox
                            {
                                Caption = "HP",
                            },
                            hpGroupFields = new FillFlowContainer
                            {
                                RelativeSizeAxes = Axes.X,
                                AutoSizeAxes = Axes.Y,
                                Direction = FillDirection.Vertical,
                                Spacing = new Vector2(5),
                                Children = new Drawable[]
                                {
                                    noDrain = new FormCheckBox
                                    {
                                        Caption = "No drain",
                                    },
                                    reverseHp = new FormCheckBox
                                    {
                                        Caption = "Reverse HP",
                                    },
                                    hpStart = new FormNumberBox(allowDecimals: true)
                                    {
                                        Caption = "HP start (opt. 0-1)",
                                        TabbableContentContainer = this,
                                    },
                                    hpCap = new FormNumberBox(allowDecimals: true)
                                    {
                                        Caption = "HP cap (opt. 0-1)",
                                        TabbableContentContainer = this,
                                    },
                                    hp300 = new FormNumberBox(allowDecimals: true)
                                    {
                                        Caption = "HP300",
                                        TabbableContentContainer = this,
                                    },
                                    hp100 = new FormNumberBox(allowDecimals: true)
                                    {
                                        Caption = "HP100",
                                        TabbableContentContainer = this,
                                    },
                                    hp50 = new FormNumberBox(allowDecimals: true)
                                    {
                                        Caption = "HP50",
                                        TabbableContentContainer = this,
                                    },
                                    hpMiss = new FormNumberBox(allowDecimals: true)
                                    {
                                        Caption = "HPMiss",
                                        TabbableContentContainer = this,
                                    },
                                    showHpSliderRouting = new FormCheckBox
                                    {
                                        Caption = "Show slider HP routing",
                                    },
                                    hpSliderRoutingFields = new FillFlowContainer
                                    {
                                        RelativeSizeAxes = Axes.X,
                                        AutoSizeAxes = Axes.Y,
                                        Direction = FillDirection.Vertical,
                                        Spacing = new Vector2(5),
                                        Children = new Drawable[]
                                        {
                                            hp300AffectsSliderEndsAndTicks = new FormCheckBox
                                            {
                                                Caption = "Use HP300 for slider hit judgements",
                                            },
                                            hp100AffectsSliderEndsAndTicks = new FormCheckBox
                                            {
                                                Caption = "Use HP100 for slider hit judgements",
                                            },
                                            hp50AffectsSliderEndsAndTicks = new FormCheckBox
                                            {
                                                Caption = "Use HP50 for slider hit judgements",
                                            },
                                            hpMissAffectsSliderEndAndTickMisses = new FormCheckBox
                                            {
                                                Caption = "Use HPMiss for slider miss judgements",
                                            },
                                        }
                                    },
                                }
                            },

                            enableNoMiss = new FormCheckBox
                            {
                                Caption = "No Miss",
                            },
                            enableAccuracyRequirement = new FormCheckBox
                            {
                                Caption = "Accuracy requirement",
                            },
                            requiredAccuracy = new FormNumberBox(allowDecimals: true)
                            {
                                Caption = "Required accuracy (0.00-100.00%)",
                                TabbableContentContainer = this,
                            },

                            enableCountLimits = new FormCheckBox
                            {
                                Caption = "Count limits",
                            },
                            countLimitFields = new FillFlowContainer
                            {
                                RelativeSizeAxes = Axes.X,
                                AutoSizeAxes = Axes.Y,
                                Direction = FillDirection.Vertical,
                                Spacing = new Vector2(5),
                                Children = new Drawable[]
                                {
                                    max300s = new FormNumberBox
                                    {
                                        Caption = "Max300s (-1 unlimited)",
                                        TabbableContentContainer = this,
                                    },
                                    max100s = new FormNumberBox
                                    {
                                        Caption = "Max100s (-1 unlimited)",
                                        TabbableContentContainer = this,
                                    },
                                    max50s = new FormNumberBox
                                    {
                                        Caption = "Max50s (-1 unlimited)",
                                        TabbableContentContainer = this,
                                    },
                                    maxMisses = new FormNumberBox
                                    {
                                        Caption = "MaxMisses (-1 unlimited)",
                                        TabbableContentContainer = this,
                                    },
                                    showCountSliderRouting = new FormCheckBox
                                    {
                                        Caption = "Show slider count routing",
                                    },
                                    countSliderRoutingFields = new FillFlowContainer
                                    {
                                        RelativeSizeAxes = Axes.X,
                                        AutoSizeAxes = Axes.Y,
                                        Direction = FillDirection.Vertical,
                                        Spacing = new Vector2(5),
                                        Children = new Drawable[]
                                        {
                                            max300sAffectsSliderEndsAndTicks = new FormCheckBox
                                            {
                                                Caption = "Count slider hits as 300",
                                            },
                                            max100sAffectsSliderEndsAndTicks = new FormCheckBox
                                            {
                                                Caption = "Count slider hits as 100",
                                            },
                                            max50sAffectsSliderEndsAndTicks = new FormCheckBox
                                            {
                                                Caption = "Count slider hits as 50",
                                            },
                                            maxMissesAffectsSliderEndAndTickMisses = new FormCheckBox
                                            {
                                                Caption = "Count slider misses as Miss",
                                            },
                                        }
                                    },
                                }
                            },

                            enableNoMissedSliderEnd = new FormCheckBox
                            {
                                Caption = "No missed slider ends",
                            },

                            enableGreatOffsetPenalty = new FormCheckBox
                            {
                                Caption = "Great offset penalty",
                            },
                            greatOffsetFields = new FillFlowContainer
                            {
                                RelativeSizeAxes = Axes.X,
                                AutoSizeAxes = Axes.Y,
                                Direction = FillDirection.Vertical,
                                Spacing = new Vector2(5),
                                Children = new Drawable[]
                                {
                                    greatOffsetThreshold = new FormNumberBox(allowDecimals: true)
                                    {
                                        Caption = "GreatOffsetThresholdMs",
                                        TabbableContentContainer = this,
                                    },
                                    greatOffsetPenaltyHp = new FormNumberBox(allowDecimals: true)
                                    {
                                        Caption = "GreatOffsetPenaltyHP",
                                        TabbableContentContainer = this,
                                    },
                                }
                            },

                            enableDifficultyOverrides = new FormCheckBox
                            {
                                Caption = "Difficulty overrides (CS/AR/OD)",
                            },
                            difficultyOverrideFields = new FillFlowContainer
                            {
                                RelativeSizeAxes = Axes.X,
                                AutoSizeAxes = Axes.Y,
                                Direction = FillDirection.Vertical,
                                Spacing = new Vector2(5),
                                Children = new Drawable[]
                                {
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
                                    enableSectionCircleSizeWindow = new FormCheckBox
                                    {
                                        Caption = "CS custom window",
                                    },
                                    sectionCircleSizeWindowFields = new FillFlowContainer
                                    {
                                        RelativeSizeAxes = Axes.X,
                                        AutoSizeAxes = Axes.Y,
                                        Direction = FillDirection.Vertical,
                                        Spacing = new Vector2(5),
                                        Children = new Drawable[]
                                        {
                                            enableGradualSectionCircleSizeChange = new FormCheckBox
                                            {
                                                Caption = "Gradual CS",
                                            },
                                            new GridContainer
                                            {
                                                RelativeSizeAxes = Axes.X,
                                                AutoSizeAxes = Axes.Y,
                                                RowDimensions = new[] { new Dimension(GridSizeMode.AutoSize) },
                                                ColumnDimensions = new[] { new Dimension(), new Dimension() },
                                                Content = new[]
                                                {
                                                    new Drawable[]
                                                    {
                                                        sectionCircleSizeStartTime = new FormNumberBox(allowDecimals: true)
                                                        {
                                                            Caption = "CS start (-1 section start)",
                                                            TabbableContentContainer = this,
                                                        },
                                                        setSectionCircleSizeStartNowButton = new RoundedButton
                                                        {
                                                            Text = "Use current time",
                                                            RelativeSizeAxes = Axes.X,
                                                            Action = () => mutateSetting(s => s.SectionCircleSizeStartTimeMs = (float)clock.CurrentTime),
                                                        },
                                                    }
                                                }
                                            },
                                            new GridContainer
                                            {
                                                RelativeSizeAxes = Axes.X,
                                                AutoSizeAxes = Axes.Y,
                                                RowDimensions = new[] { new Dimension(GridSizeMode.AutoSize) },
                                                ColumnDimensions = new[] { new Dimension(), new Dimension() },
                                                Content = new[]
                                                {
                                                    new Drawable[]
                                                    {
                                                        sectionCircleSizeEndTime = new FormNumberBox(allowDecimals: true)
                                                        {
                                                            Caption = "CS end (-1 section end)",
                                                            TabbableContentContainer = this,
                                                        },
                                                        setSectionCircleSizeEndNowButton = new RoundedButton
                                                        {
                                                            Text = "Use current time",
                                                            RelativeSizeAxes = Axes.X,
                                                            Action = () => mutateSetting(s => s.SectionCircleSizeEndTimeMs = (float)clock.CurrentTime),
                                                        },
                                                    }
                                                }
                                            },
                                        }
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
                                    enableSectionApproachRateWindow = new FormCheckBox
                                    {
                                        Caption = "AR custom window",
                                    },
                                    sectionApproachRateWindowFields = new FillFlowContainer
                                    {
                                        RelativeSizeAxes = Axes.X,
                                        AutoSizeAxes = Axes.Y,
                                        Direction = FillDirection.Vertical,
                                        Spacing = new Vector2(5),
                                        Children = new Drawable[]
                                        {
                                            enableGradualSectionApproachRateChange = new FormCheckBox
                                            {
                                                Caption = "Gradual AR",
                                            },
                                            new GridContainer
                                            {
                                                RelativeSizeAxes = Axes.X,
                                                AutoSizeAxes = Axes.Y,
                                                RowDimensions = new[] { new Dimension(GridSizeMode.AutoSize) },
                                                ColumnDimensions = new[] { new Dimension(), new Dimension() },
                                                Content = new[]
                                                {
                                                    new Drawable[]
                                                    {
                                                        sectionApproachRateStartTime = new FormNumberBox(allowDecimals: true)
                                                        {
                                                            Caption = "AR start (-1 section start)",
                                                            TabbableContentContainer = this,
                                                        },
                                                        setSectionApproachRateStartNowButton = new RoundedButton
                                                        {
                                                            Text = "Use current time",
                                                            RelativeSizeAxes = Axes.X,
                                                            Action = () => mutateSetting(s => s.SectionApproachRateStartTimeMs = (float)clock.CurrentTime),
                                                        },
                                                    }
                                                }
                                            },
                                            new GridContainer
                                            {
                                                RelativeSizeAxes = Axes.X,
                                                AutoSizeAxes = Axes.Y,
                                                RowDimensions = new[] { new Dimension(GridSizeMode.AutoSize) },
                                                ColumnDimensions = new[] { new Dimension(), new Dimension() },
                                                Content = new[]
                                                {
                                                    new Drawable[]
                                                    {
                                                        sectionApproachRateEndTime = new FormNumberBox(allowDecimals: true)
                                                        {
                                                            Caption = "AR end (-1 section end)",
                                                            TabbableContentContainer = this,
                                                        },
                                                        setSectionApproachRateEndNowButton = new RoundedButton
                                                        {
                                                            Text = "Use current time",
                                                            RelativeSizeAxes = Axes.X,
                                                            Action = () => mutateSetting(s => s.SectionApproachRateEndTimeMs = (float)clock.CurrentTime),
                                                        },
                                                    }
                                                }
                                            },
                                        }
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
                                    enableSectionOverallDifficultyWindow = new FormCheckBox
                                    {
                                        Caption = "OD custom window",
                                    },
                                    sectionOverallDifficultyWindowFields = new FillFlowContainer
                                    {
                                        RelativeSizeAxes = Axes.X,
                                        AutoSizeAxes = Axes.Y,
                                        Direction = FillDirection.Vertical,
                                        Spacing = new Vector2(5),
                                        Children = new Drawable[]
                                        {
                                            enableGradualSectionOverallDifficultyChange = new FormCheckBox
                                            {
                                                Caption = "Gradual OD",
                                            },
                                            new GridContainer
                                            {
                                                RelativeSizeAxes = Axes.X,
                                                AutoSizeAxes = Axes.Y,
                                                RowDimensions = new[] { new Dimension(GridSizeMode.AutoSize) },
                                                ColumnDimensions = new[] { new Dimension(), new Dimension() },
                                                Content = new[]
                                                {
                                                    new Drawable[]
                                                    {
                                                        sectionOverallDifficultyStartTime = new FormNumberBox(allowDecimals: true)
                                                        {
                                                            Caption = "OD start (-1 section start)",
                                                            TabbableContentContainer = this,
                                                        },
                                                        setSectionOverallDifficultyStartNowButton = new RoundedButton
                                                        {
                                                            Text = "Use current time",
                                                            RelativeSizeAxes = Axes.X,
                                                            Action = () => mutateSetting(s => s.SectionOverallDifficultyStartTimeMs = (float)clock.CurrentTime),
                                                        },
                                                    }
                                                }
                                            },
                                            new GridContainer
                                            {
                                                RelativeSizeAxes = Axes.X,
                                                AutoSizeAxes = Axes.Y,
                                                RowDimensions = new[] { new Dimension(GridSizeMode.AutoSize) },
                                                ColumnDimensions = new[] { new Dimension(), new Dimension() },
                                                Content = new[]
                                                {
                                                    new Drawable[]
                                                    {
                                                        sectionOverallDifficultyEndTime = new FormNumberBox(allowDecimals: true)
                                                        {
                                                            Caption = "OD end (-1 section end)",
                                                            TabbableContentContainer = this,
                                                        },
                                                        setSectionOverallDifficultyEndNowButton = new RoundedButton
                                                        {
                                                            Text = "Use current time",
                                                            RelativeSizeAxes = Axes.X,
                                                            Action = () => mutateSetting(s => s.SectionOverallDifficultyEndTimeMs = (float)clock.CurrentTime),
                                                        },
                                                    }
                                                }
                                            },
                                        }
                                    },
                                    allowUnsafeStackLeniencyOverrideValues = new FormCheckBox
                                    {
                                        Caption = "Allow stack leniency values past limits (unsafe)",
                                    },
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
                                    allowUnsafeTickRateOverrideValues = new FormCheckBox
                                    {
                                        Caption = "Allow tick rate values past limits (unsafe)",
                                    },
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
                                    },
                                    allowUnsafeDifficultyOverrideValues = new FormCheckBox
                                    {
                                        Caption = "Allow values past limits (unsafe)",
                                    },
                                    difficultyOverrideStartWithBeatmapValues = new FormCheckBox
                                    {
                                        Caption = "Start with beatmap values",
                                    },
                                    enableGradualDifficultyChange = new FormCheckBox
                                    {
                                        Caption = "Gradual change",
                                    },
                                    new GridContainer
                                    {
                                        RelativeSizeAxes = Axes.X,
                                        AutoSizeAxes = Axes.Y,
                                        RowDimensions = new[] { new Dimension(GridSizeMode.AutoSize) },
                                        ColumnDimensions = new[] { new Dimension(), new Dimension() },
                                        Content = new[]
                                        {
                                            new Drawable[]
                                            {
                                                gradualDifficultyChangeEndTime = new FormNumberBox(allowDecimals: true)
                                                {
                                                    Caption = "Gradual finish time (ms)",
                                                    TabbableContentContainer = this,
                                                },
                                                setGradualFinishTimeButton = new RoundedButton
                                                {
                                                    Text = "Use current time",
                                                    RelativeSizeAxes = Axes.X,
                                                    Action = () => mutateSetting(s => s.GradualDifficultyChangeEndTimeMs = (float)clock.CurrentTime),
                                                },
                                            }
                                        }
                                    },
                                    keepDifficultyOverridesAfterSection = new FormCheckBox
                                    {
                                        Caption = "Keep overrides after section",
                                    },
                                    inheritFromPreviousButton = new RoundedButton
                                    {
                                        Text = "Inherit from previous",
                                        RelativeSizeAxes = Axes.X,
                                        Action = inheritDifficultyFromPrevious,
                                    },
                                }
                            },

                            showForceMods = new FormCheckBox
                            {
                                Caption = "Show forced mods",
                            },
                            forceModsFields = new FillFlowContainer
                            {
                                RelativeSizeAxes = Axes.X,
                                AutoSizeAxes = Axes.Y,
                                Direction = FillDirection.Vertical,
                                Spacing = new Vector2(5),
                                Children = new Drawable[]
                                {
                                    forceHidden = new FormCheckBox
                                    {
                                        Caption = "Force HD",
                                    },
                                    forceNoApproachCircle = new FormCheckBox
                                    {
                                        Caption = "Force no approach circle",
                                    },
                                    forceHardRock = new FormCheckBox
                                    {
                                        Caption = "Force HR",
                                    },
                                    forceFlashlight = new FormCheckBox
                                    {
                                        Caption = "Force FL",
                                    },
                                    flashlightRadius = new FormNumberBox(allowDecimals: true)
                                    {
                                        Caption = "FL radius (20-400)",
                                        TabbableContentContainer = this,
                                    },
                                    enableGradualFlashlightRadiusChange = new FormCheckBox
                                    {
                                        Caption = "Gradually shrink to radius",
                                    },
                                    enableGradualFlashlightFadeIn = new FormCheckBox
                                    {
                                        Caption = "Gradually fade in",
                                    },
                                    new GridContainer
                                    {
                                        RelativeSizeAxes = Axes.X,
                                        AutoSizeAxes = Axes.Y,
                                        RowDimensions = new[] { new Dimension(GridSizeMode.AutoSize) },
                                        ColumnDimensions = new[] { new Dimension(), new Dimension() },
                                        Content = new[]
                                        {
                                            new Drawable[]
                                            {
                                                gradualFlashlightRadiusEndTime = new FormNumberBox(allowDecimals: true)
                                                {
                                                    Caption = "FL gradual finish time (ms)",
                                                    TabbableContentContainer = this,
                                                },
                                                setFlGradualFinishTimeButton = new RoundedButton
                                                {
                                                    Text = "Use current time",
                                                    RelativeSizeAxes = Axes.X,
                                                    Action = () => mutateSetting(s => s.GradualFlashlightRadiusEndTimeMs = (float)clock.CurrentTime),
                                                },
                                            }
                                        }
                                    },
                                    forceDoubleTime = new FormCheckBox
                                    {
                                        Caption = "Force DT",
                                    },
                                }
                            },

                            showFunMods = new FormCheckBox
                            {
                                Caption = "Fun mods",
                            },
                            funModsFields = new FillFlowContainer
                            {
                                RelativeSizeAxes = Axes.X,
                                AutoSizeAxes = Axes.Y,
                                Direction = FillDirection.Vertical,
                                Spacing = new Vector2(5),
                                Children = new Drawable[]
                                {
                                    forceTransform = new FormCheckBox
                                    {
                                        Caption = "Force Transform",
                                    },
                                    forceWiggle = new FormCheckBox
                                    {
                                        Caption = "Force Wiggle",
                                    },
                                    wiggleStrength = new FormNumberBox(allowDecimals: true)
                                    {
                                        Caption = "Wiggle strength",
                                        TabbableContentContainer = this,
                                    },
                                    forceSpinIn = new FormCheckBox
                                    {
                                        Caption = "Force Spin In",
                                    },
                                    forceGrow = new FormCheckBox
                                    {
                                        Caption = "Force Grow",
                                    },
                                    growStartScale = new FormNumberBox(allowDecimals: true)
                                    {
                                        Caption = "Grow start scale",
                                        TabbableContentContainer = this,
                                    },
                                    forceDeflate = new FormCheckBox
                                    {
                                        Caption = "Force Deflate",
                                    },
                                    deflateStartScale = new FormNumberBox(allowDecimals: true)
                                    {
                                        Caption = "Deflate start scale",
                                        TabbableContentContainer = this,
                                    },
                                    forceBarrelRoll = new FormCheckBox
                                    {
                                        Caption = "Force Barrel Roll",
                                    },
                                    barrelRollSpinSpeed = new FormNumberBox(allowDecimals: true)
                                    {
                                        Caption = "Barrel roll spin speed",
                                        TabbableContentContainer = this,
                                    },
                                    forceApproachDifferent = new FormCheckBox
                                    {
                                        Caption = "Force Approach Different",
                                    },
                                    approachDifferentScale = new FormNumberBox(allowDecimals: true)
                                    {
                                        Caption = "Approach different scale",
                                        TabbableContentContainer = this,
                                    },
                                    forceMuted = new FormCheckBox
                                    {
                                        Caption = "Force Muted",
                                    },
                                    mutedMuteComboCount = new FormNumberBox
                                    {
                                        Caption = "Muted combo count",
                                        TabbableContentContainer = this,
                                    },
                                    forceNoScope = new FormCheckBox
                                    {
                                        Caption = "Force No Scope",
                                    },
                                    forceTraceable = new FormCheckBox
                                    {
                                        Caption = "Force Traceable",
                                    },
                                    noScopeHiddenComboCount = new FormNumberBox
                                    {
                                        Caption = "No scope hidden combo count",
                                        TabbableContentContainer = this,
                                    },
                                    forceMagnetised = new FormCheckBox
                                    {
                                        Caption = "Force Magnetised",
                                    },
                                    magnetisedAttractionStrength = new FormNumberBox(allowDecimals: true)
                                    {
                                        Caption = "Magnetised attraction",
                                        TabbableContentContainer = this,
                                    },
                                    forceRepel = new FormCheckBox
                                    {
                                        Caption = "Force Repel",
                                    },
                                    repelRepulsionStrength = new FormNumberBox(allowDecimals: true)
                                    {
                                        Caption = "Repel strength",
                                        TabbableContentContainer = this,
                                    },
                                    forceFreezeFrame = new FormCheckBox
                                    {
                                        Caption = "Force Freeze Frame",
                                    },
                                    forceBubbles = new FormCheckBox
                                    {
                                        Caption = "Force Bubbles",
                                    },
                                    forceSynesthesia = new FormCheckBox
                                    {
                                        Caption = "Force Synesthesia",
                                    },
                                    forceDepth = new FormCheckBox
                                    {
                                        Caption = "Force Depth",
                                    },
                                    depthMaxDepth = new FormNumberBox(allowDecimals: true)
                                    {
                                        Caption = "Depth max",
                                        TabbableContentContainer = this,
                                    },
                                    forceBloom = new FormCheckBox
                                    {
                                        Caption = "Force Bloom",
                                    },
                                    bloomMaxSizeComboCount = new FormNumberBox
                                    {
                                        Caption = "Bloom max size combo",
                                        TabbableContentContainer = this,
                                    },
                                    bloomMaxCursorSize = new FormNumberBox(allowDecimals: true)
                                    {
                                        Caption = "Bloom max cursor size",
                                        TabbableContentContainer = this,
                                    },
                                }
                            },

                            validationStatus = new OsuSpriteText
                            {
                                Text = "Validation: OK",
                                Font = OsuFont.GetFont(size: 12, weight: FontWeight.SemiBold),
                                Colour = Color4.White,
                            },
                        }
                    },
                }
            };

            bindModelEvents();
            bindControlEvents();
            updateDifficultyOverrideDefaults();

            updateSectionDropdown();
            updateControlsFromSelection();
        }

        private void bindModelEvents()
        {
            selectedHitObjects.BindTo(editorBeatmap.SelectedHitObjects);
            selectedHitObjects.BindCollectionChanged((_, e) =>
            {
                if (e.NewItems != null && e.NewItems.Count > 0)
                {
                    if (e.NewItems[e.NewItems.Count - 1] is HitObject mostRecent)
                        trySelectSectionForHitObject(mostRecent);
                }
                else
                {
                    trySelectSectionFromCurrentObjectSelection();
                }
            }, true);

            model.Sections.BindCollectionChanged((_, _) =>
            {
                updateSectionDropdown();
                updateControlsFromSelection();
            }, true);

            selectedSectionId.BindValueChanged(_ => updateControlsFromSelection(), true);

            sectionDropdown.Current.BindValueChanged(v =>
            {
                if (updatingControls)
                    return;

                selectedSectionId.Value = v.NewValue?.Id ?? -1;

                if (v.NewValue != null)
                    clock.SeekSmoothlyTo(v.NewValue.StartTime);
            });
        }

        private void bindControlEvents()
        {
            // Commit section names on commit/focus loss to avoid reloading controls every keypress,
            // which can steal focus from the textbox while typing.
            sectionNameBox.OnCommit += (_, _) => mutateSetting(s => s.SectionName = sectionNameBox.Current.Value);

            startTimeBox.OnCommit += (_, _) =>
            {
                if (tryParseDouble(startTimeBox.Current.Value, out double value))
                    model.SetSelectedStartTime(value);
            };

            endTimeBox.OnCommit += (_, _) =>
            {
                if (tryParseDouble(endTimeBox.Current.Value, out double value))
                    model.SetSelectedEndTime(value);
            };

            enableHpGimmick.Current.BindValueChanged(v => mutateSetting(s => s.EnableHPGimmick = v.NewValue));
            noDrain.Current.BindValueChanged(v => mutateSetting(s => s.NoDrain = v.NewValue));
            reverseHp.Current.BindValueChanged(v => mutateSetting(s => s.ReverseHP = v.NewValue));

            bindFloatSetting(hp300, (s, v) => s.HP300 = v, v => Math.Clamp(v, -2f, 2f));
            bindFloatSetting(hpStart, (s, v) => s.HPStart = v, v => Math.Clamp(v, 0f, 1f));
            bindFloatSetting(hpCap, (s, v) => s.HPCap = v, v => Math.Clamp(v, 0f, 1f));
            bindFloatSetting(hp100, (s, v) => s.HP100 = v, v => Math.Clamp(v, -2f, 2f));
            bindFloatSetting(hp50, (s, v) => s.HP50 = v, v => Math.Clamp(v, -2f, 2f));
            bindFloatSetting(hpMiss, (s, v) => s.HPMiss = v, v => Math.Clamp(v, -2f, 2f));
            hp300AffectsSliderEndsAndTicks.Current.BindValueChanged(v => mutateSetting(s => s.HP300AffectsSliderEndsAndTicks = v.NewValue));
            hp100AffectsSliderEndsAndTicks.Current.BindValueChanged(v => mutateSetting(s => s.HP100AffectsSliderEndsAndTicks = v.NewValue));
            hp50AffectsSliderEndsAndTicks.Current.BindValueChanged(v => mutateSetting(s => s.HP50AffectsSliderEndsAndTicks = v.NewValue));
            hpMissAffectsSliderEndAndTickMisses.Current.BindValueChanged(v => mutateSetting(s => s.HPMissAffectsSliderEndAndTickMisses = v.NewValue));
            showHpSliderRouting.Current.BindValueChanged(_ => updateGroupVisibility());

            enableNoMiss.Current.BindValueChanged(v => mutateSetting(s => s.EnableNoMiss = v.NewValue));
            enableAccuracyRequirement.Current.BindValueChanged(v => mutateSetting(s => s.EnableAccuracyRequirement = v.NewValue));
            bindAccuracyPercentageSetting(requiredAccuracy, (s, v) => s.RequiredAccuracy = v);

            enableCountLimits.Current.BindValueChanged(v => mutateSetting(s => s.EnableCountLimits = v.NewValue));
            bindIntSetting(max300s, (s, v) => s.Max300s = v, v => Math.Max(-1, v));
            bindIntSetting(max100s, (s, v) => s.Max100s = v, v => Math.Max(-1, v));
            bindIntSetting(max50s, (s, v) => s.Max50s = v, v => Math.Max(-1, v));
            bindIntSetting(maxMisses, (s, v) => s.MaxMisses = v, v => Math.Max(-1, v));
            max300sAffectsSliderEndsAndTicks.Current.BindValueChanged(v => mutateSetting(s => s.Max300sAffectsSliderEndsAndTicks = v.NewValue));
            max100sAffectsSliderEndsAndTicks.Current.BindValueChanged(v => mutateSetting(s => s.Max100sAffectsSliderEndsAndTicks = v.NewValue));
            max50sAffectsSliderEndsAndTicks.Current.BindValueChanged(v => mutateSetting(s => s.Max50sAffectsSliderEndsAndTicks = v.NewValue));
            maxMissesAffectsSliderEndAndTickMisses.Current.BindValueChanged(v => mutateSetting(s => s.MaxMissesAffectsSliderEndAndTickMisses = v.NewValue));
            showCountSliderRouting.Current.BindValueChanged(_ => updateGroupVisibility());

            enableNoMissedSliderEnd.Current.BindValueChanged(v => mutateSetting(s => s.EnableNoMissedSliderEnd = v.NewValue));

            enableGreatOffsetPenalty.Current.BindValueChanged(v => mutateSetting(s => s.EnableGreatOffsetPenalty = v.NewValue));
            bindFloatSetting(greatOffsetThreshold, (s, v) => s.GreatOffsetThresholdMs = v, v => Math.Max(0f, v));
            bindFloatSetting(greatOffsetPenaltyHp, (s, v) => s.GreatOffsetPenaltyHP = v, v => Math.Min(0f, v));

            enableDifficultyOverrides.Current.BindValueChanged(v => mutateSetting(s => s.EnableDifficultyOverrides = v.NewValue));
            enableDifficultyOverrides.Current.BindValueChanged(v =>
            {
                if (updatingControls || !v.NewValue)
                    return;

                mutateSetting(s =>
                {
                    if (float.IsNaN(s.SectionStackLeniency))
                        s.SectionStackLeniency = editorBeatmap.StackLeniency;

                    if (double.IsNaN(s.SectionTickRate))
                        s.SectionTickRate = editorBeatmap.Difficulty.SliderTickRate;
                });
            });
            allowUnsafeDifficultyOverrideValues.Current.BindValueChanged(v =>
            {
                if (!updatingControls && v.NewValue)
                    postUnsafeDifficultyWarning();

                mutateSetting(s => s.AllowUnsafeDifficultyOverrideValues = v.NewValue);
            });
            allowUnsafeStackLeniencyOverrideValues.Current.BindValueChanged(v =>
            {
                if (!updatingControls && v.NewValue)
                    postUnsafeDifficultyWarning();

                mutateSetting(s => s.AllowUnsafeStackLeniencyOverrideValues = v.NewValue);
            });
            allowUnsafeTickRateOverrideValues.Current.BindValueChanged(v =>
            {
                if (!updatingControls && v.NewValue)
                    postUnsafeDifficultyWarning();

                mutateSetting(s => s.AllowUnsafeTickRateOverrideValues = v.NewValue);
            });
            difficultyOverrideStartWithBeatmapValues.Current.BindValueChanged(v => mutateSetting(s => s.DifficultyOverrideStartWithBeatmapValues = v.NewValue));
            enableGradualDifficultyChange.Current.BindValueChanged(v => mutateSetting(s => s.EnableGradualDifficultyChange = v.NewValue));
            bindFloatSetting(gradualDifficultyChangeEndTime, (s, v) => s.GradualDifficultyChangeEndTimeMs = v, v => Math.Max(0f, v));
            keepDifficultyOverridesAfterSection.Current.BindValueChanged(v => mutateSetting(s => s.KeepDifficultyOverridesAfterSection = v.NewValue));
            bindSliderSetting(sectionCircleSize, (s, v) => s.SectionCircleSize = v, v => isUnsafeDifficultyOverrideEnabled() ? v : SectionGimmickValueClamper.ClampCircleSize(v));
            enableSectionCircleSizeWindow.Current.BindValueChanged(v => mutateSetting(s => s.EnableSectionCircleSizeWindow = v.NewValue));
            bindFloatSettingOnCommitOnly(sectionCircleSizeStartTime, (s, v) => s.SectionCircleSizeStartTimeMs = v, v => v < -1 ? -1 : v);
            bindFloatSettingOnCommitOnly(sectionCircleSizeEndTime, (s, v) => s.SectionCircleSizeEndTimeMs = v, v => v < -1 ? -1 : v);
            enableGradualSectionCircleSizeChange.Current.BindValueChanged(v => mutateSetting(s => s.EnableGradualSectionCircleSizeChange = v.NewValue));
            bindSliderSetting(sectionApproachRate, (s, v) => s.SectionApproachRate = v, v => isUnsafeDifficultyOverrideEnabled() ? v : SectionGimmickValueClamper.ClampApproachRate(v));
            enableSectionApproachRateWindow.Current.BindValueChanged(v => mutateSetting(s => s.EnableSectionApproachRateWindow = v.NewValue));
            bindFloatSettingOnCommitOnly(sectionApproachRateStartTime, (s, v) => s.SectionApproachRateStartTimeMs = v, v => v < -1 ? -1 : v);
            bindFloatSettingOnCommitOnly(sectionApproachRateEndTime, (s, v) => s.SectionApproachRateEndTimeMs = v, v => v < -1 ? -1 : v);
            enableGradualSectionApproachRateChange.Current.BindValueChanged(v => mutateSetting(s => s.EnableGradualSectionApproachRateChange = v.NewValue));
            bindSliderSetting(sectionOverallDifficulty, (s, v) => s.SectionOverallDifficulty = v, v => isUnsafeDifficultyOverrideEnabled() ? v : SectionGimmickValueClamper.ClampOverallDifficulty(v));
            enableSectionOverallDifficultyWindow.Current.BindValueChanged(v => mutateSetting(s => s.EnableSectionOverallDifficultyWindow = v.NewValue));
            bindFloatSettingOnCommitOnly(sectionOverallDifficultyStartTime, (s, v) => s.SectionOverallDifficultyStartTimeMs = v, v => v < -1 ? -1 : v);
            bindFloatSettingOnCommitOnly(sectionOverallDifficultyEndTime, (s, v) => s.SectionOverallDifficultyEndTimeMs = v, v => v < -1 ? -1 : v);
            enableGradualSectionOverallDifficultyChange.Current.BindValueChanged(v => mutateSetting(s => s.EnableGradualSectionOverallDifficultyChange = v.NewValue));
            bindSliderSetting(sectionStackLeniency, (s, v) => s.SectionStackLeniency = v, v => isUnsafeStackLeniencyOverrideEnabled() ? v : SectionGimmickValueClamper.ClampStackLeniency(v));
            bindSliderSetting(sectionTickRate, (s, v) => s.SectionTickRate = v, v => isUnsafeTickRateOverrideEnabled() ? v : SectionGimmickValueClamper.ClampTickRate(v));

            sectionCircleSize.Current.BindValueChanged(_ => updateDifficultyOverrideDefaults());
            sectionApproachRate.Current.BindValueChanged(_ => updateDifficultyOverrideDefaults());
            sectionOverallDifficulty.Current.BindValueChanged(_ => updateDifficultyOverrideDefaults());
            sectionStackLeniency.Current.BindValueChanged(_ => updateDifficultyOverrideDefaults());
            sectionTickRate.Current.BindValueChanged(_ => updateDifficultyOverrideDefaults());

            forceHidden.Current.BindValueChanged(v => mutateSetting(s => s.ForceHidden = v.NewValue));
            forceNoApproachCircle.Current.BindValueChanged(v => mutateSetting(s => s.ForceNoApproachCircle = v.NewValue));
            forceHardRock.Current.BindValueChanged(v => mutateSetting(s => s.ForceHardRock = v.NewValue));
            forceFlashlight.Current.BindValueChanged(v => mutateSetting(s => s.ForceFlashlight = v.NewValue));
            bindFloatSettingOnCommitOnly(flashlightRadius, (s, v) => s.FlashlightRadius = v, v => Math.Clamp(v, 20f, 400f));
            enableGradualFlashlightRadiusChange.Current.BindValueChanged(v => mutateSetting(s => s.EnableGradualFlashlightRadiusChange = v.NewValue));
            bindFloatSetting(gradualFlashlightRadiusEndTime, (s, v) => s.GradualFlashlightRadiusEndTimeMs = v, v => Math.Max(0f, v));
            enableGradualFlashlightFadeIn.Current.BindValueChanged(v => mutateSetting(s => s.EnableGradualFlashlightFadeIn = v.NewValue));
            forceDoubleTime.Current.BindValueChanged(v => mutateSetting(s => s.ForceDoubleTime = v.NewValue));
            showForceMods.Current.BindValueChanged(_ => updateGroupVisibility());

            forceTransform.Current.BindValueChanged(v => mutateSetting(s => s.ForceTransform = v.NewValue));
            forceWiggle.Current.BindValueChanged(v => mutateSetting(s => s.ForceWiggle = v.NewValue));
            bindFloatSetting(wiggleStrength, (s, v) => s.WiggleStrength = v, v => Math.Clamp(v, 0.1f, 2f));
            forceSpinIn.Current.BindValueChanged(v => mutateSetting(s => s.ForceSpinIn = v.NewValue));
            forceGrow.Current.BindValueChanged(v => mutateSetting(s => s.ForceGrow = v.NewValue));
            bindFloatSetting(growStartScale, (s, v) => s.GrowStartScale = v, v => Math.Clamp(v, 0f, 0.99f));
            forceDeflate.Current.BindValueChanged(v => mutateSetting(s => s.ForceDeflate = v.NewValue));
            bindFloatSetting(deflateStartScale, (s, v) => s.DeflateStartScale = v, v => Math.Clamp(v, 1f, 25f));
            forceBarrelRoll.Current.BindValueChanged(v => mutateSetting(s => s.ForceBarrelRoll = v.NewValue));
            bindDoubleSetting(barrelRollSpinSpeed, (s, v) => s.BarrelRollSpinSpeed = v, v => Math.Clamp(v, 0.02, 12));
            forceApproachDifferent.Current.BindValueChanged(v => mutateSetting(s => s.ForceApproachDifferent = v.NewValue));
            bindFloatSetting(approachDifferentScale, (s, v) => s.ApproachDifferentScale = v, v => Math.Clamp(v, 1.5f, 10f));
            forceMuted.Current.BindValueChanged(v => mutateSetting(s => s.ForceMuted = v.NewValue));
            bindIntSetting(mutedMuteComboCount, (s, v) => s.MutedMuteComboCount = v, v => Math.Clamp(v, 0, 500));
            forceNoScope.Current.BindValueChanged(v => mutateSetting(s => s.ForceNoScope = v.NewValue));
            forceTraceable.Current.BindValueChanged(v => mutateSetting(s => s.ForceTraceable = v.NewValue));
            bindIntSetting(noScopeHiddenComboCount, (s, v) => s.NoScopeHiddenComboCount = v, v => Math.Clamp(v, 0, 50));
            forceMagnetised.Current.BindValueChanged(v => mutateSetting(s => s.ForceMagnetised = v.NewValue));
            bindFloatSetting(magnetisedAttractionStrength, (s, v) => s.MagnetisedAttractionStrength = v, v => Math.Clamp(v, 0.05f, 1f));
            forceRepel.Current.BindValueChanged(v => mutateSetting(s => s.ForceRepel = v.NewValue));
            bindFloatSetting(repelRepulsionStrength, (s, v) => s.RepelRepulsionStrength = v, v => Math.Clamp(v, 0.05f, 1f));
            forceFreezeFrame.Current.BindValueChanged(v => mutateSetting(s => s.ForceFreezeFrame = v.NewValue));
            forceBubbles.Current.BindValueChanged(v => mutateSetting(s => s.ForceBubbles = v.NewValue));
            forceSynesthesia.Current.BindValueChanged(v => mutateSetting(s => s.ForceSynesthesia = v.NewValue));
            forceDepth.Current.BindValueChanged(v => mutateSetting(s => s.ForceDepth = v.NewValue));
            bindFloatSetting(depthMaxDepth, (s, v) => s.DepthMaxDepth = v, v => Math.Clamp(v, 50f, 200f));
            forceBloom.Current.BindValueChanged(v => mutateSetting(s => s.ForceBloom = v.NewValue));
            bindIntSetting(bloomMaxSizeComboCount, (s, v) => s.BloomMaxSizeComboCount = v, v => Math.Clamp(v, 5, 100));
            bindFloatSetting(bloomMaxCursorSize, (s, v) => s.BloomMaxCursorSize = v, v => Math.Clamp(v, 5f, 15f));
            showFunMods.Current.BindValueChanged(_ => updateGroupVisibility());
        }

        private void mutateSetting(Action<SectionGimmickSettings> settingMutation)
        {
            if (updatingControls)
                return;

            model.SetSelectedSetting(settingMutation);
        }

        private void updateSectionDropdown()
        {
            var orderedSections = model.Sections.OrderBy(s => s.StartTime).ToList();

            SectionGimmickSection? selected = orderedSections.FirstOrDefault(s => s.Id == selectedSectionId.Value);
            var sections = selected == null
                ? orderedSections
                : orderedSections.Where(s => s.Id != selected.Id).Prepend(selected).ToList();

            updatingControls = true;
            sectionDropdown.Items = sections;

            if (selected != null)
                sectionDropdown.Current.Value = selected;
            updatingControls = false;
        }

        private LocalisableString buildSectionTooltip(SectionGimmickSection section)
        {
            string end = section.EndTime < 0 ? "map end" : section.EndTime.ToString(CultureInfo.InvariantCulture);
            return $"{section.StartTime.ToString(CultureInfo.InvariantCulture)}ms → {end}";
        }

        private void updateControlsFromSelection()
        {
            var selected = model.Sections.FirstOrDefault(s => s.Id == selectedSectionId.Value);
            bool hasSelection = selected != null;

            removeSectionButton.Enabled.Value = hasSelection;
            selectedSectionFlow.FadeTo(hasSelection ? 1 : 0, 200, Easing.OutQuint);
            selectedSectionFlow.AlwaysPresent = true;

            pasteSettingsButton.Enabled.Value = hasSelection && model.HasCopiedSettings;

            // Enable "Inherit from Previous" only if there's a previous section with difficulty overrides
            bool canInherit = false;
            if (selected != null)
            {
                var orderedSections = model.Sections.OrderBy(s => s.StartTime).ToList();
                int currentIndex = orderedSections.FindIndex(s => s.Id == selected.Id);
                if (currentIndex > 0)
                {
                    var previous = orderedSections[currentIndex - 1];
                    canInherit = previous.Settings.EnableDifficultyOverrides;
                }
            }
            inheritFromPreviousButton.Enabled.Value = canInherit;

            sectionCircleSize.Current.Disabled = !hasSelection;
            sectionApproachRate.Current.Disabled = !hasSelection;
            sectionOverallDifficulty.Current.Disabled = !hasSelection;
            sectionStackLeniency.Current.Disabled = !hasSelection;
            sectionTickRate.Current.Disabled = !hasSelection;

            updatingControls = true;

            if (selected != null)
            {
                sectionDropdown.Current.Value = selected;
                sectionNameBox.Current.Value = selected.Settings.SectionName;

                startTimeBox.Current.Value = selected.StartTime.ToString(CultureInfo.InvariantCulture);
                endTimeBox.Current.Value = selected.EndTime.ToString(CultureInfo.InvariantCulture);

                var settings = selected.Settings;

                enableHpGimmick.Current.Value = settings.EnableHPGimmick;
                noDrain.Current.Value = settings.NoDrain;
                reverseHp.Current.Value = settings.ReverseHP;
                hp300.Current.Value = formatFloat(settings.HP300);
                hpStart.Current.Value = formatFloat(settings.HPStart);
                hpCap.Current.Value = formatFloat(settings.HPCap);
                hp100.Current.Value = formatFloat(settings.HP100);
                hp50.Current.Value = formatFloat(settings.HP50);
                hpMiss.Current.Value = formatFloat(settings.HPMiss);
                hp300AffectsSliderEndsAndTicks.Current.Value = settings.HP300AffectsSliderEndsAndTicks;
                hp100AffectsSliderEndsAndTicks.Current.Value = settings.HP100AffectsSliderEndsAndTicks;
                hp50AffectsSliderEndsAndTicks.Current.Value = settings.HP50AffectsSliderEndsAndTicks;
                hpMissAffectsSliderEndAndTickMisses.Current.Value = settings.HPMissAffectsSliderEndAndTickMisses;
                showHpSliderRouting.Current.Value = settings.HP300AffectsSliderEndsAndTicks
                                                    || settings.HP100AffectsSliderEndsAndTicks
                                                    || settings.HP50AffectsSliderEndsAndTicks
                                                    || settings.HPMissAffectsSliderEndAndTickMisses;

                enableNoMiss.Current.Value = settings.EnableNoMiss;
                enableAccuracyRequirement.Current.Value = settings.EnableAccuracyRequirement;
                requiredAccuracy.Current.Value = formatAccuracyPercent(settings.RequiredAccuracy);

                enableCountLimits.Current.Value = settings.EnableCountLimits;
                max300s.Current.Value = settings.Max300s.ToString(CultureInfo.InvariantCulture);
                max100s.Current.Value = settings.Max100s.ToString(CultureInfo.InvariantCulture);
                max50s.Current.Value = settings.Max50s.ToString(CultureInfo.InvariantCulture);
                maxMisses.Current.Value = settings.MaxMisses.ToString(CultureInfo.InvariantCulture);
                max300sAffectsSliderEndsAndTicks.Current.Value = settings.Max300sAffectsSliderEndsAndTicks;
                max100sAffectsSliderEndsAndTicks.Current.Value = settings.Max100sAffectsSliderEndsAndTicks;
                max50sAffectsSliderEndsAndTicks.Current.Value = settings.Max50sAffectsSliderEndsAndTicks;
                maxMissesAffectsSliderEndAndTickMisses.Current.Value = settings.MaxMissesAffectsSliderEndAndTickMisses;
                showCountSliderRouting.Current.Value = settings.Max300sAffectsSliderEndsAndTicks
                                                       || settings.Max100sAffectsSliderEndsAndTicks
                                                       || settings.Max50sAffectsSliderEndsAndTicks
                                                       || settings.MaxMissesAffectsSliderEndAndTickMisses;

                enableNoMissedSliderEnd.Current.Value = settings.EnableNoMissedSliderEnd;

                enableGreatOffsetPenalty.Current.Value = settings.EnableGreatOffsetPenalty;
                greatOffsetThreshold.Current.Value = formatFloat(settings.GreatOffsetThresholdMs);
                greatOffsetPenaltyHp.Current.Value = formatFloat(settings.GreatOffsetPenaltyHP);

                enableDifficultyOverrides.Current.Value = settings.EnableDifficultyOverrides;
                allowUnsafeDifficultyOverrideValues.Current.Value = settings.AllowUnsafeDifficultyOverrideValues;
                allowUnsafeStackLeniencyOverrideValues.Current.Value = settings.AllowUnsafeStackLeniencyOverrideValues;
                allowUnsafeTickRateOverrideValues.Current.Value = settings.AllowUnsafeTickRateOverrideValues;
                difficultyOverrideStartWithBeatmapValues.Current.Value = settings.DifficultyOverrideStartWithBeatmapValues;
                enableGradualDifficultyChange.Current.Value = settings.EnableGradualDifficultyChange;
                gradualDifficultyChangeEndTime.Current.Value = formatFloat(settings.GradualDifficultyChangeEndTimeMs);
                keepDifficultyOverridesAfterSection.Current.Value = settings.KeepDifficultyOverridesAfterSection;
                sectionCircleSize.Current.Value = hasSectionValue(settings.SectionCircleSize) ? settings.SectionCircleSize : 0f;
                enableSectionCircleSizeWindow.Current.Value = settings.EnableSectionCircleSizeWindow;
                sectionCircleSizeStartTime.Current.Value = formatOptionalWindowTime(settings.SectionCircleSizeStartTimeMs);
                sectionCircleSizeEndTime.Current.Value = formatOptionalWindowTime(settings.SectionCircleSizeEndTimeMs);
                enableGradualSectionCircleSizeChange.Current.Value = settings.EnableGradualSectionCircleSizeChange;
                sectionApproachRate.Current.Value = hasSectionValue(settings.SectionApproachRate) ? settings.SectionApproachRate : -20f;
                enableSectionApproachRateWindow.Current.Value = settings.EnableSectionApproachRateWindow;
                sectionApproachRateStartTime.Current.Value = formatOptionalWindowTime(settings.SectionApproachRateStartTimeMs);
                sectionApproachRateEndTime.Current.Value = formatOptionalWindowTime(settings.SectionApproachRateEndTimeMs);
                enableGradualSectionApproachRateChange.Current.Value = settings.EnableGradualSectionApproachRateChange;
                sectionOverallDifficulty.Current.Value = hasSectionValue(settings.SectionOverallDifficulty) ? settings.SectionOverallDifficulty : 0f;
                enableSectionOverallDifficultyWindow.Current.Value = settings.EnableSectionOverallDifficultyWindow;
                sectionOverallDifficultyStartTime.Current.Value = formatOptionalWindowTime(settings.SectionOverallDifficultyStartTimeMs);
                sectionOverallDifficultyEndTime.Current.Value = formatOptionalWindowTime(settings.SectionOverallDifficultyEndTimeMs);
                enableGradualSectionOverallDifficultyChange.Current.Value = settings.EnableGradualSectionOverallDifficultyChange;
                sectionStackLeniency.Current.Value = hasSectionValue(settings.SectionStackLeniency) ? settings.SectionStackLeniency : 0f;
                sectionTickRate.Current.Value = hasSectionValue(settings.SectionTickRate) ? settings.SectionTickRate : 0;

                forceHidden.Current.Value = settings.ForceHidden;
                forceNoApproachCircle.Current.Value = settings.ForceNoApproachCircle;
                forceHardRock.Current.Value = settings.ForceHardRock;
                forceFlashlight.Current.Value = settings.ForceFlashlight;
                flashlightRadius.Current.Value = formatFloat(settings.FlashlightRadius);
                enableGradualFlashlightRadiusChange.Current.Value = settings.EnableGradualFlashlightRadiusChange;
                enableGradualFlashlightFadeIn.Current.Value = settings.EnableGradualFlashlightFadeIn;
                gradualFlashlightRadiusEndTime.Current.Value = formatFloat(settings.GradualFlashlightRadiusEndTimeMs);
                forceDoubleTime.Current.Value = settings.ForceDoubleTime;
                forceTransform.Current.Value = settings.ForceTransform;
                forceWiggle.Current.Value = settings.ForceWiggle;
                wiggleStrength.Current.Value = formatFloat(settings.WiggleStrength);
                forceSpinIn.Current.Value = settings.ForceSpinIn;
                forceGrow.Current.Value = settings.ForceGrow;
                growStartScale.Current.Value = formatFloat(settings.GrowStartScale);
                forceDeflate.Current.Value = settings.ForceDeflate;
                deflateStartScale.Current.Value = formatFloat(settings.DeflateStartScale);
                forceBarrelRoll.Current.Value = settings.ForceBarrelRoll;
                barrelRollSpinSpeed.Current.Value = formatDouble(settings.BarrelRollSpinSpeed);
                forceApproachDifferent.Current.Value = settings.ForceApproachDifferent;
                approachDifferentScale.Current.Value = formatFloat(settings.ApproachDifferentScale);
                forceMuted.Current.Value = settings.ForceMuted;
                mutedMuteComboCount.Current.Value = settings.MutedMuteComboCount.ToString(CultureInfo.InvariantCulture);
                forceNoScope.Current.Value = settings.ForceNoScope;
                forceTraceable.Current.Value = settings.ForceTraceable;
                noScopeHiddenComboCount.Current.Value = settings.NoScopeHiddenComboCount.ToString(CultureInfo.InvariantCulture);
                forceMagnetised.Current.Value = settings.ForceMagnetised;
                magnetisedAttractionStrength.Current.Value = formatFloat(settings.MagnetisedAttractionStrength);
                forceRepel.Current.Value = settings.ForceRepel;
                repelRepulsionStrength.Current.Value = formatFloat(settings.RepelRepulsionStrength);
                forceFreezeFrame.Current.Value = settings.ForceFreezeFrame;
                forceBubbles.Current.Value = settings.ForceBubbles;
                forceSynesthesia.Current.Value = settings.ForceSynesthesia;
                forceDepth.Current.Value = settings.ForceDepth;
                depthMaxDepth.Current.Value = formatFloat(settings.DepthMaxDepth);
                forceBloom.Current.Value = settings.ForceBloom;
                bloomMaxSizeComboCount.Current.Value = settings.BloomMaxSizeComboCount.ToString(CultureInfo.InvariantCulture);
                bloomMaxCursorSize.Current.Value = formatFloat(settings.BloomMaxCursorSize);
                showForceMods.Current.Value = settings.ForceHidden
                                               || settings.ForceNoApproachCircle
                                               || settings.ForceHardRock
                                               || settings.ForceFlashlight
                                               || settings.ForceDoubleTime;
                showFunMods.Current.Value = hasAnyForcedFunMods(settings);

                if (!hasSectionValue(settings.SectionCircleSize))
                    sectionCircleSize.Current.Value = float.NaN;

                if (!hasSectionValue(settings.SectionApproachRate))
                    sectionApproachRate.Current.Value = float.NaN;

                if (!hasSectionValue(settings.SectionOverallDifficulty))
                    sectionOverallDifficulty.Current.Value = float.NaN;

                if (!hasSectionValue(settings.SectionStackLeniency))
                    sectionStackLeniency.Current.Value = float.NaN;

                if (!hasSectionValue(settings.SectionTickRate))
                    sectionTickRate.Current.Value = double.NaN;
            }

            updatingControls = false;

            updateGroupVisibility();
            updateValidationState();
        }

        private void updateGroupVisibility()
        {
            scheduleFade(hpGroupFields, enableHpGimmick.Current.Value, 0);
            hpGroupFields.AlwaysPresent = enableHpGimmick.Current.Value;

            scheduleFade(hpSliderRoutingFields, enableHpGimmick.Current.Value && showHpSliderRouting.Current.Value, 1);
            hpSliderRoutingFields.AlwaysPresent = enableHpGimmick.Current.Value && showHpSliderRouting.Current.Value;

            scheduleFade(requiredAccuracy, enableAccuracyRequirement.Current.Value, 2);
            requiredAccuracy.AlwaysPresent = enableAccuracyRequirement.Current.Value;

            scheduleFade(countLimitFields, enableCountLimits.Current.Value, 3);
            countLimitFields.AlwaysPresent = enableCountLimits.Current.Value;

            scheduleFade(countSliderRoutingFields, enableCountLimits.Current.Value && showCountSliderRouting.Current.Value, 4);
            countSliderRoutingFields.AlwaysPresent = enableCountLimits.Current.Value && showCountSliderRouting.Current.Value;

            scheduleFade(greatOffsetFields, enableGreatOffsetPenalty.Current.Value, 5);
            greatOffsetFields.AlwaysPresent = enableGreatOffsetPenalty.Current.Value;

            scheduleFade(difficultyOverrideFields, enableDifficultyOverrides.Current.Value, 6);
            difficultyOverrideFields.AlwaysPresent = enableDifficultyOverrides.Current.Value;

            scheduleFade(gradualDifficultyChangeEndTime, enableGradualDifficultyChange.Current.Value, 7);
            gradualDifficultyChangeEndTime.AlwaysPresent = enableGradualDifficultyChange.Current.Value;

            scheduleFade(setGradualFinishTimeButton, enableGradualDifficultyChange.Current.Value, 8);
            setGradualFinishTimeButton.AlwaysPresent = enableGradualDifficultyChange.Current.Value;

            bool showCsWindow = enableDifficultyOverrides.Current.Value && enableSectionCircleSizeWindow.Current.Value;
            scheduleFade(sectionCircleSizeWindowFields, showCsWindow, 12);
            sectionCircleSizeWindowFields.AlwaysPresent = showCsWindow;

            bool showArWindow = enableDifficultyOverrides.Current.Value && enableSectionApproachRateWindow.Current.Value;
            scheduleFade(sectionApproachRateWindowFields, showArWindow, 13);
            sectionApproachRateWindowFields.AlwaysPresent = showArWindow;

            bool showOdWindow = enableDifficultyOverrides.Current.Value && enableSectionOverallDifficultyWindow.Current.Value;
            scheduleFade(sectionOverallDifficultyWindowFields, showOdWindow, 14);
            sectionOverallDifficultyWindowFields.AlwaysPresent = showOdWindow;

            scheduleFade(forceModsFields, showForceMods.Current.Value, 9);
            forceModsFields.AlwaysPresent = showForceMods.Current.Value;

            scheduleFade(funModsFields, showFunMods.Current.Value, 10);
            funModsFields.AlwaysPresent = showFunMods.Current.Value;

            bool showGradualFlRadius = showForceMods.Current.Value && forceFlashlight.Current.Value && enableGradualFlashlightRadiusChange.Current.Value;
            scheduleFade(gradualFlashlightRadiusEndTime, showGradualFlRadius, 11);
            gradualFlashlightRadiusEndTime.AlwaysPresent = showGradualFlRadius;

            scheduleFade(setFlGradualFinishTimeButton, showGradualFlRadius, 13);
            setFlGradualFinishTimeButton.AlwaysPresent = showGradualFlRadius;
        }

        private void scheduleFade(Drawable drawable, bool visible, int slot)
        {
            float target = visible ? 1 : 0;

            if (Math.Abs(drawable.Alpha - target) < 0.0001f)
                return;

            fadeSchedules[slot]?.Cancel();
            fadeSchedules[slot] = Scheduler.AddDelayed(() => drawable.FadeTo(target, 200, Easing.OutQuint), 0);
        }

        private void updateValidationState()
        {
            try
            {
                var cloned = model.CreateClonedCurrentGimmicks();
                SectionGimmicksValidator.Validate(cloned);
                validationStatus.Text = "Validation: OK";
                validationStatus.Colour = Color4.White;
            }
            catch (Exception e)
            {
                validationStatus.Text = $"Validation: {e.Message}";
                validationStatus.Colour = Color4.IndianRed;
            }
        }

        private void trySelectSectionFromCurrentObjectSelection()
        {
            if (updatingControls)
                return;

            if (!selectedHitObjects.Any())
                return;

            var latestSelected = selectedHitObjects.LastOrDefault();
            if (latestSelected == null)
                return;

            trySelectSectionForHitObject(latestSelected);
        }

        private void trySelectSectionForHitObject(HitObject hitObject)
        {
            if (updatingControls)
                return;

            var section = SectionGimmickSectionResolver.Resolve(editorBeatmap.SectionGimmicks, hitObject.StartTime);
            if (section == null)
                return;

            if (selectedSectionId.Value == section.Id)
                return;

            selectedSectionId.Value = section.Id;
            clock.SeekSmoothlyTo(section.StartTime);
        }

        private void updateFloatSetting(FormNumberBox box, Action<SectionGimmickSettings, float> mutation)
        {
            if (!tryParseFloat(box.Current.Value, out float value))
                return;

            mutateSetting(s => mutation(s, value));
        }

        private void updateIntSetting(FormNumberBox box, Action<SectionGimmickSettings, int> mutation)
        {
            if (!int.TryParse(box.Current.Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int value))
                return;

            mutateSetting(s => mutation(s, value));
        }

        private void updateDoubleSetting(FormNumberBox box, Action<SectionGimmickSettings, double> mutation)
        {
            if (!tryParseDouble(box.Current.Value, out double value))
                return;

            mutateSetting(s => mutation(s, value));
        }

        private void bindFloatSetting(FormNumberBox box, Action<SectionGimmickSettings, float> mutation, Func<float, float> clamp)
            => box.OnCommit += (_, _) => updateClampedFloatSetting(box, mutation, clamp);

        private void bindAccuracyPercentageSetting(FormNumberBox box, Action<SectionGimmickSettings, float> mutation)
            => box.OnCommit += (_, _) => updateAccuracyPercentageSetting(box, mutation);

        private void bindSliderSetting(FormSliderBar<float> slider, Action<SectionGimmickSettings, float> mutation, Func<float, float> clamp)
            => slider.Current.BindValueChanged(v => updateClampedSliderSetting(slider, mutation, v.NewValue, clamp));

        private void bindSliderSetting(FormSliderBar<double> slider, Action<SectionGimmickSettings, double> mutation, Func<double, double> clamp)
            => slider.Current.BindValueChanged(v => updateClampedSliderSetting(slider, mutation, v.NewValue, clamp));

        private static bool hasSectionValue(float value) => !float.IsNaN(value);

        private static bool hasSectionValue(double value) => !double.IsNaN(value);

        private void bindFloatSettingOnCommitOnly(FormNumberBox box, Action<SectionGimmickSettings, float> mutation, Func<float, float> clamp)
            => box.OnCommit += (_, _) => updateClampedFloatSetting(box, mutation, clamp);

        private void bindIntSetting(FormNumberBox box, Action<SectionGimmickSettings, int> mutation, Func<int, int> clamp)
            => box.OnCommit += (_, _) => updateClampedIntSetting(box, mutation, clamp);

        private void bindDoubleSetting(FormNumberBox box, Action<SectionGimmickSettings, double> mutation, Func<double, double> clamp)
            => box.OnCommit += (_, _) => updateClampedDoubleSetting(box, mutation, clamp);

        private void updateClampedFloatSetting(FormNumberBox box, Action<SectionGimmickSettings, float> mutation, Func<float, float> clamp)
        {
            if (updatingControls)
                return;

            if (!tryParseFloat(box.Current.Value, out float value))
                return;

            float clamped = clamp(value);
            string formatted = formatFloat(clamped);

            if (box.Current.Value != formatted)
            {
                box.Current.Value = formatted;
                return;
            }

            mutateSetting(s => mutation(s, clamped));
        }

        private void updateClampedSliderSetting(FormSliderBar<float> slider, Action<SectionGimmickSettings, float> mutation, float value, Func<float, float> clamp)
        {
            if (updatingControls)
                return;

            float clamped = clamp(value);

            if (Math.Abs(clamped - value) > 0.0001f)
            {
                slider.Current.Value = clamped;
                return;
            }

            mutateSetting(s => mutation(s, clamped));
        }

        private void updateClampedSliderSetting(FormSliderBar<double> slider, Action<SectionGimmickSettings, double> mutation, double value, Func<double, double> clamp)
        {
            if (updatingControls)
                return;

            double clamped = clamp(value);

            if (Math.Abs(clamped - value) > 0.0001)
            {
                slider.Current.Value = clamped;
                return;
            }

            mutateSetting(s => mutation(s, clamped));
        }

        private void updateClampedIntSetting(FormNumberBox box, Action<SectionGimmickSettings, int> mutation, Func<int, int> clamp)
        {
            if (updatingControls)
                return;

            if (!int.TryParse(box.Current.Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int value))
                return;

            int clamped = clamp(value);
            string formatted = clamped.ToString(CultureInfo.InvariantCulture);

            if (box.Current.Value != formatted)
            {
                box.Current.Value = formatted;
                return;
            }

            mutateSetting(s => mutation(s, clamped));
        }

        private void updateAccuracyPercentageSetting(FormNumberBox box, Action<SectionGimmickSettings, float> mutation)
        {
            if (updatingControls)
                return;

            if (!tryParseFloat(box.Current.Value, out float percentValue))
                return;

            float clampedPercent = Math.Clamp(percentValue, 0f, 100f);
            float roundedPercent = MathF.Round(clampedPercent, 2);
            string formatted = formatFloat(roundedPercent, "0.##");

            if (box.Current.Value != formatted)
                box.Current.Value = formatted;

            mutateSetting(s => mutation(s, roundedPercent / 100f));
        }

        private void updateClampedDoubleSetting(FormNumberBox box, Action<SectionGimmickSettings, double> mutation, Func<double, double> clamp)
        {
            if (updatingControls)
                return;

            if (!tryParseDouble(box.Current.Value, out double value))
                return;

            double clamped = clamp(value);
            string formatted = formatDouble(clamped);

            if (box.Current.Value != formatted)
            {
                box.Current.Value = formatted;
                return;
            }

            mutateSetting(s => mutation(s, clamped));
        }

        private void applyCurrentSettingsByScope()
        {
            // Explicitly flush current section name before apply/scope actions.
            // This avoids losing text if apply is clicked while the text box still has focus.
            mutateSetting(s => s.SectionName = sectionNameBox.Current.Value);

            if (applyScopeDropdown.Current.Value == SectionGimmickApplyScope.ThisDifficulty)
                return;

            editor?.ApplySectionGimmicksToWholeMapset(model.CreateClonedCurrentGimmicks());
        }

        private void inheritDifficultyFromPrevious()
        {
            var current = model.Sections.FirstOrDefault(s => s.Id == model.SelectedSectionId.Value);
            if (current == null)
                return;

            var orderedSections = model.Sections.OrderBy(s => s.StartTime).ToList();
            int currentIndex = orderedSections.FindIndex(s => s.Id == current.Id);

            if (currentIndex <= 0)
                return;

            var previous = orderedSections[currentIndex - 1];

            if (!previous.Settings.EnableDifficultyOverrides)
                return;

            mutateSetting(s =>
            {
                s.EnableDifficultyOverrides = true;
                s.SectionCircleSize = previous.Settings.SectionCircleSize;
                s.SectionApproachRate = previous.Settings.SectionApproachRate;
                s.SectionOverallDifficulty = previous.Settings.SectionOverallDifficulty;
                s.AllowUnsafeStackLeniencyOverrideValues = previous.Settings.AllowUnsafeStackLeniencyOverrideValues;
                s.SectionStackLeniency = previous.Settings.SectionStackLeniency;
                s.AllowUnsafeTickRateOverrideValues = previous.Settings.AllowUnsafeTickRateOverrideValues;
                s.SectionTickRate = previous.Settings.SectionTickRate;
            });
        }

        private static string formatFloat(float value)
            => float.IsNaN(value) ? string.Empty : value.ToString(CultureInfo.InvariantCulture);

        private static string formatFloat(float value, string format)
            => float.IsNaN(value) ? string.Empty : value.ToString(format, CultureInfo.InvariantCulture);

        private static string formatAccuracyPercent(float fraction)
            => formatFloat(Math.Clamp(fraction, 0f, 1f) * 100f, "0.##");

        private static string formatOptionalWindowTime(float value)
            => value < 0 ? "-1" : formatFloat(value);

        private static string formatDouble(double value)
            => double.IsNaN(value) ? string.Empty : value.ToString(CultureInfo.InvariantCulture);

        private static bool tryParseFloat(string input, out float value)
            => float.TryParse(input, NumberStyles.Float, CultureInfo.InvariantCulture, out value);

        private static bool tryParseDouble(string input, out double value)
            => double.TryParse(input, NumberStyles.Float, CultureInfo.InvariantCulture, out value);

        private static bool hasAnyForcedFunMods(SectionGimmickSettings settings)
            => settings.ForceTransform
               || settings.ForceWiggle
               || settings.ForceSpinIn
               || settings.ForceGrow
               || settings.ForceDeflate
               || settings.ForceBarrelRoll
               || settings.ForceApproachDifferent
               || settings.ForceMuted
               || settings.ForceNoScope
               || settings.ForceTraceable
               || settings.ForceMagnetised
               || settings.ForceRepel
               || settings.ForceFreezeFrame
               || settings.ForceBubbles
               || settings.ForceSynesthesia
               || settings.ForceDepth
               || settings.ForceBloom;

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
                    // Can happen during early load when controls are transiently disabled.
                }
            }
        }

        private void postUnsafeDifficultyWarning()
            => notifications?.Post(new SimpleNotification
            {
                Text = "unsafe difficulty overrides enabled - values past normal limits can break gameplay or crash",
            });

        public enum SectionGimmickApplyScope
        {
            ThisDifficulty,
            WholeMapset,
        }

        private partial class SectionSelectionDropdown : FormDropdown<SectionGimmickSection>
        {
            protected override LocalisableString GenerateItemText(SectionGimmickSection item)
                => string.IsNullOrEmpty(item.Settings.SectionName)
                    ? $"Section {item.Id}"
                    : $"Section {item.Id} - {item.Settings.SectionName}";
        }
    }
}
