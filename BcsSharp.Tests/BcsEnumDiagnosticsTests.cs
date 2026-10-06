using System.Reflection;
using System.Reflection.Emit;
using BcsSharp.Core;
using BcsSharp.Core.Attributes;

namespace BcsSharp.Tests;

/// <summary>
/// A <c>[BcsEnum]</c> whose variants cannot be discovered, or whose variants share an index, must fail with a message that says what to change.
/// </summary>
public class BcsEnumDiagnosticsTests
{
    [Fact]
    public void VariantsInAnotherAssembly_AreNamedInTheError()
    {
        var foreign = EmitForeignVariants();
        var circle = (IForeignShape)Activator.CreateInstance(foreign.GetType("ForeignCircle", throwOnError: true)!)!;

        var ex = Assert.Throws<InvalidOperationException>(() => BcsSerializer.Serialize<IForeignShape>(circle));

        Assert.Equal(
            "IForeignShape has no [BcsEnumVariant] types in its own assembly BcsSharp.Tests, the only place variants are discovered. " +
            "Found in other assemblies: BcsSharp.Tests.Foreign (ForeignCircle, ForeignSquare). " +
            "Declare the variants next to the marker, or model the enum as a union.",
            ex.Message);
    }

    [Fact]
    public void NoVariantsAnywhere_SaysWhatAVariantNeeds()
    {
        var ex = Assert.Throws<InvalidOperationException>(() => BcsSerializer.Serialize<IOrphan>(new NotAVariant()));

        Assert.Equal(
            "IOrphan has no [BcsEnumVariant] types in its own assembly BcsSharp.Tests, the only place variants are discovered. " +
            "Each variant must be declared there, implement IOrphan and carry [BcsEnumVariant(index)].",
            ex.Message);
    }

    [Fact]
    public void DuplicateVariantIndex_NamesBothVariants()
    {
        var ex = Assert.Throws<InvalidOperationException>(() => BcsSerializer.Serialize<IDuplicated>(new DuplicateA()));

        Assert.Equal("IDuplicated declares [BcsEnumVariant(0)] on both DuplicateA and DuplicateB.", ex.Message);
    }

    /// <summary>
    /// Saves ForeignCircle and ForeignSquare as a separate assembly and loads it back, because the formatter skips dynamic assemblies when naming misplaced variants.
    /// </summary>
    private static Assembly EmitForeignVariants()
    {
        var assembly = new PersistedAssemblyBuilder(new AssemblyName("BcsSharp.Tests.Foreign"), typeof(object).Assembly);
        var module = assembly.DefineDynamicModule("BcsSharp.Tests.Foreign");
        var variantConstructor = typeof(BcsEnumVariantAttribute).GetConstructor([typeof(uint)])!;

        foreach (var (name, index) in new[] { ("ForeignCircle", 0u), ("ForeignSquare", 1u) })
        {
            var variant = module.DefineType(name, TypeAttributes.Public | TypeAttributes.Sealed, typeof(object), [typeof(IForeignShape)]);
            variant.SetCustomAttribute(new CustomAttributeBuilder(variantConstructor, [index]));
            variant.DefineDefaultConstructor(MethodAttributes.Public);
            variant.CreateType();
        }

        using var image = new MemoryStream();
        assembly.Save(image);

        return Assembly.Load(image.ToArray());
    }

    // Its variants exist only in the assembly EmitForeignVariants builds.
    [BcsEnum]
    public interface IForeignShape;

    [BcsEnum]
    public interface IOrphan;

    // Implements the marker but lacks [BcsEnumVariant], so it is not a variant.
    public sealed class NotAVariant : IOrphan;

    [BcsEnum]
    public interface IDuplicated;

    [BcsEnumVariant(0)]
    public sealed class DuplicateA : IDuplicated;

    [BcsEnumVariant(0)]
    public sealed class DuplicateB : IDuplicated;
}
