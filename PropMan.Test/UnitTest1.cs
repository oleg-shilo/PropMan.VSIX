using NUnit.Framework;
using OlegShilo.PropMan;
using static OlegShilo.PropMan.CSharpRefactor;

namespace PropMan.Test
{
    public class Tests
    {
        [Test]
        public void ProbeAsSimpleField()
        {
            FldInfo info = new CSharpRefactor().ProbeAsField("        int test;");

            Assert.True(info.IsValid);
            Assert.AreEqual("test", info.Name);
            Assert.AreEqual("int", info.AccessModifiersAndType);
            Assert.True(string.IsNullOrEmpty(info.Intitializer));
        }

        [Test]
        public void ProbeAsFieldWithInitializer()
        {
            var reflector = new CSharpRefactor();
            FldInfo info = reflector.ProbeAsField("        int test = 0;");

            Assert.True(info.IsValid);
            Assert.AreEqual("test", info.Name);
            Assert.AreEqual("int", info.AccessModifiersAndType);
            Assert.AreEqual("= 0", info.Intitializer);

            var property = reflector.EmittFullProperty(info);
        }
    }
}