using BcsSharp.Core.Types;

namespace BcsSharp.Core
{
    /// <summary>
    /// Predefined instances of basic types for easy access
    /// </summary>
    public static class Bcs
    {
        // Unsigned integers
        public static readonly U8Type U8 = new();
        public static readonly U16Type U16 = new();
        public static readonly U32Type U32 = new();
        public static readonly U64Type U64 = new();
        public static readonly U128Type U128 = new();
        public static readonly U256Type U256 = new();

        // Signed integers
        public static readonly I8Type I8 = new();
        public static readonly I16Type I16 = new();
        public static readonly I32Type I32 = new();
        public static readonly I64Type I64 = new();
        public static readonly I128Type I128 = new();

        // Other basic types
        public static readonly BoolType Bool = new();
        public static readonly StringType String = new();
        public static readonly UnitType Unit = new();

        // Factory methods for complex types
        public static VectorType<T> Vector<T>(BcsType<T> elementType) => new(elementType);
        public static OptionType<T> Option<T>(BcsType<T> innerType) => new(innerType);
        public static MapType<TKey, TValue> Map<TKey, TValue>(BcsType<TKey> keyType, BcsType<TValue> valueType)
            where TKey : notnull => new(keyType, valueType);
        public static SetType<T> Set<T>(BcsType<T> elementType)
            where T : notnull => new(elementType);
        public static TupleType<T1, T2> Tuple<T1, T2>(BcsType<T1> type1, BcsType<T2> type2) => new(type1, type2);
        public static TupleType<T1, T2, T3> Tuple<T1, T2, T3>(BcsType<T1> type1, BcsType<T2> type2, BcsType<T3> type3) => new(type1, type2, type3);
        public static TupleType<T1, T2, T3, T4> Tuple<T1, T2, T3, T4>(BcsType<T1> type1, BcsType<T2> type2, BcsType<T3> type3, BcsType<T4> type4) => new(type1, type2, type3, type4);
        public static TupleType<T1, T2, T3, T4, T5> Tuple<T1, T2, T3, T4, T5>(BcsType<T1> type1, BcsType<T2> type2, BcsType<T3> type3, BcsType<T4> type4, BcsType<T5> type5) => new(type1, type2, type3, type4, type5);
        public static FixedArrayType<T> FixedArray<T>(BcsType<T> elementType, int length) => new(elementType, length);
        
        // Note: Struct types are created using BcsStruct.Create(name).AddField(...).Build()
    }
}