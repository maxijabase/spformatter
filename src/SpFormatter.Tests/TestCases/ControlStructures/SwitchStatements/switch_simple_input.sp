void TestFunction() {
    switch(value) {
        case 1: {
            PrintToServer("One");
        }
        case 2, 3: {
            PrintToServer("Two or Three");
        }
        default: {
            PrintToServer("Other");
        }
    }
}