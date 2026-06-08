using NTDLS.EmbeddedResource;

namespace TestHarness
{
    internal class Program
    {
        static void Main()
        {
            var formattedText = EmbeddedResourceReader.Format(@"TextFiles\TestResource.txt", ["World"]);

            Console.WriteLine(formattedText);
        }
    }
}
