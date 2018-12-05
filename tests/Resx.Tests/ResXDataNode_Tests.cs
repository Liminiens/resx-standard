using Resx.Resources;
using System.Collections;
using System.ComponentModel.Design;
using System.Drawing;
using System.IO;
using System.Linq;
using Xunit;

namespace Resx.Tests
{
    public class ResXDataNode_Tests
    {
        [Fact]
        public void Audio_WhenRetrieved_IsCorrect()
        {
            using (var reader = new ResXResourceReader(TestResxFile.File()))
            {
                reader.UseResXDataNodes = true;

                var file = reader
                    .Cast<DictionaryEntry>()
                    .Select(x => (ResXDataNode)x.Value)
                    .First(x => x.FileRef.FileName == "Resources\\cello82.wav");

                var type = typeof(MemoryStream);
                Assert.Equal(type.AssemblyQualifiedName, file.FileRef.TypeName);
            }
        }

        [Fact]
        public void Photo_WhenRetrieved_IsCorrect()
        {
            using (var reader = new ResXResourceReader(TestResxFile.File()))
            {
                reader.UseResXDataNodes = true;

                var file = reader
                    .Cast<DictionaryEntry>()
                    .Select(x => (ResXDataNode)x.Value)
                    .First(x => x.Name == "Flowers");

                var value = (Bitmap)file.GetValue((ITypeResolutionService)null);

                Assert.NotNull(value);
                Assert.False(value.Size.IsEmpty);
            }
        }

        [Fact]
        public void Other_WhenRetrieved_IsCorrect()
        {
            using (var reader = new ResXResourceReader(TestResxFile.File()))
            {
                reader.UseResXDataNodes = true;

                var file = reader
                    .Cast<DictionaryEntry>()
                    .Select(x => (ResXDataNode)x.Value)
                    .First(x => x.FileRef.FileName == "Resources\\cello82.mp3");

                var value = (byte[])file.GetValue((ITypeResolutionService)null);

                Assert.NotNull(value);
                Assert.True(value.Length > 0);
            }
        }

        [Fact]
        public void Icon_WhenRetrieved_IsCorrect()
        {
            using (var reader = new ResXResourceReader(TestResxFile.File()))
            {
                reader.UseResXDataNodes = true;

                var file = reader
                    .Cast<DictionaryEntry>()
                    .Select(x => (ResXDataNode)x.Value)
                    .First(x => x.FileRef.FileName == "Resources\\Package.ico");

                var value = (byte[])file.GetValue((ITypeResolutionService)null);

                Assert.NotNull(value);
                Assert.True(value.Length > 0);
            }
        }
    }
}
