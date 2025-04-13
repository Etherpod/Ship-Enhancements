namespace ShipEnhancements;

public static class SEAchievementTracker
{
    public static bool TorqueExplosion;
    public static bool DeadInTheWater;
    public static bool FireHazard;
    public static bool ScoutLostConnection;
    public static bool BlackHole;
    public static bool BadInternet;
    public static bool HowDidWeGetHere;
    public static bool HulkSmash;
    public static bool AnglerfishKill;

    public static void Reset()
    {
        TorqueExplosion = false;
        DeadInTheWater = false;
        FireHazard = false;
        ScoutLostConnection = false;
        BlackHole = false;
        BadInternet = false;
        HowDidWeGetHere = false;
        HulkSmash = false;
        AnglerfishKill = false;
    }
}
