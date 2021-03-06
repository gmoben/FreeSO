﻿/*
This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
If a copy of the MPL was not distributed with this file, You can obtain one at
http://mozilla.org/MPL/2.0/.
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FSO.Client.UI.Framework;
using Microsoft.Xna.Framework.Graphics;
using FSO.Client.UI.Controls;
using FSO.Common;
using FSO.HIT;
using FSO.HIT.Model;
using FSO.Client.UI.Screens;

namespace FSO.Client.UI.Panels
{

    /// <summary>
    /// Options Panel
    /// </summary>
    public class UIOptions : UICachedContainer
    {
        public UIImage Background;
        public UIImage Divider;

        public UIButton ExitButton { get; set; }
        public UIButton GraphicsButton { get; set; }
        public UIButton ProfanityButton { get; set; }
        public UIButton SelectSimButton { get; set; }
        public UIButton SoundButton { get; set; }

        public Texture2D BackgroundGameImage { get; set; }
        public Texture2D DividerImage { get; set; }

        private UIContainer Panel;
        private int CurrentPanel;

        public UIOptions()
        {
            var script = this.RenderScript("optionspanel.uis");

            Background = new UIImage(GetTexture((FSOEnvironment.UIZoomFactor>1f || GlobalSettings.Default.GraphicsWidth < 1024) ? (ulong)0x000000D800000002 : (ulong)0x0000018300000002));
            this.AddAt(0, Background);
            Background.BlockInput();
            Size = Background.Size.ToVector2();

            Divider = new UIImage(DividerImage);
            Divider.X = 227;
            Divider.Y = 17;
            this.Add(Divider);

            ExitButton.OnButtonClick += new ButtonClickDelegate(ExitButton_OnButtonClick);
            SelectSimButton.OnButtonClick += new ButtonClickDelegate(SelectSimButton_OnButtonClick);

            GraphicsButton.OnButtonClick += new ButtonClickDelegate(GraphicsButton_OnButtonClick);
            ProfanityButton.OnButtonClick += new ButtonClickDelegate(ProfanityButton_OnButtonClick);
            SoundButton.OnButtonClick += new ButtonClickDelegate(SoundButton_OnButtonClick);

            CurrentPanel = -1;
        }

        public void SetPanel(int newPanel)
        {
            GraphicsButton.Selected = false;
            ProfanityButton.Selected = false;
            SoundButton.Selected = false;

            if (CurrentPanel != -1) this.Remove(Panel);
            if (newPanel != CurrentPanel)
            {
                switch (newPanel)
                {
                    case 0:
                        Panel = new UIGraphicOptions();
                        GraphicsButton.Selected = true;
                        break;
                    case 1:
                        Panel = new UIProfanityOptions();
                        ProfanityButton.Selected = true;
                        break;
                    case 2:
                        Panel = new UISoundOptions();
                        SoundButton.Selected = true;
                        break;
                    default:
                        break;
                }
                Panel.X = 240;
                Panel.Y = 0;
                this.Add(Panel);
                CurrentPanel = newPanel;
            }
            else
            {
                CurrentPanel = -1;
            }

        }

        private void ExitButton_OnButtonClick(UIElement button)
        {
            UIScreen.ShowDialog(new UIExitDialog(), true);
        }

        private void GraphicsButton_OnButtonClick(UIElement button)
        {
            SetPanel(0);
        }

        private void ProfanityButton_OnButtonClick(UIElement button)
        {
            SetPanel(1);
        }

        private void SoundButton_OnButtonClick(UIElement button)
        {
            SetPanel(2);
        }

        private void SelectSimButton_OnButtonClick(UIElement button)
        {
            UIAlert alert = null;
            var options = new UIAlertOptions
            {
                Title = GameFacade.Strings.GetString("185", "6"),
                Message = GameFacade.Strings.GetString("185", "7"),
                Buttons = new UIAlertButton[]
                {
                    new UIAlertButton(UIAlertButtonType.Yes, (btn) => {
                        FSOFacade.Controller.Disconnect();
                        UIScreen.RemoveDialog(alert);
                    }),
                    new UIAlertButton(UIAlertButtonType.No, (btn) => { UIScreen.RemoveDialog(alert); })
                }
            };
            alert = UIScreen.GlobalShowAlert(options, true);
        }
    }

    public class UIProfanityOptions : UIContainer
    {
        public UIProfanityOptions()
        {
            var alert = UIScreen.GlobalShowAlert(new UIAlertOptions { Title = "Not Implemented", Message = "This feature is not implemented yet!" }, true);
            //this.RenderScript("profanitypanel.uis");
            //don't draw, this currently breaks the uis parser
        }
    }

    public class UISoundOptions : UIContainer
    {
        public UISlider FXSlider { get; set; }
        public UISlider MusicSlider { get; set; }
        public UISlider VoxSlider { get; set; }
        public UISlider AmbienceSlider { get; set; }

        public UISoundOptions()
        {
            this.RenderScript("soundpanel.uis");

            FXSlider.OnChange += new ChangeDelegate(ChangeVolume);
            MusicSlider.OnChange += new ChangeDelegate(ChangeVolume);
            VoxSlider.OnChange += new ChangeDelegate(ChangeVolume);
            AmbienceSlider.OnChange += new ChangeDelegate(ChangeVolume);

            FXSlider.Value = GlobalSettings.Default.FXVolume;
            MusicSlider.Value = GlobalSettings.Default.MusicVolume;
            VoxSlider.Value = GlobalSettings.Default.VoxVolume;
            AmbienceSlider.Value = GlobalSettings.Default.AmbienceVolume;
        }

        void ChangeVolume(UIElement slider)
        {
            UISlider elm = (UISlider)slider;

            if (elm == FXSlider) GlobalSettings.Default.FXVolume = (byte)elm.Value;
            else if (elm == MusicSlider) GlobalSettings.Default.MusicVolume = (byte)elm.Value;
            else if (elm == VoxSlider) GlobalSettings.Default.VoxVolume = (byte)elm.Value;
            else if (elm == AmbienceSlider) GlobalSettings.Default.AmbienceVolume = (byte)elm.Value;

            var hit = HITVM.Get();
            hit.SetMasterVolume(HITVolumeGroup.FX, GlobalSettings.Default.FXVolume / 10f);
            hit.SetMasterVolume(HITVolumeGroup.MUSIC, GlobalSettings.Default.MusicVolume / 10f);
            hit.SetMasterVolume(HITVolumeGroup.VOX, GlobalSettings.Default.VoxVolume / 10f);
            hit.SetMasterVolume(HITVolumeGroup.AMBIENCE, GlobalSettings.Default.AmbienceVolume / 10f);

            GlobalSettings.Default.Save();
        }
    }

    public class UIGraphicOptions : UIContainer
    {

        public UIButton AntiAliasCheckButton { get; set; }
        public UIButton ShadowsCheckButton { get; set; }
        public UIButton LightingCheckButton { get; set; }
        public UIButton UIEffectsCheckButton { get; set; }
        public UIButton EdgeScrollingCheckButton { get; set; }
        public UIButton Wall3DButton { get; set; }

        // High-Medium-Low detail buttons:

        public UIButton TerrainDetailLowButton { get; set; }
        public UIButton TerrainDetailMedButton { get; set; }
        public UIButton TerrainDetailHighButton { get; set; }

        public UIButton CharacterDetailLowButton { get; set; }
        public UIButton CharacterDetailMedButton { get; set; }
        public UIButton CharacterDetailHighButton { get; set; }

        public UILabel UIEffectsLabel { get; set; }
        public UILabel CharacterDetailLabel { get; set; }
        public UILabel ShadowsLabel { get; set; }
        public UILabel LightingLabel { get; set; }

        public UILabel TerrainDetailLabel { get; set; }
        public UILabel Wall3DLabel { get; set; }

        public UIGraphicOptions()
        {
            var script = this.RenderScript("graphicspanel.uis");
            
            UIEffectsLabel.Caption = GameFacade.Strings.GetString("f103", "2");
            UIEffectsLabel.Alignment = TextAlignment.Middle;
            CharacterDetailLabel.Caption = GameFacade.Strings.GetString("f103", "4");
            TerrainDetailLabel.Caption = GameFacade.Strings.GetString("f103", "1");
            ShadowsLabel.Caption = GameFacade.Strings.GetString("f103", "6");
            LightingLabel.Caption = GameFacade.Strings.GetString("f103", "3");

            AntiAliasCheckButton.OnButtonClick += new ButtonClickDelegate(FlipSetting);
            ShadowsCheckButton.OnButtonClick += new ButtonClickDelegate(FlipSetting);
            LightingCheckButton.OnButtonClick += new ButtonClickDelegate(FlipSetting);
            UIEffectsCheckButton.OnButtonClick += new ButtonClickDelegate(FlipSetting);
            EdgeScrollingCheckButton.OnButtonClick += new ButtonClickDelegate(FlipSetting);

            ShadowsCheckButton.Tooltip = ShadowsLabel.Caption;
            LightingCheckButton.Tooltip = LightingLabel.Caption;
            UIEffectsCheckButton.Tooltip = UIEffectsLabel.Caption;

            CharacterDetailLowButton.OnButtonClick += new ButtonClickDelegate(ChangeShadowDetail);
            CharacterDetailMedButton.OnButtonClick += new ButtonClickDelegate(ChangeShadowDetail);
            CharacterDetailHighButton.OnButtonClick += new ButtonClickDelegate(ChangeShadowDetail);

            TerrainDetailLowButton.OnButtonClick += new ButtonClickDelegate(ChangeSurroundingDetail);
            TerrainDetailMedButton.OnButtonClick += new ButtonClickDelegate(ChangeSurroundingDetail);
            TerrainDetailHighButton.OnButtonClick += new ButtonClickDelegate(ChangeSurroundingDetail);

            TerrainDetailLowButton.Tooltip = GameFacade.Strings.GetString("f103", "8");
            TerrainDetailMedButton.Tooltip = GameFacade.Strings.GetString("f103", "9");
            TerrainDetailHighButton.Tooltip = GameFacade.Strings.GetString("f103", "10");

            Wall3DButton = new UIButton(AntiAliasCheckButton.Texture);
            Wall3DButton.Position = UIEffectsCheckButton.Position + new Microsoft.Xna.Framework.Vector2(110, 0);
            Wall3DButton.OnButtonClick += new ButtonClickDelegate(FlipSetting);
            Add(Wall3DButton);
            Wall3DLabel = new UILabel();
            Wall3DLabel.Caption = GameFacade.Strings.GetString("f103", (FSOEnvironment.Enable3D)?"12":"11");
            Wall3DLabel.CaptionStyle = UIEffectsLabel.CaptionStyle;
            Wall3DLabel.Position = UIEffectsLabel.Position + new Microsoft.Xna.Framework.Vector2(110, 0);
            Add(Wall3DLabel);

            SettingsChanged();
        }

        private void FlipSetting(UIElement button)
        {
            var settings = GlobalSettings.Default;
            if (button == AntiAliasCheckButton) settings.AntiAlias = !(settings.AntiAlias);
            else if (button == ShadowsCheckButton) settings.SmoothZoom = !(settings.SmoothZoom);
            else if (button == LightingCheckButton) settings.Lighting = !(settings.Lighting);
            else if (button == UIEffectsCheckButton) settings.CityShadows = !(settings.CityShadows);
            else if (button == EdgeScrollingCheckButton) settings.EdgeScroll = !(settings.EdgeScroll);
            else if (button == Wall3DButton)
            {
                if (FSOEnvironment.Enable3D) settings.CitySkybox = !settings.CitySkybox;
                else settings.Shadows3D = !settings.Shadows3D;
            }
            GlobalSettings.Default.Save();
            SettingsChanged();
        }

        private void ChangeShadowDetail(UIElement button)
        {
            var settings = GlobalSettings.Default;
            if (button == CharacterDetailLowButton) settings.ShadowQuality = 512;
            else if (button == CharacterDetailMedButton) settings.ShadowQuality = 1024;
            else if (button == CharacterDetailHighButton) settings.ShadowQuality = 2048;
            GlobalSettings.Default.Save();
            SettingsChanged();
        }

        private void ChangeSurroundingDetail(UIElement button)
        {
            var settings = GlobalSettings.Default;
            if (button == TerrainDetailLowButton) settings.SurroundingLotMode = 0;
            else if (button == TerrainDetailMedButton) settings.SurroundingLotMode = 1;
            else if (button == TerrainDetailHighButton) settings.SurroundingLotMode = 2;
            GlobalSettings.Default.Save();
            SettingsChanged();
        }

        private void SettingsChanged()
        {
            var settings = GlobalSettings.Default;
            AntiAliasCheckButton.Selected = settings.AntiAlias; //antialias for render targets
            ShadowsCheckButton.Selected = settings.SmoothZoom;
            LightingCheckButton.Selected = settings.Lighting;
            UIEffectsCheckButton.Selected = settings.CityShadows; //instead of being able to disable UI transparency, you can toggle City Shadows.
            EdgeScrollingCheckButton.Selected = settings.EdgeScroll;

            // Character detail changed for city shadow detail.
            CharacterDetailLowButton.Selected = (settings.ShadowQuality <= 512);
            CharacterDetailMedButton.Selected = (settings.ShadowQuality > 512 && settings.ShadowQuality <= 1024);
            CharacterDetailHighButton.Selected = (settings.ShadowQuality > 1024);

            //not used right now! We need to determine if this should be ingame or not... It affects the density of grass blades on the simulation terrain.
            TerrainDetailLowButton.Selected = (settings.SurroundingLotMode == 0);
            TerrainDetailMedButton.Selected = (settings.SurroundingLotMode == 1);
            TerrainDetailHighButton.Selected = (settings.SurroundingLotMode == 2);

            Wall3DButton.Selected = (FSOEnvironment.Enable3D)?settings.CitySkybox:settings.Shadows3D;

            var oldSurrounding = LotView.WorldConfig.Current.SurroundingLots;
            LotView.WorldConfig.Current = new LotView.WorldConfig()
            {
                AdvancedLighting = settings.Lighting,
                SmoothZoom = settings.SmoothZoom,
                SurroundingLots = settings.SurroundingLotMode,
                AA = settings.AntiAlias,
                Shadow3D = settings.Shadows3D
            };

            var vm = ((IGameScreen)GameFacade.Screens.CurrentUIScreen)?.vm;
            if (vm != null)
            {
                vm.Context.World.ChangedWorldConfig(GameFacade.GraphicsDevice);
                if (oldSurrounding != settings.SurroundingLotMode) SimAntics.Utils.VMLotTerrainRestoreTools.RestoreSurroundings(vm, vm.HollowAdj);
            }
        }
    }
}
