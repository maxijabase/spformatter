void TestFunction()
{
    ExplodeString(versionText, /* split */ ".", versionNumbers, .maxStrings = 4, .maxStringLength = 4);
    ProcessTargetString(arg1, client, target_list, MAXPLAYERS, COMMAND_FILTER_ALIVE, /* Only allow alive players */ target_name, sizeof(target_name), tn_is_ml);
}
