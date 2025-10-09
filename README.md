# AuroraLib.Pixel.Formats

Provides support for various image and texture file formats built on top of [AuroraLib.Pixel](https://github.com/Venomalia/AuroraLib.Pixel).
The library provides efficient format support while preserving loaded images and storing their pixel data in native image structures.

### AuroraLib.Pixel.Formats
Provides support for common image and texture formats.

[![NuGet Package](https://img.shields.io/nuget/v/AuroraLib.Pixel.Formats.svg?style=flat-square&label=NuGet%20Package)](https://www.nuget.org/packages/AuroraLib.Pixel.Formats)

| Format  | Description                                                               |
|---------|----------------------------------------------------------------------------|
| **PNG** | Portable Network Graphics image format.                                   |
| **BMP** | Windows Bitmap image format.                                              |
| **DDS** | DirectDraw Surface texture format, commonly used for textures. |

> **Note:** All supported formats can be both read and written. BC6 and BC7 texture formats are currently not supported.

### AuroraLib.Pixel.Formats.Dolphin
Provides support for image and texture formats used by Nintendo's GameCube and Wii software.
The also supports generating Dolphin texture hashes.

[![NuGet Package](https://img.shields.io/nuget/v/AuroraLib.Pixel.Formats.Dolphin.svg?style=flat-square&label=NuGet%20Package)](https://www.nuget.org/packages/AuroraLib.Pixel.Formats.Dolphin)

| Format  | Description                                                       |
| ------- | ----------------------------------------------------------------- |
| **BTI** | Nintendo Binary Texture Image for GameCube & Wii.                 |
| **TPL** | Nintendo Texture Palette Library for GameCube & Wii.              |
| **TXE** | Nintendo Dolphin texture format used by *Pikmin*.                 |

> **Note:** All supported formats can be both read and written. Texture formats that contain sampling information preserve this information in the image metadata.

The Dolphin texture hash is automatically stored in the image metadata when a supported Dolphin texture is loaded.
```csharp
using IImage image = new BTI().ReadImage("texture.bti");

if (image.Metadata?.Text.TryGetValue("DolphinHash", out string? hash) == true)
{
    Console.WriteLine(hash);
}
```

## Example

### Load an Image Using a Specific Image Format
``` csharp
	using IImage image = new PNG().ReadImage("image.png");
```

### Save an Image Using a Specific Image Format
``` csharp
	using var image = new MemoryImage<RGB565>(10, 10);
	new PNG().WriteImage(image, "image.png");
```

### Check If a File Matches a Specific Image Format
``` csharp
	using FileStream source = File.OpenRead("image.png");
	bool isPng = PNG.IsMatchStatic(source);
```

### Load Multiple Images from an Image Container
``` csharp
	List<IImage> images = new TPL().ReadImages("image.tpl");
```

### Automatically Detect a Supported Image Format

``` csharp
FormatDictionary formats = new FormatDictionary(new Assembly[]
{
    typeof(PNG).Assembly, // Base formats assembly
    typeof(BTI).Assembly, // Dolphin formats assembly
});

using FileStream source = new("input.dat", FileMode.Open, FileAccess.Read, FileShare.Read);
ReadOnlySpan<char> fileName = Path.GetFileName("input.dat");

// Identify the image format
if (formats.Identify(source, fileName, out IFormatInfo format) && format.Class != null)
{
    // Create an instance of the detected format
    var instance = format.CreateInstance();

    if (instance is IImageDecoder decoder)
    {
        using IImage image = decoder.ReadImage(source);
    }
	else if (instance is IImageContainerFormat containerDecoder)
	{
		List<IImage> images = containerDecoder.ReadImages(source);
	}
}
```
