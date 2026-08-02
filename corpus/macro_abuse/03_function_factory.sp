#pragma semicolon 1
#pragma newdecls required

#include <sourcemod>

// Emits a whole native-looking wrapper function from a name token.
#define MAKE_CMD(%1,%2) \
public Action Cmd_%1(int client, int args) \
{ \
    ReplyToCommand(client, "%2"); \
    return Plugin_Handled; \
}

public Plugin myinfo =
{
    name = "macro abuse 03 function factory",
    author = "spformatter corpus",
    description = "Macro expands into full function definitions",
    version = "1.0.0",
    url = ""
};

MAKE_CMD(Hello, "hello")
MAKE_CMD(World, "world")

public void OnPluginStart()
{
    RegConsoleCmd("sm_hello", Cmd_Hello);
    RegConsoleCmd("sm_world", Cmd_World);
}
