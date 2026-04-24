using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;
using OWML.Common;
using static ShipEnhancements.ShipEnhancements;
using static ShipEnhancements.Settings;
using UnityEngine;
using OWML.ModHelper.Menus.NewMenuSystem;
using OWML.ModHelper;
using OWML.Utils;
using ShipEnhancements.Utils;
using UnityEngine.UI;

namespace ShipEnhancements.ModSettings;

public static class SEMenuManager
{
	public static SettingPresets.PresetName CurrentPreset { get; set; } = (SettingPresets.PresetName)(-1);
	public static List<Settings> HiddenSettings { get; private set; } = [];
	public static bool HidePreset { get; set; } = false;

	private static readonly IModHelper ModHelper = Instance.ModHelper;

	private static bool _detectValueChanged = true;
	private static int _resetButtonCount = 0;

	private static readonly (string blendType, string suffix, Func<int, int, bool> canShow)[] _customSettingNames =
	[
		("Time", "1", (index, num) => index == 1),
		("Time", "2", (index, num) => index == 2),
		("Time", "3", (index, num) => index == 3),
		("Temperature", "(Hot)", (index, num) => index == 1),
		("Temperature", "(Default)", (index, num) => index != 1 && index != num),
		("Temperature", "(Cold)", (index, num) => index == num),
		("Ship Temperature", "(Hot)", (index, num) => index == 1),
		("Ship Temperature", "(Default)", (index, num) => index != 1 && index != num),
		("Ship Temperature", "(Cold)", (index, num) => index == num),
		("Reactor State", "(Default)", (index, num) => index == num - 2),
		("Reactor State", "(Damaged)", (index, num) => index == num - 1),
		("Reactor State", "(Critical)", (index, num) => index == num),
		("Ship Damage %", "(No Damage)", (index, num) => index == num - 2),
		("Ship Damage %", "(Low Damage)", (index, num) => index == num - 1),
		("Ship Damage %", "(High Damage)", (index, num) => index == num),
		("Fuel", "(Max Fuel)", (index, num) => index == 1),
		("Fuel", "(Low Fuel)", (index, num) => index != 1 && index == num - 1),
		("Fuel", "(No Fuel)", (index, num) => index == num),
		("Oxygen", "(Max Oxygen)", (index, num) => index == 1),
		("Oxygen", "(Low Oxygen)", (index, num) => index != 1 && index == num - 1),
		("Oxygen", "(No Oxygen)", (index, num) => index == num),
		("Velocity", "(Positive)", (index, num) => index == 1),
		("Velocity", "(Matched)", (index, num) => index != 1 && index != num),
		("Velocity", "(Negative)", (index, num) => index == num),
		("Gravity", "(Zero Gravity)", (index, num) => index == num - 2),
		("Gravity", "(Low Gravity)", (index, num) => index == num - 1),
		("Gravity", "(High Gravity)", (index, num) => index == num),
	];

	private static readonly Dictionary<string, string> _customTooltips = new()
	{
		{ "Time", "Time mode blends between colors over a set amount of time." },
		{ "Temperature", "Temperature mode blends between colors based on the ship's temperature." },
		{ "Ship Temperature", "Ship Temperature mode blends between colors based on the ship's internal temperature." },
		{ "Reactor State", "Reactor State mode changes the color if the reactor is damaged or is about to explode." },
		{
			"Ship Damage %", "Ship Damage % mode blends between colors based on how many parts of the ship are damaged."
		},
		{ "Fuel", "Fuel mode blends between colors based on the amount of fuel left in the ship." },
		{ "Oxygen", "Oxygen mode blends between colors based on the amount of oxygen left in the ship." },
		{
			"Velocity",
			"Velocity mode blends between colors based on how fast you're moving towards your current lock-on target."
		},
		{ "Gravity", "Gravity mode blends between colors based on how high the gravity is." },
	};

	private static readonly Dictionary<string, string> _stemToSuffix = new()
	{
		{ "shipLight", "Light Color" },
		{ "interiorHull", "Interior Hull Color" },
		{ "exteriorHull", "Exterior Hull Color" },
		{ "interiorWood", "Interior Wood Color" },
		{ "exteriorWood", "Exterior Wood Color" },
		{ "thruster", "Thruster Color" },
		{ "indicator", "Indicator Color" }
	};
	
	private static bool IsChangelogUpdated()
	{
		var currentVersion = "Release v" + ModHelper.Manifest.Version;
		return currentVersion == SaveData.LastChangelogVersion;
	}

	private static bool TryUpdateChangelogVersion()
	{
		var currentVersion = "Release v" + ModHelper.Manifest.Version;
		if (currentVersion != SaveData.LastChangelogVersion)
		{
			ShipEnhancements.WriteDebugMessage($"Updated from {SaveData.LastChangelogVersion} to {currentVersion}");
			SaveData.LastChangelogVersion = currentVersion;
			UpdateSaveFile();
			return true;
		}

		return false;
	}

	private static List<string> GetDecorationSettings()
	{
		int start = ModHelper.Config.Settings.Keys.ToList()
			.IndexOf("enableColorBlending");
		int end = ModHelper.Config.Settings.Keys.ToList()
			.IndexOf("indicatorColor3");

		var range = ModHelper.Config.Settings.Keys.ToList()
			.GetRange(start, end - start + 1);
		return range;
	}

	private static List<string> GetTemperatureSettings()
	{
		int start = ModHelper.Config.Settings.Keys.ToList()
			.IndexOf("enableShipTemperature");
		int end = ModHelper.Config.Settings.Keys.ToList()
			.IndexOf("enableReactorOverload");

		var range = ModHelper.Config.Settings.Keys.ToList()
			.GetRange(start, end - start + 1);
		return range;
	}

	private static List<string> GetWaterSettings()
	{
		int start = ModHelper.Config.Settings.Keys.ToList()
			.IndexOf("addWaterTank");
		int end = ModHelper.Config.Settings.Keys.ToList()
			.IndexOf("addWaterCooling");

		var range = ModHelper.Config.Settings.Keys.ToList()
			.GetRange(start, end - start + 1);
		return range;
	}

