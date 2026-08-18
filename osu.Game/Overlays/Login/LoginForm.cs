// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Allocation;
using osu.Framework.Extensions.LocalisationExtensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.UserInterface;
using osu.Framework.Input;
using osu.Framework.Input.Events;
using osu.Framework.Platform;
using osu.Game.Configuration;
using osu.Game.Graphics;
using osu.Game.Graphics.Containers;
using osu.Game.Graphics.UserInterface;
using osu.Game.Online.API;
using osu.Game.Overlays.Settings;
using osu.Game.Resources.Localisation.Web;
using osuTK;
using osu.Game.Localisation;
using osu.Framework.Graphics.Shapes;


namespace osu.Game.Overlays.Login
{
    public partial class LoginForm : FillFlowContainer
    {
        private TextBox username = null!;
        private TextBox password = null!;
        private ShakeContainer shakeSignIn = null!;

        [Resolved]
        private IAPIProvider api { get; set; } = null!;

        public Action? RequestHide;

        public override bool AcceptsFocus => true;

        [BackgroundDependencyLoader(permitNulls: true)]
        private void load(OsuConfigManager config, AccountCreationOverlay accountCreation, GameHost host, OsuColour colours)
        {
            RelativeSizeAxes = Axes.X;
            AutoSizeAxes = Axes.Y;
            Direction = FillDirection.Vertical;
            Spacing = new Vector2(0, SettingsSection.ITEM_SPACING);

            ErrorTextFlowContainer errorText;
            LinkFlowContainer forgottenPasswordLink;
            LinkFlowContainer accountHeaderFlow;

            Children = new Drawable[]
            {
                new FillFlowContainer
                {
                    RelativeSizeAxes = Axes.X,
                    AutoSizeAxes = Axes.Y,
                    Padding = new MarginPadding { Horizontal = SettingsPanel.CONTENT_MARGINS },
                    Direction = FillDirection.Vertical,
                    Spacing = new Vector2(0f, SettingsSection.ITEM_SPACING),
                    Children = new Drawable[]
                    {
                        accountHeaderFlow = new LinkFlowContainer(t =>
                        {
                            t.Font = OsuFont.GetFont(weight: FontWeight.Bold);
                        })
                        {
                            RelativeSizeAxes = Axes.X,
                            AutoSizeAxes = Axes.Y,
                        },
                        username = new OsuTextBox
                        {
                            InputProperties = new TextInputProperties(TextInputType.Username, false),
                            PlaceholderText = UsersStrings.LoginUsername.ToLower(),
                            RelativeSizeAxes = Axes.X,
                            Text = api.ProvidedUsername,
                            TabbableContentContainer = this
                        },
                        password = new OsuPasswordTextBox
                        {
                            PlaceholderText = UsersStrings.LoginPassword.ToLower(),
                            RelativeSizeAxes = Axes.X,
                            TabbableContentContainer = this,
                        },
                        new Container
                        {
                            RelativeSizeAxes = Axes.X,
                            AutoSizeAxes = Axes.Y,
                            Masking = true,
                            CornerRadius = 5,
                            Margin = new MarginPadding { Top = 5 },
                            Children = new Drawable[]
                            {
                                new Box
                                {
                                    RelativeSizeAxes = Axes.Both,
                                    Colour = colours.CarmineDark
                                },
                                new OsuTextFlowContainer(t =>
                                {
                                    t.Font = OsuFont.Default.With(size: 14, weight: FontWeight.SemiBold);
                                    t.Colour = Colour4.White;
                                })
                                {
                                    RelativeSizeAxes = Axes.X,
                                    AutoSizeAxes = Axes.Y,
                                    Padding = new MarginPadding(10),
                                    Text = "Logins are currently disabled, as this client still connects to servers upstream. \nDeltaLazer contains features that make it incompatible with the game's current infrastructure, with Bancho blocking score uploads through modified clients \nSorry! :P"
                                }
                            }
                        },

                        errorText = new ErrorTextFlowContainer
                        {
                            RelativeSizeAxes = Axes.X,
                            AutoSizeAxes = Axes.Y,
                            Alpha = 0,
                        },
                    },
                },
                new SettingsCheckbox
                {
                    LabelText = LoginPanelStrings.RememberUsername,
                    Current = config.GetBindable<bool>(OsuSetting.SaveUsername),
                },
                new SettingsCheckbox
                {
                    LabelText = LoginPanelStrings.StaySignedIn,
                    Current = config.GetBindable<bool>(OsuSetting.SavePassword),
                },
                forgottenPasswordLink = new LinkFlowContainer
                {
                    Padding = new MarginPadding { Horizontal = SettingsPanel.CONTENT_MARGINS },
                    RelativeSizeAxes = Axes.X,
                    AutoSizeAxes = Axes.Y,
                },
                new Container
                {
                    RelativeSizeAxes = Axes.X,
                    AutoSizeAxes = Axes.Y,
                    Children = new Drawable[]
                    {
                        shakeSignIn = new ShakeContainer
                        {
                            RelativeSizeAxes = Axes.X,
                            AutoSizeAxes = Axes.Y,
                            Child = new SettingsButton
                            {
                                Text = UsersStrings.LoginButton,
                                Action = null, // Standard Action: `performLogin`
                            },
                        }
                    }
                },
                new SettingsButton
                {
                    Text = LoginPanelStrings.Register,
                    Action = null,
                    /* Defailt Action:
                    Action = () =>
                    {
                        RequestHide?.Invoke();
                        accountCreation.Show();
                    }
                    */
                }
            };


            accountHeaderFlow.AddText($"{LoginPanelStrings.Account.ToUpper()} - ");
            accountHeaderFlow.AddLink($"Delta Server", () => host.OpenUrlExternally("https://deltalazer.vercel.app/"), "Go to Delta Lazer's homepage");

            // forgottenPasswordLink.AddLink(LayoutStrings.PopupLoginLoginForgot, $"{api.Endpoints.WebsiteUrl}/home/password-reset");
            // Remove comment if you wish to re-activate this!

            password.OnCommit += (_, _) => performLogin();

            if (api.LastLoginError?.Message is string error)
            {
                errorText.Alpha = 1;
                errorText.AddErrors(new[] { error });
            }
        }

        private void performLogin()
        {
            if (!string.IsNullOrEmpty(username.Text) && !string.IsNullOrEmpty(password.Text))
                api.Login(username.Text, password.Text);
            else
                shakeSignIn.Shake();
        }

        protected override bool OnClick(ClickEvent e) => true;

        protected override void OnFocus(FocusEvent e)
        {
            Schedule(() => { GetContainingFocusManager()!.ChangeFocus(string.IsNullOrEmpty(username.Text) ? username : password); });
        }
    }
}
