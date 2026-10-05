using System.Globalization;
using BcsSharp.Core;

namespace BcsSharp.Tests;

public class BooleanValidationTests
{
    [Fact]
    public void ValidBooleanValues_ShouldBeAccepted()
    {
        // Test valid boolean values: 0x00 (false) and 0x01 (true)
        var falseBytes = new byte[] { 0x00 };
        var trueBytes = new byte[] { 0x01 };

        var falseReader = new BcsReader(falseBytes);
        var trueReader = new BcsReader(trueBytes);

        var falseResult = falseReader.ReadBool();
        var trueResult = trueReader.ReadBool();

        Assert.False(falseResult);
        Assert.True(trueResult);
    }

    [Fact]
    public void InvalidBooleanValues_ShouldBeRejected()
    {
        // Test invalid boolean values (2-255)
        var invalidValues = new byte[] { 2, 3, 4, 5, 10, 100, 255 };

        foreach (var invalidValue in invalidValues)
        {
            var bytes = new byte[] { invalidValue };
            var reader = new BcsReader(bytes);

            try
            {
                reader.ReadBool();
                Assert.Fail($"Expected InvalidOperationException for value {invalidValue}");
            }
            catch (InvalidOperationException ex)
            {
                Assert.Contains("Invalid boolean value", ex.Message);
                Assert.Contains(invalidValue.ToString(CultureInfo.InvariantCulture), ex.Message);
            }
        }
    }

    [Fact]
    public void BooleanSerialization_ShouldMatchRustBcs()
    {
        // Test that our boolean serialization matches Rust BCS
        var falseBytes = BcsSerializer.Serialize(false);
        var trueBytes = BcsSerializer.Serialize(true);

        // Should serialize to exactly what Rust BCS produces
        Assert.Equal(new byte[] { 0x00 }, falseBytes);
        Assert.Equal(new byte[] { 0x01 }, trueBytes);

        // Round-trip test
        var deserializedFalse = BcsSerializer.Deserialize<bool>(falseBytes);
        var deserializedTrue = BcsSerializer.Deserialize<bool>(trueBytes);

        Assert.False(deserializedFalse);
        Assert.True(deserializedTrue);
    }
}