	private static void OnValueChanged(string name, object oldValue, object newValue)
	{
		if (!_detectValueChanged)
		{
			return;
		}

		if (GetDecorationSettings().Contains(name) && 
			!int.TryParse(name.Substring(name.Length - 1), out _) &&
			!name.Contains("Texture"))
		{
			int optionsNew;
			int optionsOld;
			if (name == "indicatorColorOptions")
			{
				optionsNew = int.Parse((string)newValue);
				optionsOld = int.Parse((string)oldValue);
			}
			else
			{
				optionsNew = int.Parse((string)indicatorColorOptions.GetValue());
				optionsOld = optionsNew;
			}

			if ((name == "enableColorBlending") ? (bool)oldValue : (bool)enableColorBlending.GetValue())
			{
				RedrawSettingsMenu("enableColorBlending", "indicatorColor" + optionsNew, "enableColorBlending",
					"indicatorColor" + optionsOld);
			}
			else
			{
				RedrawSettingsMenu("enableColorBlending", "indicatorColor" + optionsNew, "enableColorBlending",
					"indicatorColor1");
			}

			return;
		}

		if (name == "enableShipTemperature")
		{
			if ((bool)oldValue)
			{
				RedrawSettingsMenu("enableShipTemperature", "enableShipTemperature", "enableShipTemperature",
					"enableReactorOverload");
			}
			else
			{
				RedrawSettingsMenu("enableShipTemperature", "enableReactorOverload", "enableShipTemperature",
					"enableShipTemperature");
			}

			return;
		}

		if (name == "addWaterTank")
		{
			if ((bool)oldValue)
			{
				RedrawSettingsMenu("addWaterTank", "addWaterTank", "addWaterTank", "addWaterCooling");
			}
			else
			{
				RedrawSettingsMenu("addWaterTank", "addWaterCooling", "addWaterTank", "addWaterTank");
			}

			return;
		}

		if (CurrentPreset != SettingPresets.PresetName.Custom
			&& CurrentPreset != SettingPresets.PresetName.Random
			&& CurrentPreset.GetPresetSetting(name) != null
			&& !newValue.Equals(oldValue))
		{
			CurrentPreset = SettingPresets.PresetName.Custom;
			ModHelper.Config.SetSettingsValue("preset", CurrentPreset.GetName());
			RedrawSettingsMenu("preset", "preset");

			return;
		}

		if (name == "preset" && !newValue.Equals(oldValue))
		{
			var allSettings = Enum.GetValues(typeof(Settings)) as Settings[];
			var newPreset = (string)newValue;
			var oldPreset = (string)oldValue;

			CurrentPreset = (SettingPresets.GetPresetFromConfig(newPreset));
			ModHelper.Config.SetSettingsValue("preset", CurrentPreset.GetName());

			if (newPreset == "Custom" || newPreset == "Random")
			{
				WriteDebugMessage("Load");
				SettingExtensions.LoadCustomSettings();
				foreach (Settings setting in allSettings)
				{
					ModHelper.Config.SetSettingsValue(setting.GetName(), setting.GetValue());
				}
			}
			else if (oldPreset == "Custom" || oldPreset == "Random")
			{
				WriteDebugMessage("Save");
				SettingExtensions.SaveCustomSettings();
			}

			SettingPresets.ApplyPreset(SettingPresets.GetPresetFromConfig(newPreset), ModHelper.Config);
			foreach (Settings setting in allSettings)
			{
				setting.SetValue(ModHelper.Config.GetSettingsValue<object>(setting.GetName()));
			}

			if (newPreset == "Random" && oldPreset == "Custom")
			{
				RedrawSettingsMenu("preset", "randomDifficulty", "preset", "preset");
			}
			else if (newPreset == "Custom" && oldPreset == "Random")
			{
				RedrawSettingsMenu("preset", "preset", "preset", "randomDifficulty");
			}
			else
			{
				RedrawSettingsMenu();
			}

			return;
		}

		if (name == "repairWrenchType" && !newValue.Equals(oldValue))
		{
			RedrawSettingsMenu("repairWrenchType", "repairWrenchType");
		}
		
		if (name == "shipSignalType" && !newValue.Equals(oldValue))
		{
			RedrawSettingsMenu("shipSignalType", "shipSignalType");
		}
	}

