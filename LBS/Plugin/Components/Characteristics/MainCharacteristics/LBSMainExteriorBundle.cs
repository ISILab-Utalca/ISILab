namespace ISILab.LBS.Characteristics
{
    [System.Serializable]
    [LBSCharacteristic("Main Exterior", "")]
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
    
