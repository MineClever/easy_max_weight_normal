namespace EasyMaxWeightedNormal
{
    internal sealed class WeightedNormalSettings
    {
        public bool UseAreaWeight { get; set; }

        public bool UseAngleWeight { get; set; }

        public bool RespectSmoothingGroups { get; set; }

        public static WeightedNormalSettings Default
        {
            get
            {
                return new WeightedNormalSettings
                {
                    UseAreaWeight = true,
                    UseAngleWeight = true,
                    RespectSmoothingGroups = true
                };
            }
        }
    }
}
