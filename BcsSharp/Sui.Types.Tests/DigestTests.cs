using Sui.Types;

namespace Sui.Types.Tests;

public class DigestTests
{
    public class TransactionDigestTests
    {
        [Fact]
        public void Constructor_WithValidBytes_CreatesDigest()
        {
            // Arrange
            var bytes = new byte[32];
            bytes[0] = 0x01;

            // Act
            var digest = new TransactionDigest(bytes);

            // Assert
            Assert.Equal(32, digest.Bytes.Length);
            Assert.Equal(0x01, digest.Bytes[0]);
        }

        [Fact]
        public void Constructor_WithInvalidBytesLength_ThrowsException()
        {
            // Arrange
            var invalidBytes = new byte[16];

            // Act & Assert
            Assert.Throws<ArgumentException>(() => new TransactionDigest(invalidBytes));
        }

        [Fact]
        public void Zero_IsAllZeros()
        {
            // Act
            var zero = TransactionDigest.Zero;

            // Assert
            Assert.True(zero.Bytes.All(b => b == 0));
        }

        [Fact]
        public void FromTransactionData_CreatesConsistentDigest()
        {
            // Arrange
            var transactionData = "test transaction data"u8.ToArray();

            // Act
            var digest1 = TransactionDigest.FromTransactionData(transactionData);
            var digest2 = TransactionDigest.FromTransactionData(transactionData);

            // Assert
            Assert.Equal(digest1, digest2);
            Assert.NotEqual(TransactionDigest.Zero, digest1);
        }

        [Fact]
        public void CompareTo_WorksCorrectly()
        {
            // Arrange
            var bytes1 = new byte[32];
            bytes1[0] = 0x01;
            var bytes2 = new byte[32];
            bytes2[0] = 0x02;

            var digest1 = new TransactionDigest(bytes1);
            var digest2 = new TransactionDigest(bytes2);
            var digest3 = new TransactionDigest(bytes1);

            // Act & Assert
            Assert.True(digest1.CompareTo(digest2) < 0);
            Assert.True(digest2.CompareTo(digest1) > 0);
            Assert.Equal(0, digest1.CompareTo(digest3));

            Assert.True(digest1 < digest2);
            Assert.True(digest2 > digest1);
            Assert.True(digest1 <= digest3);
            Assert.True(digest1 >= digest3);
        }
    }

    public class ObjectDigestTests
    {
        [Fact]
        public void Constructor_WithValidBytes_CreatesDigest()
        {
            // Arrange
            var bytes = new byte[32];
            bytes[0] = 0x01;

            // Act
            var digest = new ObjectDigest(bytes);

            // Assert
            Assert.Equal(32, digest.Bytes.Length);
            Assert.Equal(0x01, digest.Bytes[0]);
        }

        [Fact]
        public void Random_GeneratesUniqueDigests()
        {
            // Act
            var digest1 = ObjectDigest.Random();
            var digest2 = ObjectDigest.Random();

            // Assert
            Assert.NotEqual(digest1, digest2);
        }

        [Fact]
        public void FromObjectData_CreatesConsistentDigest()
        {
            // Arrange
            var objectData = "test object data"u8.ToArray();

            // Act
            var digest1 = ObjectDigest.FromObjectData(objectData);
            var digest2 = ObjectDigest.FromObjectData(objectData);

            // Assert
            Assert.Equal(digest1, digest2);
        }

        [Fact]
        public void ToString_ReturnsBase58String()
        {
            // Arrange
            var digest = ObjectDigest.Random();

            // Act
            var base58String = digest.ToString();

            // Assert
            Assert.True(IsValidBase58(base58String));
        }
    }

    public class ObjectIdTests
    {
        [Fact]
        public void Constructor_WithValidBytes_CreatesObjectId()
        {
            // Arrange
            var bytes = new byte[32];
            bytes[0] = 0x01;

            // Act
            var objectId = new ObjectId(bytes);

            // Assert
            Assert.Equal(32, objectId.Bytes.Length);
            Assert.Equal(0x01, objectId.Bytes[0]);
        }

