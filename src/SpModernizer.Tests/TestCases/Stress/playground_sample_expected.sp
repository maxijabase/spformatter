float g_x = 5.0;
int g_y = 7;
char g_name[32];
bool g_ready;
char g_scratch[64];
Handle g_timer;
static float g_static = 1.0;

stock bool IsUserACrab(int client)
{
    return false;
}

public void OnReceivedString(const char[] name, float fval)
{
    float scaled = view_as<float>(fval);
    int plain = 0;
    PrintToServer("%s %f", name, scaled);
}

forward Action OnSomething(Handle timer, any data);

native float NativeAdd(float a, float b);

typedef SrvCmd = function Action (int args);
typedef ConCmd = function void (int client, int args);

typeset Timer {
  function Action (Handle timer, Handle hndl);
  function Action (Handle timer);
};

struct PluginInfo {
    public const char[] name;
    public const char[] author;
    public const char[] description;
    public const char[] version;
    public const char[] url;
};

public void OnPluginStart()
{
    float local = view_as<float>(0);
    char buf[32];
    int i = 0;
    Handle arr;

    for (int j = 0; j < 3; j++)
    {
        local = view_as<float>(j);
    }

    while (!g_ready)
{
        g_ready = true;
    }

    do
{
        i++;
    }
while (!g_ready);
}

public void AlreadyModern(int client, const char[] msg)
{
    float ok = 1.0;
    ArrayList list = new ArrayList();
    int[] players = new int[MaxClients + 1];
    delete list;
}
