#pragma semicolon 0
public void Event()
{
    if (!IsValidEntity(Victim))
        return;
    StealthTimeRemain[Victim] = 0;
}
