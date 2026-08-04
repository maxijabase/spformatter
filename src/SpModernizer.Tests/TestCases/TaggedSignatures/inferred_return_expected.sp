public void OnPluginStart()
{
}

public int GetTeamBalance()
{
	return TEAMS_UNBALANCED;
}

public Action EventRoundStart(Handle event, const char[] name, bool dontBroadcast)
{
	return Plugin_Continue;
}

public Action BareEvent(Handle event, const char[] name, bool dontBroadcast)
{
	return Plugin_Continue;
}

public int SortKDR(int elem1, int elem2, const int[] array, Handle hndl)
{
	return FloatCompare(1.0, 2.0);
}

public bool IsReady()
{
	return true;
}
