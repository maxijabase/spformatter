void TestFunction()
{
    int x = 5;
    PrintToServer("Value: %d", x);
    if(x > 0)
    {
        return x;
    }
}