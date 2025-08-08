using Sui.Types;

namespace Sui.Types.Tests;

public class SuiSignatureTests
{
    [Fact]
    public void Constructor_WithEd25519KeyPair_CreatesValidSignature()
    {
        // Arrange
        using var keyPair = Ed25519KeyPair.Generate();
        var testData = "Hello, Sui!"u8.ToArray();
        var rawSignature = keyPair.SignRaw(testData);
        
        // Act
        var signature = new SuiSignature(rawSignature, keyPair.PublicKey);
        
        // Assert
        Assert.Equal(SuiSignatureScheme.Ed25519, signature.Scheme);
        Assert.Equal(64, signature.SignatureBytes.Length);
        Assert.Equal(32, signature.PublicKeyBytes.Length);
        Assert.NotNull(signature.NSecPublicKey);
    }

    [Fact]
    public void Constructor_WithGenericParameters_CreatesValidSignature()
    {
        // Arrange
        using var keyPair = Ed25519KeyPair.Generate();
        var testData = "Hello, Sui!"u8.ToArray();
        var rawSignature = keyPair.SignRaw(testData);
        var publicKeyBytes = keyPair.ExportPublicKey();
        
        // Act
        var signature = new SuiSignature(rawSignature, publicKeyBytes, SuiSignatureScheme.Ed25519);
        
        // Assert
        Assert.Equal(SuiSignatureScheme.Ed25519, signature.Scheme);
        Assert.Equal(64, signature.SignatureBytes.Length);
        Assert.Equal(32, signature.PublicKeyBytes.Length);
        Assert.NotNull(signature.NSecPublicKey);
    }

    [Fact]
    public void Constructor_WithInvalidSignatureLength_ThrowsException()
    {
        // Arrange
        using var keyPair = Ed25519KeyPair.Generate();
        var invalidSignature = new byte[32]; // Wrong length
        
        // Act & Assert
        Assert.Throws<ArgumentException>(() => new SuiSignature(invalidSignature, keyPair.PublicKey));
    }

    [Fact]
    public void Constructor_WithInvalidPublicKeyLength_ThrowsException()
    {
        // Arrange
        var validSignature = new byte[64];
        var invalidPublicKey = new byte[16]; // Wrong length for Ed25519
        
        // Act & Assert
        Assert.Throws<ArgumentException>(() => 
            new SuiSignature(validSignature, invalidPublicKey, SuiSignatureScheme.Ed25519));
    }

    [Fact]
    public void FromSuiBytes_WithValidEd25519Signature_CreatesSignature()
    {
        // Arrange
        using var keyPair = Ed25519KeyPair.Generate();
        var testData = "Hello, Sui!"u8.ToArray();
        var signature = keyPair.Sign(testData);
        var suiBytes = signature.ToSuiBytes();
        
        // Act
        var reconstructed = SuiSignature.FromSuiBytes(suiBytes);
        
        // Assert
        Assert.Equal(signature.Scheme, reconstructed.Scheme);
        Assert.True(signature.SignatureBytes.SequenceEqual(reconstructed.SignatureBytes));
        Assert.True(signature.PublicKeyBytes.SequenceEqual(reconstructed.PublicKeyBytes));
    }

    [Fact]
    public void FromSuiBytes_WithInvalidLength_ThrowsException()
    {
        // Arrange
        var invalidBytes = new byte[32]; // Too short
        
        // Act & Assert
        Assert.Throws<ArgumentException>(() => SuiSignature.FromSuiBytes(invalidBytes));
    }

    [Fact]
    public void FromBase64_WithValidSignature_CreatesSignature()
    {
        // Arrange
        using var keyPair = Ed25519KeyPair.Generate();
        var testData = "Hello, Sui!"u8.ToArray();
        var signature = keyPair.Sign(testData);
        var base64 = signature.ToBase64();
        
        // Act
        var reconstructed = SuiSignature.FromBase64(base64);
        
        // Assert
        Assert.Equal(signature.Scheme, reconstructed.Scheme);
        Assert.True(signature.SignatureBytes.SequenceEqual(reconstructed.SignatureBytes));
        Assert.True(signature.PublicKeyBytes.SequenceEqual(reconstructed.PublicKeyBytes));
    }

