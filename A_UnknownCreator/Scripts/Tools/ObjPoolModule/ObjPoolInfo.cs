namespace UnknownCreator.Modules
{
    public struct ObjPoolInfo
    {
        public int maxNum;
        public int remainingNum;
        public float clearInterval;
        public bool isAutoClear;

        public readonly static ObjPoolInfo defaultInfo = new()
        {
            maxNum = 200,
            remainingNum = 0,
            clearInterval = 90F,
            isAutoClear = true,
        };
    }
}