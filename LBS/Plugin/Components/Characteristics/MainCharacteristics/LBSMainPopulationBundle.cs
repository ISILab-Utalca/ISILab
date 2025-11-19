namespace ISILab.LBS.Characteristics
{
    [System.Serializable]
    [LBSCharacteristic("Main Population", "")]
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
    }
}
