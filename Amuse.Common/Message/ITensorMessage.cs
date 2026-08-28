using System.Collections.Generic;
using TensorStack.Common.Tensor;

namespace Amuse.Common.Message
{
    /// <summary>
    /// Tensor messages are used for small tensors that dont require a large contiguous memory mapping, eg: latent previews 
    /// Large or mutiple tensors should be send via the <see cref="Amuse.Common.PipelineTensorChannel"/> 
    /// </summary>
    public interface ITensorMessage
    {
        IReadOnlyList<Tensor<float>> Tensors { get; set; }
    }
}
