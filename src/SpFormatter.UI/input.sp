/*
 * Comprehensive SourcePawn Formatter Test File
 * This file contains complex permutations and variations of SourcePawn syntax
 * to thoroughly test the formatter and identify potential bugs.
 */

#pragma semicolon 1
#pragma newdecls required

#include <sourcemod>
#include <sdktools>
#include <cstrike>

#define PLUGIN_VERSION "1.0.0"
#define MAX_PLAYERS 64
#define INVALID_TIMER_HANDLE INVALID_HANDLE

// Global variables with various types
Handle g_hDatabase = INVALID_HANDLE;
bool g_bIsPluginEnabled = true;
int g_iPlayerCount = 0;
float g_fSpawnTime[MAXPLAYERS + 1];
char g_sPlayerNames[MAXPLAYERS + 1][MAX_NAME_LENGTH];
ArrayList g_alPlayerList;
Handle g_hTimers[MAXPLAYERS + 1] = {INVALID_HANDLE, ...};

// Native declarations
native bool IsValidClient(int client, bool bots = false, bool dead = false);
native void DisplayCustomMenu(int client, Handle menu, int time = MENU_TIME_FOREVER);

// Forward declarations
forward void OnPlayerSpawned(int client);
forward Action OnPlayerCommand(int client, const char[] command, int args);

// Plugin information
public Plugin myinfo = {
    name = "Comprehensive Test Plugin",
    author = "Test Author",
    description = "A comprehensive test for the SourcePawn formatter",
    version = PLUGIN_VERSION,
    url = "https://example.com"
};

// Complex function with multiple parameter types
public void OnPluginStart() {
    // Variable declarations with different initialization patterns
    char sBuffer[256], sTemp[64];
    int iValue = GetRandomInt(1, 100);
    float fTime = GetEngineTime();
    bool bResult;
    Handle hConVar;
    
    // Preprocessor conditionals
    #if defined DEBUG_MODE
        PrintToServer("[DEBUG] Plugin starting in debug mode");
    #endif
    
    // Multiple assignment operators
    g_iPlayerCount = 0;
    g_iPlayerCount += 5;
    g_iPlayerCount *= 2;
    g_iPlayerCount /= 3;
    g_iPlayerCount %= 4;
    
    // Complex expressions with operators
    bResult = (iValue > 50) && (fTime >= 0.0) || !g_bIsPluginEnabled;
    int iCalculation = ((iValue + 10) * 2 - 5) / 3 + (iValue % 2 == 0 ? 1 : 0);
    
    // Nested function calls
    LogMessage("Plugin started with calculation result: %d", 
               GetRandomInt(1, iCalculation + GetClientCount()));
    
    // Event hooking
    HookEvent("player_spawn", Event_PlayerSpawn, EventHookMode_Post);
    HookEvent("player_death", Event_PlayerDeath);
    HookEvent("round_start", Event_RoundStart, EventHookMode_Pre);
    
    // Command registration with various patterns
    RegConsoleCmd("sm_test", Command_Test, "Test command");
    RegConsoleCmd("sm_menu", Command_ShowMenu);
    RegAdminCmd("sm_admin", Command_Admin, ADMFLAG_GENERIC, "Admin command");
    
    // ConVar creation
    hConVar = CreateConVar("sm_plugin_enabled", "1", "Enable/disable the plugin", 
                          FCVAR_PLUGIN, true, 0.0, true, 1.0);
    HookConVarChange(hConVar, OnConVarChanged);
    
    // Array and handle initialization
    g_alPlayerList = new ArrayList(MAX_NAME_LENGTH);
    
    // Complex for loops with various patterns
    for (int i = 1; i <= MaxClients; i++) {
        if (IsClientInGame(i) && !IsFakeClient(i)) {
            g_fSpawnTime[i] = 0.0;
            GetClientName(i, g_sPlayerNames[i], sizeof(g_sPlayerNames[]));
            g_alPlayerList.PushString(g_sPlayerNames[i]);
        }
    }
}

// Function with complex parameter list
public Action Command_Test(int client, int args) {
    // Input validation with nested conditions
    if (!IsValidClient(client)) {
        ReplyToCommand(client, "Invalid client");
        return Plugin_Handled;
    }
    
    if (args < 1) {
        ReplyToCommand(client, "Usage: sm_test <value> [optional]");
        return Plugin_Handled;
    }
    
    // String manipulation
    char sArg[32], sMessage[256];
    GetCmdArg(1, sArg, sizeof(sArg));
    int iValue = StringToInt(sArg);
    
    // Switch statement with multiple cases
    switch (iValue) {
        case 1: {
            Format(sMessage, sizeof(sMessage), "You chose option one");
        }
        case 2, 3, 4: {
            Format(sMessage, sizeof(sMessage), "You chose option %d (grouped)", iValue);
        }
        case 5: {
            Format(sMessage, sizeof(sMessage), "Special case five");
            // Fall through to default
        }
        default: {
            Format(sMessage, sizeof(sMessage), "Default case for value: %d", iValue);
        }
    }
    
    PrintToChat(client, sMessage);
    return Plugin_Handled;
}

