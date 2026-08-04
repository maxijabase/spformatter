void F(int client)
{
#if USE_COOKIES
    if (!IsFakeClient(client) && AreClientCookiesCached(client))
#else
    if (!IsFakeClient(client))
#endif
    {
        Foo();
    }
}
