using Resx.Resources;
using System.Collections;
using System.Linq;
using Xunit;

namespace Resx.Tests
{
    public class ResXDataNode_Tests
    {
        [Fact]
        public void Enumerator_WhenCalled_ReturnsAllEntries()
        {
            var reader = new ResXResourceReader(TestResxFile.File);
            foreach (DictionaryEntry dictionaryEntry in reader)
            {

            }
        }
    }
}
