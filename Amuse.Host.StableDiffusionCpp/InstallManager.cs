using Amuse.Common;
using System;
using System.Formats.Tar;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Threading.Tasks;
using TensorStack.Common.Common;

namespace Amuse.Host.StableDiffusionCpp
{
    internal static class InstallManager
    {
        /// <summary>
        /// Initialize StableDiffusion.cpp environment
        /// </summary>
        /// <param name="options">The options.</param>
        /// <param name="progressCallback">The progress callback.</param>
        public static async Task<bool> InitializeAsync(PipelineCreateOptions options, IProgress<PipelineProgress> progressCallback)
        {
            var workingDirectory = options.Directory;
            Directory.CreateDirectory(workingDirectory);
            var environmentDirectory = Path.Combine(workingDirectory, options.Environment);
            var applicationPath = Path.Combine(environmentDirectory, "sd-server.exe");
            if (options.Mode == EnvironmentMode.Update || options.Mode == EnvironmentMode.Reinstall || options.Mode == EnvironmentMode.Rebuild)
            {
                progressCallback.SendProgressMessage("Uninstall Environment...");
                FileHelper.DeleteDirectory(environmentDirectory);
                progressCallback.SendProgressMessage("Environment Uninstalled.");
            }

            if (File.Exists(applicationPath))
                return true; // Already Installed

            // Download StableDiffusion.cpp Requirements
            await DownloadRequirementsAsync(options.Requirements, workingDirectory, progressCallback);

            // Install StableDiffusion.cpp Requirements
            await InstallRequirementsAsync(options.Requirements, environmentDirectory, workingDirectory, progressCallback);

            // Verify StableDiffusion.cpp Install
            if (!File.Exists(applicationPath))
            {
                progressCallback.SendProgressMessage("Environment Install Failed.");
                FileHelper.DeleteDirectory(environmentDirectory);
                return false;
            }

            progressCallback.SendProgressMessage("Environment Install Complete.");
            return true;
        }


        /// <summary>
        /// Download environment requirements
        /// </summary>
        /// <param name="requirements">The requirements.</param>
        /// <param name="workingDirectory">The working directory.</param>
        /// <param name="progressCallback">The progress callback.</param>
        /// <returns>A Task representing the asynchronous operation.</returns>
        private static async Task DownloadRequirementsAsync(string[] requirements, string workingDirectory, IProgress<PipelineProgress> progressCallback)
        {
            progressCallback.SendProgressMessage("Download Environment...");
            using (var httpClient = new HttpClient())
            {
                foreach (var requirement in requirements)
                {
                    var name = Path.GetFileName(requirement);
                    var destination = Path.Combine(workingDirectory, name);
                    progressCallback.SendProgressMessage($"Downloading {name}...");
                    await DownloadFileAsync(httpClient, requirement, destination);
                }
            }
            progressCallback.SendProgressMessage("Download Environment Complete.");
        }


        /// <summary>
        /// Install environment requirements
        /// </summary>
        /// <param name="requirements">The requirements.</param>
        /// <param name="environmentDirectory">The environment directory.</param>
        /// <param name="workingDirectory">The working directory.</param>
        /// <param name="progressCallback">The progress callback.</param>
        private static async Task InstallRequirementsAsync(string[] requirements, string environmentDirectory, string workingDirectory, IProgress<PipelineProgress> progressCallback)
        {
            progressCallback.SendProgressMessage("Installing Environment...");
            Directory.CreateDirectory(environmentDirectory);
            foreach (var requirement in requirements)
            {
                var filename = Path.GetFileName(requirement);
                var requirementFile = Path.Combine(workingDirectory, filename);
                if (!File.Exists(requirementFile))
                    continue;

                progressCallback.SendProgressMessage($"Unpacking {filename}...");
                await UnpackRequirementAsync(requirementFile, environmentDirectory, workingDirectory);
            }
        }


        /// <summary>
        /// Unpacks the requirement.
        /// </summary>
        /// <param name="sourceFile">The source file.</param>
        /// <param name="environmentDirectory">The environment directory.</param>
        /// <param name="workingDirectory">The working directory.</param>
        private static async Task UnpackRequirementAsync(string sourceFile, string environmentDirectory, string workingDirectory)
        {
            var extension = Path.GetExtension(sourceFile);
            if (extension.Equals(".zip"))
            {
                await ExtractZipAsync(sourceFile, environmentDirectory);
                FileHelper.DeleteFile(sourceFile);
            }
            else if (extension.Equals(".gz"))
            {
                await ExtractTarGzAsync(sourceFile, environmentDirectory, workingDirectory);
                FileHelper.DeleteFile(sourceFile);
            }
            else
            {
                File.Move(sourceFile, Path.Combine(environmentDirectory, Path.GetFileName(sourceFile)));
            }
        }


