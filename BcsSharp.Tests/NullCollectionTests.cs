using BcsSharp.Core;
using BcsSharp.Core.Attributes;
using BcsSharp.Core.Unions;

namespace BcsSharp.Tests;

/// <summary>
/// BCS has no null: a Rust <c>Vec</c> or <c>BTreeMap</c> is always present, and <c>Option&lt;T&gt;</c> is the optional form.
/// A null <c>List</c> or <c>Dictionary</c> used to encode as an empty one, which is right only when the Rust side is <c>Vec&lt;T&gt;</c> and silently drops the discriminant when it is <c>Option&lt;Vec&lt;T&gt;&gt;</c>, so null is rejected the way a null string is.
/// </summary>
public class NullCollectionTests
{
    [Fact]
    public void NullList_IsRejected()
    {
        var ex = Assert.Throws<ArgumentNullException>(() => BcsSerializer.Serialize<List<Item>>(null!));

        Assert.Equal("BCS has no null vector; use Option<List<T>> for an optional one. (Parameter 'value')", ex.Message);
    }

    [Fact]
    public void NullPrimitiveList_IsRejected()
    {
        var ex = Assert.Throws<ArgumentNullException>(() => BcsSerializer.Serialize<List<uint>>(null!));

        Assert.Equal("BCS has no null vector; use Option<List<T>> for an optional one. (Parameter 'value')", ex.Message);
    }

    [Fact]
    public void NullDictionary_IsRejected()
    {
        var ex = Assert.Throws<ArgumentNullException>(() => BcsSerializer.Serialize<Dictionary<string, uint>>(null!));

        Assert.Equal("BCS has no null map; use Option<Dictionary<TKey, TValue>> for an optional one. (Parameter 'value')", ex.Message);
    }

    [Fact]
    public void NullListField_IsRejected()
    {
        // The common way to hit this: a DTO whose list property was never initialized.
        Assert.Throws<ArgumentNullException>(() => BcsSerializer.Serialize(new WithUninitializedList()));
    }

    [Fact]
    public void EmptyCollections_EncodeAsZeroLength()
    {
        Assert.Equal(new byte[] { 0x00 }, BcsSerializer.Serialize(new List<Item>()));
        Assert.Equal(new byte[] { 0x00 }, BcsSerializer.Serialize(new List<uint>()));
        Assert.Equal(new byte[] { 0x00 }, BcsSerializer.Serialize(new Dictionary<string, uint>()));
    }

    [Fact]
    public void OptionalList_CarriesTheOptionDiscriminant()
    {
        Option<List<uint>> none = None.Instance;
        Option<List<uint>> empty = new List<uint>();
        Option<List<uint>> some = new List<uint> { 7 };

        Assert.Equal(new byte[] { 0x00 }, BcsSerializer.Serialize(none));
        Assert.Equal(new byte[] { 0x01, 0x00 }, BcsSerializer.Serialize(empty));
        Assert.Equal(new byte[] { 0x01, 0x01, 0x07, 0x00, 0x00, 0x00 }, BcsSerializer.Serialize(some));

        Assert.Same(None.Instance, BcsSerializer.Deserialize<Option<List<uint>>>(new byte[] { 0x00 }).Value);
        Assert.Equal(new[] { 7u }, Assert.IsType<List<uint>>(BcsSerializer.Deserialize<Option<List<uint>>>(new byte[] { 0x01, 0x01, 0x07, 0x00, 0x00, 0x00 }).Value));
    }

    [BcsStruct]
    public sealed class Item
    {
        [BcsField(0)] public uint Id { get; set; }
    }

    [BcsStruct]
    public sealed class WithUninitializedList
    {
        [BcsField(0)] public List<uint> Items { get; set; }
    }
}
