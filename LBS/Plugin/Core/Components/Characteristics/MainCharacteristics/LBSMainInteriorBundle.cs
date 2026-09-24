namespace ISILab.LBS.Characteristics
{
    /// <summary>
    /// Characteristic used to identify its owner <see cref="Plugin.Components.Bundles.Bundle"/> as a Main Bundle of type Interior.
    /// </summary>
    [System.Serializable]
    //[LBSCharacteristic("Main Interior", "")]
    public class LBSMainInteriorBundle : LBSCharacteristic
    {
        public LBSMainInteriorBundle() { }

        public override object Clone()
        {
            return new LBSMainInteriorBundle();
        }

        public override bool Equals(object obj)
        {
            return obj is LBSMainInteriorBundle;
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
    }
}
