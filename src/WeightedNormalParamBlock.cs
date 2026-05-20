using System;
using Autodesk.Max;

namespace EasyMaxWeightedNormal
{
    internal static class WeightedNormalParamBlock
    {
        public const short BlockId = 0;
        public const int ReferenceNumber = 0;
        public const int Version = 1;

        public const short UseAreaWeightId = 0;
        public const short UseAngleWeightId = 1;
        public const short RespectSmoothingGroupsId = 2;

        public static IParamBlockDesc2 Create(IGlobal global, IClassDesc2 classDesc)
        {
            IParamBlockDesc2 paramBlockDesc = global.ParamBlockDesc2.Create(
                BlockId,
                "Parameters",
                IntPtr.Zero,
                classDesc,
                (ParamBlock2Flags)((int)ParamBlock2Flags.Version + (int)ParamBlock2Flags.AutoConstruct + (int)ParamBlock2Flags.AutoUi),
                new object[] { Version, ReferenceNumber });

            paramBlockDesc.AddParam(
                UseAreaWeightId,
                new object[]
                {
                    "useAreaWeight",
                    ParamType2.Int,
                    0,
                    1,
                    0,
                    1
                });

            paramBlockDesc.AddParam(
                UseAngleWeightId,
                new object[]
                {
                    "useAngleWeight",
                    ParamType2.Int,
                    0,
                    1,
                    0,
                    1
                });

            paramBlockDesc.AddParam(
                RespectSmoothingGroupsId,
                new object[]
                {
                    "respectSmoothingGroups",
                    ParamType2.Int,
                    0,
                    1,
                    0,
                    1
                });

            return paramBlockDesc;
        }
    }
}
