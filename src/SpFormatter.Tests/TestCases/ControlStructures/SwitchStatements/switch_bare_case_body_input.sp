void TestFunction(int type)
{
    switch (type)
    {
        case 1: entity = CreateEntityByName("weapon_pistol");
        case 2: entity = CreateEntityByName("weapon_pistol_magnum");
        case 3: entity = CreateEntityByName("weapon_melee");
    }
}
