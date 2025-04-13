using static ShipEnhancements.ShipEnhancements.Settings;

namespace ShipEnhancements;

public static class ErnestoDetectiveController
{
    private static string _hypothesis;
    private static string _reactorCause;
    private static bool _reachedConclusion;

    public static void Initialize()
    {
        _hypothesis = "I got no clue. Maybe it just felt like breaking today.";
        _reachedConclusion = false;
    }

    public static string GetHypothesis()
    {
        return _hypothesis;
    }

    public static void ItWasTemperatureDamage(bool hot)
    {
        if (_reachedConclusion) return;

        if (hot)
        {
            _hypothesis = "Your ship got too hot and it broke apart. What were you thinking leaving it in a place like that?";
        }
        else
        {
            _hypothesis = "Your ship got too cold and broke apart. What were you thinking leaving it in a place like that?";
        }
        _reachedConclusion = true;
    }

    public static void ItWasAnglerfish(bool hullBreach = false)
    {
        if (_reachedConclusion) return;

        if (hullBreach)
        {
            _hypothesis = "An anglerfish ate your ship. It broke one of the hulls off, so your ship probably isn't flying again.";
        }
        else
        {
            _hypothesis = "An anglerfish ate your ship. The electronics couldn't handle the acidity levels so the ship decided to stop working.";
        }
        _reachedConclusion = true;
    }

    public static void ItWasBrokenWarp()
    {
        if (_reachedConclusion) return;

        _hypothesis = "One of the hulls snapped off because you warped with a broken warp core. I'm surprised it warped at all, actually.";
        _reachedConclusion = true;
    }

    public static void ItWasFuelTank(bool temperature = false, bool impact = false, bool chain = false)
    {
        if (_reachedConclusion) return;

        if (temperature)
        {
            _hypothesis = "You know that fuel tank you have sitting around? Yeah, it blew up. Keep it away from hot places next time.";
        }
        else if (impact)
        {
            _hypothesis = "You know that fuel tank you have sitting around? It took a pretty hard hit from something and blew up, so maybe be more careful with it next time.";
        }
        else if (chain)
        {
            _hypothesis = "The ship got hit by some sort of explosion, I think. Whatever it was, it blew up that fuel tank you have laying around, and things just went downhill from there.";
        }
        _reachedConclusion = true;
    }

    public static void ItWasFluidDamage()
    {
        if (_reachedConclusion) return;

        _hypothesis = "Your ship really didn't appreciate being swarmed by tiny particles, so it broke apart.";
        _reachedConclusion = true;
    }

    public static void ItWasExplosionDamage()
    {
        if (_reachedConclusion) return;

        _hypothesis = "Your ship got hit by some sort of explosion. It knocked one of the hulls clean off.";
        _reachedConclusion = true;
    }

    public static void SetReactorCause(string cause)
    {
        _reactorCause = cause;
    }

    public static void ItWasExplosion(bool fromReactor = false, bool fromSpeed = false, bool fromOverdrive = false, bool sabotage = false,
        bool fromTorque = false, bool fromFluid = false, bool fromErnesto = false, bool fromTemperature = false)
    {
        if (_reachedConclusion) return;

        if (fromReactor)
        {
            if (_reactorCause == "overdrive")
            {
                _hypothesis = "The reactor blew up after you damaged it with the overdrive. You should probably be more careful with your ship.";
            }
            else if (_reactorCause == "campfire")
            {
                _hypothesis = "You lit a fire inside the ship, that's what happened.";
            }
            else if (_reactorCause == "electricity")
            {
                _hypothesis = "You zapped your ship one too many times and the reactor blew up. What? Why do you look so shocked?";
            }
            else if (_reactorCause == "temperature_hot")
            {
                _hypothesis = "Your ship got too hot, reactor was set off, and the rest is history. Don't you think the reactor was hot enough already?";
            }
            else if (_reactorCause == "temperature_cold")
            {
                _hypothesis = "Your ship got too cold, reactor was damaged, and the rest is history. Did you really think you could cool down the reactor?";
            }
            else if (_reactorCause == "anglerfish")
            {
                _hypothesis = "An anglerfish tried to eat your ship and damaged the reactor. You can guess what happened next.";
            }
            else if (_reactorCause == "warp")
            {
                _hypothesis = "Your reactor got damaged when you warped and it blew up the ship. Next time fix the warp core before warping.";
            }
            else if (_reactorCause == "fluid")
            {
                _hypothesis = "Did you forget your ship is afraid of tiny particles? You left it sitting in something for too long and the reactor got damaged, and then the ship blew up.";
            }
            else
            {
                if (ShipEnhancements.Instance.engineOn && !(bool)disableDamageIndicators.GetProperty())
                {
                    _hypothesis = "The reactor overheated and blew up the ship. There was an alarm going off and everything, it was kinda cool.";
                }
                else
                {
                    _hypothesis = "The reactor got too hot and blew up. It's kind of strange that they put a bomb in your ship, don't you think?";
                }
            }
        }
        else if (fromSpeed)
        {
            _hypothesis = "The ship crashed into something and it blew up. Couldn't they have made the ship's hull a little stronger?";
        }
        else if (fromOverdrive)
        {
            _hypothesis = "Why are you asking me? You're the one who tried to use the overdrive when the reactor was damaged.";
        }
        else if (sabotage)
        {
            _hypothesis = "Don't act all innocent. I know what you did.";
        }
        else if (fromTorque)
        {
            _hypothesis = "It spun way faster than any ship should be able to spin. I'm surprised it held on for as long as it did.";
        }
        else if (fromFluid)
        {
            _hypothesis = "Your ship touched something it didn't like, so it blew up. Doesn't get much simpler than that.";
        }
        else if (fromErnesto)
        {
            _hypothesis = "You rolled a 1. You said so yourself.";
        }
        else if (fromTemperature)
        {
            _hypothesis = "Your ship got a little uncomfortable with the temperature, so it decided to blow up and call it a day.";
        }
        else
        {
            _hypothesis = "The ship blew up, I don't know what else to tell you. Maybe the reactor was just having a bad day.";
        }
        _reachedConclusion = true;
    }

    public static void ItWasHullBreach(bool ejected = false, bool sabotage = false, bool impact = false)
    {
        if (_reachedConclusion) return;

        if (ejected)
        {
            _hypothesis = "I think you might have activated one of those eject buttons by accident. Don't worry, I do that sometimes too.";
        }
        else if (sabotage)
        {
            _hypothesis = "Someone must have detached one of the hulls on purpose. It wasn't you, right?";
        }
        else if (impact)
        {
            _hypothesis = "Your ship ran into something and broke apart. Kind of a boring way to go out, if you ask me.";
        }
        else
        {
            _hypothesis = "One of the hulls completely broke off, no idea what caused it. Maybe you should try asking the ship.";
        }
        _reachedConclusion = true;
    }
}
