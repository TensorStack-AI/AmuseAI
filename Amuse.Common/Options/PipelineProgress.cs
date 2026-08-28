using Amuse.Common.Message;
using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using TensorStack.Common.Tensor;


namespace Amuse.Common
{
    public sealed class PipelineProgress : ITensorMessage
    {
        public string Key { get; init; }
        public string Subkey { get; init; }
        public string ElapsedKey { get; init; }
        public DateTime Timestamp { get; init; }
        public float Elapsed { get; init; }
        public int Value { get; init; }
        public int Maximum { get; init; }
        public int BatchValue { get; init; }
        public int BatchMaximum { get; init; }
        public string Message { get; init; }

        [JsonIgnore]
        public IReadOnlyList<Tensor<float>> Tensors { get; set; }

        public float IterationsPerSecond => Elapsed > 0 ? 1000f / Elapsed : 0;
        public float SecondsPerIteration => Elapsed > 0 ? Elapsed / 1000f : 0;
    }
}
