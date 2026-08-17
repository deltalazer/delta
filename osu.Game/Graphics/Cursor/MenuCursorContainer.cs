// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Allocation;
using osu.Framework.Audio;
using osu.Framework.Audio.Sample;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Primitives;
using osu.Framework.Graphics.Cursor;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.Textures;
using osu.Framework.Input.Events;
using osu.Framework.Extensions.ObjectExtensions;
using osu.Framework.Utils;
using osu.Game.Configuration;
using osu.Game.Localisation;
using osu.Game.Overlays;
using osu.Game.Overlays.Notifications;
using osu.Game.Skinning;
using osuTK;

namespace osu.Game.Graphics.Cursor
{
    public partial class MenuCursorContainer : CursorContainer
    {
        private readonly IBindable<bool> screenshotCursorVisibility = new Bindable<bool>(true);
        public override bool IsPresent => screenshotCursorVisibility.Value && base.IsPresent;

        private bool hideCursorOnNonMouseInput;

        public bool HideCursorOnNonMouseInput
        {
            get => hideCursorOnNonMouseInput;
            set
            {
                if (hideCursorOnNonMouseInput == value)
                    return;

                hideCursorOnNonMouseInput = value;
                updateState();
            }
        }

        protected override Drawable CreateCursor() => activeCursor = new Cursor();

        private Cursor activeCursor = null!;

        private DragRotationState dragRotationState;
        private Vector2 positionMouseDown;
        private Vector2 lastMovePosition;

        private Bindable<bool> cursorRotate = null!;
        private Sample tapSample = null!;

        private MouseInputDetector mouseInputDetector = null!;

        private SkinCursorTrail trail = null!;
        private Texture? trailTexture;
        private Bindable<bool> cursorTrail = null!;
        private Bindable<bool> trailFromSkin = null!;

        [Resolved(canBeNull: true)]
        private ISkinSource? trailSkin { get; set; }

        private bool visible;

        [BackgroundDependencyLoader]
        private void load(OsuConfigManager config, ScreenshotManager? screenshotManager, AudioManager audio)
        {
            cursorRotate = config.GetBindable<bool>(OsuSetting.CursorRotation);

            if (screenshotManager != null)
                screenshotCursorVisibility.BindTo(screenshotManager.CursorVisibility);

            tapSample = audio.Samples.Get(@"UI/cursor-tap");

            cursorTrail = config.GetBindable<bool>(OsuSetting.MenuCursorTrail);
            trailFromSkin = config.GetBindable<bool>(OsuSetting.MenuCursorFromSkin);

            Add(trail = new SkinCursorTrail { Depth = 1, Alpha = 0 });
            Add(mouseInputDetector = new MouseInputDetector());
        }

        [Resolved]
        private OsuGame? game { get; set; }

        private readonly IBindable<bool> lastInputWasMouse = new BindableBool();
        private readonly IBindable<bool> gameActive = new BindableBool(true);
        private readonly IBindable<bool> gameIdle = new BindableBool();

        protected override void LoadComplete()
        {
            base.LoadComplete();

            lastInputWasMouse.BindTo(mouseInputDetector.LastInputWasMouseSource);
            lastInputWasMouse.BindValueChanged(_ => updateState(), true);

            cursorTrail.BindValueChanged(_ => updateTrailVisibility());
            trailFromSkin.BindValueChanged(_ => updateTrailVisibility());

            if (trailSkin != null)
                trailSkin.SourceChanged += updateTrailTexture;

            updateTrailTexture();

            if (game != null)
            {
                gameIdle.BindTo(game.IsIdle);
                gameIdle.BindValueChanged(_ => updateState());

                gameActive.BindTo(game.IsActive);
                gameActive.BindValueChanged(_ => updateState());
            }
        }

        protected override void UpdateState(ValueChangedEvent<Visibility> state) => updateState();

        private void updateState()
        {
            bool combinedVisibility = getCursorVisibility();

            if (visible == combinedVisibility)
                return;

            visible = combinedVisibility;

            if (visible)
                PopIn();
            else
                PopOut();

            updateTrailVisibility();
        }

