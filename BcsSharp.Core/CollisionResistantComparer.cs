using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using BcsSharp.Core.Helpers;

namespace BcsSharp.Core;

/// <summary>
/// Key comparers that keep a map decoded from untrusted input from being flooded with keys whose hash codes collide.
/// </summary>
/// <remarks>
/// The default hash of an integer is predictable (a <see cref="long"/> hashes to its two halves XORed), so an attacker can send thousands of distinct keys that land in one bucket and turn decoding into O(n²) work.
/// Integer, <see cref="bool"/>, <see cref="Int128"/>/<see cref="UInt128"/> and enum keys are hashed instead with SipHash-2-4 over their bytes, keyed by a random per-process seed.
/// String keys keep the default comparer, because <see cref="Dictionary{TKey, TValue}"/> already switches strings to randomized hashing once it sees collisions.
/// Any other key type also keeps the default comparer: hashing its bytes is only sound when its equality is bitwise, which a custom type does not promise.
/// MessagePack-CSharp takes the same approach in <see href="https://github.com/MessagePack-CSharp/MessagePack-CSharp/blob/master/src/MessagePack/MessagePackSecurity.cs">MessagePackSecurity</see>.
/// </remarks>
public static class CollisionResistantComparer
{
    private static readonly SipHash Hash = CreateHash();

    /// <summary>Returns the collision-resistant comparer for <typeparamref name="T"/>, or <see langword="null"/> when the default comparer is kept.</summary>
    public static IEqualityComparer<T>? For<T>() => Cache<T>.Comparer;

    private static SipHash CreateHash()
    {
        Span<ulong> key = stackalloc ulong[2];
        RandomNumberGenerator.Fill(MemoryMarshal.AsBytes(key));

        return new SipHash(key[0], key[1]);
    }

    private static bool IsBitwiseKey(Type type) =>
        type.IsEnum ||
        type == typeof(bool) || type == typeof(byte) || type == typeof(sbyte) ||
        type == typeof(ushort) || type == typeof(short) ||
        type == typeof(uint) || type == typeof(int) ||
        type == typeof(ulong) || type == typeof(long) ||
        type == typeof(UInt128) || type == typeof(Int128);

    private static class Cache<T>
    {
        public static readonly IEqualityComparer<T>? Comparer = IsBitwiseKey(typeof(T))
            ? (IEqualityComparer<T>)Activator.CreateInstance(typeof(BitwiseComparer<>).MakeGenericType(typeof(T)))!
            : null;
    }

    private sealed class BitwiseComparer<T> : IEqualityComparer<T>
        where T : unmanaged
    {
        public bool Equals(T x, T y) => EqualityComparer<T>.Default.Equals(x, y);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int GetHashCode(T obj) => (int)Hash.Compute(MemoryMarshal.AsBytes(new ReadOnlySpan<T>(in obj)));
    }
}