// Complex event handler with multiple nested conditions
public void Event_PlayerSpawn(Event event, const char[] name, bool dontBroadcast) {
    int client = GetClientOfUserId(event.GetInt("userid"));
    
    // Complex nested if-else chain
    if (IsValidClient(client)) {
        if (GetClientTeam(client) == CS_TEAM_T) {
            if (IsPlayerAlive(client)) {
                // Timer creation with callback
                CreateTimer(1.0, Timer_DelayedMessage, GetClientUserId(client), 
                           TIMER_FLAG_NO_MAPCHANGE);
                
                // Complex array operations
                g_fSpawnTime[client] = GetEngineTime();
                
                // Bitwise operations
                int iFlags = GetEntityFlags(client);
                iFlags |= FL_GODMODE;
                iFlags &= ~FL_FROZEN;
                SetEntityFlags(client, iFlags);
            }
        } else if (GetClientTeam(client) == CS_TEAM_CT) {
            // Different handling for CT team
            GivePlayerItem(client, "weapon_ak47");
        } else {
            // Spectator or unassigned
            ChangeClientTeam(client, CS_TEAM_T);
        }
    }
}

// Function with while loop and complex break conditions
public void ProcessPlayerQueue() {
    int iProcessed = 0;
    int iMaxProcess = 10;
    
    while (g_alPlayerList.Length > 0 && iProcessed < iMaxProcess) {
        char sPlayerName[MAX_NAME_LENGTH];
        g_alPlayerList.GetString(0, sPlayerName, sizeof(sPlayerName));
        g_alPlayerList.Erase(0);
        
        // Complex condition with multiple checks
        bool bFound = false;
        for (int i = 1; i <= MaxClients; i++) {
            if (IsClientInGame(i)) {
                char sCurrentName[MAX_NAME_LENGTH];
                GetClientName(i, sCurrentName, sizeof(sCurrentName));
                
                if (StrEqual(sPlayerName, sCurrentName, false)) {
                    bFound = true;
                    break;
                }
            }
        }
        
        if (!bFound) {
            LogMessage("Player %s is no longer connected", sPlayerName);
        }
        
        iProcessed++;
    }
}

// Timer callback with various return types
public Action Timer_DelayedMessage(Handle timer, int userid) {
    int client = GetClientOfUserId(userid);
    
    if (!IsValidClient(client)) {
        return Plugin_Stop;
    }
    
    // Complex ternary operators
    char sTeamName[32];
    int iTeam = GetClientTeam(client);
    sTeamName = (iTeam == CS_TEAM_T) ? "Terrorist" : 
                (iTeam == CS_TEAM_CT) ? "Counter-Terrorist" : "Spectator";
    
    PrintToChat(client, "Welcome to the %s team!", sTeamName);
    
    // Conditional timer continuation
    return (GetClientHealth(client) > 50) ? Plugin_Continue : Plugin_Stop;
}

// Function with array parameters and reference parameters
public void ProcessArray(int[] array, int size, int &total, float &average) {
    total = 0;
    
    // Enhanced for loop with array access
    for (int i = 0; i < size; i++) {
        total += array[i];
        
        // Complex array assignment with calculations
        array[i] = array[i] * 2 + (i % 2 == 0 ? 1 : -1);
    }
    
    average = float(total) / float(size);
}

// Menu handling function
public void DisplayPlayerMenu(int client) {
    Handle hMenu = CreateMenu(MenuHandler_PlayerMenu);
    SetMenuTitle(hMenu, "Player Menu:");
    
    // Dynamic menu population
    char sInfo[8], sDisplay[64];
    for (int i = 1; i <= MaxClients; i++) {
        if (IsClientInGame(i) && i != client) {
            IntToString(i, sInfo, sizeof(sInfo));
            Format(sDisplay, sizeof(sDisplay), "%N (Health: %d)", 
                   i, GetClientHealth(i));
            AddMenuItem(hMenu, sInfo, sDisplay);
        }
    }
    
    DisplayMenu(hMenu, client, MENU_TIME_FOREVER);
}

// Menu handler with complex switch
public int MenuHandler_PlayerMenu(Handle menu, MenuAction action, int param1, int param2) {
    switch (action) {
        case MenuAction_Select: {
            int client = param1;
            int target = StringToInt(GetMenuSelectionInfo(menu, param2));
            
            // Perform action on selected target
            if (IsValidClient(target)) {
                FakeClientCommand(client, "sm_slay %d", target);
            }
        }
        case MenuAction_Cancel: {
            // Handle menu cancellation
            PrintToChat(param1, "Menu cancelled");
        }
        case MenuAction_End: {
            CloseHandle(menu);
        }
    }
    
    return 0;
}

// Plugin end cleanup
public void OnPluginEnd() {
    // Cleanup timers
    for (int i = 1; i <= MaxClients; i++) {
        if (g_hTimers[i] != INVALID_HANDLE) {
            KillTimer(g_hTimers[i]);
            g_hTimers[i] = INVALID_HANDLE;
        }
    }
    
    // Cleanup handles
    if (g_alPlayerList != null) {
        delete g_alPlayerList;
    }
    
    if (g_hDatabase != INVALID_HANDLE) {
        CloseHandle(g_hDatabase);
        g_hDatabase = INVALID_HANDLE;
    }
}
