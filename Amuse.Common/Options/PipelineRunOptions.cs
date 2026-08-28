namespace Amuse.Common
{
    public sealed record PipelineRunOptions
    {
        public GenerateImageOptions ImageOptions { get; set; }
        public GenerateVideoOptions VideoOptions { get; set; }
        public GenerateAudioOptions AudioOptions { get; set; }
        public GenerateTextOptions TextOptions { get; set; }
    }
}
