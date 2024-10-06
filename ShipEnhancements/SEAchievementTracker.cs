namespace ShipEnhancements;

public static class SEAchievementTracker
{
    public static bool TorqueExplosion;
    public static bool DeadInTheWater;
    public static bool FireHazard;
    public static bool ScoutLostConnection;
    //public static bool RGBSetup;
    public static bool BadInternet;
    public static bool HowDidWeGetHere;
    public static bool HulkSmash;

    public static OWRigidbody LastHitBody;
    public static bool PlayerCausedExplosion = false;
    public static bool ShipExploded = false;

    public static void Reset()
    {
        TorqueExplosion = false;
        DeadInTheWater = false;
        FireHazard = false;
        ScoutLostConnection = false;
        BadInternet = false;
        HowDidWeGetHere = false;
        HulkSmash = false;

        LastHitBody = null;
        PlayerCausedExplosion = false;
        ShipExploded = false;
    }
}