	public static void RedrawSettingsMenu(string startSetting = "", string endSetting = "",
		string startDestroySetting = "", string endDestroySetting = "")
	{
		if (startDestroySetting == "")
		{
			startDestroySetting = startSetting;
		}

		if (endDestroySetting == "")
		{
			endDestroySetting = endSetting;
		}

		MenuManager menuManager = StartupPopupPatches.menuManager;
		IOptionsMenuManager OptionsMenuManager = menuManager.OptionsMenuManager;

		var menus = typeof(MenuManager).GetField("ModSettingsMenus", BindingFlags.Public
				| BindingFlags.NonPublic | BindingFlags.Static)?.GetValue(menuManager)
			as List<(IModBehaviour behaviour, Menu modMenu)>;

		if (menus == null) return;

		Menu newModTab = null;

		for (int i = 0; i < menus.Count; i++)
		{
			if ((object)menus[i].behaviour == Instance)
			{
				newModTab = menus[i].modMenu;
			}
		}

		if (newModTab == null) return;

		newModTab._menuOptions = [];

		Scrollbar scrollbar = newModTab.transform.Find("Scroll View/Scrollbar Vertical").GetComponent<Scrollbar>();
		float lastScrollValue = scrollbar.value;

		Transform settingsParent = newModTab.transform.Find("Scroll View/Viewport/Content");

		if (!DestroyExistingSettings(newModTab, settingsParent, startDestroySetting, endDestroySetting,
			out int insertionIndex))
		{
			return;
		}

		_detectValueChanged = false;

		if (startSetting == "")
		{
			CreateChangelogButton(newModTab, OptionsMenuManager);
			
			OptionsMenuManager.AddSeparator(newModTab, true);
			OptionsMenuManager.CreateLabel(newModTab, "Any changes to the settings are applied on the next loop!");
			OptionsMenuManager.AddSeparator(newModTab, true);
		}

		int startIndex = 0;
		int endIndex = ModHelper.Config.Settings.Count - 1;
		if (startSetting != "")
		{
			startIndex = ModHelper.Config.Settings.Keys.ToList().IndexOf(startSetting);
		}

		if (endSetting != "")
		{
			endIndex = ModHelper.Config.Settings.Keys.ToList().IndexOf(endSetting);
		}

		Dictionary<int, string> cachedNames = [];

		for (int i = startIndex; i < ModHelper.Config.Settings.Count; i++)
		{
			string name = ModHelper.Config.Settings.ElementAt(i).Key;

			if (ShouldHideSetting(i, name))
			{
				continue;
			}

			object setting = ModHelper.Config.Settings.ElementAt(i).Value;
			var settingType = GetSettingType(setting);
			var label = ModHelper.MenuTranslations.GetLocalizedString(name);
			var tooltip = "";

			var settingObject = setting as JObject;

			if (settingObject != default(JObject))
			{
				if (settingObject["dlcOnly"]?.ToObject<bool>() ?? false)
				{
					if (EntitlementsManager.IsDlcOwned() == EntitlementsManager.AsyncOwnershipStatus.NotOwned)
					{
						continue;
					}
				}

				if (settingObject["title"] != null)
				{
					if (!SetCustomSettingName(settingsParent, ref label, ref cachedNames, name))
					{
						label = ModHelper.MenuTranslations.GetLocalizedString(settingObject["title"].ToString());

						if (SettingExtensions.customObjLabels.ContainsKey(name))
						{
							string old = SettingExtensions.customObjLabels[name];
							for (int c = 0; c < settingsParent.childCount; c++)
							{
								if (settingsParent.GetChild(c).name == "UIElement-" + old)
								{
									var id = settingsParent.GetChild(c).GetInstanceID();
									if (!cachedNames.ContainsKey(id))
									{
										cachedNames.Add(id, "UIElement-" + label);
									}
								}
							}

							SettingExtensions.customObjLabels[name] = label;
						}
					}
				}

				if (settingObject["tooltip"] != null)
				{
					if (!SetCustomTooltip(ref tooltip, name))
					{
						tooltip = ModHelper.MenuTranslations.GetLocalizedString(settingObject["tooltip"].ToString());
					}
				}
			}

			if (endSetting != "" && i > endIndex)
			{
				for (int j = 0; j < settingsParent.childCount; j++)
				{
					if (settingsParent.GetChild(j).name == "UIElement-" + label)
					{
						MenuOption option = settingsParent.GetChild(j).GetComponentInChildren<MenuOption>();
						if (option != null)
						{
							newModTab._menuOptions = newModTab._menuOptions.Add(option);
						}
					}
				}

				continue;
			}

			switch (settingType)
			{
				case SettingType.CHECKBOX:
					var currentCheckboxValue = ModHelper.Config.GetSettingsValue<bool>(name);
					var settingCheckbox =
						OptionsMenuManager.AddCheckboxInput(newModTab, label, tooltip, currentCheckboxValue);
					settingCheckbox.ModSettingKey = name;
					settingCheckbox.OnValueChanged += (bool newValue) =>
					{
						var oldValue = ModHelper.Config.GetSettingsValue<bool>(name);
						ModHelper.Config.SetSettingsValue(name, newValue);
						ModHelper.Storage.Save(ModHelper.Config, Constants.ModConfigFileName);
						Instance.Configure(ModHelper.Config);
						OnValueChanged(name, oldValue, newValue);
					};
					break;
				case SettingType.TOGGLE:
					var currentToggleValue = ModHelper.Config.GetSettingsValue<bool>(name);
					var yes = settingObject["yes"].ToString();
					var no = settingObject["no"].ToString();
					var settingToggle =
						OptionsMenuManager.AddToggleInput(newModTab, label, yes, no, tooltip, currentToggleValue);
					settingToggle.ModSettingKey = name;
					settingToggle.OnValueChanged += (bool newValue) =>
					{
						var oldValue = ModHelper.Config.GetSettingsValue<bool>(name);
						ModHelper.Config.SetSettingsValue(name, newValue);
						ModHelper.Storage.Save(ModHelper.Config, Constants.ModConfigFileName);
						Instance.Configure(ModHelper.Config);
						OnValueChanged(name, oldValue, newValue);
					};
					break;
				case SettingType.SELECTOR:
					var currentSelectorValue = ModHelper.Config.GetSettingsValue<string>(name);
					var options = settingObject["options"].ToArray().Select(x => x.ToString()).ToArray();
					var currentSelectedIndex = Array.IndexOf(options, currentSelectorValue);
					var settingSelector = OptionsMenuManager.AddSelectorInput(newModTab, label, options, tooltip, true,
						currentSelectedIndex);
					settingSelector.ModSettingKey = name;
					settingSelector.OnValueChanged += (int newIndex, string newSelection) =>
					{
						var oldValue = ModHelper.Config.GetSettingsValue<string>(name);
						ModHelper.Config.SetSettingsValue(name, newSelection);
						ModHelper.Storage.Save(ModHelper.Config, Constants.ModConfigFileName);
						Instance.Configure(ModHelper.Config);
						OnValueChanged(name, oldValue, newSelection);
					};
					break;
				case SettingType.SEPARATOR:
					if (settingObject["title"] != null)
					{
						if (settingObject["title"].ToString() == "line")
						{
							OptionsMenuManager.AddSeparator(newModTab, true);
						}
						else if (settingObject["tooltip"] != null
							&& settingObject["tooltip"].ToString() == "side")
						{
							CreateSideLabel(newModTab, settingObject["title"].ToString());
						}
						else
						{
							OptionsMenuManager.AddSeparator(newModTab, true);
							OptionsMenuManager.CreateLabel(newModTab, label);
							//OptionsMenuManager.AddSeparator(newModTab, false);
						}
					}
					else
					{
						OptionsMenuManager.AddSeparator(newModTab, false);
					}

					break;
				case SettingType.SLIDER:
					var currentSliderValue = ModHelper.Config.GetSettingsValue<float>(name);
					var lower = settingObject["min"].ToObject<float>();
					var upper = settingObject["max"].ToObject<float>();
					var settingSlider =
						OptionsMenuManager.AddSliderInput(newModTab, label, lower, upper, tooltip, currentSliderValue);
					settingSlider.ModSettingKey = name;
					settingSlider.OnValueChanged += (float newValue) =>
					{
						var oldValue = ModHelper.Config.GetSettingsValue<float>(name);
						ModHelper.Config.SetSettingsValue(name, newValue);
						ModHelper.Storage.Save(ModHelper.Config, Constants.ModConfigFileName);
						Instance.Configure(ModHelper.Config);
						OnValueChanged(name, oldValue, newValue);
					};
					break;
				case SettingType.TEXT:
					var currentTextValue = ModHelper.Config.GetSettingsValue<string>(name);
					var textInput =
						OptionsMenuManager.AddTextEntryInput(newModTab, label, currentTextValue, tooltip, false);
					textInput.ModSettingKey = name;
					textInput.OnConfirmEntry += () =>
					{
						var oldValue = ModHelper.Config.GetSettingsValue<string>(name);
						var newValue = textInput.GetInputText();
						ModHelper.Config.SetSettingsValue(name, newValue);
						ModHelper.Storage.Save(ModHelper.Config, Constants.ModConfigFileName);
						Instance.Configure(ModHelper.Config);
						textInput.SetText(newValue);
						OnValueChanged(name, oldValue, newValue);
					};
					break;
				case SettingType.NUMBER:
					var currentValue = ModHelper.Config.GetSettingsValue<double>(name);
					var numberInput = OptionsMenuManager.AddTextEntryInput(newModTab, label,
						currentValue.ToString(CultureInfo.CurrentCulture), tooltip, true);
					numberInput.ModSettingKey = name;
					numberInput.OnConfirmEntry += () =>
					{
						if (!string.IsNullOrEmpty(numberInput.GetInputText()))
						{
							var oldValue = ModHelper.Config.GetSettingsValue<double>(name);
							var newValue = double.Parse(numberInput.GetInputText());
							ModHelper.Config.SetSettingsValue(name, newValue);
							ModHelper.Storage.Save(ModHelper.Config, Constants.ModConfigFileName);
							Instance.Configure(ModHelper.Config);
							numberInput.SetText(newValue.ToString());
							OnValueChanged(name, oldValue, newValue);
						}
					};
					break;
				default:
					WriteDebugMessage($"Couldn't generate input for unkown input type {settingType}", error: true);
					OptionsMenuManager.CreateLabel(newModTab, $"Unknown {settingType} : {name}");
					break;
			}

			if (startSetting != "")
			{
				if (insertionIndex >= 0)
				{
					var addedSetting = settingsParent.GetChild(settingsParent.childCount - 1);
					addedSetting.SetSiblingIndex(insertionIndex);
					insertionIndex++;

					if (GetDecorationSettings().Contains(name) && ShouldSplitDecoration(name))
					{
						OptionsMenuManager.AddSeparator(newModTab, false);
						var sep = settingsParent.GetChild(settingsParent.childCount - 1);
						sep.name = "UIElement-" + label;
						sep.SetSiblingIndex(insertionIndex);
						insertionIndex++;
					}
				}
			}
			else
			{
				if (GetDecorationSettings().Contains(name) && ShouldSplitDecoration(name))
				{
					OptionsMenuManager.AddSeparator(newModTab, false);
				}
			}
		}

		if (endSetting == "")
		{
			OptionsMenuManager.AddSeparator(newModTab, true);
			OptionsMenuManager.CreateLabel(newModTab,
				"Found a bug? Have an idea for a new setting?\nFeel free to come chat about it on the Outer Wilds Modding Discord server!");
			OptionsMenuManager.AddSeparator(newModTab, true);
		}

		if (newModTab._tooltipDisplay != null)
		{
			foreach (MenuOption option in newModTab.GetComponentsInChildren<MenuOption>(true))
			{
				option.SetTooltipDisplay(newModTab._tooltipDisplay);
			}
		}

		bool foundSelectable = false;
		newModTab._listSelectables = newModTab.GetComponentsInChildren<Selectable>(true);
		foreach (Selectable selectable in newModTab._listSelectables)
		{
			selectable.gameObject.GetAddComponent<Menu.MenuSelectHandler>().OnSelectableSelected +=
				newModTab.OnMenuItemSelected;

			if (newModTab._lastSelected != null
				&& selectable.gameObject.name == newModTab._lastSelected.gameObject.name)
			{
				SelectableAudioPlayer component = newModTab._selectOnActivate.GetComponent<SelectableAudioPlayer>();
				if (component != null)
				{
					component.SilenceNextSelectEvent();
				}

				Locator.GetMenuInputModule().SelectOnNextUpdate(selectable);
				foundSelectable = true;
			}
		}

		if (!foundSelectable && newModTab._selectOnActivate != null)
		{
			SelectableAudioPlayer component = newModTab._selectOnActivate.GetComponent<SelectableAudioPlayer>();
			if (component != null)
			{
				component.SilenceNextSelectEvent();
			}

			Locator.GetMenuInputModule().SelectOnNextUpdate(newModTab._selectOnActivate);
			newModTab._lastSelected = newModTab._selectOnActivate;
		}

		if (newModTab._setMenuNavigationOnActivate)
		{
			Menu.SetVerticalNavigation(newModTab, newModTab._menuOptions);
		}

		ModHelper.Events.Unity.FireInNUpdates(() => { scrollbar.value = lastScrollValue; }, 2);

		ModHelper.Events.Unity.FireInNUpdates(() => { _detectValueChanged = true; }, 5);
	}

