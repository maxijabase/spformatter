void TestFunc() {
    int x = 5;        // Valid
    if(condition {    // Missing closing paren
        DoSomething();
    }
    int y = 10;       // Valid after error
}