        private void updateTrailTexture()
        {
            trailTexture = isolate(trailSkin?.GetTexture(@"cursortrail"));

            if (trailTexture != null)
            {
                var cursorProvider = trailSkin?.FindProvider(s => s.GetTexture(@"cursor") != null);

                trail.DisjointTrail = cursorProvider?.GetTexture(@"cursormiddle") == null;
                trail.Blending = trail.DisjointTrail ? BlendingParameters.Inherit : BlendingParameters.Additive;
                trail.Texture = trailTexture;
            }

            updateTrailVisibility();
        }

        private void updateTrailVisibility()
            => trail.Alpha = trailTexture != null && cursorTrail.Value && trailFromSkin.Value && visible ? 1 : 0;

        private static Texture? isolate(Texture? texture)
        {
            if (texture == null)
                return null;

            var isolated = texture.Crop(new RectangleF(0, 0, texture.Width, texture.Height));

            isolated.ScaleAdjust = texture.ScaleAdjust;
            return isolated;
        }

        private bool getCursorVisibility()
        {
            // do not display when explicitly set to hidden state.
            if (State.Value == Visibility.Hidden)
                return false;

            // only hide cursor when game is focused, otherwise it should always be displayed.
            if (gameActive.Value)
            {
                // do not display when last input is not mouse.
                if (hideCursorOnNonMouseInput && !lastInputWasMouse.Value)
                    return false;

                // do not display when game is idle.
                if (gameIdle.Value)
                    return false;
            }

            return true;
        }

        protected override void Update()
        {
            base.Update();

            if (trail.CursorScale != activeCursor.CurrentScale)
                trail.CursorScale = activeCursor.CurrentScale;

            if (dragRotationState != DragRotationState.NotDragging
                && Vector2.Distance(positionMouseDown, lastMovePosition) > 60)
            {
                // make the rotation centre point floating.
                positionMouseDown = Interpolation.ValueAt(0.04f, positionMouseDown, lastMovePosition, 0, Clock.ElapsedFrameTime);
            }
        }

        protected override bool OnMouseMove(MouseMoveEvent e)
        {
            if (dragRotationState != DragRotationState.NotDragging)
            {
                lastMovePosition = e.MousePosition;

                float distance = Vector2Extensions.Distance(lastMovePosition, positionMouseDown);

                // don't start rotating until we're moved a minimum distance away from the mouse down location,
                // else it can have an annoying effect.
                if (dragRotationState == DragRotationState.DragStarted && distance > 80)
                    dragRotationState = DragRotationState.Rotating;

                // don't rotate when distance is zero to avoid NaN
                if (dragRotationState == DragRotationState.Rotating && distance > 0)
                {
                    Vector2 offset = e.MousePosition - positionMouseDown;
                    float degrees = float.RadiansToDegrees(MathF.Atan2(-offset.X, offset.Y)) + 24.3f;

                    // Always rotate in the direction of least distance
                    float diff = (degrees - activeCursor.Rotation) % 360;
                    if (diff < -180) diff += 360;
                    if (diff > 180) diff -= 360;
                    degrees = activeCursor.Rotation + diff;

                    activeCursor.RotateTo(degrees, 120, Easing.OutQuint);
                }
            }

            return base.OnMouseMove(e);
        }

        protected override bool OnMouseDown(MouseDownEvent e)
        {
            if (State.Value == Visibility.Visible)
            {
                if (!activeCursor.UsingSkinCursor)
                {
                    // only trigger animation for main mouse buttons
                    activeCursor.Scale = new Vector2(1);
                    activeCursor.ScaleTo(0.90f, 800, Easing.OutQuint);

                    activeCursor.AdditiveLayer.Alpha = 0;
                    activeCursor.AdditiveLayer.FadeInFromZero(800, Easing.OutQuint);

                    if (cursorRotate.Value && dragRotationState != DragRotationState.Rotating)
                    {
                        // if cursor is already rotating don't reset its rotate origin
                        dragRotationState = DragRotationState.DragStarted;
                        positionMouseDown = e.MousePosition;
                    }
                }

                playTapSample();
            }

            return base.OnMouseDown(e);
        }

