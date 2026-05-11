using System.Diagnostics.CodeAnalysis;

namespace BcsSharp.Core.Helpers;

internal static class ThrowHelper
{
    [DoesNotReturn]
    public static void ThrowInvalidOperationException(string message)
    {
        throw new InvalidOperationException(message);
    }

    [DoesNotReturn]
    public static void ThrowEndOfStreamException(int count)
    {
        throw new EndOfStreamException($"Not enough bytes to read {count} bytes");
    }
}
