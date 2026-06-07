# NTDLS.EmbeddedResource

📦 Be sure to check out the NuGet package: https://www.nuget.org/packages/NTDLS.EmbeddedResource


# Example usage:
Make sure the file in the project is marked as "Embedded Resource" in its properties.

```csharp
var formattedText = EmbeddedResource.Format(@"TextFiles\TestResource.txt", ["World"]);

Console.WriteLine(formattedText);
```