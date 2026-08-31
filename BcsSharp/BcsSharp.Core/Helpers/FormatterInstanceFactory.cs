using System.Reflection;

namespace BcsSharp.Core.Helpers;

/// <summary>
/// Materialises a formatter instance for a given closed formatter type, preferring the project-wide singleton convention (<c>public static readonly Instance</c> field, or a <c>public static Instance</c> property) before falling back to a parameterless constructor.
/// </summary>
internal static class FormatterInstanceFactory
{
    public static object Create(Type formatterType)
    {
        var instanceField = formatterType.GetField("Instance", BindingFlags.Public | BindingFlags.Static);
        if (instanceField is not null && formatterType.IsAssignableFrom(instanceField.FieldType))
        {
            if (instanceField.GetValue(null) is { } fromField)
            {
                return fromField;
            }
        }

        var instanceProperty = formatterType.GetProperty("Instance", BindingFlags.Public | BindingFlags.Static);
        if (instanceProperty is not null && formatterType.IsAssignableFrom(instanceProperty.PropertyType))
        {
            if (instanceProperty.GetValue(null) is { } fromProperty)
            {
                return fromProperty;
            }
        }

        try
        {
            return Activator.CreateInstance(formatterType)
                ?? throw new InvalidOperationException($"Activator returned null for formatter {formatterType.FullName}");
        }
        catch (MissingMethodException ex)
        {
            throw new InvalidOperationException(
                $"Formatter {formatterType.FullName} must expose a public static 'Instance' member or a public parameterless constructor.",
                ex);
        }
    }
}