        [Fact]
        public void Constructor_WithHexString_CreatesObjectId()
        {
            // Arrange
            var hexString = "0x0000000000000000000000000000000000000000000000000000000000000123";

            // Act
            var objectId = new ObjectId(hexString);

            // Assert
            Assert.Equal(hexString, objectId.ToString());
        }

        [Fact]
        public void Constructor_WithShortHexString_PadsCorrectly()
        {
            // Arrange
            var shortHex = "0x123";
            var expectedFull = "0x0000000000000000000000000000000000000000000000000000000000000123";

            // Act
            var objectId = new ObjectId(shortHex);

            // Assert
            Assert.Equal(expectedFull, objectId.ToString());
        }

        [Fact]
        public void WellKnownObjectIds_HaveCorrectValues()
        {
            // Assert
            Assert.Equal("0x0000000000000000000000000000000000000000000000000000000000000000", ObjectId.Zero.ToString());
            Assert.Equal("0x0000000000000000000000000000000000000000000000000000000000000006", ObjectId.Clock.ToString());
            Assert.Equal("0x0000000000000000000000000000000000000000000000000000000000000008", ObjectId.SystemRandom.ToString());
        }

        [Fact]
        public void ToString_ReturnsHexString()
        {
            // Arrange
            var objectId = ObjectId.Random();

            // Act
            var hexString = objectId.ToString();

            // Assert
            Assert.StartsWith("0x", hexString);
            Assert.Equal(66, hexString.Length); // 0x + 64 hex chars
            Assert.True(hexString[2..].All(c => "0123456789abcdef".Contains(c)));
        }

        [Fact]
        public void ToShortString_ReturnsCorrectFormat()
        {
            // Arrange
            var objectId = new ObjectId("0x1234567890abcdef1234567890abcdef1234567890abcdef1234567890abcdef");

            // Act
            var shortString = objectId.ToShortString();

            // Assert
            Assert.Equal("0x12345678", shortString);
        }

        [Fact]
        public void ToAddress_CreatesEquivalentAddress()
        {
            // Arrange
            var objectId = ObjectId.Random();

            // Act
            var address = objectId.ToAddress();

            // Assert
            Assert.Equal(objectId.ToString(), address.ToString());
        }

        [Fact]
        public void Conversion_ObjectIdToAddress_WorksCorrectly()
        {
            // Arrange
            var objectId = ObjectId.Random();

            // Act
            SuiAddress address = objectId;
            ObjectId convertedBack = address;

            // Assert
            Assert.Equal(objectId, convertedBack);
        }
    }

    public class ObjectRefTests
    {
        [Fact]
        public void Constructor_CreatesValidObjectRef()
        {
            // Arrange
            var objectId = ObjectId.Random();
            var version = 5UL;
            var digest = ObjectDigest.Random();

            // Act
            var objRef = new ObjectRef(objectId, version, digest);

            // Assert
            Assert.Equal(objectId, objRef.ObjectId);
            Assert.Equal(version, objRef.Version);
            Assert.Equal(digest, objRef.Digest);
        }

        [Fact]
        public void Initial_CreatesVersionOne()
        {
            // Arrange
            var objectId = ObjectId.Random();
            var digest = ObjectDigest.Random();

            // Act
            var objRef = ObjectRef.Initial(objectId, digest);

            // Assert
            Assert.Equal(objectId, objRef.ObjectId);
            Assert.Equal(1UL, objRef.Version);
            Assert.Equal(digest, objRef.Digest);
            Assert.True(objRef.IsInitialVersion);
        }

