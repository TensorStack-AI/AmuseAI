using System.Collections.Generic;
using TensorStack.Common.Tensor;

namespace Amuse.Common
{
    public interface IGenerateOptions
    {
        List<ImageTensor> InputImages { get; set; }
        List<ImageTensor> InputControlImages { get; set; }
        List<AudioTensor> InputAudios { get; set; }
        List<VideoSequence> InputVideos { get; set; }
    }
}
