using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using BcsSharp.Core;

namespace BcsSharp.Tests
{
    [TestClass]
    public class MinimalFormatterTest
    {
        [TestMethod]
        public void TestBasicFormatterArchitecture()
        {
            // Test basic serialization with new architecture
            var value = 42;
            var bytes = BcsSerializer.Serialize(value);
            var deserialized = BcsSerializer.Deserialize<int>(bytes);
            
            Assert.AreEqual(value, deserialized);
        }
        
        [TestMethod]
        public void TestStringFormatterArchitecture()
        {
            var value = "Hello, BCS Formatter!";
            var bytes = BcsSerializer.Serialize(value);
            var deserialized = BcsSerializer.Deserialize<string>(bytes);
            
            Assert.AreEqual(value, deserialized);
        }
        
        [TestMethod]
        public void TestByteArrayFormatterArchitecture()
        {
            var value = new byte[] { 1, 2, 3, 4, 5 };
            var bytes = BcsSerializer.Serialize(value);
            var deserialized = BcsSerializer.Deserialize<byte[]>(bytes);
            
            CollectionAssert.AreEqual(value, deserialized);
        }
    }
}