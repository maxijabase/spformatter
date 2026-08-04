new Float:g_x = 5.0;
new g_y = 7;
new String:g_name[32];
new bool:g_ready;
decl String:g_scratch[64];
new Handle:g_timer;
static Float:g_static = 1.0;

stock bool:IsUserACrab(client)
{
    return false;
}

public OnReceivedString(const String:name[], Float:fval)
{
    new Float:scaled = Float:fval;
    new _:plain = 0;
    PrintToServer("%s %f", name, scaled);
}

forward Action:OnSomething(Handle:timer, any:data);

native Float:NativeAdd(Float:a, Float:b);

functag public Action:SrvCmd(args);
functag public ConCmd(client, args);

funcenum Timer {
    Action:public(Handle:timer, Handle:hndl),
    Action:public(Handle:timer),
};

struct PluginInfo {
    const String:name[],
    const String:author[],
    const String:description[],
    const String:version[],
    const String:url[]
};

public OnPluginStart()
{
    new Float:local = Float:0;
    new String:buf[32];
    new i = 0;
    new Handle:arr;

    for (new j = 0; j < 3; j++)
    {
        local = Float:j;
    }

    while !g_ready do
    {
        g_ready = true;
    }

    do
    {
        i++;
    }
    while !g_ready;
}

public void AlreadyModern(int client, const char[] msg)
{
    float ok = 1.0;
    ArrayList list = new ArrayList();
    int[] players = new int[MaxClients + 1];
    delete list;
}
