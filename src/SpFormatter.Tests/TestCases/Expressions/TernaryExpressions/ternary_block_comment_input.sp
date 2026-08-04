void t(int activator)
{
    new flDefuseLength = (GetEntData(activator, m_bHasDefuser, 1) & (1 << 0)) /*m_bHasDefuser */
        ? gArrPlayerDefuseTime[activator][HasKit] : gArrPlayerDefuseTime[activator][WithoutKit];
}
