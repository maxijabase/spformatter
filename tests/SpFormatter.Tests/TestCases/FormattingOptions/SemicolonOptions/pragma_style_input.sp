void OnPluginStart() {
    PrintToServer("Plugin started");
    CreateTimer(1.0, Timer_Example);
}