﻿using Macrocosm.Common.DataStructures;
using Macrocosm.Common.UI;
using Macrocosm.Common.Utils;
using Macrocosm.Content.Rockets.Customization;
using Macrocosm.Content.Rockets.Modules;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using ReLogic.OS;
using System;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent.UI.Elements;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.UI;

namespace Macrocosm.Content.Rockets.Navigation
{
	internal class UICustomizationTab : UIPanel, ITabUIElement, IRocketDataConsumer
    {
		private Rocket rocket = new();
		public Rocket Rocket {
			get => rocket;
			set
			{
				bool changed = rocket != value;
				rocket = value;

				if (changed)
					OnRocketChanged();
			}
		} 


		private readonly Dictionary<Rocket, Rocket> rocketDummyPairs = new();
		public Rocket CustomizationDummy { 
			get 
			{
				if(!rocketDummyPairs.ContainsKey(Rocket))
					rocketDummyPairs[Rocket] = Rocket.Clone();

				return rocketDummyPairs[Rocket];
			}
			set 
			{
				rocketDummyPairs[Rocket] = value;
			} 
		}

		private readonly Dictionary<(Rocket rocket, string moduleName), List<UIPatternIcon>> dummyPatternEdits = new();

		private UIPanel rocketPreviewBackground;
		private UIHoverImageButton rocketPreviewZoomButton;
		private UIRocketPreviewLarge rocketPreview;

		private UIPanel customizationPanelBackground;

		private UIPanel modulePicker;
		private UIText modulePickerTitle;
		private UIHoverImageButton leftButton;
		private UIHoverImageButton rightButton;
		private string currentModuleName = "CommandPod";
		private RocketModule currentModule;

		private UIPanel rocketCustomizationControlPanel;
		private UIPanelIconButton rocketApplyButton;
		private UIPanelIconButton rocketCancelButton;
		private UIPanelIconButton rocketResetButton;
		private UIPanelIconButton rocketCopyButton;
		private UIPanelIconButton rocketPasteButton;

		private UIPanel nameplateConfigPanel;
		private UIInputTextBox nameplateTextBox;
		private UIFocusIconButton nameplateColorPicker;
		private UIFocusIconButton alignLeft;
		private UIFocusIconButton alignCenterHorizontal;
		private UIFocusIconButton alignRight;
		private UIFocusIconButton alignTop;
		private UIFocusIconButton alignCenterVertical;
		private UIFocusIconButton alignBottom;

		private UIPanel detailConfigPanel;

		private UIPanel patternConfigPanel;
		private UIListScrollablePanel patternSelector;
		private UIPatternIcon currentPatternIcon;

		private UIPanelIconButton resetPatternButton;
		private UIVerticalSeparator colorPickerSeparator;
		private List<(UIFocusIconButton picker, int colorIndex)> patternColorPickers;
		
		private UIColorMenuHSL hslMenu;
		private float luminanceSliderFactor = 0.85f;

		public UICustomizationTab()
		{
		}

		public override void OnInitialize()
        {
			Width.Set(0, 1f);
			Height.Set(0, 1f);
			HAlign = 0.5f;
			VAlign = 0.5f;

			SetPadding(6f);

			BackgroundColor = new Color(13, 23, 59, 127);
			BorderColor = new Color(15, 15, 15, 255);

			rocketPreviewBackground = CreateRocketPreview();
			Append(rocketPreviewBackground);

			customizationPanelBackground = new()
			{
				Width = new(0, 0.6f),
				Height = new(0, 1f),
				HAlign = 0f,
				BackgroundColor = new Color(53, 72, 135),
				BorderColor = new Color(89, 116, 213, 255)
			};
			customizationPanelBackground.SetPadding(6f);
			Append(customizationPanelBackground);

			modulePicker = CreateModulePicker();
			customizationPanelBackground.Append(modulePicker);

			nameplateConfigPanel = CreateNameplateConfigPanel();
			customizationPanelBackground.Append(nameplateConfigPanel);

			detailConfigPanel = CreateDetailConfigPanel();
			customizationPanelBackground.Append(detailConfigPanel);

			patternConfigPanel = CreatePatternConfigPanel();
			customizationPanelBackground.Append(patternConfigPanel);
			 
			rocketCustomizationControlPanel = CreateRocketControlPanel();
			customizationPanelBackground.Append(rocketCustomizationControlPanel);

			hslMenu = new(luminanceSliderFactor)
			{
				HAlign = 0.98f,
				Top = new(0f, 0.092f)
			};
			hslMenu.SetupApplyAndCancelButtons(ColorPickersLoseFocus, OnHSLMenuCancel);

			customizationPanelBackground.Activate();
		}

