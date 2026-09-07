using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TensorStack.Common;
using TensorStack.Common.Audio;
using TensorStack.Common.Common;
using TensorStack.Common.Video;
using TensorStack.Media.Audio;
using TensorStack.Media.Video;

namespace Amuse.App.Services
{
    public sealed class MediaService : IMediaService
    {
        private readonly Settings _settings;

        /// <summary>
        /// Initializes a new instance of the <see cref="VideoService"/> class.
        /// </summary>
        /// <param name="configuration">The configuration.</param>
        public MediaService(Settings settings)
        {
            _settings = settings;
        }


        /// <summary>
        /// Gets a new temporary video filename.
        /// </summary>
        /// <returns>System.String.</returns>
        public string GetTempFile(MediaType mediaType)
        {
            var extension = mediaType.GetExtension();
            return FileHelper.RandomFileName(_settings.DirectoryTemp, extension);
        }


        /// <summary>
        /// Get video information
        /// </summary>
        /// <param name="filename">The filename.</param>
        public async Task<VideoInfo> GetVideoInfoAsync(string filename)
        {
            return await VideoManager.LoadVideoInfoAsync(filename);
        }


        /// <summary>
        /// Get the Video stream
        /// </summary>
        /// <param name="filename">The filename.</param>
        public async Task<VideoInputStream> GetStreamAsync(string filename)
        {
            return await VideoInputStream.CreateAsync(filename);
        }


        /// <summary>
        /// Saves the stream with audio from the original input.
        /// </summary>
        /// <param name="videoStream">The video stream.</param>
        /// <param name="sourceFile">The source file.</param>
        /// <param name="resultVideoFile">The result video file.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A Task&lt;VideoInputStream&gt; representing the asynchronous operation.</returns>
        public async Task<VideoInputStream> SaveWithAudioAsync(IAsyncEnumerable<VideoFrame> videoStream, string sourceFile, string resultVideoFile, CancellationToken cancellationToken = default)
        {
            await videoStream.SaveAsync(resultVideoFile, cancellationToken: cancellationToken);
            await AudioManager.AddAudioAsync(resultVideoFile, sourceFile, cancellationToken);
            return new VideoInputStream(resultVideoFile);
        }


        /// <summary>
        /// Saves the stream with audio from the original input.
        /// </summary>
        /// <param name="videoInput">The video input.</param>
        /// <param name="videoFile">The video file.</param>
        /// <param name="frameProcessor">The frame processor.</param>
        /// <param name="cancellationToken">The cancellation token that can be used by other objects or threads to receive notice of cancellation.</param>
        public async Task<VideoInputStream> SaveWithAudioAsync(VideoInputStream videoInput, string videoOutputFile, Func<VideoFrame, Task<VideoFrame>> frameProcessor, CancellationToken cancellationToken = default)
        {
            var videoFrames = videoInput.GetAsync(cancellationToken: cancellationToken);
            await VideoManager.WriteVideoStreamAsync(videoOutputFile, videoFrames, frameProcessor, _settings.ReadBuffer, _settings.ReadBuffer, _settings.VideoCodec, cancellationToken: cancellationToken);
            await AudioManager.AddAudioAsync(videoOutputFile, videoInput.SourceFile, cancellationToken);
            return await VideoInputStream.CreateAsync(videoOutputFile);
        }


        /// <summary>
        /// Saves the stream with audio from the specified AudioTimeline.
        /// </summary>
        /// <param name="videoStream">The video stream.</param>
        /// <param name="audioTimeline">The audio timeline.</param>
        /// <param name="cancellationToken">The cancellation token that can be used by other objects or threads to receive notice of cancellation.</param>
        public async Task<VideoInputStream> SaveWithAudioAsync(VideoInputStream videoStream, AudioTimeline audioTimeline, CancellationToken cancellationToken = default)
        {
            var audioOutput = string.Empty;
            try
            {
                audioOutput = await AudioManager.CreateAudioTimelineAsync(audioTimeline, cancellationToken);
                await AudioManager.AddAudioAsync(videoStream.SourceFile, audioOutput, cancellationToken);
                return await VideoInputStream.CreateAsync(videoStream.SourceFile);
            }
            finally
            {
                FileHelper.DeleteFile(audioOutput);
            }
        }
    }


    public interface IMediaService : IVideoService
    {
        string GetTempFile(MediaType mediaType);
        Task<VideoInputStream> GetStreamAsync(string filename);
        Task<VideoInputStream> SaveWithAudioAsync(IAsyncEnumerable<VideoFrame> processedVideo, string sourceFile, string resultVideoFile, CancellationToken cancellationToken = default);
        Task<VideoInputStream> SaveWithAudioAsync(VideoInputStream videoInput, string videoOutputFile, Func<VideoFrame, Task<VideoFrame>> frameProcessor, CancellationToken cancellationToken = default);
        Task<VideoInputStream> SaveWithAudioAsync(VideoInputStream videoStream, AudioTimeline audioTimeline, CancellationToken cancellationToken = default);
    }
}
