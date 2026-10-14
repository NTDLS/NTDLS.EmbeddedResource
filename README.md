# NTDLS.EmbeddedResource

A small utility for reading and enumerating embedded resources (text files, images, etc.) in .NET applications. It lets you reference a resource by a simple slash-separated path — without having to know or spell out the assembly's fully-qualified dotted resource name — and it caches results in memory so repeated reads are cheap. Targets .NET 8, 9, and 10.

📦 Be sure to check out the NuGet package: https://www.nuget.org/packages/NTDLS.EmbeddedResource

## When to use this package

You want to ship data files with your application — templates, configuration text, SQL scripts, images, or any other content that should be compiled into the assembly as an **Embedded Resource** — and you'd rather not deal with the awkwardness of .NET's manifest resource names. This package is useful when:

- You want to read embedded resources by a friendly path like `TextFiles/TestResource.txt` instead of guessing the generated name such as `MyProject.TextFiles.TestResource.txt`.
- You don't want to hard-code which assembly a resource lives in — all loaded assemblies are searched automatically.
- You need to load the same resource many times and want the bytes cached in memory (with a sliding expiration) so you only pay the cost once.
- You want to discover what resources exist at runtime, e.g. enumerate every `.txt` file under a folder prefix and process them.
- You need raw bytes (`LoadBytes`), decoded text (`LoadText`, UTF-8 by default with BOM stripping), or pre-formatted text (`Format`) from the same resource.

## Usage

First, make sure the file(s) you want to embed are marked as **Embedded Resource** in their properties (or via `<EmbeddedResource Include="..." />` in your `.csproj`). Then reference the resource by a slash- or backslash-separated path relative to its folder structure; lookups are case-insensitive.

### Load and format a text resource

```csharp
using NTDLS.EmbeddedResource;

// Loads TextFiles/TestResource.txt (contents: "Hello {0}") and formats it.
var formattedText = EmbeddedResourceReader.Format(@"TextFiles\TestResource.txt", ["World"]);

Console.WriteLine(formattedText); // Hello World
```

### Load raw text

```csharp
// Decodes the resource as UTF-8 (default) and strips any leading BOM.
var text = EmbeddedResourceReader.LoadText("TextFiles/TestResource.txt");

// Or specify an explicit encoding if your resource isn't UTF-8.
var latinText = EmbeddedResourceReader.LoadText("TextFiles/Latin.txt", System.Text.Encoding.Latin1);
```

### Load raw bytes (images, binaries, etc.)

```csharp
// Works for any embedded file type, not just text.
byte[] logoBytes = EmbeddedResourceReader.LoadBytes("Images/Logo.png");
```

### Enumerate resources under a folder

```csharp
// Lists every .txt file under the "TextFiles" folder across all loaded assemblies.
foreach (var resourceName in EmbeddedResourceReader.EnumerateResourceNames("TextFiles", "*.txt"))
{
    Console.WriteLine(resourceName); // e.g. MyProject/TextFiles/TestResource.txt
}

// The returned paths can be passed straight back into LoadText / LoadBytes / Format.
var first = EmbeddedResourceReader.LoadText(EmbeddedResourceReader.EnumerateResourceNames("TextFiles").First());
```

Notes:

- Paths may use `/` or `\` as separators and are matched case-insensitively.
- If no resource matches, an exception is thrown; if more than one matches, an "ambiguous resource name" exception is thrown.
- Loaded bytes are cached in a `MemoryCache` with a one-hour sliding expiration.

## License

[MIT](https://choosealicense.com/licenses/mit/)
