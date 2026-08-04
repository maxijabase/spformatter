public void TestFunction{i}(int param{i})
{
    if(param{i} > 0)
    {
        PrintToServer("Function {i}: %d", param{i});
    }
    for(int j = 0; j < param{i}; j++)
    {
        DoSomething{i}(j);
    }
}