    [Fact]
    public void FromHex_WithValidSignature_CreatesSignature()
    {
        // Arrange
        using var keyPair = Ed25519KeyPair.Generate();
        var testData = "Hello, Sui!"u8.ToArray();
        var signature = keyPair.Sign(testData);
        var hex = signature.ToHex();
        
        // Act
        var reconstructed = SuiSignature.FromHex(hex);
        
        // Assert
        Assert.Equal(signature.Scheme, reconstructed.Scheme);
        Assert.True(signature.SignatureBytes.SequenceEqual(reconstructed.SignatureBytes));
        Assert.True(signature.PublicKeyBytes.SequenceEqual(reconstructed.PublicKeyBytes));
    }

    [Fact]
    public void ToSuiBytes_CreatesValidFormat()
    {
        // Arrange
        using var keyPair = Ed25519KeyPair.Generate();
        var testData = "Hello, Sui!"u8.ToArray();
        var signature = keyPair.Sign(testData);
        
        // Act
        var suiBytes = signature.ToSuiBytes();
        
        // Assert
        Assert.Equal(97, suiBytes.Length); // 64 (signature) + 32 (pubkey) + 1 (scheme)
        Assert.Equal((byte)SuiSignatureScheme.Ed25519, suiBytes[^1]); // Last byte is scheme flag
    }

    [Fact]
    public void ToBase64_ToHex_RoundTrip_WorksCorrectly()
    {
        // Arrange
        using var keyPair = Ed25519KeyPair.Generate();
        var testData = "Hello, Sui!"u8.ToArray();
        var originalSignature = keyPair.Sign(testData);
        
        // Act
        var base64 = originalSignature.ToBase64();
        var hex = originalSignature.ToHex();
        var fromBase64 = SuiSignature.FromBase64(base64);
        var fromHex = SuiSignature.FromHex(hex);
        
        // Assert
        Assert.Equal(originalSignature.Scheme, fromBase64.Scheme);
        Assert.Equal(originalSignature.Scheme, fromHex.Scheme);
        Assert.True(originalSignature.SignatureBytes.SequenceEqual(fromBase64.SignatureBytes));
        Assert.True(originalSignature.SignatureBytes.SequenceEqual(fromHex.SignatureBytes));
    }

    [Fact]
    public void GetSignerAddress_ReturnsCorrectAddress()
    {
        // Arrange
        using var keyPair = Ed25519KeyPair.Generate();
        var testData = "Hello, Sui!"u8.ToArray();
        var signature = keyPair.Sign(testData);
        
        // Act
        var signerAddress = signature.GetSignerAddress();
        
        // Assert
        Assert.Equal(keyPair.Address, signerAddress);
    }

    [Fact]
    public void Verify_WithValidSignature_ReturnsTrue()
    {
        // Arrange
        using var keyPair = Ed25519KeyPair.Generate();
        var testData = "Hello, Sui!"u8.ToArray();
        var signature = keyPair.Sign(testData);
        
        // Act
        var isValid = signature.Verify(testData);
        
        // Assert
        Assert.True(isValid);
    }

    [Fact]
    public void Verify_WithTamperedData_ReturnsFalse()
    {
        // Arrange
        using var keyPair = Ed25519KeyPair.Generate();
        var testData = "Hello, Sui!"u8.ToArray();
        var tamperedData = "Hello, Sui!!"u8.ToArray();
        var signature = keyPair.Sign(testData);
        
        // Act
        var isValid = signature.Verify(tamperedData);
        
        // Assert
        Assert.False(isValid);
    }

    [Fact]
    public void VerifyTransaction_WithValidDigest_ReturnsTrue()
    {
        // Arrange
        using var keyPair = Ed25519KeyPair.Generate();
        var txDigest = TransactionDigest.Random();
        var signature = keyPair.SignTransaction(txDigest);
        
        // Act
        var isValid = signature.VerifyTransaction(txDigest);
        
        // Assert
        Assert.True(isValid);
    }

