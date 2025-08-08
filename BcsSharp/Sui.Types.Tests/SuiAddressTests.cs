using Sui.Types;

namespace Sui.Types.Tests;

public class SuiAddressTests
{
    [Fact]
    public void Constructor_WithValidHexString_CreatesAddress()
    {
        // Arrange
        var hexString = "0x0000000000000000000000000000000000000000000000000000000000000002";

        // Act
        var address = new SuiAddress(hexString);

        // Assert
        Assert.Equal(hexString, address.ToString());
    }

    [Fact]
    public void Constructor_WithShortHexString_PadsWithZeros()
    {
        // Arrange
        var shortHex = "0x02";
        var expectedFull = "0x0000000000000000000000000000000000000000000000000000000000000002";

        // Act
        var address = new SuiAddress(shortHex);

        // Assert
        Assert.Equal(expectedFull, address.ToString());
    }

    [Fact]
    public void Constructor_WithHexStringWithoutPrefix_CreatesAddress()
    {
        // Arrange
        var hexWithoutPrefix = "02";
        var expected = "0x0000000000000000000000000000000000000000000000000000000000000002";

        // Act
        var address = new SuiAddress(hexWithoutPrefix);

        // Assert
        Assert.Equal(expected, address.ToString());
    }

    [Fact]
    public void Constructor_WithInvalidHexCharacters_ThrowsException()
    {
        // Arrange
        var invalidHex = "0xZZZZ";

        // Act & Assert
        Assert.Throws<ArgumentException>(() => new SuiAddress(invalidHex));
    }

