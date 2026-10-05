using BcsSharp.Core;
using BcsSharp.Core.Attributes;

namespace BcsSharp.Tests;

/// <summary>
/// A map key needs no <c>IComparable&lt;TKey&gt;</c>: entries are ordered by their serialized key bytes, as in Rust's <c>bcs</c>, so any BCS type can be a key.
/// The realistic case is a Sui/Move-shaped <c>ObjectID</c> struct.
/// </summary>
public class DictionaryKeyConstraintTests
{
    [Fact]
    public void Dictionary_WithBcsStructKey_WithoutIComparable_ShouldRoundTrip()
    {
        var dict = new Dictionary<ObjectId, uint>
        {
            { new ObjectId { Hi = 0xAA, Lo = 0x01 }, 100u },
            { new ObjectId { Hi = 0xAA, Lo = 0x02 }, 200u },
        };

        var bytes = BcsSerializer.Serialize(dict);
        var back = BcsSerializer.Deserialize<Dictionary<ObjectId, uint>>(bytes);

        Assert.Equal(2, back.Count);
        Assert.Equal(100u, back[new ObjectId { Hi = 0xAA, Lo = 0x01 }]);
        Assert.Equal(200u, back[new ObjectId { Hi = 0xAA, Lo = 0x02 }]);
    }

    [BcsStruct]
    public struct ObjectId
    {
        [BcsField(0)] public ulong Hi { get; set; }
        [BcsField(1)] public ulong Lo { get; set; }
    }
}
