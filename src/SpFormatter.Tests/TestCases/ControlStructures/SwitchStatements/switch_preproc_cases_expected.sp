void OnMapStart()
{
#if defined INCLUDE_SPEC_TEAMS
    new g_iTeamUnasIndex;
#endif
    new g_iTeamRedIndex;

    switch (TFTeam:iTeamNum)
    {
#if defined INCLUDE_SPEC_TEAMS
        case TFTeam_Unassigned:
            g_iTeamUnasIndex = iTeam;
#endif
        case TFTeam_Red:
            g_iTeamRedIndex = iTeam;
    }
}
