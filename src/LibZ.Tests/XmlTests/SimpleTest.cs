using System.Linq;
using System.Xml.Linq;
using Xunit;

namespace LibZ.Tests.XmlTests
{
    public class SimpleTest
    {
        //C:\work\DenebLab\LibZ\src\LibZ.Tests\data\app.dll.config

        [Fact]
        public void FactMethodName()
        {
            var doc = XDocument.Load("C:\\work\\DenebLab\\LibZ\\src\\LibZ.Tests\\data\\app.dll.config");
            var txt = doc.Descendants().FirstOrDefault(p => p.Name.LocalName == "assemblyBinding")?.ToString();
        }
    }
}