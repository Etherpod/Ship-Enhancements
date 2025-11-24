using ShipEnhancements.Settings;

namespace ShipEnhancements;

[SESetting("OxygenDrainMultiplier"    , typeof(float), SESettingType.Number  , 1)]
[SESetting("FuelDrainMultiplier"      , typeof(float), SESettingType.Number  , 1)]
[SESetting("ShipDamageMultiplier"     , typeof(float), SESettingType.Number  , 1)]
[SESetting("ShipDamageSpeedMultiplier", typeof(float), SESettingType.Slider  , 1)]
[SESetting("ShipOxygenRefill"         , typeof(int)  , SESettingType.Number  , 1)]
[SESetting("DisableGravityCrystal"    , typeof(bool) , SESettingType.Checkbox, false)]
[SESetting("DisableShipLights"        , typeof(bool) , SESettingType.Checkbox, false)]
[SESetting("DisableShipOxygen"        , typeof(bool) , SESettingType.Toggle  , false)]
[SESetting("EnableGravityLandingGear" , typeof(bool) , SESettingType.Number  , true)]
[SESetting("DisableAirAutoRoll"       , typeof(bool) , SESettingType.Number  , false)]
public static partial class SESettings;
