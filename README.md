Ship Enhancements makes numerous changes to the ship that are all toggleable in the options.

## Disable Ship Parts
- Gravity Crystal
- Eject Button
- Headlights
- Landing Camera
- Interior/Exterior Lights: Basically enables dark mode.
- Oxygen: Literally removes all of the oxygen in the ship unless you're near trees or in an oxygen-filled atmosphere. This takes priority over all of the other oxygen modifications.
- Ship Repair: Be careful not to damage your ship.
- Auto Roll: Lets you fly around upside down in air and water, which is really confusing when paired with no ship gravity.

## Adjust Ship Functions
- Oxygen Drain Multiplier: Larger numbers make your ship's oxygen drain faster. Set it to something like 100 for it to completely drain after being in the ship for half the loop or so.
- Fuel Drain Multiplier: Larger numbers make your ship's fuel drain faster.
- Ship Damage Multiplier: Larger numbers increase the damage done to the ship from impacts and temperature (if temperature damage is enabled).
- Ship Damage Speed Multiplier: Larger numbers increase the speed you need to be traveling at before taking damage. Set this to something really small to blow up your ship when you touch something.

## Add Ship Functions
- Oxygen Refill: Refills the ship's oxygen tank when it gets close to trees or enters an oxygen-filled atmosphere.
- Fuel Transfer: Lets you transfer your jetpack fuel to the ship's fuel tank to refill it.
- Jetpack Refuel Drain: Drains the ship's fuel reserve when you refuel your jetpack. The amount drained is the same as the amount gained if you transfer your fuel back to the ship.
- Gravity Landing Gear: Equips your ship's landing gear with the latest gravity-powered technology, letting you stick to surfaces if your landing gear is intact.
- Thrust Modulator: Adds an interface to the cockpit that lets you lower the maximum thrust your ship can use. Stacks with the smooth thrust option from General Enhancements.
- Temperature Zones: Implements a temperature mechanic for the unused temperature dial in the ship. High and low temperature zones are added around the solar system that increase or decrease your ship's temperature.
- Temperature Damage: Damages your ship over time in extreme temperatures, like near the Sun or on the dark side of the Interloper. Your temperature dial will start flashing if it's hot or cold enough to take damage.

## API
- CreateTemperatureZone() - In case you're a modder and want to add your own high/low temperature zones. Temperature zones can stack, and will just take the sum of the temperatures.