        [Fact]
        public void NextVersion_IncrementsVersionCorrectly()
        {
            // Arrange
            var objectId = ObjectId.Random();
            var digest1 = ObjectDigest.Random();
            var digest2 = ObjectDigest.Random();
            var objRef1 = new ObjectRef(objectId, 3, digest1);

            // Act
            var objRef2 = objRef1.NextVersion(digest2);

            // Assert
            Assert.Equal(objectId, objRef2.ObjectId);
            Assert.Equal(4UL, objRef2.Version);
            Assert.Equal(digest2, objRef2.Digest);
        }

        [Fact]
        public void ToString_ReturnsCorrectFormat()
        {
            // Arrange - Create a hex string that will produce a recognizable short string
            var objectId = new ObjectId("0x123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdef");
            var digest = ObjectDigest.Random();
            var objRef = new ObjectRef(objectId, 5, digest);

            // Act
            var toString = objRef.ToString();
            var expectedFormat = $"{objectId.ToShortString()}:v5:{digest.ToShortString()}";

            // Assert
            Assert.Equal(expectedFormat, toString);
            Assert.Contains("0x01234567", toString); // Short object ID (first 8 hex chars after 0x)
            Assert.Contains(":v5:", toString);       // Version with colons
        }

        [Fact]
        public void ToDetailedString_ReturnsFullInformation()
        {
            // Arrange
            var objectId = ObjectId.Random();
            var digest = ObjectDigest.Random();
            var objRef = new ObjectRef(objectId, 5, digest);

            // Act
            var detailedString = objRef.ToDetailedString();

            // Assert
            Assert.Contains("ObjectRef", detailedString);
            Assert.Contains(objectId.ToString(), detailedString);
            Assert.Contains("version=5", detailedString);
            Assert.Contains(digest.ToString(), detailedString);
        }

        [Fact]
        public void RefersToSameObject_WorksCorrectly()
        {
            // Arrange
            var objectId = ObjectId.Random();
            var digest1 = ObjectDigest.Random();
            var digest2 = ObjectDigest.Random();
            var objRef1 = new ObjectRef(objectId, 1, digest1);
            var objRef2 = new ObjectRef(objectId, 2, digest2);
            var objRef3 = new ObjectRef(ObjectId.Random(), 1, digest1);

            // Act & Assert
            Assert.True(objRef1.RefersToSameObject(objRef2));
            Assert.False(objRef1.RefersToSameObject(objRef3));
        }

        [Fact]
        public void IsNewerThan_WorksCorrectly()
        {
            // Arrange
            var objectId = ObjectId.Random();
            var digest = ObjectDigest.Random();
            var objRef1 = new ObjectRef(objectId, 1, digest);
            var objRef2 = new ObjectRef(objectId, 2, digest);

            // Act & Assert
            Assert.True(objRef2.IsNewerThan(objRef1));
            Assert.False(objRef1.IsNewerThan(objRef2));
        }

        [Fact]
        public void IsNewerThan_WithDifferentObjects_ThrowsException()
        {
            // Arrange
            var objRef1 = new ObjectRef(ObjectId.Random(), 1, ObjectDigest.Random());
            var objRef2 = new ObjectRef(ObjectId.Random(), 2, ObjectDigest.Random());

            // Act & Assert
            Assert.Throws<ArgumentException>(() => objRef1.IsNewerThan(objRef2));
        }

        [Fact]
        public void Serialize_Deserialize_RoundTrip_WorksCorrectly()
        {
            // Arrange
            var originalRef = new ObjectRef(ObjectId.Random(), 42, ObjectDigest.Random());

            // Act
            var serialized = originalRef.Serialize();
            var deserialized = ObjectRef.Deserialize(serialized);

            // Assert
            Assert.Equal(originalRef.ObjectId, deserialized.ObjectId);
            Assert.Equal(originalRef.Version, deserialized.Version);
            Assert.Equal(originalRef.Digest, deserialized.Digest);
        }
    }

    // Helper method to validate Base58 strings
    private static bool IsValidBase58(string input)
    {
        const string base58Alphabet = "123456789ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz";
        return !string.IsNullOrEmpty(input) && input.All(base58Alphabet.Contains);
    }
}