using NSec.Cryptography;
using Sui.Types;

namespace Sui.Types.Tests;

public class Ed25519KeyPairTests
{
    [Fact]
    public void Generate_CreatesValidKeyPair()
    {
        // Act
        using var keyPair = Ed25519KeyPair.Generate();

        // Assert
        Assert.NotNull(keyPair);
        Assert.Equal(SuiSignatureScheme.Ed25519, keyPair.Scheme);
        Assert.NotEqual(SuiAddress.Zero, keyPair.Address);
        Assert.NotNull(keyPair.PublicKey);
    }

    [Fact]
    public void Generate_MultipleKeyPairs_AreUnique()
    {
        // Act
        using var keyPair1 = Ed25519KeyPair.Generate();
        using var keyPair2 = Ed25519KeyPair.Generate();

        // Assert
        Assert.NotEqual(keyPair1.Address, keyPair2.Address);
        Assert.False(keyPair1.ExportPrivateKey().SequenceEqual(keyPair2.ExportPrivateKey()));
        Assert.False(keyPair1.ExportPublicKey().SequenceEqual(keyPair2.ExportPublicKey()));
    }

    [Fact]
    public void FromSeed_WithValidSeed_CreatesKeyPair()
    {
        // Arrange
        var seed = new byte[32];
        seed[0] = 0x01; // Make it non-zero

        // Act
        using var keyPair = Ed25519KeyPair.FromSeed(seed);

        // Assert
        Assert.NotNull(keyPair);
        Assert.Equal(SuiSignatureScheme.Ed25519, keyPair.Scheme);
        Assert.NotEqual(SuiAddress.Zero, keyPair.Address);
    }

    [Fact]
    public void FromSeed_WithSameSeed_CreatesSameKeyPair()
    {
        // Arrange
        var seed = new byte[32];
        seed[0] = 0x01;

        // Act
        using var keyPair1 = Ed25519KeyPair.FromSeed(seed);
        using var keyPair2 = Ed25519KeyPair.FromSeed(seed);

        // Assert
        Assert.Equal(keyPair1.Address, keyPair2.Address);
        Assert.True(keyPair1.ExportPrivateKey().SequenceEqual(keyPair2.ExportPrivateKey()));
        Assert.True(keyPair1.ExportPublicKey().SequenceEqual(keyPair2.ExportPublicKey()));
    }

    [Fact]
    public void FromSeed_WithInvalidSeedLength_ThrowsException()
    {
        // Arrange
        var invalidSeed = new byte[16]; // Too short

        // Act & Assert
        Assert.Throws<ArgumentException>(() => Ed25519KeyPair.FromSeed(invalidSeed));
    }

    [Fact]
    public void FromSeed_WithHexString_CreatesKeyPair()
    {
        // Arrange
        var hexSeed = "0123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdef";

        // Act
        using var keyPair = Ed25519KeyPair.FromSeed(hexSeed);

        // Assert
        Assert.NotNull(keyPair);
        Assert.Equal(SuiSignatureScheme.Ed25519, keyPair.Scheme);
    }

    [Fact]
    public void ExportPrivateKey_ReturnsCorrectLength()
    {
        // Arrange
        using var keyPair = Ed25519KeyPair.Generate();

        // Act
        var privateKey = keyPair.ExportPrivateKey();

        // Assert
        Assert.Equal(32, privateKey.Length);
    }

    [Fact]
    public void ExportPublicKey_ReturnsCorrectLength()
    {
        // Arrange
        using var keyPair = Ed25519KeyPair.Generate();

        // Act
        var publicKey = keyPair.ExportPublicKey();

        // Assert
        Assert.Equal(32, publicKey.Length);
    }

    [Fact]
    public void ExportPrivateKeyHex_ReturnsValidHex()
    {
        // Arrange
        using var keyPair = Ed25519KeyPair.Generate();

        // Act
        var hexPrivateKey = keyPair.ExportPrivateKeyHex();

        // Assert
        Assert.Equal(64, hexPrivateKey.Length); // 32 bytes = 64 hex chars
        Assert.True(hexPrivateKey.All(c => "0123456789abcdef".Contains(c)));
    }

    [Fact]
    public void ExportPrivateKeyBase64_WithSchemePrefix_StartsWithCorrectByte()
    {
        // Arrange
        using var keyPair = Ed25519KeyPair.Generate();

        // Act
        var base64Key = keyPair.ExportPrivateKeyBase64();
        var decoded = Convert.FromBase64String(base64Key);

        // Assert
        Assert.Equal(0x00, decoded[0]); // Ed25519 scheme flag
        Assert.Equal(33, decoded.Length); // 1 byte scheme + 32 bytes key
    }

