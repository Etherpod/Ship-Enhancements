using Newtonsoft.Json;

namespace ShipEnhancements.Models.Json;

public record ExperimentalSettingsJson(
    [JsonProperty] bool UnrestrictedItemPlacement,
    [JsonProperty] bool FuelCanister_RemoteActivation,
    [JsonProperty] float FuelCanister_MaxFuel,
    [JsonProperty] bool ResourcePump_RemoteActivation,
    [JsonProperty] bool ResourcePump_UltraThrust,
    [JsonProperty] float ResourcePump_ThrustStrength,
    [JsonProperty] float ResourcePump_SignalRange,
    [JsonProperty] float ResourcePump_TransferMultiplier,
    [JsonProperty] bool TractorBeam_MakeTurboInverse,
    [JsonProperty] float TractorBeam_SpeedMultiplier,
    [JsonProperty] float Tether_MaxLength,
    [JsonProperty] float Tether_ReelMultiplier,
    [JsonProperty] float ThrustModulator_OverdriveStrength,
    [JsonProperty] float Eject_SpeedMultiplier,
    [JsonProperty] bool RealisticClock,
    [JsonProperty] bool MakeWaterDamageEverythingDamage,
    [JsonProperty] bool QuantumShip
);