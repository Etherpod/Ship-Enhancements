using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json.Linq;
using OWML.Common;
using static ShipEnhancements.ShipEnhancements;
using static ShipEnhancements.ShipEnhancements.Settings;
using UnityEngine;
using OWML.ModHelper.Menus.NewMenuSystem;
using OWML.ModHelper;
using OWML.Utils;
using UnityEngine.UI;

namespace ShipEnhancements.ModMenu;

public static class SEMenuManager
{
	public static SettingsPresets.PresetName CurrentPreset { get; set; } = (SettingsPresets.PresetName)(-1);
	public static List<Settings> HiddenSettings { get; private set; } = [];
	public static bool HidePreset { get; set; } = false;

	private static readonly IModHelper ModHelper = Instance.ModHelper;

	private static bool _detectValueChanged = true;

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
		{ "interiorHull", "Interior Color" },
		{ "exteriorHull", "Exterior Color" },
		{ "thruster", "Thruster Color" },
		{ "indicator", "Indicator Color" }
	};

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

		if (GetDecorationSettings().Contains(name)
			&& !int.TryParse(name.Substring(name.Length - 1), out _))
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

		if (CurrentPreset != SettingsPresets.PresetName.Custom
			&& CurrentPreset != SettingsPresets.PresetName.Random
			&& CurrentPreset.GetPresetSetting(name) != null
			&& !newValue.Equals(oldValue))
		{
			CurrentPreset = SettingsPresets.PresetName.Custom;
			ModHelper.Config.SetSettingsValue("preset", CurrentPreset.GetName());
			RedrawSettingsMenu("preset", "preset");

			return;
		}

		if (name == "preset" && !newValue.Equals(oldValue))
		{
			var allSettings = Enum.GetValues(typeof(Settings)) as Settings[];
			var newPreset = (string)newValue;
			var oldPreset = (string)oldValue;

			CurrentPreset = (SettingsPresets.GetPresetFromConfig(newPreset));
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

			SettingsPresets.ApplyPreset(SettingsPresets.GetPresetFromConfig(newPreset), ModHelper.Config);
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
		if (name == "enableColorBlending") return true;
		
		if (name.Contains("HullTexture"))
		{
			string hull = name.Replace("Texture", "");
			if (!(bool)(hull + "Type").AsEnum<Settings>().GetValue())
			{
				return true;
			}
		}
		
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

	private static void CreateSideLabel(Menu menu, string label)
	{
		var newObj = new GameObject("Label");

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
		text.fontSize = 36;
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

		if (HidePreset || CurrentPreset != SettingsPresets.PresetName.Random)
		{
			if (name == "randomIterations" || name == "randomDifficulty")
			{
				return true;
			}
		}

		if (GetDecorationSettings().Contains(name))
		{
			if (name.Contains("teriorHullType"))
			{
				return false;
			}

			if (name.Contains("Hull"))
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
			if (CurrentPreset == SettingsPresets.PresetName.VanillaPlus)
			{
				tooltip =
					"Vanilla Plus is the default preset. It turns everything off except for some Quality of Life features.";
			}
			else if (CurrentPreset == SettingsPresets.PresetName.Minimal)
			{
				tooltip = "The Minimal preset disables anything related to the ship that you could consider useful.";
			}
			else if (CurrentPreset == SettingsPresets.PresetName.Impossible)
			{
				tooltip =
					"The Impossible preset doesn't add or disable anything, but it changes the ship to be as annoying as possible.";
			}
			else if (CurrentPreset == SettingsPresets.PresetName.NewStuff)
			{
				tooltip = "The New Stuff preset gives the ship a ton of new features that it doesn't normally have.";
			}
			else if (CurrentPreset == SettingsPresets.PresetName.Pandemonium)
			{
				tooltip = "The Pandemonium preset just turns everything on. Good luck.";
			}
			else if (CurrentPreset == SettingsPresets.PresetName.Random)
			{
				tooltip =
					"The Random preset randomizes the mod settings each loop. You can customize the randomizer by using the two sliders below or by using the RandomizerSettings.json file in the mod folder.";
			}
			else if (CurrentPreset == SettingsPresets.PresetName.Custom)
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