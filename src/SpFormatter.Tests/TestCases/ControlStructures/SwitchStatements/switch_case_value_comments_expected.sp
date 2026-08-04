void OnEntityCreated(int entity)
{
    switch (c)
    {
        case 'm', // molotov_projectile
            'p', // pipe_bomb_projectile
            'v', // vomitjar_projectile
            'g': // grenade_launcher_projectile
        {
            RequestFrame(OnNextFrame, entity);
        }
    }
}
