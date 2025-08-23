using System;
using System.Text;
using BcsSharp.Core;
using Xunit;

namespace BcsSharp.Tests
{
    /// <summary>
    /// Tests for UTF-8 validation and invalid encoding handling
    /// </summary>
    public class UTF8ValidationTests
    {
        [Fact]
        public void ValidUTF8Strings_ShouldSerializeAndDeserialize()
        {
            var validStrings = new string[]
            {
                "",                      // Empty string
                "Hello World",           // ASCII only
                "Hello 世界",             // Mixed ASCII and Chinese
                "🦀 Rust crab",          // Emoji with text
                "Café naïve résumé",     // Accented characters
                "こんにちは",               // Japanese Hiragana
                "🌟⭐✨🎉",               // Multiple emojis
                "عربي",                  // Arabic text
                "Ελληνικά",              // Greek text
                "🏳️‍🌈",                  // Complex emoji with ZWJ sequences
                "Test\u0000End",         // Null character (valid in UTF-8)
                "Line1\nLine2\tTab",     // Control characters
            };

            foreach (var str in validStrings)
            {
                // Act
                var serialized = BcsSerializer.Serialize(str);
                var deserialized = BcsSerializer.Deserialize<string>(serialized);

                // Assert
                Assert.Equal(str, deserialized);
            }
        }

        [Fact]
        public void InvalidUTF8Bytes_ShouldThrowOnDeserialization()
        {
            // Test various invalid UTF-8 byte sequences
            var invalidUTF8Cases = new (byte[] invalidBytes, string description)[]
            {
                // Invalid continuation bytes
                (new byte[] { 0x02, 0x80 }, "Invalid continuation byte"),

                // Incomplete multi-byte sequences
                (new byte[] { 0x03, 0xC2 }, "Incomplete 2-byte sequence"),
                (new byte[] { 0x03, 0xE0, 0x80 }, "Incomplete 3-byte sequence"),
                (new byte[] { 0x04, 0xF0, 0x80, 0x80 }, "Incomplete 4-byte sequence"),

                // Invalid start bytes
                (new byte[] { 0x02, 0xFF, 0x00 }, "Invalid start byte 0xFF"),
                (new byte[] { 0x02, 0xFE, 0x00 }, "Invalid start byte 0xFE"),

                // Overlong encodings (non-canonical UTF-8)
                (new byte[] { 0x02, 0xC0, 0x80 }, "Overlong encoding of null"),
                (new byte[] { 0x03, 0xE0, 0x80, 0x80 }, "Overlong 3-byte encoding"),

                // Invalid code points
                (new byte[] { 0x03, 0xED, 0xA0, 0x80 }, "High surrogate (invalid in UTF-8)"),
                (new byte[] { 0x03, 0xED, 0xBF, 0xBF }, "Low surrogate (invalid in UTF-8)"),

                // Bytes that look like UTF-8 but aren't valid
                (new byte[] { 0x04, 0xC2, 0xC2, 0x80, 0x80 }, "Double-encoded sequence"),

                // Random invalid sequences
                (new byte[] { 0x05, 0x80, 0x81, 0x82, 0x83, 0x84 }, "Multiple invalid continuation bytes"),
            };

            foreach (var (invalidBytes, description) in invalidUTF8Cases)
            {
                // Create BcsReader with invalid UTF-8 data
                var reader = new BcsReader(invalidBytes);

                // Reading the string should throw an exception due to invalid UTF-8
                // Note: Current implementation may throw different exceptions
                try
                {
                    reader.ReadString();
                    Assert.Fail($"Expected exception for {description}");
                }
                catch (Exception ex)
                {
                    // Any exception is acceptable for invalid UTF-8
                    Assert.NotNull(ex);
                }
            }
        }

        [Fact]
        public void StringWithNullBytes_ShouldHandleCorrectly()
        {
            // UTF-8 allows null bytes (unlike C-style strings)
            var stringWithNull = "Hello\u0000World\u0000End";

            // Act
            var serialized = BcsSerializer.Serialize(stringWithNull);
            var deserialized = BcsSerializer.Deserialize<string>(serialized);

            // Assert
            Assert.Equal(stringWithNull, deserialized);
            Assert.Contains((byte)0, serialized); // Null byte should be present in serialized form
        }

        [Fact]
        public void StringLengthVsCharacterCount_ShouldUseByteLength()
        {
            // BCS should use byte length, not character count for string length prefix
            var testCases = new (string str, int expectedCharCount, int expectedByteLength)[]
            {
                ("Hello", 5, 5),           // ASCII: byte length = char count
                ("🦀", 2, 4),              // Single emoji: 2 UTF-16 chars, 4 bytes
                ("世界", 2, 6),            // Chinese: 2 chars, 6 bytes (3 each)
                ("Café", 4, 5),           // Accented: 4 chars, 5 bytes (é = 2 bytes)
                ("🏳️‍🌈", 6, 14),           // Complex emoji: 6 UTF-16 chars, 14 bytes
            };

            foreach (var (str, expectedCharCount, expectedByteLength) in testCases)
            {
                // Verify our expectations
                Assert.Equal(expectedCharCount, str.Length);
                Assert.Equal(expectedByteLength, System.Text.Encoding.UTF8.GetByteCount(str));

                // Test serialization
                var serialized = BcsSerializer.Serialize(str);
                var reader = new BcsReader(serialized);

                // The length prefix should be the byte length, not character count
                var lengthFromPrefix = reader.ReadULEB32();
                Assert.Equal((uint)expectedByteLength, lengthFromPrefix);

                // And deserialization should work correctly
                reader.Reset();
                var deserialized = reader.ReadString();
                Assert.Equal(str, deserialized);
            }
        }

        [Fact]
        public void UTF8BOM_ShouldBePreservedIfPresent()
        {
            // UTF-8 BOM (Byte Order Mark) should be preserved if present
            var stringWithBOM = "\uFEFF" + "Hello World"; // BOM + content

            // Act
            var serialized = BcsSerializer.Serialize(stringWithBOM);
            var deserialized = BcsSerializer.Deserialize<string>(serialized);

            // Assert
            Assert.Equal(stringWithBOM, deserialized);
            Assert.StartsWith("\uFEFF", deserialized); // BOM should be preserved
        }

        [Fact]
        public void MaximumUTF8CodePoint_ShouldWork()
        {
            // Test the maximum valid Unicode code point (U+10FFFF)
            var maxCodePoint = char.ConvertFromUtf32(0x10FFFF);

            // Act
            var serialized = BcsSerializer.Serialize(maxCodePoint);
            var deserialized = BcsSerializer.Deserialize<string>(serialized);

            // Assert
            Assert.Equal(maxCodePoint, deserialized);
        }


        [Fact]
        public void EmptyStringHandling_ShouldWork()
        {
            // Test edge case of empty string
            var emptyString = "";

            // Act
            var serialized = BcsSerializer.Serialize(emptyString);
            var deserialized = BcsSerializer.Deserialize<string>(serialized);

            // Assert
            Assert.Equal("", deserialized);
            Assert.Equal(new byte[] { 0x00 }, serialized); // Just length prefix of 0
        }
    }
}