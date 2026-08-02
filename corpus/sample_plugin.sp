#include <sourcemod>

public Plugin myinfo =
{
    name = "Corpus Sample",
    author = "spformatter",
    description = "Tiny plugin used by formatter corpus checks",
    version = "1.0.0",
    url = "",
};
public void OnPluginStart()
{
    RegConsoleCmd("sm_hello", Command_Hello);
}
public Action Command_Hello(int client, int args)
{
    if(client > 0)
    {
        PrintToChat(client, "Hello");
        return Plugin_Handled;
    }
    return Plugin_Continue;
}