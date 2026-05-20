using System;
using Autodesk.Max;
using Autodesk.Max.Plugins;

namespace EasyMaxWeightedNormal
{
    public sealed class EasyWeightedNormalModifier : Modifier
    {
        private readonly IGlobal global;
        private readonly WeightedNormalSettings settings;

        public EasyWeightedNormalModifier()
        {
            this.global = GlobalInterface.Instance;
            settings = WeightedNormalSettings.Default;
        }
        
        internal EasyWeightedNormalModifier(WeightedNormalSettings settings)
        {
            this.global = GlobalInterface.Instance;
            this.settings = settings;
        }


        public override ICreateMouseCallBack CreateMouseCallBack
        {
            get { return null; }
        }

        public override uint ChannelsUsed
        {
            get { return PluginConstants.TopoChannel | PluginConstants.GeomChannel; }
        }

        public override uint ChannelsChanged
        {
            get { return PluginConstants.GeomChannel | PluginConstants.GfxDataChannel; }
        }

        public override IClass_ID InputType
        {
            get { return global.TriObjectClassID; }
        }

        public override RefResult NotifyRefChanged(
            IInterval changeInt,
            IReferenceTarget hTarget,
            ref UIntPtr partID,
            RefMessage message,
            bool propagate)
        {
            return RefResult.Dontcare;
        }

        public override void ModifyObject(int t, IModContext mc, IObjectState os, IINode node)
        {
            if (os == null || os.Obj == null)
            {
                return;
            }

            var triObject = os.Obj as ITriObject;
            if (triObject == null)
            {
                return;
            }

            //triObject.Update(t);

            var mesh = triObject.Mesh;
            if (mesh == null || mesh.NumFaces <= 0 || mesh.NumVerts <= 0)
            {
                return;
            }

            try
            {
                WeightedNormalProcessor.Apply(mesh, settings);

                // Usually enough after changing explicit normals.
                mesh.InvalidateGeomCache();
                //os.Invalidate(PluginConstants.GeomChannel | PluginConstants.GfxDataChannel, false);    // Do NOT call os.Invalidate() here.
                // This function is already running during modifier stack evaluation.
                // Invalidating the object state from inside ModifyObject can cause recursive reevaluation.
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.ToString());
            }


        }
    }
}
