using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using BcsSharp.Core;
using BcsSharp.Core.Resolvers;
using Nethermind.Int256;

namespace BcsSharp.Formatters.Tests
{
    [TestClass]
    public class FormatterTests
    {
        [TestMethod]
        public void TestPrimitiveFormatters()
        {
            // Test various primitive types
            TestValue<byte>(42);
            TestValue<sbyte>(-42);
            TestValue<ushort>(1000);
            TestValue<short>(-1000);
            TestValue<uint>(100000);
            TestValue<int>(-100000);
            TestValue<ulong>(1000000000UL);
            TestValue<long>(-1000000000L);
            TestValue<bool>(true);
            TestValue<bool>(false);
            TestValue<string>("Hello, BCS!");
            TestValue<string>("");
        }
        
        [TestMethod]
        public void TestCollectionFormatters()
        {
            // Test arrays
            TestValue(new int[] { 1, 2, 3, 4, 5 });
            TestValue(new string[] { "hello", "world", "" });
            TestValue(new byte[] { 0x01, 0x02, 0x03, 0xFF });
            
            // Test lists
            TestValue(new List<int> { 1, 2, 3, 4, 5 });
            TestValue(new List<string> { "hello", "world", "" });
            
            // Test empty collections
            TestValue(new int[0]);
            TestValue(new List<int>());
        }
        
        [TestMethod]
        public void TestLargeIntegers()
        {
            TestValue(UInt128.MaxValue);
            TestValue(Int128.MaxValue);
            TestValue(Int128.MinValue);
            TestValue(UInt256.Parse("0x123456789ABCDEF123456789ABCDEF123456789ABCDEF123456789ABCDEF12345678"));
        }
        
        [TestMethod]
        public void TestSerializationMethods()
        {
            var testData = new List<string> { "test", "data", "serialization" };
            
            // Test hex serialization
            var hex = BcsSerializer.SerializeToHex(testData);
            var deserialized1 = BcsSerializer.DeserializeFromHex<List<string>>(hex);
            CollectionAssert.AreEqual(testData, deserialized1);
            
            // Test base64 serialization
            var base64 = BcsSerializer.SerializeToBase64(testData);
            var deserialized2 = BcsSerializer.DeserializeFromBase64<List<string>>(base64);
            CollectionAssert.AreEqual(testData, deserialized2);
        }
        
        [TestMethod]
        public void TestSerializedSize()
        {
            // Test deterministic sizes
            Assert.AreEqual(1, BcsSerializer.GetSerializedSize<byte>(42));
            Assert.AreEqual(4, BcsSerializer.GetSerializedSize<int>(12345));
            Assert.AreEqual(8, BcsSerializer.GetSerializedSize<long>(123456789L));
            
            // Test string size calculation
            var str = "Hello";
            var expectedSize = 1 + System.Text.Encoding.UTF8.GetByteCount(str); // ULEB + content
            Assert.AreEqual(expectedSize, BcsSerializer.GetSerializedSize(str));
            
            // Test array size calculation
            var intArray = new int[] { 1, 2, 3 };
            var expectedArraySize = 1 + (3 * 4); // ULEB length + 3 * sizeof(int)
            Assert.AreEqual(expectedArraySize, BcsSerializer.GetSerializedSize(intArray));
        }
        
        [TestMethod]
        public void TestPerformance()
        {
            // Test that the new ref struct architecture is indeed performant
            var data = new int[1000];
            for (int i = 0; i < data.Length; i++)
            {
                data[i] = i;
            }
            
            // Measure serialization performance
            var start = DateTime.Now;
            for (int i = 0; i < 1000; i++)
            {
                var bytes = BcsSerializer.Serialize(data);
                var deserialized = BcsSerializer.Deserialize<int[]>(bytes);
            }
            var elapsed = DateTime.Now - start;
            
            // Should complete quickly (less than 1 second for 1000 iterations)
            Assert.IsTrue(elapsed.TotalSeconds < 1.0, $"Performance test took {elapsed.TotalSeconds} seconds");
        }
        
        private void TestValue<T>(T value)
        {
            // Serialize and deserialize
            var bytes = BcsSerializer.Serialize(value);
            var deserialized = BcsSerializer.Deserialize<T>(bytes);
            
            // Verify equality
            if (value is Array array1 && deserialized is Array array2)
            {
                CollectionAssert.AreEqual(array1, array2);
            }
            else if (value is System.Collections.ICollection collection1 && deserialized is System.Collections.ICollection collection2)
            {
                CollectionAssert.AreEqual(collection1, collection2);
            }
            else
            {
                Assert.AreEqual(value, deserialized);
            }
        }
    }
}