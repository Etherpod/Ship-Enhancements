﻿using System;

namespace ShipEnhancements;

public static class AchievementTracker
{
    public static bool TorqueExplosion;
    public static bool DeadInTheWater;
    public static bool FireHazard;
    public static bool ScoutLostConnection;
    //public static bool RGBSetup;
    public static bool BadInternet;
    public static bool HowDidWeGetHere;

    public static void Reset()
    {
        TorqueExplosion = false;
        DeadInTheWater = false;
        FireHazard = false;
        ScoutLostConnection = false;
        BadInternet = false;
        HowDidWeGetHere = false;
    }
}