    [Fact]
    public void Sign_WithData_CreatesValidSignature()
    {
        // Arrange
        using var keyPair = Ed25519KeyPair.Generate();
        var testData = "Hello, Sui!"u8.ToArray();

        // Act
        var signature = keyPair.Sign(testData);

        // Assert
        Assert.NotNull(signature);
        Assert.Equal(SuiSignatureScheme.Ed25519, signature.Scheme);
        Assert.Equal(64, signature.SignatureBytes.Length);
        Assert.Equal(32, signature.PublicKeyBytes.Length);
    }

    [Fact]
    public void SignRaw_WithData_ReturnsCorrectLength()
    {
        // Arrange
        using var keyPair = Ed25519KeyPair.Generate();
        var testData = "Hello, Sui!"u8.ToArray();

        // Act
        var signatureBytes = keyPair.SignRaw(testData);

        // Assert
        Assert.Equal(64, signatureBytes.Length);
    }

    [Fact]
    public void Verify_WithValidSignature_ReturnsTrue()
    {
        // Arrange
        using var keyPair = Ed25519KeyPair.Generate();
        var testData = "Hello, Sui!"u8.ToArray();
        var signature = keyPair.Sign(testData);

        // Act
        var isValid = keyPair.Verify(testData, signature);

        // Assert
        Assert.True(isValid);
    }

    [Fact]
    public void Verify_WithInvalidData_ReturnsFalse()
    {
        // Arrange
        using var keyPair = Ed25519KeyPair.Generate();
        var testData = "Hello, Sui!"u8.ToArray();
        var tamperedData = "Hello, Sui!!"u8.ToArray();
        var signature = keyPair.Sign(testData);

        // Act
        var isValid = keyPair.Verify(tamperedData, signature);

        // Assert
        Assert.False(isValid);
    }

    [Fact]
    public void Verify_WithRawSignature_WorksCorrectly()
    {
        // Arrange
        using var keyPair = Ed25519KeyPair.Generate();
        var testData = "Hello, Sui!"u8.ToArray();
        var rawSignature = keyPair.SignRaw(testData);

        // Act
        var isValid = keyPair.Verify(testData, rawSignature);

        // Assert
        Assert.True(isValid);
    }

    [Fact]
    public void SignPersonalMessage_CreatesValidSignature()
    {
        // Arrange
        using var keyPair = Ed25519KeyPair.Generate();
        var message = "Personal message"u8.ToArray();

        // Act
        var signature = keyPair.SignPersonalMessage(message);

        // Assert
        Assert.NotNull(signature);
        Assert.Equal(SuiSignatureScheme.Ed25519, signature.Scheme);
    }

    [Fact]
    public void Address_IsDerivedCorrectlyFromPublicKey()
    {
        // Arrange
        using var keyPair = Ed25519KeyPair.Generate();

        // Act
        var addressFromSigner = SuiAddress.FromSigner(keyPair);
        var addressFromKeyPair = keyPair.Address;

        // Assert
        Assert.Equal(addressFromKeyPair, addressFromSigner);
    }

    [Fact]
    public void Dispose_DoesNotThrow()
    {
        // Arrange
        var keyPair = Ed25519KeyPair.Generate();

        // Act & Assert
        keyPair.Dispose(); // Should not throw

        // Verify disposed state
        Assert.Throws<ObjectDisposedException>(() => keyPair.ExportPrivateKey());
    }

    [Fact]
    public void ToString_ReturnsCorrectFormat()
    {
        // Arrange
        using var keyPair = Ed25519KeyPair.Generate();

        // Act
        var toString = keyPair.ToString();

        // Assert
        Assert.StartsWith("Ed25519KeyPair(address=", toString);
        Assert.Contains(keyPair.Address.ToString(), toString);
    }

    [Fact]
    public void CorrectAddress_FromPublicKey()
    {
        // Arrange
        using var keyPair = Ed25519KeyPair.FromBase64PrivateKey("AFUrgDeNoOE2Y2X0YIa7UsXEi4NhSabVKINlLENt2IKX");

        // Act
        var address = keyPair.GenerateAddress();

        // Assert
        Assert.Equal("0x63acfb0924b4d7f2674a45edb09b3162caf963c6e41235d64d9aac34c0c66474", address.ToString());
    }
}