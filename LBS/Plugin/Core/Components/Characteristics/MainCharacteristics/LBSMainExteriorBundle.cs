namespace ISILab.LBS.Characteristics
{
    /// <summary>
    /// Characteristic used to identify its owner <see cref="Plugin.Components.Bundles.Bundle"/> as a Main Bundle of type Exterior.
    /// </summary>
    [System.Serializable]
    //[LBSCharacteristic("Main Exterior", "")]
    public class LBSMainExteriorBundle : LBSCharacteristic
    {
        public LBSMainExteriorBundle() { }

        public override object Clone()
        {
            return new LBSMainExteriorBundle();
        }

        public override bool Equals(object obj)
        {
            return obj is LBSMainExteriorBundle;
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
    }
}
    
