namespace ISILab.LBS.Characteristics
{
    /// <summary>
    /// Characteristic used to identify its owner <see cref="Plugin.Components.Bundles.Bundle"/> as a Main Bundle of type Population.
    /// </summary>
    [System.Serializable]
    //[LBSCharacteristic("Main Population", "")]
    public class LBSMainPopulationBundle : LBSCharacteristic
    {
        public LBSMainPopulationBundle() { }

        public override object Clone()
        {
            return new LBSMainPopulationBundle();
        }

        public override bool Equals(object obj)
        {
            return obj is LBSMainPopulationBundle;
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
    }
}