        /// <summary>
        /// Extracts the zip contents.
        /// </summary>
        /// <param name="sourceFile">The source file.</param>
        /// <param name="environmentDirectory">The environment directory.</param>
        private static async Task ExtractZipAsync(string sourceFile, string environmentDirectory)
        {
            using (var archive = ZipFile.OpenRead(sourceFile))
            {
                foreach (var entry in archive.Entries)
                {
                    var relativePath = entry.FullName.Replace('/', '\\');
                    if (string.IsNullOrWhiteSpace(relativePath))
                        continue;

                    var filename = relativePath.TrimStart('\\').TrimEnd('\\');
                    var destination = Path.Combine(environmentDirectory, filename);
                    if (relativePath.EndsWith('\\'))
                    {
                        Directory.CreateDirectory(destination);
                        continue;
                    }

                    Directory.CreateDirectory(Path.GetDirectoryName(destination));
                    await entry.ExtractToFileAsync(destination, overwrite: true);
                }
            }
        }


        /// <summary>
        /// Extract tar gz contents.
        /// </summary>
        /// <param name="sourceFile">The source file.</param>
        /// <param name="environmentDirectory">The environment directory.</param>
        /// <param name="workingDirectory">The working directory.</param>
        private static async Task ExtractTarGzAsync(string sourceFile, string environmentDirectory, string workingDirectory)
        {
            using (var file = File.OpenRead(sourceFile))
            using (var gzip = new GZipStream(file, CompressionMode.Decompress))
            {
                if (IsBinFolderOnly(sourceFile))
                {
                    var tempDirectory = CreateTempDirectory(workingDirectory);
                    try
                    {
                        await TarFile.ExtractToDirectoryAsync(gzip, tempDirectory, overwriteFiles: true);
                        var binDirectory = FileHelper.FindDirectory(tempDirectory, "bin");
                        if (binDirectory?.Exists == true)
                        {
                            MoveDirectoryContents(binDirectory, environmentDirectory);
                        }
                    }
                    finally
                    {
                        FileHelper.DeleteDirectory(tempDirectory);
                    }
                }
                else
                {
                    await TarFile.ExtractToDirectoryAsync(gzip, environmentDirectory, overwriteFiles: true);
                }
            }
        }


        /// <summary>
        /// Download file
        /// </summary>
        /// <param name="httpClient">The HTTP client.</param>
        /// <param name="sourceUrl">The source URL.</param>
        /// <param name="destinationFile">The destination file.</param>
        private static async Task DownloadFileAsync(HttpClient httpClient, string sourceUrl, string destinationFile)
        {
            const int bufferSize = 1024 * 1024;
            using (var response = await httpClient.GetAsync(sourceUrl, HttpCompletionOption.ResponseHeadersRead))
            {
                response.EnsureSuccessStatusCode();
                await using (var inputStream = await response.Content.ReadAsStreamAsync())
                await using (var outputStream = new FileStream(destinationFile, FileMode.Create, FileAccess.Write, FileShare.None, bufferSize: bufferSize, useAsync: true))
                {
                    await inputStream.CopyToAsync(outputStream, bufferSize);
                }
            }
        }


        /// <summary>
        /// Moves the directory contents.
        /// </summary>
        /// <param name="source">The source.</param>
        /// <param name="destination">The destination.</param>
        private static void MoveDirectoryContents(DirectoryInfo source, string destination)
        {
            foreach (var file in source.EnumerateFiles())
            {
                file.MoveTo(Path.Combine(destination, file.Name));
            }

            foreach (var directory in source.EnumerateDirectories())
            {
                directory.MoveTo(Path.Combine(destination, directory.Name));
            }
        }


        /// <summary>
        /// Creates the temporary directory.
        /// </summary>
        /// <param name="directory">The directory.</param>
        /// <returns>System.String.</returns>
        private static string CreateTempDirectory(string directory)
        {
            var tempDirectory = new DirectoryInfo(Path.Combine(directory, Path.GetFileNameWithoutExtension(Path.GetRandomFileName())));
            tempDirectory.Create();
            return tempDirectory.FullName;
        }


        /// <summary>
        /// Determines whether to extract just the bin folder from the archive
        /// </summary>
        /// <param name="sourceFile">The source file.</param>
        /// <returns><c>true</c> if only extract bin contents; otherwise, <c>false</c>.</returns>
        private static bool IsBinFolderOnly(string sourceFile)
        {
            return sourceFile.Contains("therock-dist-windows");
        }

    }
}
