// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Linq;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Input;
using osu.Game.Beatmaps;
using osu.Game.Input.Handlers;
using osu.Game.Replays;
using osu.Game.Rulesets.Mods;
using osu.Game.Rulesets.Objects.Drawables;
using osu.Game.Rulesets.Osu.Configuration;
using osu.Game.Rulesets.Osu.Mods;
using osu.Game.Rulesets.Osu.Objects;
using osu.Game.Rulesets.Osu.Replays;
using osu.Game.Rulesets.Scoring;
using osu.Game.Rulesets.UI;
using osu.Game.Scoring;
using osu.Game.Screens.Play;
using osuTK;

namespace osu.Game.Rulesets.Osu.UI
{
    public partial class DrawableOsuRuleset : DrawableRuleset<OsuHitObject>
    {
        private Bindable<bool>? cursorHideEnabled;
        [Resolved(canBeNull: true)]
        private HealthProcessor? healthProcessor { get; set; }

        public new OsuInputManager KeyBindingInputManager => (OsuInputManager)base.KeyBindingInputManager;

        public new OsuPlayfield Playfield => (OsuPlayfield)base.Playfield;

        protected new OsuRulesetConfigManager Config => (OsuRulesetConfigManager)base.Config;

        public DrawableOsuRuleset(Ruleset ruleset, IBeatmap beatmap, IReadOnlyList<Mod>? mods = null)
            : base(ruleset, beatmap, mods)
        {
        }

        [BackgroundDependencyLoader]
        private void load(ReplayPlayer? replayPlayer)
        {
            if (!Mods.Any(m => m is ModFlashlight))
            {
                Overlays.Add(new SectionGimmickFlashlightOverlay(Beatmap, this)
                {
                    Depth = float.MinValue,
                });
            }

            if (!Mods.Any(m => m is InputBlockingMod) && SectionGimmickInputBlockingOverlay.HasAnyForcedInputBlockingSection(Beatmap))
            {
                Overlays.Add(new SectionGimmickInputBlockingOverlay(Beatmap, this)
                {
                    Depth = float.MinValue,
                });
            }

            if (SectionGimmickFunModsOverlay.HasAnyForcedFunMods(Beatmap))
            {
                Overlays.Add(new SectionGimmickFunModsOverlay(Beatmap, this, Mods)
                {
                    Depth = float.MinValue,
                });
            }

            // Section gimmick displays are now provided through the skin system
            // They will appear in MainHUDComponents container when enabled
            if (replayPlayer != null)

                if (replayPlayer != null)
                {
                    ReplayAnalysisOverlay analysisOverlay;
                    PlayfieldAdjustmentContainer.Add(analysisOverlay = new ReplayAnalysisOverlay(replayPlayer.Score.Replay));
                    Overlays.Add(analysisOverlay.CreateProxy().With(p => p.Depth = float.NegativeInfinity));
                    replayPlayer.AddSettings(new ReplayAnalysisSettings(Config));

                    cursorHideEnabled = Config.GetBindable<bool>(OsuRulesetSetting.ReplayCursorHideEnabled);

                    // I have little faith in this working (other things touch cursor visibility) but haven't broken it yet.
                    // Let's wait for someone to report an issue before spending too much time on it.
                    cursorHideEnabled.BindValueChanged(enabled => Playfield.Cursor.FadeTo(enabled.NewValue ? 0 : 1), true);
                }
        }

        public override DrawableHitObject<OsuHitObject>? CreateDrawableRepresentation(OsuHitObject h) => null;

        public override bool ReceivePositionalInputAt(Vector2 screenSpacePos) => true; // always show the gameplay cursor

        protected override Playfield CreatePlayfield() => new OsuPlayfield();

        protected override PassThroughInputManager CreateInputManager() => new OsuInputManager(Ruleset.RulesetInfo);

        public override PlayfieldAdjustmentContainer CreatePlayfieldAdjustmentContainer() => new OsuPlayfieldAdjustmentContainer { AlignWithStoryboard = true };

        protected override ResumeOverlay CreateResumeOverlay()
        {
            if (Mods.Any(m => m is OsuModAutopilot or OsuModTouchDevice))
                return new DelayedResumeOverlay { Scale = new Vector2(0.65f) };

            return new OsuResumeOverlay();
        }

        protected override ReplayInputHandler CreateReplayInputHandler(Replay replay) => new OsuFramedReplayInputHandler(replay);

        protected override ReplayRecorder CreateReplayRecorder(Score score) => new OsuReplayRecorder(score);

        public override double GameplayStartTime
        {
            get
            {
                if (Objects.FirstOrDefault() is OsuHitObject first)
                    return first.StartTime - Math.Max(2000, first.TimePreempt);

                return 0;
            }
        }
    }
}
