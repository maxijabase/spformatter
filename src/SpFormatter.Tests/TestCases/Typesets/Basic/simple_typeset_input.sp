typeset EventHook {
    function Action (Event event, const char[] name, bool dontBroadcast);
    function void (Event event, const char[] name, bool dontBroadcast);
};
