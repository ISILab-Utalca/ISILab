namespace ISILab.LBS.Characteristics
{
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
