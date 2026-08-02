#pragma semicolon 1
#pragma newdecls required

#include <sourcemod>

#define DECL_INT(%1,%2) int %1 = %2
#define CALL3(%1,%2,%3,%4) %1(%2, %3, %4)
#define LOG(%1) PrintToServer(%1)

public Plugin myinfo =
{
    name = "macro abuse 06 decl call mix",
    author = "spformatter corpus",
    description = "Decls and calls hidden behind macros",
    version = "1.0.0",
    url = ""
};

public void OnPluginStart()
{
    DECL_INT(a, 10);
    DECL_INT(b, 20);
    LOG("boot");
    CALL3(PrintToServer, "a=%d b=%d", a, b);
}
