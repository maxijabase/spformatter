methodmap Foo
{
    public native int Len();

    public native void Clear();

    property int Size
    {
        public get() = GetSize;
    }
};
