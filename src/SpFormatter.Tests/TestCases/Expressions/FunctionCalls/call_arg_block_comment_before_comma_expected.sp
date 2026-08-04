void OnPluginStart()
{
    CreateConVar("l4d_wab_cooldown", "10", "desc", FCVAR_NOTIFY, true /*hasmin*/, 1.0, true /*hasmax*/, 3600.00);
}
