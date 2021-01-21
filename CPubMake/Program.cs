using CPubLib;
using McMaster.Extensions.CommandLineUtils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace CPubMake
{
    [Command(Name = "cpubmake", Description = "Make fixed layout epubs from images")]
    [HelpOption("-?")]
    class Program
    {
        private static ISet<string> SupportedImageExtension { get; } = new HashSet<string> { ".jpg", ".jpeg", ".png", ".gif" };
        private const char TagsSeparator = ',';

        public static Task Main(string[] args) => CommandLineApplication.ExecuteAsync<Program>(args);

        [Option("-c|--cover", CommandOptionType.SingleValue, Description = "Path to image to include as cover")]
        [FileExists]
        public string CoverPath { get; }

        [Option("-i|--image", CommandOptionType.MultipleValue, Description = "Path to image to include in epub. Specify multiple times in the order in which images should be included")]
        [FileExists]
        public IReadOnlyList<string> InputImagePaths { get; }

        [Option("-d|--directory", CommandOptionType.MultipleValue, Description = "Path to image folder to include in epub. All files of supported format found inside will be included in the epub in alphabetical path order")]
        [DirectoryExists]
        public IReadOnlyList<string> InputDirectoryPaths { get; }

        [Option("-o|--output", CommandOptionType.SingleValue, Description = "Path to output file")]
        [LegalFilePath]
        public string OutputPath { get; }

        [Option("--title", CommandOptionType.SingleValue)]
        public string Title { get; }

        [Option("--author", CommandOptionType.SingleValue)]
        public string Author { get; }

        [Option("--publisher", CommandOptionType.SingleValue)]
        public string Publisher { get; }

        [Option("--description", CommandOptionType.SingleValue)]
        public string Description { get; }

        [Option("--tags", CommandOptionType.SingleValue)]
        public string Tags { get; }

        [Option("--meta", CommandOptionType.MultipleValue, Description = "Custom metadata, specify as key=val")]
        public List<string> Metadata { get; }

        [Option("-rtl|--right-to-left", CommandOptionType.NoValue, Description = "Reading direction is right to left")]
        public bool RightToLeftReading { get; }

        private async Task<int> OnExecuteAsync()
        {
            if (string.IsNullOrEmpty(OutputPath))
            {
                Console.WriteLine("Specify an output file");
                return -1;
            }

            var outputFile = new FileInfo(OutputPath);
            var tempFile = new FileInfo(OutputPath + "_part");
            Console.WriteLine($"Generating {outputFile.Name}");

            var targetFiles = GenerateTargetFilesList();
            if (!targetFiles.pages.Any())
            {
                Console.WriteLine("No images for pages found");
                return -1;
            }

            try
            {
                using (var outStream = tempFile.Open(FileMode.Create))
                using (var writer = new EPUBWriter(outStream))
                {
                    var metadata = writer.Metadata;
                    metadata.Title = !string.IsNullOrEmpty(Title) ? Title : Path.GetFileNameWithoutExtension(outputFile.Name);
                    metadata.Author = !string.IsNullOrEmpty(Author) ? Author : nameof(CPubMake);
                    metadata.Publisher = !string.IsNullOrEmpty(Publisher) ? Publisher : nameof(CPubMake);
                    metadata.Description = Description;
                    metadata.RightToLeftReading = RightToLeftReading;
                    if (!string.IsNullOrEmpty(Tags))
                    {
                        foreach (var i in Tags.Split(',').Select(d => d.Trim()))
                        {
                            metadata.Tags.Add(i);
                        }
                    }

                    if (Metadata != null)
                    {
                        foreach (var i in Metadata)
                        {
                            var components = i.Split('=');
                            if (components.Length == 2)
                            {
                                metadata.Custom[components[0]] = components[1];
                            }
                        }
                    }

                    if (targetFiles.cover != null)
                    {
                        Console.Write("\r" + new string(' ', Console.WindowWidth) + "\r");
                        Console.Write("Adding cover");
                        await AddImageToEpub(writer, targetFiles.cover, true);
                    }

                    var ctr = 1;
                    foreach (var i in targetFiles.pages)
                    {
                        Console.Write("\r" + new string(' ', Console.WindowWidth) + "\r");
                        Console.Write($"Adding image {ctr}/{targetFiles.pages.Count}");
                        await AddImageToEpub(writer, i, false);
                        ctr++;
                    }

                    Console.WriteLine(string.Empty);
                    await writer.FinalizeAsync();
                }
            }
            catch
            {
                Console.WriteLine($"Error generating {outputFile.FullName}");
                return -1;
            }

            tempFile.MoveTo(outputFile.FullName, true);
            return 0;
        }

        private (IList<FileInfo> pages, FileInfo cover) GenerateTargetFilesList()
        {
            var pages = new List<FileInfo>();
            if (InputImagePaths != null)
            {
                pages.AddRange(InputImagePaths.Select(d => new FileInfo(d)));
            }

            var directories = InputDirectoryPaths != null ? InputDirectoryPaths.Select(d => new DirectoryInfo(d)).Where(d => d.Exists) : Enumerable.Empty<DirectoryInfo>();
            foreach (var i in directories)
            {
                GetSupportedFilesRecursive(pages, i);
            }

            if (!pages.Any())
            {
                return (pages, null);
            }

            var cover = pages.First();
            if (!string.IsNullOrEmpty(CoverPath))
            {
                var testCover = new FileInfo(CoverPath);
                if (testCover.Exists)
                {
                    cover = testCover;
                }
            }

            //Remove cover from pages list if present
            pages = pages.Where(d => d.FullName != cover.FullName).ToList();

            return (pages, cover);
        }

        private async Task AddImageToEpub(EPUBWriter writer, FileInfo imageFile, bool asCover)
        {
            if (!imageFile.Exists)
            {
                Console.WriteLine($"{imageFile.FullName} not found");
            }

            try
            {
                using (var imageStream = imageFile.OpenRead())
                {
                    if (asCover)
                    {
                        await writer.SetCoverAsync(imageStream, false);
                    }
                    else
                    {
                        await writer.AddPageAsync(imageStream);
                    }
                }
            }
            catch
            {
                Console.WriteLine($"Unable to add {imageFile.FullName} to epub");
                throw;
            }
        }

        private void GetSupportedFilesRecursive(IList<FileInfo> files, DirectoryInfo target)
        {
            foreach (var i in target.EnumerateFiles().Where(d => SupportedImageExtension.Contains(d.Extension.ToLowerInvariant())).OrderBy(d => d.Name))
            {
                files.Add(i);
            }

            foreach (var i in target.EnumerateDirectories().OrderBy(d => d.Name))
            {
                GetSupportedFilesRecursive(files, i);
            }
        }
    }
}
