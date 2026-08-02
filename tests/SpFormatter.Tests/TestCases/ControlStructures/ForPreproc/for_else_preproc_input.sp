void F()
{
#if VOTE_TESTING
    for (i = 1; i < MAX; i++)
#else
    for (i = 1; i <= MaxClients; i++)
#endif
    {
        Foo();
    }
}
