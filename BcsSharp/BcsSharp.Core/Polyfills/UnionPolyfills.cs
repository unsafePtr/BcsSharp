// Polyfills for the C# 15 union feature's compiler-required types.
//
// As of .NET 11 preview 3, the BCL does NOT yet ship
// System.Runtime.CompilerServices.UnionAttribute or IUnion, but the language
// (with <LangVersion>preview</LangVersion>) requires them to exist for the
// `union` keyword to compile. We declare them here in the canonical namespace
// so user code and BcsSharp itself can use the feature.
//
// TODO: delete this file once the BCL ships these types (likely .NET 11 RC or
// .NET 12). Until then, downstream assemblies referencing BcsSharp transitively
// inherit these definitions.

namespace System.Runtime.CompilerServices;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = false)]
public sealed class UnionAttribute : Attribute;

public interface IUnion
{
    object? Value { get; }
}
