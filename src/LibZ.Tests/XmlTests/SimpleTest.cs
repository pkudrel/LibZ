using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using Xunit;

namespace LibZ.Tests.XmlTests
{
    public class SimpleTest
    {
        public void Parse(string t)
        {
            var pos = 0;

            var list = new List<DependentAssembly>();
            while (true)
            {
                var start = t.IndexOf("<dependentAssembly>", pos, StringComparison.Ordinal);
                if (start < 0) break;
                var name = GetValue(t, "name", start);
                var publicKeyToken = GetValue(t, "publicKeyToken", start);
                var culture = GetValue(t, "culture", start);
                var oldVersion = GetValue(t, "oldVersion", start);
                var newVersion = GetValue(t, "newVersion", start);
                var record = new DependentAssembly(name, publicKeyToken, culture, oldVersion, newVersion);
                list.Add(record);
                var end = t.IndexOf("</dependentAssembly>", start, StringComparison.Ordinal);
                if (end < 0) break;
                pos = end;
            }
        }

        private string GetValue(string txt, string name, int start)
        {
            var posToken = txt.IndexOf(name, start, StringComparison.OrdinalIgnoreCase);
            if (posToken < 0) return string.Empty;
            var posStart = txt.IndexOf("\"", posToken, StringComparison.OrdinalIgnoreCase);
            if (posStart < 0) return string.Empty;
            posStart++;

            var posEnd = txt.IndexOf("\"", posStart, StringComparison.OrdinalIgnoreCase);
            var ret = txt.Substring(posStart, posEnd - posStart);

            return ret;
        }

        internal class TokenValue
        {
            public TokenValue(bool valid)
            {
                Valid = valid;
            }

            public TokenValue(int posStart, int posEnd, string value)
            {
                Start = posStart;
                End = posEnd;
                Value = value;
            }

            public bool Valid { get; set; }
            public string Value { get; set; }
            public int End { get; set; }
            public int Start { get; set; }
        }

        internal class DependentAssembly
        {
            public DependentAssembly(string name, string publicKeyToken, string culture, string oldVersion,
                string newVersion)
            {
                Name = name;
                PublicKeyToken = publicKeyToken;
                Culture = culture;
                OldVersion = oldVersion;
                NewVersion = newVersion;
            }

            public string Name { get; set; }
            public string PublicKeyToken { get; set; }
            public string Culture { get; set; }
            public string OldVersion { get; set; }
            public string NewVersion { get; set; }
        }
        //C:\work\DenebLab\LibZ\src\LibZ.Tests\data\app.dll.config

        [Fact]
        public void FactMethodName()
        {
            var doc = XDocument.Load("C:\\work\\DenebLab\\LibZ\\src\\LibZ.Tests\\data\\app.dll.config");
            var txt = doc.Descendants().FirstOrDefault(p => p.Name.LocalName == "assemblyBinding")?.ToString();
            Parse(txt);
        }
    }
}