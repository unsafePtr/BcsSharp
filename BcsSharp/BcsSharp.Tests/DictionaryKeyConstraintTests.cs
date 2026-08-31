using BcsSharp.Core;
using BcsSharp.Core.Attributes;

namespace BcsSharp.Tests;

/// <summary>
/// Documents a known over-restriction in <c>StandardResolver</c>: it refuses to resolve a <c>Dictionary&lt;TKey, TValue&gt;</c> formatter unless <c>TKey</c> implements <c>IComparable&lt;TKey&gt;</c>.
/// The check is a leftover assumption; <c>MapFormatter</c> itself never calls <c>IComparable</c> — BCS canonical map ordering is by serialized key bytes (see <see cref="BcsSharp.Core.Formatters.MapFormatter{TKey,TValue}"/>).
///
/// Realistic case it blocks today: a Sui/Move-shaped <c>ObjectID</c> struct (or any <c>[BcsStruct]</c> user struct) used as a map key without manually implementing <c>IComparable&lt;ObjectID&gt;</c>.
///
/// Fix when convenient: drop the <c>IComparable&lt;&gt;.IsAssignableFrom</c> gate in <c>StandardResolver.CreateFormatter</c> for <c>Dictionary&lt;,&gt;</c>.
/// Remove the <c>Skip</c> on the test below to verify.
/// </summary>
public class DictionaryKeyConstraintTests
{
    [Fact(Skip = "StandardResolver requires IComparable<TKey> for Dictionary<,>; remove the gate to enable.")]
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

    /// <summary>
    /// Locks the current (wrong) behavior so a regression that "fixes" it accidentally trips this test and forces a conscious decision.
    /// Pair with the skipped test above: when you delete the gate, also delete this test.
    /// </summary>
    [Fact]
    public void Dictionary_WithBcsStructKey_WithoutIComparable_ThrowsToday()
    {
        var dict = new Dictionary<ObjectId, uint>
        {
            { new ObjectId { Hi = 1, Lo = 2 }, 99u },
        };

        var ex = Assert.Throws<InvalidOperationException>(() => BcsSerializer.Serialize(dict));
        Assert.Contains("No formatter found", ex.Message);
    }

    [BcsStruct]
    public struct ObjectId
    {
        [BcsField(0)] public ulong Hi { get; set; }
        [BcsField(1)] public ulong Lo { get; set; }
    }
}
