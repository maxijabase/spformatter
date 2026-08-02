#pragma semicolon 1
#pragma newdecls required

#include <sourcemod>

// Injects a full for-header + opening brace. Body/close brace are written by the caller.
#define FOR(%1,%2,%3) for (%1; %2; %3) {

public Plugin myinfo =
{
    name = "macro abuse 01 for brace",
    author = "spformatter corpus",
    description = "Sarrus-style control-flow macro",
    version = "1.0.0",
    url = ""
};

public void OnPluginStart()
{
    int sum = 0;
    FOR(int i = 0, i < 3, i++)
        sum += i;
    }

    PrintToServer("sum=%d", sum);
}