	private static bool ShouldSplitDecoration(string name)
	{
		if (name is "enableColorBlending" or "shipGlassTexture") return true;
		
		/*if (name.Contains("Texture"))
		{
			string hull = name.Replace("Texture", "");
			if (!(bool)(hull + "Type").AsEnum<Settings>().GetValue())
			{
				return true;
			}
		}*/
		
		string stem = name.Substring(0, name.Length - 6);
		bool correctStem = _stemToSuffix.ContainsKey(stem) && stem != "indicator";

		if (!correctStem) return false;
		
		if (!(bool)enableColorBlending.GetValue() && name.Substring(name.Length - 1) == "1")
		{
			return true;
		}
		
		return name.Substring(name.Length - 1) == 
			(string)(stem + "ColorOptions").AsEnum<Settings>().GetValue();
	}
	
	private static void CreateChangelogButton(Menu modMenu, IOptionsMenuManager optionsManager)
    {
        SubmitAction submitAction;
        if (IsChangelogUpdated())
        {
            submitAction = optionsManager.CreateButton(modMenu, "View Changelog",
                "Opens a menu showing the release notes for each update.", MenuSide.CENTER);
        }
        else
        {
            submitAction = CreateButtonWithIndicator(modMenu, "View Changelog",
                "Opens a menu showing the release notes for each update.", MenuSide.CENTER);
        }
        
        submitAction.OnSubmitAction += () =>
        {
            _resetButtonCount = 0;
            
            var newTab = optionsManager.CreateStandardTab("CHANGELOG");
            var newMenu = newTab.menu;

            optionsManager.CreateLabel(newMenu, "Ship Enhancements - Release Notes");

            var returnButton = optionsManager.CreateButton(newMenu, "Close", 
                "Close the changelog to return to the Ship Enhancements settings menu.", MenuSide.CENTER);
            returnButton.OnSubmitAction += () =>
            {
                //optionsManager.OpenOptionsAtTab(modMenu);

                // Give time for the modsMenu to activate before switching tabs
                var modsMenu = newMenu.transform.parent.Find("Menu-MODS").GetComponent<TabbedMenu>();
                var seButton = modsMenu.transform
                    .Find("MenuMODS/Scroll View/Viewport/Content/UIElement-Ship Enhancements")
                    .GetComponentInChildren<SubmitAction>();
                ModHelper.Events.Unity.FireInNUpdates(() => seButton.Submit(), 2);
            };

            optionsManager.AddSeparator(newMenu, true);
            
            newMenu.OnActivateMenu += () =>
            {
                var settingsMenuView = UnityEngine.Object.FindObjectOfType<SettingsMenuView>();
                settingsMenuView._resetToDefaultsPrompt.SetText($"This button does nothing");
                settingsMenuView._resetToDefaultButton.RefreshTextAndImages(false);
                settingsMenuView._resetSettingsAction.OnSubmitAction += UpdateResetButtonPrompt;
            };

            newMenu.OnDeactivateMenu += () =>
            {
                var settingsMenuView = UnityEngine.Object.FindObjectOfType<SettingsMenuView>();
                settingsMenuView._resetSettingsAction.OnSubmitAction -= UpdateResetButtonPrompt;
            };

            var logText = NetworkFileHandler.GetChangelog().text;
            logText = Regex.Replace(logText, @"\*\*((.(?!\*\*))*[^*]?)\*\*", "<b>$1</b>");
            logText = Regex.Replace(logText, @"(\n?\r?)*(?<=\n|^)#\s(.*)\b(\n?\r?)*", "\n\n\n<size=36><b>$2</b></size>\n\n");
            logText = Regex.Replace(logText, @"(?<=\n|^)-\s(?!\s)", "\t\u2022  ");
            logText = Regex.Replace(logText, @"(?<=\n|^)\s\s-\s(?!\s)", "\t\t\u25E6  ");
            string[] logs = Regex.Split(logText, @"(?<=\n|^)---\b");
            
            // shave one off because of weird whitespace split at start
            for (int i = 1; i < logs.Length; i++)
            {
                if (string.IsNullOrWhiteSpace(logs[i]))
                {
                    continue;
                }
                
                var titleText = Regex.Match(logs[i], @"^.*\b");
                
                if (!IsChangelogUpdated() && titleText.Value == SaveData.LastChangelogVersion)
                {
                    CreateUnreadSeparator(newMenu);
                }
                
                logs[i] = Regex.Replace(logs[i], $@"({titleText})(\n?\r?)*", "");
                CreateSideLabel(newMenu, titleText.Value, 45);
                optionsManager.AddSeparator(newMenu, true);
                CreateTextField(newMenu, logs[i], 27, true);
            }

            newMenu.OnDeactivateMenu += () =>
            {
                // Fixes tab dissapearing when you click on it again
                // Clicking on a tab closes and opens it again
                ModHelper.Events.Unity.FireOnNextUpdate(() =>
                {
                    if (!newMenu._isActivated)
                    {
                        optionsManager.RemoveTab(newMenu);
                    }
                });
            };
            
            optionsManager.OpenOptionsAtTab(newTab.button);
            Locator.GetMenuAudioController().PlayChangeTab();
            
            TryUpdateChangelogVersion();
        };
    }

