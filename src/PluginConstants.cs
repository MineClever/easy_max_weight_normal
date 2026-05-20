using Autodesk.Max;

namespace EasyMaxWeightedNormal
{
    internal static class PluginConstants
    {
        public const string ClassName = "Easy Weighted Normal";
        public const string InternalName = "EasyMaxWeightedNormal";
        public const string Category = "MaxTools";

        public const uint TopoChannel = 1u << 0;
        public const uint GeomChannel = 1u << 1;
        public const uint GfxDataChannel = 1u << 8;
        public const uint MeshNormalModifierSupport = 0x20u;

        public static IClass_ID CreateClassId(IGlobal global)
        {
            return global.Class_ID.Create(0x74a82e31u, 0x51bb0e52u);
        }
    }
}
