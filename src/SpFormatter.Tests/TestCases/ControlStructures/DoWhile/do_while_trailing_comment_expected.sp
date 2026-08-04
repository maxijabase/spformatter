public Action Cmd(int client)
{
    do
    {
        BlockCommand(CVARName);
    } //While:
    while(FindNextConCommand(CVAR, CVARName, sizeof(CVARName), IsCommand, Flags));
}
