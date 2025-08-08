using System;

namespace BcsSharp.Core
{
    /// <summary>
    /// Supported encodings for string conversion
    /// </summary>
    public enum Encoding
    {
        Base58,
        Base64,
        Hex
    }

    /// <summary>
    /// Options for BcsType validation and configuration
    /// </summary>
    public class BcsTypeOptions<T>
    {
        public string? Name { get; set; }
        public Action<T>? Validate { get; set; }
    }

    /// <summary>
    /// Abstract base class for all BCS types
    /// </summary>
    public abstract class BcsType<T>
    {
        public string Name { get; protected set; }
        public Action<T>? Validate { get; protected set; }

        protected BcsType(string name, Action<T>? validate = null)
        {
            Name = name;
            Validate = validate;
        }

        /// <summary>
        /// Read value from BcsReader
        /// </summary>
        public abstract T Read(BcsReader reader);

        /// <summary>
        /// Write value to BcsWriter
        /// </summary>
        public abstract void Write(T value, BcsWriter writer);

        /// <summary>
        /// Get serialized size of value (null if dynamic size)
        /// </summary>
        public virtual int? SerializedSize(T value) => null;

        /// <summary>
        /// Serialize value to byte array
        /// </summary>
        public virtual byte[] Serialize(T value, BcsWriterOptions? options = null)
        {
            ValidateValue(value);
            var writer = new BcsWriter(options);
            Write(value, writer);
            return writer.ToBytes();
        }

        /// <summary>
        /// Parse value from byte array
        /// </summary>
        public T Parse(byte[] bytes)
        {
            if (bytes == null)
                throw new ArgumentNullException(nameof(bytes));

            var reader = new BcsReader(bytes);
            return Read(reader);
        }

        /// <summary>
        /// Parse value from hex string
        /// </summary>
        public T FromHex(string hex)
        {
            if (string.IsNullOrEmpty(hex))
                throw new ArgumentException("Hex string cannot be null or empty", nameof(hex));

            var bytes = Convert.FromHexString(hex);
            return Parse(bytes);
        }

        /// <summary>
        /// Parse value from base64 string
        /// </summary>
        public T FromBase64(string base64)
        {
            if (string.IsNullOrEmpty(base64))
                throw new ArgumentException("Base64 string cannot be null or empty", nameof(base64));

            var bytes = Convert.FromBase64String(base64);
            return Parse(bytes);
        }

        /// <summary>
        /// Validate the input value
        /// </summary>
        protected virtual void ValidateValue(T value)
        {
            Validate?.Invoke(value);
        }

        /// <summary>
        /// Transform this type to another type with input/output conversion
        /// </summary>
        public BcsType<TOutput> Transform<TInput, TOutput>(
            string? name = null,
            Func<TInput, T>? inputTransform = null,
            Func<T, TOutput>? outputTransform = null,
            Action<TInput>? validate = null)
        {
            return new TransformedBcsType<T, TInput, TOutput>(
                this,
                name ?? Name,
                inputTransform,
                outputTransform,
                validate);
        }
    }

    /// <summary>
    /// Serialized BCS data wrapper
    /// </summary>
    public class SerializedBcs<T>
    {
        private readonly BcsType<T> _schema;
        private readonly byte[] _bytes;

        public SerializedBcs(BcsType<T> schema, byte[] bytes)
        {
            _schema = schema ?? throw new ArgumentNullException(nameof(schema));
            _bytes = bytes ?? throw new ArgumentNullException(nameof(bytes));
        }

        public byte[] ToBytes() => (byte[])_bytes.Clone();

        public string ToHex() => Convert.ToHexString(_bytes).ToLowerInvariant();

        public string ToBase64() => Convert.ToBase64String(_bytes);

        public T Parse() => _schema.Parse(_bytes);
    }

    /// <summary>
    /// Helper for transformed BCS types
    /// </summary>
    internal class TransformedBcsType<TOriginal, TInput, TOutput> : BcsType<TOutput>
    {
        private readonly BcsType<TOriginal> _originalType;
        private readonly Func<TInput, TOriginal>? _inputTransform;
        private readonly Func<TOriginal, TOutput>? _outputTransform;
        private readonly Action<TInput>? _inputValidate;

        public TransformedBcsType(
            BcsType<TOriginal> originalType,
            string name,
            Func<TInput, TOriginal>? inputTransform,
            Func<TOriginal, TOutput>? outputTransform,
            Action<TInput>? validate) : base(name)
        {
            _originalType = originalType;
            _inputTransform = inputTransform;
            _outputTransform = outputTransform;
            _inputValidate = validate;
        }

        public override TOutput Read(BcsReader reader)
        {
            var originalValue = _originalType.Read(reader);

            if (_outputTransform != null)
            {
                return _outputTransform.Invoke(originalValue!);
            }

            return (TOutput)(object)originalValue!;
        }

        public override void Write(TOutput value, BcsWriter writer)
        {
            // This is complex due to type transformation - would need more specific implementation
            throw new NotImplementedException("Write method for transformed types needs specific implementation");
        }

        public override int? SerializedSize(TOutput value)
        {
            // Similar complexity for size calculation
            return null;
        }
    }
}