    private static void UpdateResetButtonPrompt()
    {
        _resetButtonCount++;
        
        string prompt;
        switch (_resetButtonCount)
        {
            case 2:
                prompt = "This button doesn't do anything";
                break;
            case 3:
                prompt = "Why are you pressing it";
                break;
            case 4:
                prompt = "Stop pressing it";
                break;
            case 5:
                prompt = "Stop";
                break;
            case 6:
                prompt = "You're not accomplishing anything";
                break;
            case 7:
                prompt = "You're just wasting your time";
                break;
            case 8:
                prompt = "What do you think is going to happen?";
                break;
            case 9:
                prompt = "You think you're gonna get a prize?";
                break;
            case 10:
                prompt = "There's no prize here for you";
                break;
            case 11:
                prompt = "So stop pressing it";
                break;
            case 12:
                prompt = "STOP";
                break;
            case 13:
            case 14:
            case 15:
            case 16:
            case 17:
            case 18:
                prompt = "";
                break;
            case 19:
            case 20:
                prompt = "...";
                break;
            case 21:
                prompt = "Alright, fine. You can have your prize";
                break;
            case 22:
                prompt = "If you press this button again, you'll get it";
                break;
            case 23:
                Application.Quit();
                return;
            default:
                prompt = "This button does nothing";
                break;
        }
        
        var settingsMenuView = UnityEngine.Object.FindObjectOfType<SettingsMenuView>();
        settingsMenuView._resetToDefaultsPrompt.SetText(prompt);
        settingsMenuView._resetToDefaultButton.RefreshTextAndImages();
    }

    private static SubmitAction CreateButtonWithIndicator(Menu menu, string buttonLabel, string tooltip, MenuSide side)
    {
        var rootObj = new GameObject($"UIElement-{buttonLabel}");
        var parent = menu.transform;

        if (menu.transform.Find("Scroll View") != null)
        {
            parent = menu.transform.Find("Scroll View").Find("Viewport").Find("Content");
        }

        if (menu.transform.Find("Content") != null)
        {
            parent = menu.transform.Find("Content");
        }

        rootObj.transform.parent = parent;
        rootObj.transform.localScale = Vector3.one;
        rootObj.transform.localRotation = Quaternion.identity;
        rootObj.transform.localPosition = Vector3.zero;

        var layoutElement = rootObj.AddComponent<LayoutElement>();
        layoutElement.minHeight = 70;
        layoutElement.flexibleWidth = 1;

        var existingHorizLayout = Resources.FindObjectsOfTypeAll<Menu>()
            .Single(x => x.name == "GraphicsMenu").transform
            .Find("Scroll View")
            .Find("Viewport")
            .Find("Content")
            .Find("UIElement-ResolutionSelect")
            .Find("HorizontalLayoutGroup").gameObject;

        var newHorizLayout = UnityEngine.Object.Instantiate(existingHorizLayout, rootObj.transform);
        newHorizLayout.name = "HorizontalLayoutGroup";
        newHorizLayout.transform.localPosition = Vector3.zero;
        newHorizLayout.transform.localScale = Vector3.one;
        newHorizLayout.transform.localRotation = Quaternion.identity;

        var hrt = newHorizLayout.GetComponent<RectTransform>();
        var ohrt = existingHorizLayout.GetComponent<RectTransform>();
        //hrt.anchorMin = ohrt.anchorMin;
        //hrt.anchorMax = ohrt.anchorMax;
        hrt.offsetMin = ohrt.offsetMin;
        hrt.offsetMax = ohrt.offsetMax;
        hrt.anchoredPosition3D = ohrt.anchoredPosition3D;
        hrt.sizeDelta = ohrt.sizeDelta;

        hrt.anchorMax = new Vector2(0.5f, 1f);
        switch (side)
        {
            case MenuSide.LEFT:
                hrt.anchorMin = new Vector2(0f, 1f);
                break;
            case MenuSide.CENTER:
                hrt.anchorMin = new Vector2(0.25f, 1f);
                break;
            case MenuSide.RIGHT:
                hrt.anchorMin = new Vector2(0.5f, 1f);
                break;
        }

        UnityEngine.Object.Destroy(newHorizLayout.GetComponentInChildren<LocalizedText>());

        var oldLabelComponent = newHorizLayout.transform
            .Find("LabelBlock")
            .Find("HorizontalLayoutGroup")
            .Find("Label")
            .GetComponent<Text>();
        var labelFont = Resources.FindObjectsOfTypeAll<Font>().First(x => x.name == "Adobe - SerifGothicStd-ExtraBold");
        var labelFontSize = 28;

        UnityEngine.Object.Destroy(newHorizLayout.transform.Find("LabelBlock").gameObject);

        var controlBlock = newHorizLayout.transform.Find("ControlBlock");
        UnityEngine.Object.Destroy(controlBlock.Find("OptionSelectorBG").gameObject);
        UnityEngine.Object.Destroy(controlBlock.Find("HorizontalLayoutGroup").gameObject);

        var existingButton = Resources.FindObjectsOfTypeAll<Button>().First(x => x.name == "UIElement-ButtonContinue");

        var newButtonObj = UnityEngine.Object.Instantiate(existingButton, controlBlock);
        newButtonObj.transform.localPosition = Vector3.zero;
        newButtonObj.transform.localScale = Vector3.one;
        newButtonObj.name = $"UIElement-Button-{buttonLabel}";
        newButtonObj.transform.localRotation = Quaternion.identity;

        UnityEngine.Object.Destroy(newButtonObj.transform.Find("ForegroundLayoutGroup/RightSpacer").gameObject);
        UnityEngine.Object.Destroy(newButtonObj.transform.Find("ForegroundLayoutGroup/LeftSpacer").gameObject);

        UnityEngine.Object.Destroy(newButtonObj.GetComponent<SubmitAction>());
        var submitAction = newButtonObj.gameObject.AddComponent<SubmitAction>();

        UnityEngine.Object.Destroy(newButtonObj.gameObject.GetComponentInChildren<LocalizedText>());

        var menuOption = newButtonObj.gameObject.GetAddComponent<MenuOption>();
        menuOption._tooltipTextType = UITextType.None;
        menuOption._overrideTooltipText = tooltip;
        menuOption._label = newButtonObj.GetComponentInChildren<Text>();
        menuOption._label.text = buttonLabel;
        menuOption._label.font = labelFont;
        menuOption._label.fontSize = labelFontSize;

        menu._menuOptions = menu._menuOptions.Add(menuOption);
        
        var textObj = new GameObject("Text");

        var text = textObj.AddComponent<Text>();
        text.text = "NEW!";
        text.font = labelFont;
        text.fontSize = 20;
        text.alignment = TextAnchor.MiddleRight;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Truncate;
        text.raycastTarget = false;

        var textLayoutElement = textObj.AddComponent<LayoutElement>();
        textLayoutElement.minHeight = 70;

        textObj.transform.parent = newButtonObj.transform;
        textObj.transform.localScale = Vector3.one;
        textObj.transform.localPosition = Vector3.zero;
        textObj.transform.localRotation = Quaternion.identity;

        var rect = textObj.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(550f, rect.sizeDelta.y);

        textObj.AddComponent<UIFlashingText>();

        if (menu._selectOnActivate == null)
        {
            menu._selectOnActivate = newButtonObj.GetComponent<Selectable>();
        }

        return submitAction;
    }

