using Autodesk.Max;
using Autodesk.Max.Plugins;

namespace EasyMaxWeightedNormal
{
    public sealed class EasyWeightedNormalClassDesc : ClassDesc2
    {
        private readonly IGlobal global;
        private readonly IClass_ID classId;

        public EasyWeightedNormalClassDesc(IGlobal global)
        {
            this.global = global;
            classId = PluginConstants.CreateClassId(global);
        }

        public override bool IsPublic
        {
            get { return true; }
        }

        public override string ClassName
        {
            get { return PluginConstants.ClassName; }
        }

        public override string NonLocalizedClassName
        {
            get { return PluginConstants.ClassName; }
        }

        public override string InternalName
        {
            get { return PluginConstants.InternalName; }
        }

        public override SClass_ID SuperClassID
        {
            get { return SClass_ID.Osm; }
        }

        public override IClass_ID ClassID
        {
            get { return classId; }
        }

        public override string Category
        {
            get { return PluginConstants.Category; }
        }

        public override object Create(bool loading)
        {
            return new EasyWeightedNormalModifier(global);
        }
    }
}
