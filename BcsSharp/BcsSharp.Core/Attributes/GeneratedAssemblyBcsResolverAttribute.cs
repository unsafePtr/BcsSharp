using System;

namespace BcsSharp.Core.Attributes
{
    /// <summary>
    /// Marks an assembly as having a generated BCS resolver. This attribute is automatically
    /// applied by the source generator and used by SourceGeneratedFormatterResolver to
    /// locate the assembly-specific resolver instance.
    /// </summary>
    [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = false)]
    public sealed class GeneratedAssemblyBcsResolverAttribute : Attribute
    {
        /// <summary>
        /// The type of the generated resolver for this assembly
        /// </summary>
        public Type ResolverType { get; }

        /// <summary>
        /// Creates a new instance of the attribute
        /// </summary>
        /// <param name="resolverType">The type of the generated resolver</param>
        public GeneratedAssemblyBcsResolverAttribute(Type resolverType)
        {
            ResolverType = resolverType;
        }
    }
}