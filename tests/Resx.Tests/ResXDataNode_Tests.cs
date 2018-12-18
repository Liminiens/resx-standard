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
                    .Select(x => (ResXDataNode) x.Value)
                    .First(x => x.FileRef.FileName == "Resources\\cello82.wav");

                var type = typeof(MemoryStream);
                Assert.Equal(type.AssemblyQualifiedName, file.FileRef.TypeName);
            }
        }

        [Fact]
        public void Bitmap_WhenIsFile_IsCorrect()
        {
            using (var reader = new ResXResourceReader(TestResxFile.File()))
            {
                reader.UseResXDataNodes = true;

                var file = reader
                    .Cast<DictionaryEntry>()
                    .Select(x => (ResXDataNode) x.Value)
                    .First(x => x.FileRef.FileName == "Resources\\Image1.bmp");

                var value = (Bitmap)file.GetValue((ITypeResolutionService)null);

                var typeName = file.GetValueTypeName((ITypeResolutionService)null);

                Assert.NotNull(value);
                Assert.False(value.Size.IsEmpty);
                Assert.Equal(typeof(Bitmap).AssemblyQualifiedName, typeName);
            }
        }

        [Fact]
        public void Photo_WhenRetrieved_IsCorrect()
        {
            using (var reader = new ResXResourceReader(TestResxFile.File()))
            {
                reader.UseResXDataNodes = true;

                var photo = reader
                    .Cast<DictionaryEntry>()
                    .Select(x => (ResXDataNode) x.Value)
                    .First(x => x.Name == "Flowers");

                var value = (Bitmap) photo.GetValue((ITypeResolutionService) null);
                var typeName = photo.GetValueTypeName((ITypeResolutionService) null);

                Assert.NotNull(value);
                Assert.False(value.Size.IsEmpty);
                Assert.Equal(typeof(Bitmap).AssemblyQualifiedName, typeName);
            }
        }

        [Fact]
        public void Other_WhenRetrieved_IsCorrect()
        {
            using (var reader = new ResXResourceReader(TestResxFile.File()))
            {
                reader.UseResXDataNodes = true;

                var other = reader
                    .Cast<DictionaryEntry>()
                    .Select(x => (ResXDataNode) x.Value)
                    .First(x => x.FileRef.FileName == "Resources\\cello82.mp3");

                var value = (byte[]) other.GetValue((ITypeResolutionService) null);
                var typeName = other.GetValueTypeName((ITypeResolutionService) null);

                Assert.NotNull(value);
                Assert.Equal(@"Resources\cello82.mp3", other.FileRef.FileName);
                Assert.True(value.Length > 0);
                Assert.Equal(typeof(byte[]).AssemblyQualifiedName, typeName);
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
                    .Select(x => (ResXDataNode) x.Value)
                    .First(x => x.FileRef.FileName == "Resources\\Package.ico");

                var value = (byte[]) file.GetValue((ITypeResolutionService) null);
                var typeName = file.GetValueTypeName((ITypeResolutionService) null);


                Assert.NotNull(value);
                Assert.Equal(@"Resources\Package.ico", file.FileRef.FileName);
                Assert.True(value.Length > 0);
                Assert.Equal(typeof(byte[]).AssemblyQualifiedName, typeName);
            }
        }

        [Fact]
        public void String_WhenRetrieved_IsCorrect()
        {
            using (var reader = new ResXResourceReader(TestResxFile.File()))
            {
                reader.UseResXDataNodes = true;

                var str = reader
                    .Cast<DictionaryEntry>()
                    .Select(x => (ResXDataNode) x.Value)
                    .First(x => x.Name == "Example");

                var value = (string) str.GetValue((ITypeResolutionService) null);

                Assert.NotNull(value);
                Assert.Equal("Something", value);
                Assert.Equal("Comment", str.Comment);
            }
        }
    }
}