public OnPluginStart()
{
}

public GetTeamBalance()
{
	return TEAMS_UNBALANCED;
}

public Action:EventRoundStart(Handle:event, const String:name[], bool:dontBroadcast)
{
	return Plugin_Continue;
}

public BareEvent(Handle:event, const String:name[], bool:dontBroadcast)
{
	return Plugin_Continue;
}

public SortKDR(elem1, elem2, const array[], Handle:hndl)
{
	return FloatCompare(1.0, 2.0);
}

public IsReady()
{
	return true;
}
