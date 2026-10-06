using BcsSharp.Core;
using BcsSharp.Core.Attributes;

namespace BcsSharp.Tests;

/// <summary>
/// A <c>[BcsEnum]</c> whose variants share an index must fail with a message that names them.
/// </summary>
public class BcsEnumDiagnosticsTests
{
    [Fact]
    public void DuplicateVariantIndex_NamesBothVariants()
    {
        var ex = Assert.Throws<InvalidOperationException>(() => BcsSerializer.Serialize<IDuplicated>(new DuplicateA()));

        Assert.Equal("IDuplicated declares [BcsEnumVariant(0)] on both DuplicateA and DuplicateB.", ex.Message);
    }

    [BcsEnum]
    public interface IDuplicated;

    [BcsEnumVariant(0)]
    public sealed class DuplicateA : IDuplicated;

    [BcsEnumVariant(0)]
    public sealed class DuplicateB : IDuplicated;
}
