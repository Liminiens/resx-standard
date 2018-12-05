using System.IO;
using System.Text;

namespace Resx.Tests
{
    public static class TestResxFile
    {
        public static string File()
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            return Path.Combine(Directory.GetCurrentDirectory(), "Resource2.resx");
        }
    }
}