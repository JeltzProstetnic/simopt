namespace MatthiasToolbox.DeltaWizard
{
    public struct Statistics
    {
        public long FileSize1;
        public long FileSize2;
        public long DeltaSize;
        public long HashSize;
        public int BlockSize;
        public double HashTime;
        public double DeltaTime;
        
        public long FileSize { get { return (FileSize1 + FileSize2) / 2; } }
        public long combinedSize { get { return DeltaSize + HashSize; } }
        public double CRcombined { get { return (double)FileSize / ((double)DeltaSize + (double)HashSize); } }
        public double CRdelta { get { return (double)FileSize / (double)DeltaSize; } }
    }
}
