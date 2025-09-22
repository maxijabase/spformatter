void CheckValue(int value)
{
    if(value < 3)
    {
        PrintToServer("Low");
    }
    else if(value < 7)
    {
        PrintToServer("Medium");
    }
    else
    {
        PrintToServer("High");
    }
}