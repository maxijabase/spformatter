methodmap AdtArray < Handle
{
    public native int Length();
    property int Size
    {
        public get() = GetArraySize;
    }
};
