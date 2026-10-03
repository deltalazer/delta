// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Graphics.UserInterface;
using osu.Framework.Localisation;
using osu.Game.Configuration;
using osu.Game.Graphics.UserInterfaceV2;
using osu.Game.Localisation;
using osu.Game.Overlays.Dialog;

namespace osu.Game.Overlays.Settings.Sections.UserInterface
{
    public partial class GeneralSettings : SettingsSubsection
    {
        protected override LocalisableString Header => CommonStrings.General;

        private Bindable<bool> menuCursorFromSkin = null!;
        private Bindable<bool> menuCursorFromSkinConfirmed = null!;

        [Resolved]
        private IDialogOverlay? dialogOverlay { get; set; }

        [BackgroundDependencyLoader]
        private void load(OsuConfigManager config)
        {
            menuCursorFromSkin = config.GetBindable<bool>(OsuSetting.MenuCursorFromSkin);
            menuCursorFromSkinConfirmed = config.GetBindable<bool>(OsuSetting.MenuCursorFromSkinConfirmed);

            Children = new Drawable[]
            {
                new SettingsItemV2(new FormCheckBox
                {
                    Caption = UserInterfaceStrings.CursorRotation,
                    Current = config.GetBindable<bool>(OsuSetting.CursorRotation)
                })
                {
                    Keywords = [@"spin"],
                },
                new SettingsItemV2(new FormCheckBox
                {
                    Caption = UserInterfaceStrings.MenuCursorFromSkin,
                    Current = menuCursorFromSkin
                })
                {
                    Keywords = [@"skin", @"cursor", @"menu"],
                },
                new SettingsItemV2(new FormCheckBox
                {
                    Caption = UserInterfaceStrings.MenuCursorTrail,
                    Current = config.GetBindable<bool>(OsuSetting.MenuCursorTrail)
                })
                {
                    Keywords = [@"skin", @"cursor", @"trail"],
                },
                new SettingsItemV2(new FormSliderBar<float>
                {
                    Caption = UserInterfaceStrings.MenuCursorSize,
                    Current = config.GetBindable<float>(OsuSetting.MenuCursorSize),
                    KeyboardStep = 0.01f,
                    LabelFormat = v => $"{v:0.##}x"
                }),
                new SettingsItemV2(new FormSliderBar<float>
                {
                    Caption = UserInterfaceStrings.MenuCursorFromSkinSize,
                    Current = config.GetBindable<float>(OsuSetting.MenuCursorFromSkinSize),
                    KeyboardStep = 0.01f,
                    LabelFormat = v => $"{v:0.##}x"
                })
                {
                    Keywords = [@"skin", @"cursor", @"size"],
                },
                new SettingsItemV2(new FormSliderBar<float>
                {
                    Caption = UserInterfaceStrings.MenuCursorScaleX,
                    Current = config.GetBindable<float>(OsuSetting.MenuCursorScaleX),
                    KeyboardStep = 0.01f,
                    LabelFormat = v => $"{v:0.##}x"
                })
                {
                    Keywords = [@"cursor", @"width", @"horizontal", @"stretch"],
                },
                new SettingsItemV2(new FormSliderBar<float>
                {
                    Caption = UserInterfaceStrings.MenuCursorScaleY,
                    Current = config.GetBindable<float>(OsuSetting.MenuCursorScaleY),
                    KeyboardStep = 0.01f,
                    LabelFormat = v => $"{v:0.##}x"
                })
                {
                    Keywords = [@"cursor", @"height", @"vertical", @"stretch"],
                },
                new SettingsItemV2(new FormSliderBar<float>
                {
                    Caption = UserInterfaceStrings.Parallax,
                    Current = config.GetBindable<float>(OsuSetting.MenuParallaxScale),
                    DisplayAsPercentage = true,
                    LabelFormat = v => v == 0 ? CommonStrings.Disabled : FormSliderBar<float>.DefaultLabelFormat(v, true),
                }),
                new SettingsItemV2(new FormSliderBar<double>
                {
                    Caption = UserInterfaceStrings.HoldToConfirmActivationTime,
                    Current = config.GetBindable<double>(OsuSetting.UIHoldActivationDelay),
                    KeyboardStep = 50,
                    LabelFormat = v => $"{v:N0} ms",
                })
                {
                    Keywords = [@"delay"],
                    ApplyClassicDefault = c => ((IHasCurrentValue<double>)c).Current.Value = 0,
                },
            };
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();

            menuCursorFromSkin.BindValueChanged(fromSkin =>
            {
                if (!fromSkin.NewValue || menuCursorFromSkinConfirmed.Value)
                    return;

                dialogOverlay?.Push(new ConfirmDialog(UserInterfaceStrings.MenuCursorFromSkinConfirmation,
                    () => menuCursorFromSkinConfirmed.Value = true,
                    () => menuCursorFromSkin.Value = false)
                {
                    BodyText = UserInterfaceStrings.MenuCursorFromSkinConfirmationInfo
                });
            });
        }
    }
}
