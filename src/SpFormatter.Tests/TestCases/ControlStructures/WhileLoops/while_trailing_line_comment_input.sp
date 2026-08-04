void Test()
{
    while (first || (IsPlayerStuck(floor, client) && !failed)) // iteratively check
    {
        first = false;
    }
}