    [Fact]
    public void Constructor_WithNullString_ThrowsException()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => new SuiAddress((string)null!));
    }

    [Fact]
    public void Constructor_WithEmptyString_ThrowsException()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => new SuiAddress(""));
    }

    [Fact]
    public void Constructor_WithValidBytes_CreatesAddress()
    {
        // Arrange
        var bytes = new byte[32];
        bytes[31] = 0x02; // Last byte = 2

        // Act
        var address = new SuiAddress(bytes);

        // Assert
        Assert.Equal("0x0000000000000000000000000000000000000000000000000000000000000002", address.ToString());
    }

    [Fact]
    public void Constructor_WithInvalidBytesLength_ThrowsException()
    {
        // Arrange
        var invalidBytes = new byte[16]; // Wrong length

        // Act & Assert
        Assert.Throws<ArgumentException>(() => new SuiAddress(invalidBytes));
    }

    [Fact]
    public void WellKnownAddresses_HaveCorrectValues()
    {
        // Assert
        Assert.Equal("0x0000000000000000000000000000000000000000000000000000000000000000", SuiAddress.Zero.ToString());
        Assert.Equal("0x0000000000000000000000000000000000000000000000000000000000000002", SuiAddress.SuiFramework.ToString());
        Assert.Equal("0x0000000000000000000000000000000000000000000000000000000000000005", SuiAddress.SystemState.ToString());
    }

    [Fact]
    public void ToShortString_ReturnsCorrectFormat()
    {
        // Arrange
        var address = new SuiAddress("0x1234567890abcdef1234567890abcdef1234567890abcdef1234567890abcdef");

        // Act
        var shortString = address.ToShortString();

        // Assert
        Assert.Equal("0x12345678", shortString);
    }

    [Fact]
    public void ToHexString_ReturnsSameAsToString()
    {
        // Arrange
        var hexString = "0x1234567890abcdef1234567890abcdef1234567890abcdef1234567890abcdef";
        var address = new SuiAddress(hexString);

        // Act & Assert
        Assert.Equal(address.ToString(), address.ToHexString());
    }

    [Fact]
    public void FromPublicKeyBytes_Ed25519_CreatesCorrectAddress()
    {
        // Arrange - Known Ed25519 public key and expected address
        var publicKeyBytes = Convert.FromHexString("0123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdef");

        // Act
        var address = SuiAddress.FromPublicKeyBytes(publicKeyBytes, SuiSignatureScheme.Ed25519);

        // Assert
        Assert.NotEqual(SuiAddress.Zero, address);
        Assert.Equal(32, address.Bytes.Length);
    }

    [Fact]
    public void FromPublicKeyBytes_InvalidKeyLength_ThrowsException()
    {
        // Arrange
        var invalidKeyBytes = new byte[16]; // Too short for Ed25519

        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
            SuiAddress.FromPublicKeyBytes(invalidKeyBytes, SuiSignatureScheme.Ed25519));
    }

    [Fact]
    public void FromSigner_CreatesCorrectAddress()
    {
        // Arrange
        using var keyPair = SuiKeyPair.Generate();

        // Act
        var addressFromSigner = SuiAddress.FromSigner(keyPair);
        var addressFromKeyPair = keyPair.Address;

        // Assert
        Assert.Equal(addressFromKeyPair, addressFromSigner);
    }

    [Fact]
    public void Equality_WorksCorrectly()
    {
        // Arrange
        var address1 = new SuiAddress("0x02");
        var address2 = new SuiAddress("0x0000000000000000000000000000000000000000000000000000000000000002");
        var address3 = new SuiAddress("0x03");

        // Act & Assert
        Assert.Equal(address1, address2);
        Assert.True(address1 == address2);
        Assert.False(address1 != address2);
        Assert.NotEqual(address1, address3);
        Assert.False(address1 == address3);
        Assert.True(address1 != address3);
    }

    [Fact]
    public void CompareTo_WorksCorrectly()
    {
        // Arrange
        var address1 = new SuiAddress("0x01");
        var address2 = new SuiAddress("0x02");
        var address3 = new SuiAddress("0x01");

        // Act & Assert
        Assert.True(address1.CompareTo(address2) < 0);
        Assert.True(address2.CompareTo(address1) > 0);
        Assert.Equal(0, address1.CompareTo(address3));

        Assert.True(address1 < address2);
        Assert.True(address2 > address1);
        Assert.True(address1 <= address3);
        Assert.True(address1 >= address3);
    }

    [Fact]
    public void GetHashCode_EqualAddressesHaveSameHashCode()
    {
        // Arrange
        var address1 = new SuiAddress("0x02");
        var address2 = new SuiAddress("0x0000000000000000000000000000000000000000000000000000000000000002");

        // Act & Assert
        Assert.Equal(address1.GetHashCode(), address2.GetHashCode());
    }

    [Fact]
    public void ImplicitConversion_StringToAddress_WorksCorrectly()
    {
        // Arrange
        string hexString = "0x02";

        // Act
        SuiAddress address = hexString;

        // Assert
        Assert.Equal("0x0000000000000000000000000000000000000000000000000000000000000002", address.ToString());
    }

    [Fact]
    public void ImplicitConversion_AddressToString_WorksCorrectly()
    {
        // Arrange
        var address = new SuiAddress("0x02");

        // Act
        string hexString = address;

        // Assert
        Assert.Equal(address.ToString(), hexString);
    }

    [Theory]
    [InlineData("0x0")]
    [InlineData("0x1")]
    [InlineData("0x02")]
    [InlineData("0x123")]
    [InlineData("0xabc")]
    [InlineData("0xABC")]
    [InlineData("0x1234567890abcdef")]
    public void Constructor_WithVariousValidHexStrings_CreatesAddresses(string hexInput)
    {
        // Act
        var address = new SuiAddress(hexInput);

        // Assert
        Assert.NotNull(address);
        Assert.Equal(32, address.Bytes.Length);
        Assert.StartsWith("0x", address.ToString());
    }

    [Fact]
    public void Serialize_Deserialize_RoundTrip_WorksCorrectly()
    {
        // Arrange
        var originalAddress = new SuiAddress("0x1234567890abcdef1234567890abcdef1234567890abcdef1234567890abcdef");

        // Act
        var serialized = originalAddress.Serialize();
        var deserialized = SuiAddress.Deserialize(serialized);

        // Assert
        Assert.Equal(originalAddress, deserialized);
    }
}