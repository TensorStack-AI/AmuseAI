using System.Collections.Generic;
using TensorStack.Common.Tensor;

namespace Amuse.Common.Message
{
    public class TensorMetadata
    {
        public int ImageCount { get; set; }
        public int ControlImageCount { get; set; }
        public int AudioCount => AudioMetadata?.Count ?? 0;
        public int VideoCount => VideoMetadata?.Count ?? 0;
        public List<AudioTensorMetadata> AudioMetadata { get; set; } = [];
        public List<VideoTensorMetadata> VideoMetadata { get; set; } = [];
        public List<AudioTensorMetadata> VideoAudioMetadata { get; set; } = [];
        public List<int[]> Dimensions { get; set; } = [];


        public void AddImage(ImageTensor tensor)
        {
            ImageCount++;
            Dimensions.Add([.. tensor.Dimensions]);
        }


        public void AddControlImage(ImageTensor tensor)
        {
            ControlImageCount++;
            Dimensions.Add([.. tensor.Dimensions]);
        }


        public void AddAudio(AudioTensor tensor)
        {
            AudioMetadata.Add(new AudioTensorMetadata(tensor.SampleRate));
            Dimensions.Add([.. tensor.Dimensions]);
        }


        public void AddVideo(VideoSequence videoSequence)
        {
            VideoMetadata.Add(new VideoTensorMetadata(videoSequence.Frames.Length, videoSequence.FrameRate));
            VideoAudioMetadata.Add(new AudioTensorMetadata(videoSequence.HasAudio ? videoSequence.SampleRate : -1));
            foreach (var tensor in videoSequence.Frames)
                Dimensions.Add([.. tensor.Dimensions]);
            if (videoSequence.HasAudio)
                Dimensions.Add([.. videoSequence.Audio.Dimensions]);
        }
    }

    public record AudioTensorMetadata(int SampleRate);
    public record VideoTensorMetadata(int FrameCount, float FrameRate);
}
