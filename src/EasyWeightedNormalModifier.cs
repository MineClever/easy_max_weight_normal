using System;
using System.Windows.Forms;
using Autodesk.Max;
using Autodesk.Max.Plugins;
using ManagedServices;

namespace EasyMaxWeightedNormal
{
    public sealed class EasyWeightedNormalModifier : Modifier
    {
        private const ushort SettingsChunk = 0x1000;

        private readonly IGlobal global;
        private readonly WeightedNormalSettings settings;
        private IIObjParam editInterface;
        private WeightedNormalOptionsForm optionsForm;

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

        internal WeightedNormalSettings CurrentSettings
        {
            get { return settings.Clone(); }
        }

        public override void BeginEditParams(IIObjParam ip, uint flags, IAnimatable prev)
        {
            base.BeginEditParams(ip, flags, prev);

            editInterface = ip;
            ShowOptionsForm();
        }

        public override void EndEditParams(IIObjParam ip, uint flags, IAnimatable next)
        {
            CloseOptionsForm();
            editInterface = null;

            base.EndEditParams(ip, flags, next);
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

        public override IOResult Save(IISave isave)
        {
            if (isave == null)
            {
                return IOResult.Error;
            }

            byte mask = BuildSettingsMask();
            uint bytesWritten = 0;

            isave.BeginChunk(SettingsChunk);
            IOResult result = isave.WriteVoid(new[] { mask }, ref bytesWritten);
            isave.EndChunk();

            return result == IOResult.Ok && bytesWritten == 1 ? IOResult.Ok : IOResult.Error;
        }

        public override IOResult Load(IILoad iload)
        {
            if (iload == null)
            {
                return IOResult.Error;
            }

            IOResult result;
            while ((result = iload.OpenChunk()) == IOResult.Ok)
            {
                if (iload.CurChunkID == SettingsChunk)
                {
                    var buffer = new byte[1];
                    uint bytesRead = 0;
                    IOResult readResult = iload.ReadVoid(buffer, ref bytesRead);
                    if (readResult != IOResult.Ok || bytesRead != 1)
                    {
                        iload.CloseChunk();
                        return IOResult.Error;
                    }

                    ApplySettingsMask(buffer[0]);
                }

                IOResult closeResult = iload.CloseChunk();
                if (closeResult != IOResult.Ok)
                {
                    return closeResult;
                }
            }

            return result == IOResult.End ? IOResult.Ok : result;
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

        internal void UpdateSettings(WeightedNormalSettings newSettings)
        {
            if (newSettings == null)
            {
                return;
            }

            settings.UseAreaWeight = newSettings.UseAreaWeight;
            settings.UseAngleWeight = newSettings.UseAngleWeight;
            settings.RespectSmoothingGroups = newSettings.RespectSmoothingGroups;

            NotifySettingsChanged();
        }

        private void ShowOptionsForm()
        {
            if (optionsForm != null && !optionsForm.IsDisposed)
            {
                optionsForm.SyncFromModifier();
                ShowOptionsFormWithMaxOwner(optionsForm);
                optionsForm.Activate();
                return;
            }

            optionsForm = new WeightedNormalOptionsForm(this);
            optionsForm.FormClosed += (sender, args) => optionsForm = null;
            ShowOptionsFormWithMaxOwner(optionsForm);
            optionsForm.Activate();
        }

        private void CloseOptionsForm()
        {
            if (optionsForm == null)
            {
                return;
            }

            optionsForm.Close();
            optionsForm = null;
        }

        private void NotifySettingsChanged()
        {
            try
            {
                var partId = new UIntPtr(PluginConstants.GeomChannel | PluginConstants.GfxDataChannel);
                NotifyDependents(
                    global.Interval.Create(0, int.MaxValue),
                    partId,
                    RefMessage.Change,
                    SClass_ID.Osm,
                    true,
                    null,
                    NotifyDependentsOption.DisallowOptimizations);

                if (editInterface != null)
                {
                    editInterface.RedrawViews(editInterface.Time, RedrawFlags.Normal, null);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.ToString());
            }
        }

        private static void ShowOptionsFormWithMaxOwner(Form form)
        {
            IntPtr ownerHandle = AppSDK.GetMaxHWND();
            if (ownerHandle == IntPtr.Zero)
            {
                form.Show();
                return;
            }

            var owner = new Win32Window(ownerHandle);
            form.Show(owner);
        }

        private byte BuildSettingsMask()
        {
            byte mask = 0;
            if (settings.UseAreaWeight)
            {
                mask |= 1 << 0;
            }

            if (settings.UseAngleWeight)
            {
                mask |= 1 << 1;
            }

            if (settings.RespectSmoothingGroups)
            {
                mask |= 1 << 2;
            }

            return mask;
        }

        private void ApplySettingsMask(byte mask)
        {
            settings.UseAreaWeight = (mask & (1 << 0)) != 0;
            settings.UseAngleWeight = (mask & (1 << 1)) != 0;
            settings.RespectSmoothingGroups = (mask & (1 << 2)) != 0;
        }

        private sealed class Win32Window : IWin32Window
        {
            public Win32Window(IntPtr handle)
            {
                Handle = handle;
            }

            public IntPtr Handle { get; private set; }
        }
    }
}