        protected override void OnMouseUp(MouseUpEvent e)
        {
            if (!e.HasAnyButtonPressed)
            {
                if (!activeCursor.UsingSkinCursor)
                {
                    activeCursor.AdditiveLayer.FadeOutFromOne(500, Easing.OutQuint);
                    activeCursor.ScaleTo(1, 500, Easing.OutElastic);

                    if (dragRotationState != DragRotationState.NotDragging)
                    {
                        activeCursor.RotateTo(0, 400 * (0.5f + Math.Abs(activeCursor.Rotation / 960)), Easing.OutElasticQuarter);
                        dragRotationState = DragRotationState.NotDragging;
                    }
                }

                if (State.Value == Visibility.Visible)
                    playTapSample(0.8);
            }

            base.OnMouseUp(e);
        }

        protected override void PopIn()
        {
            activeCursor.FadeTo(1, 250, Easing.OutQuint);
            activeCursor.ScaleTo(1, 400, Easing.OutQuint);

            if (dragRotationState == DragRotationState.NotDragging)
                activeCursor.RotateTo(0, 400, Easing.OutQuint);
        }

        protected override void PopOut()
        {
            activeCursor.FadeTo(0, 250, Easing.OutQuint);
            activeCursor.ScaleTo(0.6f, 250, Easing.In);

            if (dragRotationState == DragRotationState.NotDragging)
                activeCursor.RotateTo(0, 400, Easing.OutQuint);
        }

        private void playTapSample(double baseFrequency = 1f)
        {
            const float random_range = 0.02f;
            SampleChannel channel = tapSample.GetChannel();

            // Scale to [-0.75, 0.75] so that the sample isn't fully panned left or right (sounds weird)
            channel.Balance.Value = ((activeCursor.X / DrawWidth) * 2 - 1) * OsuGameBase.SFX_STEREO_STRENGTH;
            channel.Frequency.Value = baseFrequency - (random_range / 2f) + RNG.NextDouble(random_range);
            channel.Volume.Value = baseFrequency;

            channel.Play();
        }

        protected override void Dispose(bool isDisposing)
        {
            if (trailSkin.IsNotNull())
                trailSkin.SourceChanged -= updateTrailTexture;

            base.Dispose(isDisposing);
        }

        public partial class Cursor : Container
        {
            public Vector2 CurrentScale => cursorContainer.Scale;

            private Container cursorContainer = null!;
            private Sprite cursorSprite = null!;
            private Bindable<float> cursorScale = null!;
            private Bindable<float> skinCursorScale = null!;
            private Bindable<float> cursorScaleX = null!;
            private Bindable<float> cursorScaleY = null!;
            private Bindable<bool> cursorFromSkin = null!;
            private const float base_scale = 0.15f;

            private TextureStore textures = null!;
            private float currentBaseScale = base_scale;

            public bool UsingSkinCursor { get; private set; }

            public Sprite AdditiveLayer = null!;

            [Resolved(canBeNull: true)]
            private ISkinSource? skin { get; set; }

            [Resolved]
            private INotificationOverlay? notifications { get; set; }

            public Cursor()
            {
                AutoSizeAxes = Axes.Both;
            }

            [BackgroundDependencyLoader]
            private void load(OsuConfigManager config, TextureStore textures, OsuColour colour)
            {
                this.textures = textures;

                Children = new Drawable[]
                {
                    cursorContainer = new Container
                    {
                        AutoSizeAxes = Axes.Both,
                        Children = new Drawable[]
                        {
                            cursorSprite = new Sprite(),
                            AdditiveLayer = new Sprite
                            {
                                Blending = BlendingParameters.Additive,
                                Colour = colour.Pink,
                                Alpha = 0,
                            },
                        }
                    }
                };

                cursorScale = config.GetBindable<float>(OsuSetting.MenuCursorSize);
                skinCursorScale = config.GetBindable<float>(OsuSetting.MenuCursorFromSkinSize);
                cursorScaleX = config.GetBindable<float>(OsuSetting.MenuCursorScaleX);
                cursorScaleY = config.GetBindable<float>(OsuSetting.MenuCursorScaleY);
                cursorFromSkin = config.GetBindable<bool>(OsuSetting.MenuCursorFromSkin);
            }

