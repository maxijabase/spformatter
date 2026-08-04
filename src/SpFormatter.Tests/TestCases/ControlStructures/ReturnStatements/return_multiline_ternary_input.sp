bool capped(const int client)
{
    return
        (GetEntPropEnt(client, Prop_Send, "m_tongueOwner") > 0) ? true :
        (GetEntPropEnt(client, Prop_Send, "m_carryAttacker") > 0) ? true :
        (GetEntPropEnt(client, Prop_Send, "m_pounceAttacker") > 0) ? true : false;
}
