public void OnTakeDamage(attacker, target)
{
    if((attacker != 0) && (target != 0) && (GetClientTeam(target) == CS_TEAM_T) // and freeattack should be announced
    && (GetConVarInt(sm_hosties_announce_attack) == 1) // and the target isn't a rebel
    && (!in_array(rebels, target)))
    {
        new bool:hasGun = false;
    }
}
