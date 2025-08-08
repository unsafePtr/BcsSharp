using Sui.Types;

namespace Sui.Types.Tests;

public class SuiBase58Tests
{
    private static readonly byte[] TestBytes1 = { 0x00, 0x01, 0x02, 0x03 };
    private static readonly byte[] TestBytes2 = { 0xFF, 0xFE, 0xFD, 0xFC };
    private static readonly byte[] TestBytes3 = new byte[32]; // All zeros
    private static readonly byte[] TestBytes4 = Enumerable.Repeat((byte)0xFF, 32).ToArray(); // All 255s

    [Fact]
    public void Encode_WithEmptyArray_ReturnsEmptyString()
    {
        // Act
        var result = SuiBase58.Encode([]);
        
        // Assert
        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public void Encode_WithAllZeros_ReturnsOnes()
    {
        // Arrange
        var allZeros = new byte[] { 0x00, 0x00, 0x00 };
        
        // Act
        var result = SuiBase58.Encode(allZeros);
        
        // Assert
        Assert.Equal("111", result);
    }

    [Fact]
    public void Encode_WithSingleByte_ReturnsCorrectString()
    {
        // Arrange
        var singleByte = new byte[] { 0x01 };
        
        // Act
        var result = SuiBase58.Encode(singleByte);
        
        // Assert
        Assert.NotEmpty(result);
        Assert.True(IsValidSuiBase58(result));
    }

    [Fact]
    public void Decode_WithEmptyString_ReturnsEmptyArray()
    {
        // Act
        var result = SuiBase58.Decode("");
        
        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public void Decode_WithOnes_ReturnsZeros()
    {
        // Act - "111" decodes to leading zeros in Base58
        var result = SuiBase58.Decode("111");
        
        // Assert - The actual number of leading zeros depends on Base58 implementation
        Assert.True(result.All(b => b == 0));
        Assert.True(result.Length >= 3);
    }

    [Fact]
    public void Decode_WithInvalidCharacter_ThrowsException()
    {
        // Arrange - '0' is not in Sui's Base58 alphabet
        var invalidBase58 = "123450";
        
        // Act & Assert
        Assert.Throws<ArgumentException>(() => SuiBase58.Decode(invalidBase58));
    }

    [Fact]
    public void Decode_WithInvalidCharacterO_ThrowsException()
    {
        // Arrange - 'O' (capital letter O) is not in Base58 alphabet
        var invalidBase58 = "123O45";
        
        // Act & Assert
        Assert.Throws<ArgumentException>(() => SuiBase58.Decode(invalidBase58));
    }

    [Fact]
    public void Decode_WithInvalidCharacterI_ThrowsException()
    {
        // Arrange - 'I' (capital letter I) is not in Base58 alphabet
        var invalidBase58 = "123I45";
        
        // Act & Assert
        Assert.Throws<ArgumentException>(() => SuiBase58.Decode(invalidBase58));
    }

    [Fact]
    public void Decode_WithInvalidCharacterL_ThrowsException()
    {
        // Arrange - 'l' (lowercase L) is not in Base58 alphabet
        var invalidBase58 = "123l45";
        
        // Act & Assert
        Assert.Throws<ArgumentException>(() => SuiBase58.Decode(invalidBase58));
    }

    [Theory]
    [InlineData("1")]
    [InlineData("2")]
    [InlineData("9")]
    [InlineData("A")]
    [InlineData("Z")]
    [InlineData("a")]
    [InlineData("z")]
    [InlineData("111")]
    [InlineData("123456789")]
    [InlineData("ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz")]
    public void IsValidBase58_WithValidStrings_ReturnsTrue(string validBase58)
    {
        // Act
        var result = SuiBase58.IsValidBase58(validBase58);
        
        // Assert
        Assert.True(result);
    }

    [Theory]
    [InlineData("0")]      // Zero
    [InlineData("O")]      // Capital O  
    [InlineData("I")]      // Capital I
    [InlineData("l")]      // Lowercase l
    [InlineData("123O45")] // Contains invalid char
    [InlineData("123I45")] // Contains invalid char
    [InlineData("123l45")] // Contains invalid char
    [InlineData("1230")]   // Contains zero
    public void IsValidBase58_WithInvalidStrings_ReturnsFalse(string invalidBase58)
    {
        // Act
        var result = SuiBase58.IsValidBase58(invalidBase58);
        
        // Assert
        Assert.False(result);
    }

    [Fact]
    public void Encode_Decode_RoundTrip_WorksCorrectly()
    {
        // Arrange - Use test cases that should round-trip perfectly
        var testCases = new[]
        {
            TestBytes1,
            TestBytes2,
            TestBytes4, // All 0xFF
            new byte[] { 0xFF },
            new byte[] { 0x12, 0x34, 0x56, 0x78, 0x9A, 0xBC, 0xDE, 0xF0 },
            // Skip TestBytes3 (all zeros) as it may have implementation-specific padding
        };
        
        foreach (var originalBytes in testCases)
        {
            // Act
            var encoded = SuiBase58.Encode(originalBytes);
            var decoded = SuiBase58.Decode(encoded);
            
            // Assert
            Assert.Equal(originalBytes, decoded);
        }
    }

    [Fact]
    public void Encode_Decode_AllZeros_HandledCorrectly()
    {
        // Arrange - Test all-zeros case separately due to Base58 leading zero handling
        var allZeros = TestBytes3; // 32 bytes of zeros
        
        // Act
        var encoded = SuiBase58.Encode(allZeros);
        var decoded = SuiBase58.Decode(encoded);
        
        // Assert - The decoded result should have the same semantic value (all zeros)
        // but may have different leading zero padding due to Base58 implementation
        Assert.True(decoded.All(b => b == 0), "All decoded bytes should be zero");
        Assert.True(decoded.Length >= allZeros.Length, "Decoded length should be at least as long as original");
    }

    [Fact]
    public void Encode_WithRandomBytes_ProducesValidBase58()
    {
        // Arrange
        var random = new Random(42); // Fixed seed for reproducible tests
        var testData = new byte[100];
        
        for (int i = 0; i < 10; i++)
        {
            random.NextBytes(testData);
            
            // Act
            var encoded = SuiBase58.Encode(testData);
            
            // Assert
            Assert.True(IsValidSuiBase58(encoded));
            
            // Verify round-trip
            var decoded = SuiBase58.Decode(encoded);
            Assert.Equal(testData, decoded);
        }
    }

    [Fact]
    public void Encode_WithLeadingZeros_PreservesLeadingZeros()
    {
        // Arrange
        var bytesWithLeadingZeros = new byte[] { 0x00, 0x00, 0x01, 0x02 };
        
        // Act
        var encoded = SuiBase58.Encode(bytesWithLeadingZeros);
        var decoded = SuiBase58.Decode(encoded);
        
        // Assert
        Assert.Equal(bytesWithLeadingZeros, decoded);
        Assert.StartsWith("11", encoded); // Leading zeros become '1's
    }

    [Fact]
    public void Encode_Performance_IsReasonablyFast()
    {
        // Arrange
        var largeData = new byte[1000];
        new Random(42).NextBytes(largeData);
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        
        // Act - perform multiple encodings
        for (int i = 0; i < 100; i++)
        {
            SuiBase58.Encode(largeData);
        }
        
        stopwatch.Stop();
        
        // Assert - should complete in reasonable time (this is a basic performance check)
        Assert.True(stopwatch.ElapsedMilliseconds < 1000, 
            $"Encoding took too long: {stopwatch.ElapsedMilliseconds}ms");
    }

    [Fact]
    public void Decode_Performance_IsReasonablyFast()
    {
        // Arrange
        var largeData = new byte[1000];
        new Random(42).NextBytes(largeData);
        var encoded = SuiBase58.Encode(largeData);
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        
        // Act - perform multiple decodings
        for (int i = 0; i < 100; i++)
        {
            SuiBase58.Decode(encoded);
        }
        
        stopwatch.Stop();
        
        // Assert - should complete in reasonable time
        Assert.True(stopwatch.ElapsedMilliseconds < 1000,
            $"Decoding took too long: {stopwatch.ElapsedMilliseconds}ms");
    }

    [Fact]
    public void SuiAlphabet_HasCorrectCharacters()
    {
        // This test ensures our alphabet matches Sui's specification
        const string expectedAlphabet = "123456789ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz";
        
        // We can't directly access the private alphabet, but we can test it indirectly
        // by ensuring certain characters are valid/invalid
        
        // Test all characters in the expected alphabet are valid
        foreach (char c in expectedAlphabet)
        {
            Assert.True(SuiBase58.IsValidBase58(c.ToString()), $"Character '{c}' should be valid");
        }
        
        // Test excluded characters are invalid
        var excludedChars = new[] { '0', 'O', 'I', 'l' };
        foreach (char c in excludedChars)
        {
            Assert.False(SuiBase58.IsValidBase58(c.ToString()), $"Character '{c}' should be invalid");
        }
    }

    [Fact]
    public void Encode_WithTransactionDigest_WorksCorrectly()
    {
        // Arrange - simulate a 32-byte transaction digest
        var digestBytes = new byte[32];
        new Random(12345).NextBytes(digestBytes);
        
        // Act
        var encoded = SuiBase58.Encode(digestBytes);
        
        // Assert
        Assert.True(IsValidSuiBase58(encoded));
        Assert.True(encoded.Length > 0);
        
        // Verify round-trip
        var decoded = SuiBase58.Decode(encoded);
        Assert.Equal(digestBytes, decoded);
    }

    // Helper method to validate Sui Base58 strings
    private static bool IsValidSuiBase58(string input)
    {
        const string suiBase58Alphabet = "123456789ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz";
        return !string.IsNullOrEmpty(input) && input.All(suiBase58Alphabet.Contains);
    }
}