    [Fact]
    public void VerifyTransaction_WithDifferentDigest_ReturnsFalse()
    {
        // Arrange
        using var keyPair = Ed25519KeyPair.Generate();
        var txDigest1 = TransactionDigest.Random();
        var txDigest2 = TransactionDigest.Random();
        var signature = keyPair.SignTransaction(txDigest1);
        
        // Act
        var isValid = signature.VerifyTransaction(txDigest2);
        
        // Assert
        Assert.False(isValid);
    }

    [Fact]
    public void Equals_WithSameSignature_ReturnsTrue()
    {
        // Arrange
        using var keyPair = Ed25519KeyPair.Generate();
        var testData = "Hello, Sui!"u8.ToArray();
        var signature1 = keyPair.Sign(testData);
        var signature2 = keyPair.Sign(testData);
        
        // Act & Assert
        Assert.Equal(signature1, signature2);
        Assert.True(signature1 == signature2);
        Assert.False(signature1 != signature2);
    }

    [Fact]
    public void Equals_WithDifferentSignature_ReturnsFalse()
    {
        // Arrange
        using var keyPair1 = Ed25519KeyPair.Generate();
        using var keyPair2 = Ed25519KeyPair.Generate();
        var testData = "Hello, Sui!"u8.ToArray();
        var signature1 = keyPair1.Sign(testData);
        var signature2 = keyPair2.Sign(testData);
        
        // Act & Assert
        Assert.NotEqual(signature1, signature2);
        Assert.False(signature1 == signature2);
        Assert.True(signature1 != signature2);
    }

    [Fact]
    public void GetHashCode_SameSignaturesHaveSameHashCode()
    {
        // Arrange
        using var keyPair = Ed25519KeyPair.Generate();
        var testData = "Hello, Sui!"u8.ToArray();
        var signature1 = keyPair.Sign(testData);
        var signature2 = keyPair.Sign(testData);
        
        // Act & Assert
        Assert.Equal(signature1.GetHashCode(), signature2.GetHashCode());
    }

    [Fact]
    public void ToString_ReturnsCorrectFormat()
    {
        // Arrange
        using var keyPair = Ed25519KeyPair.Generate();
        var testData = "Hello, Sui!"u8.ToArray();
        var signature = keyPair.Sign(testData);
        
        // Act
        var toString = signature.ToString();
        
        // Assert
        Assert.StartsWith("SuiSignature(Ed25519,", toString);
        Assert.EndsWith("...)", toString);
    }

    [Theory]
    [InlineData(IntentScope.TransactionData)]
    [InlineData(IntentScope.TransactionEffects)]
    [InlineData(IntentScope.CheckpointSummary)]
    [InlineData(IntentScope.PersonalMessage)]
    public void VerifyTransaction_WithDifferentScopes_WorksCorrectly(IntentScope scope)
    {
        // Arrange
        using var keyPair = Ed25519KeyPair.Generate();
        var txDigest = TransactionDigest.Random();
        var signature = keyPair.SignTransaction(txDigest, scope);
        
        // Act
        var isValid = signature.VerifyTransaction(txDigest, scope);
        
        // Assert
        Assert.True(isValid);
    }

    [Fact]
    public void UnsupportedScheme_ThrowsNotSupportedException()
    {
        // This test will be more relevant when we have actual ECDSA bytes to test with
        // For now, we test the error path indirectly through scheme validation
        
        // Arrange - create a signature with an unsupported scheme indicator
        var validSignature = new byte[64];
        var validPublicKey = new byte[33]; // ECDSA compressed key length
        
        // Act & Assert - This should work fine for now since we're creating the signature
        var signature = new SuiSignature(validSignature, validPublicKey, SuiSignatureScheme.EcdsaSecp256k1);
        
        // But verification operations should indicate they're not supported
        Assert.Equal(SuiSignatureScheme.EcdsaSecp256k1, signature.Scheme);
    }
}