    private static GameObject CreateUnreadSeparator(Menu menu)
    {
        var separatorObj = new GameObject("separator");
        var layoutElement = separatorObj.AddComponent<LayoutElement>();
        layoutElement.flexibleWidth = 1;
        layoutElement.preferredHeight = 70;

        var parent = menu.transform;

        if (menu.transform.Find("Scroll View") != null)
        {
            parent = menu.transform.Find("Scroll View").Find("Viewport").Find("Content");
        }

        if (menu.transform.Find("Content") != null)
        {
            parent = menu.transform.Find("Content");
        }

        separatorObj.transform.parent = parent;
        separatorObj.transform.localScale = Vector3.one;

        /*var dotsSprite = Resources.FindObjectsOfTypeAll<TabbedSubMenu>()
            .Single(x => x.name == "GameplayMenu").transform
            .Find("MenuGameplayBasic/Scroll View/Viewport/Content")
            .Find("UIElement-ControllerProfile")
            .Find("HorizontalLayoutGroup")
            .Find("LabelBlock")
            .Find("LineBreak_Dots")
            .GetComponent<Image>()
            .sprite;*/
        
        var dotsImage = LoadAsset<Texture2D>("Assets/ShipEnhancements/UI_Menu_ChangelogUnreadSeparator.png");
        var dotsSprite = Sprite.Create(dotsImage, new Rect(0f, 0f, 1512f, 59f), new Vector2(0.5f, 0.5f));

        var imageObj = new GameObject("dots");
        imageObj.transform.parent = separatorObj.transform;
        imageObj.transform.localPosition = Vector3.zero;
        imageObj.transform.localScale = Vector3.one;

        var rt = imageObj.GetAddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0, 0.5f);
        rt.anchorMax = new Vector2(1, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(0, 29.25f);
        rt.offsetMin = new Vector2(0, rt.offsetMin.y);
        rt.offsetMax = new Vector2(0, rt.offsetMax.y);
        rt.anchoredPosition = new Vector2(0, 0);
        rt.localScale = Vector3.one;

        var image = imageObj.AddComponent<Image>();
        image.sprite = dotsSprite;
        //image.color = new Color(0.2196079f, 0.2196079f, 0.2196079f);
        image.type = Image.Type.Tiled;
        image.pixelsPerUnitMultiplier = 2f;
        image.preserveAspect = true;
        
        var labelImage = LoadAsset<Texture2D>("Assets/ShipEnhancements/UI_Menu_ChangelogUnreadLabel.png");
        var labelSprite = Sprite.Create(labelImage, new Rect(0f, 0f, 1512f, 59f), new Vector2(0.5f, 0.5f));

        var labelObj = UnityEngine.Object.Instantiate(imageObj, imageObj.transform);
        labelObj.name = "label";
        labelObj.transform.parent = separatorObj.transform;
        labelObj.transform.localPosition = Vector3.zero;
        labelObj.transform.localScale = Vector3.one;
        
        rt = labelObj.GetAddComponent<RectTransform>();
        rt.pivot = new Vector2(0f, 0.5f);
        
        var image2 = labelObj.GetComponent<Image>();
        image2.sprite = labelSprite;
        //image2.color = new Color(0.2196079f, 0.2196079f, 0.2196079f);
        image2.type = Image.Type.Simple;
        image2.pixelsPerUnitMultiplier = 2f;
        image2.preserveAspect = true;

        return separatorObj;
    }

    private static void CreateSideLabel(Menu menu, string label, int fontSize = 36)
    {
        var newObj = new GameObject("SideLabel");

        var layoutElement = newObj.AddComponent<LayoutElement>();
        layoutElement.flexibleWidth = 1;

        var verticalLayout = newObj.AddComponent<VerticalLayoutGroup>();
        verticalLayout.padding = new RectOffset(20, 180, 0, 0);
        verticalLayout.spacing = 0;
        verticalLayout.childAlignment = TextAnchor.MiddleLeft;
        verticalLayout.childForceExpandHeight = false;
        verticalLayout.childForceExpandWidth = false;
        verticalLayout.childControlHeight = true;
        verticalLayout.childControlWidth = true;
        verticalLayout.childScaleHeight = false;
        verticalLayout.childScaleWidth = false;

        var textObj = new GameObject("Text");

        var text = textObj.AddComponent<Text>();
        text.text = label;
        text.font = Resources.Load<Font>("fonts/english - latin/Adobe - SerifGothicStd");
        text.fontSize = fontSize;
        text.alignment = TextAnchor.MiddleLeft;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Truncate;

        var textLayoutElement = textObj.AddComponent<LayoutElement>();
        textLayoutElement.minHeight = 70;

        textObj.transform.parent = newObj.transform;
        textObj.transform.localScale = Vector3.one;
        textObj.transform.localPosition = Vector3.zero;
        textObj.transform.localRotation = Quaternion.identity;

        var parent = menu.transform;

        if (menu.transform.Find("Scroll View") != null)
        {
            parent = menu.transform.Find("Scroll View").Find("Viewport").Find("Content");
        }

        if (menu.transform.Find("Content") != null)
        {
            parent = menu.transform.Find("Content");
        }

        newObj.transform.parent = parent;
        newObj.transform.localScale = Vector3.one;
        newObj.transform.localPosition = Vector3.zero;
        newObj.transform.localRotation = Quaternion.identity;
    }

