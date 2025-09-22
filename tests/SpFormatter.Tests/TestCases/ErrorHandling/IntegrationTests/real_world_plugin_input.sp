#include <sourcemod>

public Plugin myinfo = {
    name = "Test Plugin",
    author = "Author",
    description = "Test Description",
    version = "1.0",
    url = ""
};

public void OnPluginStart()
{
    RegConsoleCmd("sm_test", Command_Test);
    HookEvent("player_spawn", Event_PlayerSpawn);
}

public Action Command_Test(int client, int args)
{
    if(!IsValidClient(client))
        return Plugin_Handled;

    PrintToChat(client, "Hello World!");
    return Plugin_Handled;
}

stock bool IsValidClient(int client)
{
    return client > 0 && client <= MaxClients && IsClientInGame(client);
}