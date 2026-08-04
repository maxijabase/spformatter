public void OnTakeDamage(int attacker, int damagetype)
{
    if (hitgroup == 1 && ((damagetype & DMG_USE_HITLOCATIONS) != 0 ||  // for ambassador
    TF2_GetPlayerClass(attacker) == TFClass_Sniper // for sydney sleeper
))
    {
        return;
    }
}
