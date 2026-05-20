using Autodesk.Max;

namespace EasyMaxWeightedNormal
{
    public static class AssemblyFunctions
    {
        private static EasyWeightedNormalClassDesc descriptor;

        public static void AssemblyMain()
        {
            var global = GlobalInterface.Instance;
            descriptor = new EasyWeightedNormalClassDesc();
            global.COREInterface13.AddClass(descriptor);
        }

        public static void AssemblyShutdown()
        {
            descriptor = null;
        }
    }
}
