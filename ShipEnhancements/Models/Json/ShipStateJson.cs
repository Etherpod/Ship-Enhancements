using System.Collections.Generic;
using Newtonsoft.Json;
using ShipEnhancements.Buttons;

namespace ShipEnhancements.Models.Json;

public record ShipStateJson(
	[JsonProperty] float ShipFuel,
	[JsonProperty] float ShipOxygen,
	[JsonProperty] float ShipWater,
	[JsonProperty] Dictionary<string, float> HullIntegrities,
	[JsonProperty] Dictionary<string, float> ComponentIntegrities,
	[JsonProperty] List<string> DetachedHullPaths,
	[JsonProperty] bool HeadlightsOn,
	[JsonProperty] bool ShipVanished,
	[JsonProperty] CockpitButtonPanel.ButtonStates ButtonStates,
	[JsonProperty] List<string> EmptySocketPaths,
	[JsonProperty] Dictionary<string, object> ActiveSettings
);