using BcsSharp.Core;
using BcsSharp.Core.Attributes;

namespace BcsSharp.Tests;

/// <summary>
/// A map key needs no <c>IComparable&lt;TKey&gt;</c>: entries are ordered by their serialized key bytes, as in Rust's <c>bcs</c>, so any BCS type can be a key.
/// The realistic case is a Sui/Move-shaped <c>ObjectID</c> struct.
/// Keys must also be unique by those bytes, which <c>Equals</c> does not guarantee, so keys the dictionary holds apart but that serialize identically are rejected.
/// </summary>
public class DictionaryKeyConstraintTests
{
    [Fact]
    public void Dictionary_WithClassKeysThatSerializeIdentically_IsRejected()
    {
        // No value equality, so the dictionary holds both keys while the wire would carry one key twice.
        var dict = new Dictionary<ClassKey, uint>
        {
            { new ClassKey { Id = 7 }, 1u },
            { new ClassKey { Id = 7 }, 2u },
        };

        var ex = Assert.Throws<InvalidOperationException>(() => BcsSerializer.Serialize(dict));

        Assert.Equal("Map contains two keys with the same BCS encoding.", ex.Message);
    }

    [Fact]
    public void Dictionary_WithStringKeysThatEncodeIdentically_IsRejected()
    {
        // A lone surrogate is written as U+FFFD, so two different strings produce the same key bytes.
        var dict = new Dictionary<string, uint> { ["\uD800"] = 1u, ["�"] = 2u };

        var ex = Assert.Throws<InvalidOperationException>(() => BcsSerializer.Serialize(dict));

        Assert.Equal("Map contains two keys with the same BCS encoding.", ex.Message);
    }

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

    [BcsStruct]
    public sealed class ClassKey
    {
        [BcsField(0)] public uint Id { get; set; }
    }
}
