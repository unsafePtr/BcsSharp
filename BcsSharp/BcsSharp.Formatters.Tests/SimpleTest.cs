using Microsoft.VisualStudio.TestTools.UnitTesting;
using BcsSharp.Core;

namespace BcsSharp.Formatters.Tests
{
    [TestClass]
    public class SimpleTest
    {
        [TestMethod]
        public void TestBasicInt()
        {
            var value = 42;
            var bytes = BcsSerializer.Serialize(value);
            var result = BcsSerializer.Deserialize<int>(bytes);
            Assert.AreEqual(value, result);
        }
        
        [TestMethod]
        public void TestBcsWriterDirectly()
        {
            var writer = new BcsWriter();
            writer.Write(42);
            var bytes = writer.ToBytes();
            Assert.IsNotNull(bytes);
            Assert.AreEqual(4, bytes.Length);
        }
    }
}