    private static void CreateTextField(Menu menu, string data, int fontSize = 36, bool dynamic = false)
    {
        var newObj = new GameObject("TextField");

        var layoutElement = newObj.AddComponent<LayoutElement>();
        layoutElement.flexibleWidth = 1;

        var verticalLayout = newObj.AddComponent<VerticalLayoutGroup>();
        verticalLayout.padding = new RectOffset(100, 100, 0, 0);
        verticalLayout.spacing = 0;
        verticalLayout.childAlignment = TextAnchor.MiddleLeft;
        verticalLayout.childForceExpandHeight = false;
        verticalLayout.childForceExpandWidth = false;
        verticalLayout.childControlHeight = true;
        verticalLayout.childControlWidth = true;
        verticalLayout.childScaleHeight = false;
        verticalLayout.childScaleWidth = false;

        var textObj = new GameObject("Text");

        var text = textObj.AddComponent<Text>();
        text.text = data;
        if (dynamic)
        {
            text.font = TextTranslation.GetFont(true);
        }
        else
        {
            text.font = Resources.Load<Font>("fonts/english - latin/Adobe - SerifGothicStd");
        }
        text.fontSize = fontSize;
        text.alignment = TextAnchor.MiddleLeft;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Truncate;

        var textLayoutElement = textObj.AddComponent<LayoutElement>();
        textLayoutElement.minHeight = 70;

        textObj.transform.parent = newObj.transform;
        textObj.transform.localScale = Vector3.one;
        textObj.transform.localPosition = Vector3.zero;
        textObj.transform.localRotation = Quaternion.identity;

        var parent = menu.transform;

        if (menu.transform.Find("Scroll View") != null)
        {
            parent = menu.transform.Find("Scroll View").Find("Viewport").Find("Content");
        }

        if (menu.transform.Find("Content") != null)
        {
            parent = menu.transform.Find("Content");
        }

        newObj.transform.parent = parent;
        newObj.transform.localScale = Vector3.one;
        newObj.transform.localPosition = Vector3.zero;
        newObj.transform.localRotation = Quaternion.identity;
    }

	private static bool DestroyExistingSettings(Menu menu, Transform parent, string startSetting, string endSetting,
		out int insertionIndex)
	{
		bool hasStart = startSetting != "";
		bool hasEnd = endSetting != "";
		if (hasStart || hasEnd)
		{
			string startTitle = "";
			if (hasStart)
			{
				var setting = ModHelper.Config.Settings[startSetting] as JObject;
				if (setting != default(JObject) && setting["title"] != null)
				{
					if (SettingExtensions.customObjLabels.ContainsKey(startSetting))
					{
						startTitle = SettingExtensions.customObjLabels[startSetting];
					}
					else
					{
						startTitle = setting["title"].ToString();
					}
				}
			}

			string endTitle = "";
			if (hasEnd)
			{
				var setting = ModHelper.Config.Settings[endSetting] as JObject;
				if (setting != default(JObject) && setting["title"] != null)
				{
					if (SettingExtensions.customObjLabels.ContainsKey(endSetting))
					{
						endTitle = SettingExtensions.customObjLabels[endSetting];
					}
					else
					{
						endTitle = setting["title"].ToString();
					}
				}
			}

			int startIndex = -1;
			int endIndex = -1;
			for (int i = 0; i < parent.childCount; i++)
			{
				if (parent.GetChild(i).name == "UIElement-" + startTitle)
				{
					startIndex = i;
				}

				if (startIndex < 0)
				{
					MenuOption option = parent.GetChild(i).GetComponentInChildren<MenuOption>();
					if (option != null)
					{
						menu._menuOptions = menu._menuOptions.Add(option);
					}

					continue;
				}

				if (parent.GetChild(i).name == "UIElement-" + endTitle)
				{
					endIndex = i;
				}

				UnityEngine.Object.Destroy(parent.GetChild(i).gameObject);

				if (endIndex > 0)
				{
					insertionIndex = startIndex;
					return startIndex >= 0;
				}
			}

			insertionIndex = startIndex;
			return startIndex >= 0;
		}

		for (int i = 0; i < parent.childCount; i++)
		{
			if (i < 2)
			{
				MenuOption option = parent.GetChild(i).GetComponentInChildren<MenuOption>();
				if (option != null)
				{
					menu._menuOptions = menu._menuOptions.Add(option);
				}
			}
			else
			{
				UnityEngine.Object.Destroy(parent.GetChild(i).gameObject);
			}
		}

		insertionIndex = -1;
		return true;
	}

	private static bool ShouldHideSetting(int currIndex, string name)
	{
		foreach (Settings hiddenSetting in HiddenSettings)
		{
			if (hiddenSetting.ToString() == name)
			{
				return true;
			}
		}

		if (HidePreset && name == "preset")
		{
			return true;
		}

		if (HidePreset || CurrentPreset != SettingPresets.PresetName.Random)
		{
			if (name == "randomIterations" || name == "randomDifficulty")
			{
				return true;
			}
		}

		if (GetDecorationSettings().Contains(name))
		{
			if (name.Contains("Texture"))
			{
				return false;
			}

			/*if (name.Contains("Hull"))
			{
				string hull = name.Substring(0, 12);
				bool usingColor = (bool)(hull + "Type").AsEnum<Settings>().GetValue();
				bool isTex = name.Substring(name.Length - 7) == "Texture";

				if ((isTex && usingColor) || (!isTex && !usingColor))
				{
					return true;
				}
				if (isTex && !usingColor)
				{
					return false;
				}
			}
			
			if (name.Contains("Wood"))
			{
				string wood = name.Substring(0, 12);
				bool usingColor = (bool)(wood + "Type").AsEnum<Settings>().GetValue();
				bool isTex = name.Substring(name.Length - 7) == "Texture";

				if ((isTex && usingColor) || (!isTex && !usingColor))
				{
					return true;
				}
				if (isTex && !usingColor)
				{
					return false;
				}
			}*/
			
			// if color blending is off, hide settings that don't end in 1
			if (name != "enableColorBlending" && !(bool)enableColorBlending.GetValue()
				&& (!int.TryParse(name.Substring(name.Length - 1), out int value) || value != 1))
			{
				return true;
			}

			// if color blending is on, hide color settings that don't match their color options
			if (name.Length >= 6)
			{
				string stem = name.Substring(0, name.Length - 6);
				if (_stemToSuffix.ContainsKey(stem))
				{
					Settings numSetting = (stem + "ColorOptions").AsEnum<Settings>();
					int num = int.Parse((string)numSetting.GetValue());
					if (int.Parse(name.Substring(name.Length - 1)) > num)
					{
						return true;
					}
				}
			}
		}

		if (GetTemperatureSettings().Contains(name) && name != "enableShipTemperature")
		{
			return !(bool)enableShipTemperature.GetValue();
		}

		if (GetWaterSettings().Contains(name) && name != "addWaterTank")
		{
			return !(bool)addWaterTank.GetValue();
		}

		return false;
	}