		public void OnTabOpen()
		{
			RefreshPatternColorPickers();
		}

		public void OnTabClose()
		{
			Main.blockInput = false;
			AllLoseFocus();
		}

		public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);

			rocketPreview.RocketDummy = CustomizationDummy;
			UpdateCurrentModule();

			UpdatePatternConfig();
			UpdatePatternColorPickers();

			UpdateNamplateTextBox();
			UpdateNameplateColorPicker();
			UpdateNameplateAlignButtons();

			UpdateHSLMenuVisibility();

			UpdateKeyboardCapture();
		}

		private void OnRocketChanged()
		{
			RefreshPatternConfigPanel();
		}

		#region Update methods
		private void UpdateCurrentModule()
		{
			currentModule = CustomizationDummy.Modules[currentModuleName];
			modulePickerTitle.SetText(Language.GetText("Mods.Macrocosm.UI.Rocket.Modules." + currentModuleName));
		}

		private void UpdatePatternConfig()
		{
			Pattern currentDummyPattern = CustomizationDummy.Modules[currentModuleName].Pattern;

			if (patternSelector.Any())
			{
				if(dummyPatternEdits.ContainsKey((Rocket, currentModuleName)))
					currentPatternIcon = dummyPatternEdits[(Rocket, currentModuleName)]
						.Where((icon) => icon.Pattern.Name == currentDummyPattern.Name)
						.FirstOrDefault();

				if (currentPatternIcon is not null)
				{
					currentPatternIcon.Pattern.SetColorData(currentDummyPattern.ColorData);
					currentPatternIcon.HasFocus = true;

					if (patternColorPickers is null)
						CreatePatternColorPickers();
				}
			}
		}

		private void UpdatePatternColorPickers()
		{
			foreach (var (picker, colorIndex) in patternColorPickers)
			{
				if (picker.HasFocus)
				{
					if (rocketPreview.ZoomedOut)
					{
						foreach (var module in CustomizationDummy.Modules)
						{
							var modulePattern = CustomizationDummy.Modules[module.Key].Pattern;

							if (modulePattern.Name == currentPatternIcon.Pattern.Name && hslMenu.PreviousColor != hslMenu.PendingColor)
 								modulePattern.SetColor(colorIndex, hslMenu.PendingColor);
 						}
					}
					else
					{
						currentModule.Pattern.SetColor(colorIndex, hslMenu.PendingColor);
					}
				}

				picker.BackPanelColor = currentModule.Pattern.GetColor(colorIndex);
			}
		}

		private void UpdateNamplateTextBox()
		{
			if (nameplateTextBox.HasFocus)
				CustomizationDummy.Nameplate.Text = nameplateTextBox.Text;
			else
				nameplateTextBox.Text = CustomizationDummy.AssignedName;
		}

		private void UpdateNameplateColorPicker()
		{
			if (nameplateColorPicker.HasFocus)
			{
				CustomizationDummy.Nameplate.TextColor = hslMenu.PendingColor;
				nameplateColorPicker.BackPanelColor = hslMenu.PendingColor;
				//nameplateTextBox.TextColor = hslMenu.PendingColor; // I don't think we want colored text in the text box lol -- Feldy
			}
			else
			{
				nameplateColorPicker.BackPanelColor = CustomizationDummy.Nameplate.TextColor;
				//nameplateTextBox.TextColor = Rocket.CustomizationDummy.Nameplate.TextColor;
			}
		}

		private void UpdateNameplateAlignButtons()
		{
			switch (CustomizationDummy.Nameplate.HorizontalAlignment)
			{
				case TextAlignmentHorizontal.Left: alignLeft.HasFocus = true; break;
				case TextAlignmentHorizontal.Right: alignRight.HasFocus = true; break;
				case TextAlignmentHorizontal.Center: alignCenterHorizontal.HasFocus = true; break;
			}

			switch (CustomizationDummy.Nameplate.VerticalAlignment)
			{
				case TextAlignmentVertical.Top: alignTop.HasFocus = true; break;
				case TextAlignmentVertical.Bottom: alignBottom.HasFocus = true; break;
				case TextAlignmentVertical.Center: alignCenterVertical.HasFocus = true; break;
			}
		}

		private void UpdateHSLMenuVisibility()
		{
			if (GetFocusedColorPicker(out _))
			{
				hslMenu.UpdateKeyboardCapture();

				if (customizationPanelBackground.HasChild(rocketCustomizationControlPanel))
					customizationPanelBackground.ReplaceChildWith(rocketCustomizationControlPanel, hslMenu);
			}
			else
			{
				if (customizationPanelBackground.HasChild(hslMenu))
					customizationPanelBackground.ReplaceChildWith(hslMenu, rocketCustomizationControlPanel);
			}
		}

		private void UpdateKeyboardCapture()
		{
			Main.blockInput = !Main.keyState.KeyPressed(Keys.Escape) && !Main.keyState.KeyPressed(Keys.R);

			bool colorPickerSelected = GetFocusedColorPicker(out var colorPicker);
			int indexInList = patternColorPickers.IndexOf(colorPicker);

			if (Main.keyState.KeyPressed(Keys.Left))
			{
				if(colorPicker.colorIndex >= 0)
				{
					if (indexInList - 1 >= 0)
						patternColorPickers[indexInList - 1].picker.HasFocus = true;
				} 
				else
				{
					leftButton.TriggerRemoteInteraction();
					PickPreviousModule();
				}
			}
			else if(Main.keyState.KeyPressed(Keys.Right))
			{
				if (colorPicker.colorIndex >= 0)
				{
					if (indexInList + 1 < patternColorPickers.Count)
						patternColorPickers[indexInList + 1].picker.HasFocus = true;
				}
				else
				{
					rightButton.TriggerRemoteInteraction();
					PickNextModule();
				}
			}

			if (colorPickerSelected)
 				hslMenu.UpdateKeyboardCapture();
		}
		#endregion

		#region Control actions
		private void PickPreviousModule()
		{
			rocketPreview.PreviousModule();
			RefreshPatternColorPickers();
			AllLoseFocus();
		}

		private void PickNextModule()
		{
			rocketPreview.NextModule();
			RefreshPatternColorPickers();
			AllLoseFocus();
		}

		private void JumpToModule(string moduleName)
		{
			rocketPreview.SetModule(moduleName);
			RefreshPatternColorPickers();
		}

		private void OnCurrentModuleChange(string moduleName, int moduleIndex)
		{
			currentModuleName = moduleName;
			UpdateCurrentModule();
			RefreshPatternConfigPanel();
		}

		private void RefreshPatternConfigPanel()
		{
			customizationPanelBackground.ReplaceChildWith(patternConfigPanel, CreatePatternConfigPanel());
			RefreshPatternColorPickers();
		}

		private void OnPreviewZoomIn()
		{
			rocketPreviewZoomButton.SetImage(ModContent.Request<Texture2D>("Macrocosm/Content/Rockets/Textures/Buttons/ZoomOutButton"));
		}

		private void OnPreviewZoomOut()
		{
			rocketPreviewZoomButton.SetImage(ModContent.Request<Texture2D>("Macrocosm/Content/Rockets/Textures/Buttons/ZoomInButton"));
		}

		private void ApplyCustomizationChanges()
		{
			AllLoseFocus();

			Rocket.ApplyCustomizationChanges(CustomizationDummy);

			RefreshPatternColorPickers();
		}

		private void DiscardCustomizationChanges()
		{
			AllLoseFocus();

			CustomizationDummy = Rocket.Clone();

			UpdatePatternConfig();
			currentPatternIcon.Pattern = CustomizationDummy.Modules[currentModuleName].Pattern.Clone();

			RefreshPatternColorPickers();
		}

		private void ResetRocketToDefaults()
		{
			AllLoseFocus();

			CustomizationDummy.ResetCustomizationToDefault();

			RefreshPatternColorPickers();
		}

		private void CopyRocketData()
		{
			SoundEngine.PlaySound(SoundID.MenuTick);
			string json = CustomizationDummy.GetCustomizationDataJSON();
			Platform.Get<IClipboard>().Value = json;
		}

		private void PasteRocketData()
		{
			SoundEngine.PlaySound(SoundID.MenuTick);
			string json = Platform.Get<IClipboard>().Value;

			try
			{
				CustomizationDummy.ApplyRocketCustomizationFromJSON(json);
				RefreshPatternColorPickers();
			}
			catch (Exception ex)
			{
				Utility.Chat(ex.Message);
			}
		}

		private void CopyModuleData()
		{
			SoundEngine.PlaySound(SoundID.MenuTick);
			string json = currentModule.GetCustomizationDataJSON();
			Platform.Get<IClipboard>().Value = json;
		}

		private void PasteModuleData()
		{
			SoundEngine.PlaySound(SoundID.MenuTick);
			string json = Platform.Get<IClipboard>().Value;

			try
			{
				currentModule.ApplyCustomizationDataFromJSON(json);
				RefreshPatternColorPickers();
			}
			catch (Exception ex)
			{
				Utility.Chat(ex.Message);
			}

		}

		private void OnHSLMenuCancel()
		{
			if (GetFocusedColorPicker(out var item))
			{
				if (item.colorIndex >= 0)
				{
					item.picker.BackPanelColor = hslMenu.PreviousColor;

					if (rocketPreview.ZoomedOut)
					{
						foreach (var module in CustomizationDummy.Modules)
						{
							var modulePattern = CustomizationDummy.Modules[module.Key].Pattern;

							if (modulePattern.Name == currentPatternIcon.Pattern.Name)
								modulePattern.SetColor(item.colorIndex, hslMenu.PendingColor, evenIfNotUserModifiable: true);
						}
					}
					else
					{
						currentModule.Pattern.SetColor(item.colorIndex, hslMenu.PreviousColor);
					}	
				}
				else if (item.colorIndex == -1)
				{
					CustomizationDummy.Nameplate.TextColor = hslMenu.PreviousColor;
				}
			}

			ColorPickersLoseFocus();

		}
		#endregion

		#region Pattern selection methods
		public void SelectPattern(UIPatternIcon icon)
		{
			currentPatternIcon = dummyPatternEdits[(Rocket, currentModuleName)].FirstOrDefault(stored => stored.Pattern.Name == icon.Pattern.Name);

			if (rocketPreview.ZoomedOut)
			{
				foreach(var module in CustomizationDummy.Modules)
				{
					if(CustomizationStorage.TryGetPattern(module.Key, icon.Pattern.Name, out Pattern defaultPattern))
					{
						var currentPattern = defaultPattern.Clone();

						for(int i = 0; i < Pattern.MaxColorCount; i++)
						{
							if (defaultPattern.ColorData[i].HasColorFunction && icon.Pattern.ColorData[i].HasColorFunction)
								currentPattern.SetColorFunction(i, icon.Pattern.ColorData[i].ColorFunction);
							else if (currentPattern.ColorData[i].IsUserModifiable && icon.Pattern.ColorData[i].IsUserModifiable)
								currentPattern.SetColor(i, icon.Pattern.ColorData[i].Color);
						}

						CustomizationDummy.Modules[module.Key].Pattern = currentPattern;
					}
				}
			}
			else
			{
				//currentModule.Pattern = CustomizationStorage.GetPattern(icon.Pattern.ModuleName, icon.Pattern.Name);
				currentModule.Pattern = icon.Pattern.Clone();
			}

			RefreshPatternColorPickers();
		}

		private void RefreshPatternColorPickers()
		{
			UpdateCurrentModule();
			UpdatePatternConfig();
			ClearPatternColorPickers();
			CreatePatternColorPickers();
		}

		private void ClearPatternColorPickers()
		{
			if (patternColorPickers is not null)
				foreach (var (picker, _) in patternColorPickers)
					picker.Remove();

			colorPickerSeparator?.Remove();
			resetPatternButton?.Remove();
		}

		private List<(UIFocusIconButton, int)> CreatePatternColorPickers()
		{
			patternColorPickers = new();

			var indexes = currentModule.Pattern.UserModifiableIndexes;

			float iconSize = 32f + 8f;
			float iconLeftOffset = 3f;

			for (int i = 0; i < indexes.Count; i++)
			{
				UIFocusIconButton colorPicker = new()
				{
					HAlign = 0f,
					Top = new(0f, 0.04f),
					Left = new(iconSize * i + iconLeftOffset, 0f),
					FocusContext = "RocketCustomizationColorPicker"
				};

				colorPicker.OnLeftClick += (_, _) => { colorPicker.HasFocus = true; };
				colorPicker.OnFocusGain = PatternColorPickerOnFocusGain;

				colorPicker.OnRightClick += (_, _) => { colorPicker.HasFocus = false; };
				colorPicker.OnFocusLost += PatternColorPickerOnFocusLost;

				patternConfigPanel.Append(colorPicker);
				patternColorPickers.Add((colorPicker, indexes[i]));
			}

			colorPickerSeparator = new()
			{
				Height = new(32f, 0f),
				Top = new(0f, 0.04f),
				Left = new(iconSize * indexes.Count + iconLeftOffset - 1f, 0f),
				Color = new Color(89, 116, 213, 255) * 1.1f
			};
			patternConfigPanel.Append(colorPickerSeparator);

			resetPatternButton = new(ModContent.Request<Texture2D>(symbolsPath + "ResetWhite"))
			{
				HAlign = 0f,
				Top = new(0f, 0.04f),
				Left = new(iconSize * indexes.Count + iconLeftOffset + 7f, 0f),
				HoverText = Language.GetText("Mods.Macrocosm.UI.Rocket.Customization.ResetPattern")
			};
			resetPatternButton.OnLeftClick += (_, _) => ResetCurrentPatternToDefaults();
			patternConfigPanel.Append(resetPatternButton);

			return patternColorPickers;
		}

		private void ResetCurrentPatternToDefaults()
		{
			//UpdatePatternConfig();
			ColorPickersLoseFocus();

			var defaultPattern = CustomizationStorage.GetPattern(currentModuleName, currentModule.Pattern.Name);
			currentModule.Pattern = defaultPattern.Clone();
			currentPatternIcon.Pattern = defaultPattern.Clone();

			if(GetFocusedColorPicker(out var item) && item.colorIndex >= 0)
			{
				item.picker.BackPanelColor = defaultPattern.GetColor(item.colorIndex);
			}

		}
		#endregion

		#region Focus handlers
		private void AllLoseFocus()
		{
			nameplateTextBox.HasFocus = false;
			ColorPickersLoseFocus();
		}

		private bool GetFocusedColorPicker(out (UIFocusIconButton picker, int colorIndex) focused)
		{
			if (patternColorPickers is not null)
			{
				foreach (var (picker, colorIndex) in patternColorPickers)
				{
					if (picker.HasFocus)
					{
						focused = (picker, colorIndex);
						return true;
					}
				}
			}
			
			if (nameplateColorPicker.HasFocus)
			{
				focused = (nameplateColorPicker, -1);
				return true;
			}

			focused = (null, int.MinValue);
			return false;
		}

		private void ColorPickersLoseFocus()
		{
			if (GetFocusedColorPicker(out var item))
				item.picker.HasFocus = false;
		}

		private void NameplateTextOnFocusGain()
		{
			JumpToModule("EngineModule");
		}

		private void NameplateTextOnFocusLost()
		{
		}

		private void NameplateColorPickerOnFocusGain()
		{
			JumpToModule("EngineModule");

			hslMenu.SetColorHSL(CustomizationDummy.Nameplate.TextColor.ToScaledHSL(luminanceSliderFactor));
			hslMenu.CaptureCurrentColor();
		}

		private void NameplateColorPickerOnFocusLost()
		{
		}

		private void PatternColorPickerOnFocusGain()
		{
			if(GetFocusedColorPicker(out var item) && item.colorIndex >= 0)
			{
				hslMenu.SetColorHSL(currentModule.Pattern.GetColor(item.colorIndex).ToScaledHSL(luminanceSliderFactor));
				hslMenu.CaptureCurrentColor();
			}
		}

		private void PatternColorPickerOnFocusLost()
		{
		}
		#endregion

		#region UI creation methods
		private const string buttonsPath = "Macrocosm/Content/Rockets/Textures/Buttons/";
		private const string symbolsPath = "Macrocosm/Content/Rockets/Textures/Symbols/";


		private UIPanel CreateRocketPreview()
		{
			rocketPreviewBackground = new()
			{
				Width = new(0, 0.4f),
				Height = new(0, 1f),
				Left = new(0, 0.605f),
				HAlign = 0f,
				BackgroundColor = new Color(53, 72, 135),
				BorderColor = new Color(89, 116, 213, 255)
			};
			rocketPreviewBackground.SetPadding(2f);
			rocketPreviewBackground.OverflowHidden = true;
			rocketPreviewBackground.Activate();
			Append(rocketPreviewBackground);

			rocketPreview = new()
			{
				OnZoomedIn = OnPreviewZoomIn,
				OnZoomedOut = OnPreviewZoomOut,
				OnModuleChange = OnCurrentModuleChange,
			};
			rocketPreviewBackground.Append(rocketPreview);

			rocketPreviewZoomButton = new(ModContent.Request<Texture2D>("Macrocosm/Content/Rockets/Textures/Buttons/ZoomOutButton"), ModContent.Request<Texture2D>("Macrocosm/Content/Rockets/Textures/Buttons/ZoomButtonBorder"))
			{
				Left = new(10, 0),
				Top = new(10, 0),
				HoverText = Language.GetText("Mods.Macrocosm.UI.Common.ZoomOut")
			};
			rocketPreviewZoomButton.OnLeftClick += (_, _) => rocketPreview.ZoomedOut = !rocketPreview.ZoomedOut;
			rocketPreviewBackground.Append(rocketPreviewZoomButton);

			return rocketPreviewBackground;
		}

		private UIPanel CreateModulePicker()
		{
			var mode = ReLogic.Content.AssetRequestMode.ImmediateLoad;

			modulePicker = new()
			{
				Width = new(0, 0.36f),
				Height = new(0, 0.25f),
				HAlign = 0.007f,
				Top = new(0f, 0.092f),
				BackgroundColor = new Color(53, 72, 135),
				BorderColor = new Color(89, 116, 213, 255),
			};
			modulePicker.SetPadding(0f);

			modulePickerTitle = new(Language.GetText("Mods.Macrocosm.UI.Rocket.Modules." + currentModuleName), 0.9f, false)
			{
				IsWrapped = false,
				HAlign = 0.5f,
				VAlign = 0.05f,
				TextColor = Color.White
			};
			modulePicker.Append(modulePickerTitle);

			UIPanel moduleIconPreviewPanel = new()
			{ 
				Width = new(0f, 0.6f),
				Height = new(0f, 0.7f),
				HAlign = 0.5f,
				VAlign = 0.6f,
				BackgroundColor = new Color(53, 72, 135),
				BorderColor = new Color(89, 116, 213, 255)
			};
			modulePicker.Append(moduleIconPreviewPanel);

			leftButton = new(ModContent.Request<Texture2D>(buttonsPath + "BackArrow", mode), ModContent.Request<Texture2D>(buttonsPath + "BackArrowBorder", mode))
			{
				VAlign = 0.5f,
				Left = new StyleDimension(0f, 0f),
			};
			leftButton.SetVisibility(1f, 1f, 1f);
			leftButton.CheckInteractible = () => !rocketPreview.AnimationActive;
			leftButton.OnLeftClick += (_, _) => PickPreviousModule();
			
			modulePicker.Append(leftButton);

			rightButton = new(ModContent.Request<Texture2D>(buttonsPath + "ForwardArrow", mode), ModContent.Request<Texture2D>(buttonsPath + "ForwardArrowBorder", mode))
			{
				VAlign = 0.5f,
				Left = new StyleDimension(0, 0.79f),
			};
			rightButton.SetVisibility(1f, 1f, 1f);
			rightButton.CheckInteractible = () => !rocketPreview.AnimationActive;
			rightButton.OnLeftClick += (_, _) => PickNextModule();
	
			modulePicker.Append(rightButton);

			return modulePicker;
		}

		private UIPanel CreateRocketControlPanel()
		{
			rocketCustomizationControlPanel = new()
			{
				Width = new(0f, 0.62f),
				Height = new(0, 0.25f),
				HAlign = 0.98f,
				Top = new(0f, 0.092f),
				BackgroundColor = new Color(53, 72, 135),
				BorderColor = new Color(89, 116, 213, 255)
			};
			rocketCustomizationControlPanel.SetPadding(2f);
			customizationPanelBackground.Append(rocketCustomizationControlPanel);

			rocketCopyButton = new(Main.Assets.Request<Texture2D>("Images/UI/CharCreation/Copy"))
			{
				VAlign = 0.9f,
				Left = new(0f, 0.18f),   
				HoverText = Language.GetText("Mods.Macrocosm.UI.Rocket.Customization.CopyRocket")
			};
			rocketCopyButton.OnLeftMouseDown += (_, _) => CopyRocketData();
			rocketCustomizationControlPanel.Append(rocketCopyButton);

			rocketPasteButton = new(Main.Assets.Request<Texture2D>("Images/UI/CharCreation/Paste"))
			{
				VAlign = 0.9f,
				Left = new(0f, 0.295f),   
				HoverText = Language.GetText("Mods.Macrocosm.UI.Rocket.Customization.PasteRocket")
			};
			rocketPasteButton.OnLeftMouseDown += (_, _) => PasteRocketData();
			rocketCustomizationControlPanel.Append(rocketPasteButton);

			UIVerticalSeparator separator1 = new()
			{
				Height = new(32f, 0f),
				VAlign = 0.9f,
				Left = new(0f, 0.425f),   
				Color = new Color(89, 116, 213, 255) * 1.1f
			};
			rocketCustomizationControlPanel.Append(separator1);

			rocketResetButton = new(ModContent.Request<Texture2D>(symbolsPath + "ResetGray"))
			{
				VAlign = 0.9f,
				Left = new(0f, 0.448f),  
				HoverText = Language.GetText("Mods.Macrocosm.UI.Rocket.Customization.ResetRocket")
			};
			rocketResetButton.OnLeftClick += (_, _) => ResetRocketToDefaults();
			rocketCustomizationControlPanel.Append(rocketResetButton);

			UIVerticalSeparator separator2 = new()
			{
				Height = new(32f, 0f),
				VAlign = 0.9f,
				Left = new(0f, 0.571f),   
				Color = new Color(89, 116, 213, 255) * 1.1f
			};
			rocketCustomizationControlPanel.Append(separator2);

			rocketCancelButton = new(ModContent.Request<Texture2D>(symbolsPath + "CrossmarkRed"))
			{
				VAlign = 0.9f,
				Left = new(0f, 0.6f), 
				HoverText = Language.GetText("Mods.Macrocosm.UI.Rocket.Customization.Cancel")
			};
			rocketCancelButton.OnLeftClick += (_, _) => DiscardCustomizationChanges();
			rocketCustomizationControlPanel.Append(rocketCancelButton);

			rocketApplyButton = new(ModContent.Request<Texture2D>(symbolsPath + "CheckmarkGreen"))
			{
				VAlign = 0.9f,
				Left = new(0f, 0.715f),   
				HoverText = Language.GetText("Mods.Macrocosm.UI.Rocket.Customization.Apply")
			};
			rocketApplyButton.OnLeftClick += (_, _) => ApplyCustomizationChanges();
			rocketCustomizationControlPanel.Append(rocketApplyButton);


			/*
			randomizeButton = new(Main.Assets.Request<Texture2D>("Images/UI/CharCreation/Randomize"))
			{
				VAlign = 0.93f,
				Left = new(0f, 0.77f),
				HoverText = Language.GetText("Mods.Macrocosm.UI.Common.RandomizeColor")
			};

			randomizeButton.OnLeftMouseDown += (_, _) => RandomizeColor();
			rocketCustomizationControlPanel.Append(randomizeButton);
			*/

			return rocketCustomizationControlPanel;
		}

		private UIPanel CreateNameplateConfigPanel()
		{
			nameplateConfigPanel = new()
			{
				Width = new StyleDimension(0, 0.99f),
				Height = new StyleDimension(0, 0.08f),
				HAlign = 0.5f,
				BackgroundColor = new Color(53, 72, 135),
				BorderColor = new Color(89, 116, 213, 255),
			};
			nameplateConfigPanel.SetPadding(0f);

			nameplateTextBox = new(Language.GetText("Mods.Macrocosm.Common.Rocket").Value)
			{
				Width = new(0f, 0.46f),
				Height = new(0f, 0.74f),
				HAlign = 0.02f,
				VAlign = 0.55f,
				BackgroundColor = new Color(53, 72, 135),
				BorderColor = new Color(89, 116, 213, 255),
				HoverBorderColor = Color.Gold,
				TextMaxLenght = Nameplate.MaxChars,
				TextScale = 1f,
				FormatText = Nameplate.FormatText,
				FocusContext = "TextBox",
				OnFocusGain = NameplateTextOnFocusGain,
				OnFocusLost = NameplateTextOnFocusLost
			};
			nameplateConfigPanel.Append(nameplateTextBox);

			nameplateColorPicker = new()
			{
				VAlign = 0.5f,
				HAlign = 0f,
				Left = new(0f, 0.482f),
				FocusContext = "RocketCustomizationColorPicker"
			};

			nameplateColorPicker.OnLeftClick += (_, _) => { nameplateColorPicker.HasFocus = true; };
			nameplateColorPicker.OnFocusGain = NameplateColorPickerOnFocusGain;

			nameplateColorPicker.OnRightClick += (_, _) => { nameplateColorPicker.HasFocus = false; };
			nameplateColorPicker.OnFocusLost += NameplateColorPickerOnFocusLost;
	
			nameplateConfigPanel.Append(nameplateColorPicker);

			alignLeft = new(ModContent.Request<Texture2D>(symbolsPath + "AlignLeft"))
			{
				VAlign = 0.5f,
				HAlign = 0f,
				Left = new(0f, 0.56f),
				HoverText = Language.GetText("Mods.Macrocosm.UI.Common.AlignLeft"),
				FocusContext = "HorizontalAlignment",
				OnFocusGain = () =>
				{
					JumpToModule("EngineModule");
					CustomizationDummy.Nameplate.HorizontalAlignment = TextAlignmentHorizontal.Left;
				}
			};
			alignLeft.OnLeftClick += (_, _) => alignLeft.HasFocus = true;
			 
			nameplateConfigPanel.Append(alignLeft);

			alignCenterHorizontal = new(ModContent.Request<Texture2D>(symbolsPath + "AlignCenterHorizontal"))
			{
				VAlign = 0.5f,
				Left = new(0f, 0.628f),
				HoverText = Language.GetText("Mods.Macrocosm.UI.Common.AlignCenterHorizontal"),
				FocusContext = "HorizontalAlignment",
				OnFocusGain = () =>
				{
					JumpToModule("EngineModule");
					CustomizationDummy.Nameplate.HorizontalAlignment = TextAlignmentHorizontal.Center;
				}
			};
			alignCenterHorizontal.OnLeftClick += (_, _) => alignCenterHorizontal.HasFocus = true;
			nameplateConfigPanel.Append(alignCenterHorizontal);

			alignRight = new(ModContent.Request<Texture2D>(symbolsPath + "AlignRight"))
			{
				VAlign = 0.5f,
				Left = new(0f, 0.696f),
				HoverText = Language.GetText("Mods.Macrocosm.UI.Common.AlignRight"),
				FocusContext = "HorizontalAlignment",
				OnFocusGain = () =>
				{
					JumpToModule("EngineModule");
					CustomizationDummy.Nameplate.HorizontalAlignment = TextAlignmentHorizontal.Right;
				}
			};
			alignRight.OnLeftClick += (_, _) => alignRight.HasFocus = true;
			nameplateConfigPanel.Append(alignRight);

			alignTop = new(ModContent.Request<Texture2D>(symbolsPath + "AlignTop"))
			{
				VAlign = 0.5f,
				Left = new(0f, 0.78f),
				HoverText = Language.GetText("Mods.Macrocosm.UI.Common.AlignTop"),
				FocusContext = "VerticalAlignment",
				OnFocusGain = () =>
				{
					JumpToModule("EngineModule");
					CustomizationDummy.Nameplate.VerticalAlignment = TextAlignmentVertical.Top;
				}
			};
			alignTop.OnLeftClick += (_, _) => alignTop.HasFocus = true;
			nameplateConfigPanel.Append(alignTop);

			alignCenterVertical = new(ModContent.Request<Texture2D>(symbolsPath + "AlignCenterVertical"))
			{
				VAlign = 0.5f,
				Left = new(0f, 0.848f),
				HoverText = Language.GetText("Mods.Macrocosm.UI.Common.AlignCenterVertical"),
				FocusContext = "VerticalAlignment",
				OnFocusGain = () =>
				{
					JumpToModule("EngineModule");
					CustomizationDummy.Nameplate.VerticalAlignment = TextAlignmentVertical.Center;
				}
			};
			alignCenterVertical.OnLeftClick += (_, _) => alignCenterVertical.HasFocus = true;
			nameplateConfigPanel.Append(alignCenterVertical);

			alignBottom = new(ModContent.Request<Texture2D>(symbolsPath + "AlignBottom"))
			{
				VAlign = 0.5f,
				Left = new(0f, 0.917f),
				HoverText = Language.GetText("Mods.Macrocosm.UI.Common.AlignBottom"),
				FocusContext = "VerticalAlignment",
				OnFocusGain = () =>
				{
					JumpToModule("EngineModule");
					CustomizationDummy.Nameplate.VerticalAlignment = TextAlignmentVertical.Bottom;
				}
			};
			alignBottom.OnLeftClick += (_, _) => alignBottom.HasFocus = true;
			nameplateConfigPanel.Append(alignBottom);

			return nameplateConfigPanel;
		}

		private UIPanel CreateDetailConfigPanel()
		{
			detailConfigPanel = new()
			{
				Width = new(0, 0.99f),
				Height = new(0, 0.22f),
				HAlign = 0.5f,
				Top = new(0f, 0.36f),
				BackgroundColor = new Color(53, 72, 135),
				BorderColor = new Color(89, 116, 213, 255)
			};
			detailConfigPanel.SetPadding(0f);
			return detailConfigPanel;
		}

		private UIPanel CreatePatternConfigPanel()
		{
			patternConfigPanel = new()
			{
				Width = new(0, 0.99f),
				Height = new(0, 0.4f),
				HAlign = 0.5f,
				Top = new(0f, 0.595f),
				BackgroundColor = new Color(53, 72, 135),
				BorderColor = new Color(89, 116, 213, 255),
			};
			patternConfigPanel.SetPadding(6f);
			patternConfigPanel.PaddingTop = 0f;

			foreach (var module in Rocket.Modules.Keys)
			{
				var icons = CustomizationStorage.ProvidePatternUI(module);

				var key = (Rocket, module);
				if (!dummyPatternEdits.ContainsKey(key))
					dummyPatternEdits[key] = icons.OfType<UIPatternIcon>().ToList();

				if (module == currentModuleName)
				{
					patternSelector = icons;

					foreach (var icon in patternSelector.OfType<UIPatternIcon>().ToList())
						icon.OnLeftClick += (_, icon) => SelectPattern(icon as UIPatternIcon);
				}
 			}

			patternConfigPanel.Append(patternSelector);

			return patternConfigPanel;
		}
		#endregion 
	}
}
