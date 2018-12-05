using Resx.Resources;
using System.Collections;
using System.ComponentModel.Design;
using System.Resources;

namespace Test
{
    class Program
    {
        static void Main(string[] args)
        {
            var reader =
                new ResXResourceReader("C:\\Users\\User\\Documents\\Visual Studio 2017\\Projects\\Resx\\tests\\Resx.Tests\\bin\\Debug\\netcoreapp2.0\\Resource2.resx")
                {
                    UseResXDataNodes = true
                };
            foreach (DictionaryEntry entry in reader)
            {
                var node = (ResXDataNode)entry.Value;
                //FileRef is null if it is not a file reference.
                if (node.FileRef == null)
                {
                    //Spell check your value.
                    var value = node.GetValue((ITypeResolutionService)null);
                }
            }
        }
    }
}
