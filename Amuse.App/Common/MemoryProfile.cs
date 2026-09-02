using TensorStack.WPF;

namespace Amuse.App.Common
{
    public sealed class MemoryProfile : BaseModel
    {
        public MemoryProfile() { }
        public MemoryProfile(QualityMode qualityMode, int[] memoryModes)
        {
            QualityMode = qualityMode;
            MemoryModes = memoryModes;
        }

        public QualityMode QualityMode { get; set; }
        public int[] MemoryModes { get; set; }

        public string Recommended
        {
            get
            {
                if (MemoryModes.Length == 1)
                    return $"{MemoryModes[0]}GB";
                if (MemoryModes.Length == 2)
                    return $"{MemoryModes[0]}GB - {MemoryModes[1]}GB";
                if (MemoryModes.Length == 3)
                    return $"{MemoryModes[1]}GB - {MemoryModes[2]}GB";

                return "0GB";
            }
        }
    }
}
