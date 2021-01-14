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

        [Option("--first-image-as-cover", CommandOptionType.NoValue, Description = "Use the first image found as cover. Overrides manually set cover, if any")]
        public bool FirstImageAsCover { get; }

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

        private async Task<int> OnExecuteAsync()
        {
            var banner = $"{nameof(CPubMake)} v{typeof(Program).Assembly.GetName().Version.ToString()}";
            Console.WriteLine(banner);

            if (string.IsNullOrEmpty(OutputPath))
            {
                Console.WriteLine("Specify an output file");
                return -1;
            }

            var outputFile = new FileInfo(OutputPath);
            var targetFiles = GenerateTargetFilesList();
            try
            {
                using (var outStream = outputFile.Open(FileMode.Create))
                using (var writer = new EPUBWriter(outStream))
                {
                    var metadata = writer.Metadata;
                    metadata.Title = !string.IsNullOrEmpty(Title) ? Title : Path.GetFileNameWithoutExtension(outputFile.Name);
                    metadata.Author = !string.IsNullOrEmpty(Author) ? Author : nameof(CPubMake);
                    metadata.Publisher = !string.IsNullOrEmpty(Publisher) ? Publisher : nameof(CPubMake);
                    metadata.Description = Description;
                    if (!string.IsNullOrEmpty(Tags))
                    {
                        foreach (var i in Tags.Split(',').Select(d => d.Trim()))
                        {
                            metadata.Tags.Add(i);
                        }
                    }

                    if (targetFiles.cover != null)
                    {
                        Console.WriteLine($"Adding {targetFiles.cover.Name} as cover");
                        await AddImageToEpub(writer, targetFiles.cover, true);
                    }

                    foreach (var i in targetFiles.pages)
                    {
                        Console.WriteLine($"Adding {i.Name} as page");
                        await AddImageToEpub(writer, i, false);
                    }

                    await writer.FinalizeAsync();
                }
            }
            catch
            {
                Console.WriteLine($"Error generating {outputFile.FullName}");
            }

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
         
            var cover = default(FileInfo);
            if (FirstImageAsCover)
            {
                cover = pages.First();
            }
            else if (!string.IsNullOrEmpty(CoverPath))
            {
                var testCover = new FileInfo(CoverPath);
                if (testCover.Exists)
                {
                    cover = testCover;
                }
            }

            //Remove cover from pages list if present
            if (cover != null)
            {
                pages = pages.Where(d => d.FullName != cover.FullName).ToList();
            }

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
                        await writer.SetCoverAsync(imageStream, true);
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
            }
        }

        private void GetSupportedFilesRecursive(IList<FileInfo> files, DirectoryInfo target)
        {
            foreach (var i in target.EnumerateFiles().Where(d => SupportedImageExtension.Contains(d.Extension)).OrderBy(d => d.Name))
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
