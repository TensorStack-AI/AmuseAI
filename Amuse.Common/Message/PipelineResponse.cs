using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using TensorStack.Common;
using TensorStack.Common.Tensor;

namespace Amuse.Common.Message
{
    public sealed class PipelineResponse
    {
        public PipelineResponse() { }
        public PipelineResponse(Exception ex) : this(ex.Message)
        {
            IsCanceled = ex is OperationCanceledException;
        }
        public PipelineResponse(string errorMessage)
        {
            Error = errorMessage;
            Type = ResponseType.Error;
        }

        public PipelineResponse(params ImageTensor[] imagetensors)
        {
            Type = ResponseType.Tensor;
            ImageTensors = imagetensors;
        }

        public PipelineResponse(params AudioTensor[] audioTensors)
        {
            Type = ResponseType.Tensor;
            AudioTensors = audioTensors;
        }

        public PipelineResponse(params VideoSequence[] videoSequences)
        {
            Type = ResponseType.Tensor;
            VideoSequences = videoSequences;
        }


        public PipelineResponse(params TextInput[] textResults)
        {
            Type = ResponseType.Object;
            TextResponse = textResults;
        }

        public ResponseType Type { get; init; }
        public TextInput[] TextResponse { get; set; }

        public string Error { get; init; }
        public bool IsCanceled { get; init; }
        public TensorMetadata TensorMetadata { get; set; }

        [JsonIgnore]
        public bool IsError => !string.IsNullOrEmpty(Error);

        [JsonIgnore]
        public IReadOnlyList<ImageTensor> ImageTensors { get; set; }

        [JsonIgnore]
        public IReadOnlyList<AudioTensor> AudioTensors { get; set; }

        [JsonIgnore]
        public IReadOnlyList<VideoSequence> VideoSequences { get; set; }


        public IReadOnlyList<Tensor<float>> PackTensors()
        {
            var metadata = new TensorMetadata();
            var tensorData = new List<Tensor<float>>();
            if (!ImageTensors.IsNullOrEmpty())
            {
                foreach (var imageTensor in ImageTensors)
                {
                    if (imageTensor == null)
                        continue;
                    tensorData.Add(imageTensor);
                    metadata.AddImage(imageTensor);
                }
            }
            if (!AudioTensors.IsNullOrEmpty())
            {
                foreach (var audioTensor in AudioTensors)
                {
                    if (audioTensor == null)
                        continue;
                    tensorData.Add(audioTensor);
                    metadata.AddAudio(audioTensor);
                }
            }
            if (!VideoSequences.IsNullOrEmpty())
            {
                foreach (var videoSequence in VideoSequences)
                {
                    if (videoSequence == null)
                        continue;
                    tensorData.AddRange(videoSequence.Frames);
                    if (videoSequence.HasAudio)
                        tensorData.Add(videoSequence.Audio);
                    metadata.AddVideo(videoSequence);
                }
            }
            TensorMetadata = metadata;
            return tensorData;
        }


        public void UnpackTensors(IReadOnlyList<Tensor<float>> packedTensors)
        {
            var metadata = TensorMetadata;
            if (metadata == null || packedTensors.IsNullOrEmpty())
                return;

            var tensorIndex = 0;
            if (metadata.ImageCount > 0)
            {
                var images = new List<ImageTensor>(metadata.ImageCount);
                for (var i = 0; i < metadata.ImageCount; i++)
                {
                    var imageTensor = packedTensors[tensorIndex + i].AsImageTensor();
                    images.Add(imageTensor);
                }
                tensorIndex += metadata.ImageCount;
                ImageTensors = images;
            }
            if (metadata.AudioCount > 0)
            {
                var audioTensors = new List<AudioTensor>();
                for (var i = 0; i < metadata.AudioCount; i++)
                {
                    var audioMetadata = metadata.AudioMetadata[i];
                    var audioTensor = packedTensors[tensorIndex + i].AsAudioTensor(audioMetadata.SampleRate);
                    audioTensors.Add(audioTensor);
                }
                tensorIndex += metadata.AudioCount;
                AudioTensors = audioTensors;
            }
            if (metadata.VideoCount > 0)
            {
                var videoSequences = new List<VideoSequence>(metadata.VideoCount);
                for (var i = 0; i < metadata.VideoCount; i++)
                {
                    AudioTensor audio = default;
                    var videoMetadata = metadata.VideoMetadata[i];
                    var frames = new ImageTensor[videoMetadata.FrameCount];
                    for (var f = 0; f < videoMetadata.FrameCount; f++)
                        frames[f] = packedTensors[tensorIndex + f].AsImageTensor();

                    tensorIndex += videoMetadata.FrameCount;
                    var audioMetadata = metadata.VideoAudioMetadata[i];
                    if (audioMetadata.SampleRate > 0)
                    {
                        audio = packedTensors[tensorIndex].AsAudioTensor(audioMetadata.SampleRate);
                        tensorIndex++;
                    }
                    var videoSequence = new VideoSequence(frames, videoMetadata.FrameRate, audio);
                    videoSequences.Add(videoSequence);
                }
                VideoSequences = videoSequences;
            }
        }

    }
}
