Ship Enhancements makes numerous changes to the ship that are all toggleable in the options menu.

# Feature Overview

## Disable Ship Parts
- **Gravity Crystal**: Turns your ship into a Zero-G environment.
- **Eject Button**: Intended for multiplayer, which this mod does not currently have support for.
- **Headlights**: Disables your ship's headlights and landing camera light. You may need to rely on your scout for light.
- **Landing Camera**: Good luck.
- **Interior/Exterior Lights**: Basically enables dark mode.
- **Scout Launcher**: Disables your ship's scout launcher, meaning you can't launch or recall the scout with your ship.
- **Oxygen**: Literally removes all of the oxygen in the ship unless you're near trees or in an oxygen-filled atmosphere. This takes priority over all of the other oxygen modifications.
- **Ship Repair**: Be careful not to damage your ship.
- **Auto Roll**: Lets you fly around upside down in air and water, which is really confusing when paired with no ship gravity.
- **Lock-on**: Removes your ability to lock on to objects. You may find it difficult to use autopilot with this enabled.
- **Map markers**: Hides the markers that tell you the location of your ship, your scout, and any marked ship log locations. Ship log locations inside the Stranger are left visible so that you can actually find the Stranger.

## Adjust Ship Functions
- **Oxygen Drain Multiplier**: Larger numbers make your ship's oxygen drain faster. Set it to something like 100 for it to completely drain after being in the ship for half the loop or so.
- **Fuel Drain Multiplier**: Larger numbers make your ship's fuel drain faster.
- **Oxygen Tank Drain Multiplier**: Larger numbers increase the amount of oxygen that drains when the ship's oxygen tank is damaged.
- **Fuel Tank Drain Multiplier**: Larger numbers increase the amount of fuel that drains when the ship's fuel tank is damaged.
- **Damage Multiplier**: Larger numbers increase the damage done to the ship from impacts.
- **Damage Speed Multiplier**: Larger numbers increase the speed you need to be traveling at before taking damage. Set this to something really small to blow up your ship when you touch something.
- **Temperature Damage Multiplier**: Larger numbers increase the damage done to your ship inside high or low temperature areas.
- **Ship Gravity Multiplier**: Larger numbers increase the strength of your ship's gravity crystal. Large numbers are advised against, however, as exiting your ship will kill you.
- **Ship Gravity Direction**: Lets you change the direction of your ship's gravity.
- **Fuel Transfer Multiplier**: Larger numbers increase the amount of fuel that is subtracted when refueling your jetpack, and increase the amount of fuel that is added when transferring fuel to the ship.
- **Oxygen Refill Multiplier**: Larger numbers increase the speed at which your ship's oxygen tank refills when near an oxygen source.
- **Temperature Resistance Multiplier**: Larger numbers increase the time it takes for your ship to start taking damage in high or low temperature areas.
- **Angular Drag Multiplier**: Larger numbers make it harder to turn your ship. Setting this to 0 lets you spin your ship forever.
- **Disable Space Angular Drag**: Disables angular drag in space, meaning your ship will spin forever. The angular drag multiplier will still affect atmospheres.
- **Disable Rotation Speed Limit**: Lets you spin your ship as quickly as you want, though spinning too fast may tear your ship apart. Enabling this may lead to motion sickness.
- **Ship Explosion Multiplier**: Larger numbers increase the size of the explosion that is created when your ship blows up. Set this to something super high to simulate an atomic bomb.

## Add Ship Functions
- **Oxygen Refill**: Refills the ship's oxygen tank when it gets close to trees or enters an oxygen-filled atmosphere.
- **Fuel Transfer**: Lets you transfer your jetpack fuel to the ship's fuel tank to refill it.
- **Jetpack Refuel Drain**: Drains the ship's fuel reserve when you refuel your jetpack. The amount drained is the same as the amount gained if you transfer your fuel back to the ship.
- **Add Scout Launcher Component**: Turns the ship's scout launcher into a fully functioning ship component. If you damage it, you won't be able to launch your scout from the ship until you repair it.
- **Manual Scout Recall**: Takes you on a nostalgia trip back to the times when scouts didn't have warping technology. If you shoot your scout somewhere, you'll have to go over and pick it up to use it again. If you accidentally launch it into deep space, don't worry; the ship can still recall it.
- **Automatic Hatch**: Automatically closes the hatch when you leave the ship.
- **Ship Item Placement**: Lets you drop items inside of your ship to bring with you. Live out your dreams of item hoarding and collect as many scrolls as you can!
- **Gravity Landing Gear**: Equips your ship's landing gear with the latest gravity-powered technology, letting you stick to surfaces if your landing gear is intact.
- **Thrust Modulator**: Adds an interface to the cockpit that lets you lower the maximum thrust your ship can use. Stacks with the smooth thrust option from General Enhancements.
- **Temperature Zones**: Implements a temperature mechanic for the unused temperature dial in the ship. High and low temperature zones are added around the solar system that increase or decrease your ship's temperature.
- **Hull Temperature Damage**: Damages your ship over time in extreme temperatures, like near the Sun or on the dark side of the Interloper. Your temperature dial will start flashing if it's hot or cold enough to take damage.
- **Component Temperature Damage**: Similar to hull temperature damage, but it damages random components instead.
- **Add Portable Campfire**: Adds a portable campfire to the ship next to your suit. Place this anywhere you'd like and roast a marshmallow. Recommended to use with **Keep Helmet On**.

## Quality of Life
These are all enabled by default.
- **Keep Helmet On**: When there is no oxygen present, this prevents the game from removing your helmet when doing things such as piloting the ship or roasting a marshmallow at a campfire.
- **Show Warning Notifications**: Notifies you in the ship when resources are low or when the outside conditions are becoming dangerous.

## Presets
In case there are too many settings to deal with, there are some pre-made presets for you to use. The settings can still be meddled with after choosing one.

- **Vanilla** - The classic Outer Wilds experience.
- **Minimal** - What if Slate didn't add all those extra things to your ship? This is that reality.
- **Relaxed** - Gives you more resources and less damage.
- **Hardcore** - A much more difficult and punishing experience. Resources are lower and you are no longer able to repair damage done to your ship.
- **Wanderer** - Makes the possibility of getting lost very, very likely. Your ship's lock-on system is broken and you can no longer see the markers for your ship and scout.
- **Pandemonium** - Completely unfair. You take way too much damage and have way too little resources.

## API
- **CreateTemperatureZone()** - In case you're a modder and want to add your own high/low temperature zones. Temperature zones can stack, and will just take the sum of the temperatures.

Credit to Ditzy for helping with some of the code!