            protected override void LoadComplete()
            {
                base.LoadComplete();

                cursorScale.BindValueChanged(_ => updateScale());
                skinCursorScale.BindValueChanged(_ => updateScale());
                cursorScaleX.BindValueChanged(_ => updateScale());
                cursorScaleY.BindValueChanged(_ => updateScale());
                cursorFromSkin.BindValueChanged(_ => updateTextures());

                if (skin != null)
                    skin.SourceChanged += updateTextures;

                updateTextures();
            }

            private void updateTextures()
            {
                Texture? skinCursor = isolate(cursorFromSkin.Value ? skin?.GetTexture(@"cursor") : null);

                if (cursorFromSkin.Value && skin != null && skinCursor == null)
                {
                    cursorFromSkin.Value = false;

                    notifications?.Post(new SimpleNotification
                    {
                        Text = UserInterfaceStrings.MenuCursorFromSkinUnsupported,
                        Icon = FontAwesome.Solid.ExclamationTriangle,
                    });

                    return;
                }

                var defaultCursor = textures.Get(@"Cursor/menu-cursor");

                if (skinCursor != null)
                {
                    cursorSprite.Texture = skinCursor;
                    AdditiveLayer.Texture = skinCursor;

                    Origin = Anchor.Centre;

                    currentBaseScale = 1;

                    // effects are skipped from here on, so anything left mid-animation has to be undone.
                    Scale = Vector2.One;
                    Rotation = 0;
                    AdditiveLayer.Alpha = 0;
                }
                else
                {
                    cursorSprite.Texture = defaultCursor;
                    AdditiveLayer.Texture = textures.Get(@"Cursor/menu-cursor-additive");

                    Origin = Anchor.TopLeft;
                    currentBaseScale = base_scale;
                }

                UsingSkinCursor = skinCursor != null;
                updateScale();
            }

            private void updateScale()
            {
                float size = (UsingSkinCursor ? skinCursorScale.Value : cursorScale.Value) * currentBaseScale;

                cursorContainer.Scale = new Vector2(size * cursorScaleX.Value, size * cursorScaleY.Value);
            }

            protected override void Dispose(bool isDisposing)
            {
                if (skin.IsNotNull())
                    skin.SourceChanged -= updateTextures;

                base.Dispose(isDisposing);
            }
        }

        private partial class SkinCursorTrail : CursorTrail
        {
            private const double disjoint_trail_time_separation = 1000 / 60.0;

            public bool DisjointTrail { get; set; }

            private double lastTrailTime;
            private Vector2? currentPosition;

            protected override double FadeDuration => DisjointTrail ? 150 : 500;
            protected override float FadeExponent => 1;
            protected override bool InterpolateMovements => !DisjointTrail;
            protected override bool AvoidDrawingNearCursor => !DisjointTrail;

            protected override void Update()
            {
                base.Update();

                if (!DisjointTrail || !currentPosition.HasValue)
                    return;

                if (Time.Current - lastTrailTime >= disjoint_trail_time_separation)
                {
                    lastTrailTime = Time.Current;
                    AddTrail(currentPosition.Value);
                }
            }

            protected override bool OnMouseMove(MouseMoveEvent e)
            {
                if (!DisjointTrail)
                    return base.OnMouseMove(e);

                currentPosition = e.ScreenSpaceMousePosition;

                // Intentionally block the base call as we're adding the trails ourselves.
                return false;
            }
        }

        private partial class MouseInputDetector : Component
        {
            /// <summary>
            /// Whether the last input applied to the game is sourced from mouse.
            /// </summary>
            public IBindable<bool> LastInputWasMouseSource => lastInputWasMouseSource;

            private readonly Bindable<bool> lastInputWasMouseSource = new Bindable<bool>();

            public MouseInputDetector()
            {
                RelativeSizeAxes = Axes.Both;
            }

            protected override bool Handle(UIEvent e)
            {
                switch (e)
                {
                    case MouseDownEvent:
                    case MouseMoveEvent:
                        lastInputWasMouseSource.Value = true;
                        return false;

                    case KeyDownEvent keyDown when !keyDown.Repeat:
                    case JoystickPressEvent:
                    case MidiDownEvent:
                        lastInputWasMouseSource.Value = false;
                        return false;
                }

                return false;
            }
        }

        private enum DragRotationState
        {
            NotDragging,
            DragStarted,
            Rotating,
        }
    }
}
