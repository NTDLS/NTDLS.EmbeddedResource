using NTDLS.EmbeddedResource;

namespace TestHarness
{
    internal class Program
    {
        static void Main()
        {
            var formattedText = EmbeddedResource.Format(@"TextFiles\TestResource.txt", ["World"]);

            Console.WriteLine(formattedText);
        }
    }
}
