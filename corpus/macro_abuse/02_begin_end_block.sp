#pragma semicolon 1
#pragma newdecls required

#include <sourcemod>

#define BEGIN_IF(%1) if (%1) {
#define ELSE_IF(%1) } else if (%1) {
#define ELSE } else {
#define END }

public Plugin myinfo =
{
    name = "macro abuse 02 begin end",
    author = "spformatter corpus",
    description = "Brace paired through macros",
    version = "1.0.0",
    url = ""
};

public void OnPluginStart()
{
    int team = 2;

    BEGIN_IF(team == 2)
        PrintToServer("t");
    ELSE_IF(team == 3)
        PrintToServer("ct");
    ELSE
        PrintToServer("spec");
    END
}
