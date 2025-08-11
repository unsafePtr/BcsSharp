namespace BcsSharp.Core.Formatters
{
	/// <summary>
	/// Formatter for BCS Option<T> type - handles nullable reference types
	/// </summary>
	public sealed class NullableReferenceFormatter<T> : IBcsFormatter<T?> where T : class
	{
		private readonly IBcsFormatter<T> _valueFormatter;

		public Type TargetType => typeof(T);

		public NullableReferenceFormatter(IBcsFormatter<T> valueFormatter)
		{
			_valueFormatter = valueFormatter ?? throw new ArgumentNullException(nameof(valueFormatter));
		}

		public void Serialize(ref BcsWriter writer, T? value)
		{
			if (value != null)
			{
				// Write 1 to indicate Some(value)
				writer.Write((byte)1);
				_valueFormatter.Serialize(ref writer, value);
			}
			else
			{
				// Write 0 to indicate None
				writer.Write((byte)0);
			}
		}

		public T? Deserialize(ref BcsReader reader)
		{
			var hasValue = reader.Read8();
			if (hasValue == 0)
			{
				return null;
			}
			else if (hasValue == 1)
			{
				return _valueFormatter.Deserialize(ref reader);
			}
			else
			{
				throw new InvalidOperationException($"Invalid Option discriminant: {hasValue}. Expected 0 (None) or 1 (Some).");
			}
		}

		public int? GetSerializedSize(T? value)
		{
			if (value == null)
			{
				return 1; // Just the discriminant byte
			}

			var valueSize = _valueFormatter.GetSerializedSize(value);
			if (valueSize == null)
				return null;

			return 1 + valueSize.Value; // Discriminant + value
		}
	}
}