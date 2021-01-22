# CPubLib and CPubMake

[![Build status](https://ci.appveyor.com/api/projects/status/v7a2n4w3mng89ol5?svg=true)](https://ci.appveyor.com/project/Aftnet/cpublib)
[![NuGet version](https://img.shields.io/nuget/v/CPubLib.svg)](https://www.nuget.org/packages/CPubLib/)

A library to create epubs from collection of images and a command line app to create Epubs from image collections.

## CPubLib

- Creates fully compliant EPUBv3 files
- Non destructive: images are untouched and ordering preserved - easy to revert to CBZ or other archive without modifying reading order
- Can embed standard and custom metadata
- Intelligent handling of single image spreads (wide images meant overlap two pages)
- Tested and working on Adobe Digital Editions, Apple Books, Lithium, Kobo ereader

Get via Nuget. CPubLib works similarly to any framework writer (think TextWriter): it single use and is meant to share the lifecycle of the stream it operates on.

```C#
using(var outStream = get_some_stream()); // Doesn't need to be seekable
using (var writer = new EPUBWriter(outStream))
{
	var metadata = writer.Metadata; // Set the book metadata
	metadata.Title = !string.IsNullOrEmpty(Title) ? Title : Path.GetFileNameWithoutExtension(outputFile.Name);
	metadata.Author = !string.IsNullOrEmpty(Author) ? Author : nameof(CPubMake);
	metadata.Publisher = !string.IsNullOrEmpty(Publisher) ? Publisher : nameof(CPubMake);
	metadata.Description = Description;
	metadata.RightToLeftReading = RightToLeftReading;

	using(var imageStream = get_some_image_stream())
	{
		await writer.SetCoverAsync(imageStream, false); // Needs to be called before adding any page
	}

	using(var imageStream = get_some_other_image_stream())
	{
	    await writer.AddPageAsync(imageStream); // Add a page
	}

	using(var imageStream = get_some_image_stream())
	{
	    await writer.AddPageAsync(imageStream, "Chapter 1"); // Page that shows up in bookmarks
	}

	await writer.FinalizeAsync();
}
```

## CPubMake

Precompiled binaries for Windows, macoOS and Linux available [here](https://github.com/Aftnet/CPubLib/releases).
On macOS and Linux you may need to add executing permissions to the main executable (chmod +x /path/to/CPubMake) to run.

Show help with `CPubMake -?`

```
Make fixed layout epubs from images

Usage: cpubmake [options]

Options:
  -?                    Show help information
  -c|--cover            Path to image to include as cover
  -i|--image            Path to image to include in epub. Specify multiple times in the order in which images should be
                        included
  -d|--directory        Path to image folder to include in epub. All files of supported format found inside will be
                        included in the epub in alphabetical path order
  -o|--output           Path to output file
  --title               Title
  --author              Author
  --publisher           Publisher
  --description         Description
  --tags                Tags
  --meta				Custom metadata, specify as key=val
  -rtl|--right-to-left  Reading direction is right to left
```