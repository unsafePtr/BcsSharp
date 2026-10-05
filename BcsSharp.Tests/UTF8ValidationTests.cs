using BcsSharp.Core;

namespace BcsSharp.Tests;

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

    // Every length prefix matches its payload, so a rejection can only come from the UTF-8 check, not from running out of input.
    [Theory]
    [InlineData("0180")]         // lone continuation byte
    [InlineData("01C2")]         // truncated 2-byte sequence
    [InlineData("02E080")]       // truncated 3-byte sequence
    [InlineData("03F08080")]     // truncated 4-byte sequence
    [InlineData("02FF00")]       // 0xFF never occurs in UTF-8
    [InlineData("02FE00")]       // 0xFE never occurs in UTF-8
    [InlineData("02C080")]       // overlong encoding of NUL
    [InlineData("03E08080")]     // overlong 3-byte encoding
    [InlineData("03EDA080")]     // UTF-16 high surrogate
    [InlineData("03EDBFBF")]     // UTF-16 low surrogate
    [InlineData("04F4908080")]   // code point above U+10FFFF
    [InlineData("04C2C28080")]   // lead byte where a continuation byte is expected
    [InlineData("058081828384")] // run of continuation bytes
    public void InvalidUTF8Bytes_ShouldThrowOnDeserialization(string hex)
    {
        var bytes = Convert.FromHexString(hex);

        var ex = Assert.Throws<InvalidOperationException>(() => BcsSerializer.Deserialize<string>(bytes));

        Assert.Equal("String is not valid UTF-8.", ex.Message);
    }

    [Fact]
    public void ReplacementCharacter_DecodesOnlyFromItsOwnEncoding()
    {
        Assert.Equal("�", BcsSerializer.Deserialize<string>(Convert.FromHexString("03EFBFBD")));

        var ex = Assert.Throws<InvalidOperationException>(() => BcsSerializer.Deserialize<string>(Convert.FromHexString("01FF")));

        Assert.Equal("String is not valid UTF-8.", ex.Message);
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
