#pragma semicolon 1
#pragma newdecls required

#include <sourcemod>

#define FEATURE_A 1
#define FEATURE_B 1

#if defined FEATURE_A
    #define MAYBE_A(%1) PrintToServer("A:%d", %1)
#else
    #define MAYBE_A(%1)
#endif

#if defined FEATURE_B
    #define MAYBE_B(%1) PrintToServer("B:%d", %1)
#else
    #define MAYBE_B(%1) do { } while (false)
#endif

#define RUN_BOTH(%1) do { MAYBE_A(%1); MAYBE_B(%1); } while (false)

public Plugin myinfo =
{
    name = "macro abuse 04 ifdef soup",
    author = "spformatter corpus",
    description = "Nested ifdef + layered macro calls",
    version = "1.0.0",
    url = ""
};

public void OnPluginStart()
{
    RUN_BOTH(7);
    RUN_BOTH(GetClientCount());
}
