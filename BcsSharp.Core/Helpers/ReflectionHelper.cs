using System.Reflection;
using System.Runtime.ExceptionServices;

namespace BcsSharp.Core.Helpers;

/// <summary>
/// Reflection calls that rethrow what the callee threw instead of the <see cref="TargetInvocationException"/> around it, so a resolution failure names the field or variant at fault.
/// </summary>
internal static class ReflectionHelper
{
    public static object? InvokeUnwrapped(this MethodInfo method, object? target, params object?[] args)
    {
        try
        {
            return method.Invoke(target, args);
        }
        catch (TargetInvocationException ex) when (ex.InnerException is not null)
        {
            ExceptionDispatchInfo.Throw(ex.InnerException);
            throw;
        }
    }

    public static object CreateInstanceUnwrapped(Type type, params object?[] args)
    {
        try
        {
            return Activator.CreateInstance(type, args)!;
        }
        catch (TargetInvocationException ex) when (ex.InnerException is not null)
        {
            ExceptionDispatchInfo.Throw(ex.InnerException);
            throw;
        }
    }
}