	private static bool SetCustomSettingName(Transform settingsParent, ref string label,
		ref Dictionary<int, string> cachedNames, string settingName)
	{
		bool custom = false;

		if (!(bool)enableColorBlending.GetValue()) return false;

		string stem = settingName.Substring(0, settingName.Length - 6);
		if (!_stemToSuffix.ContainsKey(stem))
		{
			return false;
		}

		Settings numSetting = (stem + "ColorOptions").AsEnum<Settings>();
		int num = int.Parse((string)numSetting.GetValue());
		if (num == 1)
		{
			return false;
		}

		int index = int.Parse(settingName.Substring(settingName.Length - 1));
		Settings blendSetting = (stem + "ColorBlend").AsEnum<Settings>();
		string blend = (string)blendSetting.GetValue();

		var found = _customSettingNames.Where(tuple => tuple.blendType == blend
			&& tuple.canShow(index, num));

		if (found.Count() > 0)
		{
			label = _stemToSuffix[stem] + " " + found.First().suffix;
			custom = true;
		}

		if (custom)
		{
			if (!SettingExtensions.customObjLabels.ContainsKey(settingName))
			{
				SettingExtensions.customObjLabels.Add(settingName, label);
			}
			else
			{
				string old = SettingExtensions.customObjLabels[settingName];
				for (int c = 0; c < settingsParent.childCount; c++)
				{
					if (settingsParent.GetChild(c).name == "UIElement-" + old)
					{
						var id = settingsParent.GetChild(c).GetInstanceID();
						if (!cachedNames.ContainsKey(id))
							cachedNames.Add(id, "UIElement-" + label);
					}
				}

				SettingExtensions.customObjLabels[settingName] = label;
			}

			return true;
		}

		return false;
	}

	private static bool SetCustomTooltip(ref string tooltip, string settingName)
	{
		if (settingName == "preset")
		{
			if (CurrentPreset == SettingPresets.PresetName.VanillaPlus)
			{
				tooltip =
					"Vanilla Plus is the default preset. It turns everything off except for some Quality of Life features.";
			}
			else if (CurrentPreset == SettingPresets.PresetName.Minimal)
			{
				tooltip = "The Minimal preset disables anything related to the ship that you could consider useful.";
			}
			else if (CurrentPreset == SettingPresets.PresetName.Impossible)
			{
				tooltip =
					"The Impossible preset doesn't add or disable anything, but it changes the ship to be as annoying as possible.";
			}
			else if (CurrentPreset == SettingPresets.PresetName.NewStuff)
			{
				tooltip = "The New Stuff preset gives the ship a ton of new features that it doesn't normally have.";
			}
			else if (CurrentPreset == SettingPresets.PresetName.Pandemonium)
			{
				tooltip = "The Pandemonium preset just turns everything on. Good luck.";
			}
			else if (CurrentPreset == SettingPresets.PresetName.Random)
			{
				tooltip =
					"The Random preset randomizes the mod settings each loop. You can customize the randomizer by using the two sliders below or by using the RandomizerSettings.json file in the mod folder.";
			}
			else if (CurrentPreset == SettingPresets.PresetName.Custom)
			{
				tooltip = "No preset is selected. Customize your ship to your heart's desire.";
			}

			return true;
		}

		if (settingName == "repairWrenchType" && (string)repairWrenchType.GetValue() != "Disabled")
		{
			if ((string)repairWrenchType.GetValue() == "Enabled")
			{
				tooltip = "Adds a repair wrench to the cockpit. Holding it will speed up ship repairs.";
			}
			else
			{
				tooltip =
					"Adds a repair wrench to the cockpit. You need to be holding the wrench to make repairs to the ship.";
			}

			return true;
		}
		
		if (settingName == "shipSignalType" && (string)shipSignalType.GetValue() != "Disabled")
		{
			if ((string)shipSignalType.GetValue() == "Simple")
			{
				tooltip = "Simple mode will just add a regular signal to the ship that you can use to track it.";
			}
			else
			{
				tooltip = "Advanced mode will add a signal to the ship that can be used to send commands. To do this, focus on the signal using your signalscope and interact with it to open the command menu.";
			}
			return true;
		}

		if (settingName.Substring(settingName.Length - 5, 5) != "Blend")
		{
			return false;
		}

		Settings blendSetting = settingName.AsEnum<Settings>();
		tooltip = _customTooltips[(string)blendSetting.GetValue()];
		return true;
	}

	private static SettingType GetSettingType(object setting)
	{
		var settingObject = setting as JObject;

		if (setting is bool || (settingObject != null && settingObject["type"].ToString() == "toggle" &&
			(settingObject["yes"] == null || settingObject["no"] == null)))
		{
			return SettingType.CHECKBOX;
		}
		else if (setting is string || (settingObject != null && settingObject["type"].ToString() == "text"))
		{
			return SettingType.TEXT;
		}
		else if (setting is int || setting is long || setting is float || setting is double || setting is decimal ||
			(settingObject != null && settingObject["type"].ToString() == "number"))
		{
			return SettingType.NUMBER;
		}
		else if (settingObject != null && settingObject["type"].ToString() == "toggle")
		{
			return SettingType.TOGGLE;
		}
		else if (settingObject != null && settingObject["type"].ToString() == "selector")
		{
			return SettingType.SELECTOR;
		}
		else if (settingObject != null && settingObject["type"].ToString() == "slider")
		{
			return SettingType.SLIDER;
		}
		else if (settingObject != null && settingObject["type"].ToString() == "separator")
		{
			return SettingType.SEPARATOR;
		}

		WriteDebugMessage(
			$"Couldn't work out setting type. Type:{setting.GetType().Name} SettingObjectType:{settingObject?["type"].ToString()}",
			error: true);
		return SettingType.NONE;
	}

	private enum SettingType
	{
		NONE,
		CHECKBOX,
		TOGGLE,
		TEXT,
		NUMBER,
		SELECTOR,
		SLIDER,
		SEPARATOR
	}
}