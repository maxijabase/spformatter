#pragma semicolon 1
#pragma newdecls required

#include <sourcemod>

#define CASE_PRINT(%1,%2) case %1: { PrintToServer(%2); }
#define DEFAULT_PRINT(%1) default: { PrintToServer(%1); }

public Plugin myinfo =
{
    name = "macro abuse 05 switch cases",
    author = "spformatter corpus",
    description = "Case labels supplied by macros",
    version = "1.0.0",
    url = ""
};

public void OnPluginStart()
{
    int v = 2;
    switch (v)
    {
        CASE_PRINT(1, "one")
        CASE_PRINT(2, "two")
        CASE_PRINT(3, "three")
        DEFAULT_PRINT("other")
    }
}
