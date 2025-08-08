using System.Runtime.CompilerServices;

namespace Sui.Types;

/// <summary>
/// High-performance Base58 encoding/decoding using Sui's alphabet
/// Sui Base58 alphabet: 123456789ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz
/// </summary>
public static class SuiBase58
{
    /// <summary>
    /// Sui Base58 alphabet (Bitcoin alphabet without 0, O, I, l)
    /// </summary>
    private const string Alphabet = "123456789ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz";

    /// <summary>
    /// Lookup table for decoding - maps ASCII values to Base58 values
    /// </summary>
    private static readonly byte[] DecodeTable = CreateDecodeTable();

    /// <summary>
    /// Base of the encoding (58)
    /// </summary>
    private const int Base = 58;

    /// <summary>
    /// Create the decode lookup table
    /// </summary>
    private static byte[] CreateDecodeTable()
    {
        var table = new byte[128];
        
        // Initialize with invalid values
        for (int i = 0; i < table.Length; i++)
        {
            table[i] = 255; // Invalid marker
        }
        
        // Map valid characters to their Base58 values
        for (int i = 0; i < Alphabet.Length; i++)
        {
            table[Alphabet[i]] = (byte)i;
        }
        
        return table;
    }

    /// <summary>
    /// Encode byte array to Base58 string
    /// </summary>
    /// <param name="data">Bytes to encode</param>
    /// <returns>Base58 encoded string</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string Encode(ReadOnlySpan<byte> data)
    {
        if (data.IsEmpty)
            return string.Empty;

        // Count leading zeros
        int leadingZeros = 0;
        while (leadingZeros < data.Length && data[leadingZeros] == 0)
            leadingZeros++;

        // Calculate maximum output size
        // Base58 encoding can expand input by ~37% at most
        int maxOutputSize = ((data.Length * 138) / 100) + 1;
        
        // Handle the case where input is all zeros
        if (leadingZeros == data.Length)
        {
            return new string('1', leadingZeros);
        }

        // Skip leading zeros for conversion
        var inputSpan = data[leadingZeros..];
        
        // Convert to base 58
        var digits = new byte[inputSpan.Length * 2]; // Sufficient size
        int digitCount = 1;
        digits[0] = 0;

        foreach (byte b in inputSpan)
        {
            int carry = b;
            
            for (int i = 0; i < digitCount; i++)
            {
                carry += digits[i] << 8;
                digits[i] = (byte)(carry % Base);
                carry /= Base;
            }
            
            while (carry > 0)
            {
                digits[digitCount++] = (byte)(carry % Base);
                carry /= Base;
            }
        }

        // Build result string
        var result = new char[leadingZeros + digitCount];
        int index = 0;
        
        // Add leading '1's for leading zeros
        for (int i = 0; i < leadingZeros; i++)
        {
            result[index++] = '1';
        }
        
        // Add encoded digits in reverse order
        for (int i = digitCount - 1; i >= 0; i--)
        {
            result[index++] = Alphabet[digits[i]];
        }
        
        return new string(result);
    }

    /// <summary>
    /// Decode Base58 string to byte array
    /// </summary>
    /// <param name="encoded">Base58 encoded string</param>
    /// <returns>Decoded byte array</returns>
    /// <exception cref="ArgumentException">Invalid Base58 character</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static byte[] Decode(ReadOnlySpan<char> encoded)
    {
        if (encoded.IsEmpty)
            return [];

        // Count leading '1's
        int leadingOnes = 0;
        while (leadingOnes < encoded.Length && encoded[leadingOnes] == '1')
            leadingOnes++;

        // Calculate output size
        int outputSize = ((encoded.Length * 733) / 1000) + 1; // Base58 to binary ratio
        var decoded = new byte[outputSize];
        int decodedLength = 1;
        decoded[0] = 0;

        // Process each character
        for (int i = leadingOnes; i < encoded.Length; i++)
        {
            char c = encoded[i];
            
            // Validate character and get value
            if (c >= 128 || DecodeTable[c] == 255)
                throw new ArgumentException($"Invalid Base58 character: '{c}'");
            
            int carry = DecodeTable[c];
            
            // Multiply existing number by 58 and add new digit
            for (int j = 0; j < decodedLength; j++)
            {
                carry += decoded[j] * Base;
                decoded[j] = (byte)(carry & 0xFF);
                carry >>= 8;
            }
            
            // Handle overflow
            while (carry > 0)
            {
                decoded[decodedLength++] = (byte)(carry & 0xFF);
                carry >>= 8;
            }
        }

        // Create result array with proper size
        var result = new byte[leadingOnes + decodedLength];
        
        // Leading zeros for leading '1's
        Array.Clear(result, 0, leadingOnes);
        
        // Copy decoded bytes in reverse order
        for (int i = 0; i < decodedLength; i++)
        {
            result[leadingOnes + decodedLength - 1 - i] = decoded[i];
        }
        
        return result;
    }

    /// <summary>
    /// Check if a string contains only valid Base58 characters
    /// </summary>
    /// <param name="input">String to validate</param>
    /// <returns>True if all characters are valid Base58</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsValidBase58(ReadOnlySpan<char> input)
    {
        foreach (char c in input)
        {
            if (c >= 128 || DecodeTable[c] == 255)
                return false;
        }
        return true;
    }
}