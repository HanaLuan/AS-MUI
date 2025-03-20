using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Schema;
using Newtonsoft.Json.Schema.Generation;
using Newtonsoft.Json.Serialization;

namespace Eruru.CSharp.Api {

	public static class EruruApi {

		public delegate bool OnProgress (int length, long current, long total);

		public const string DateTimeIsoString = "yyyy'-'MM'-'dd'T'HH':'mm':'ss'.'fffffffK";
		public const string DateTimeLocalString = "yyyy'年'MM'月'dd'日' HH'时'mm'分'ss'秒'";
		public static readonly string[] FileSizeUnits = new string[] { "B", "KB", "MB", "GB", "TB", "PB" };
		public static Encoding Utf8BomEncoding { get; set; } = new UTF8Encoding (true);
		public static Func<JsonSerializerSettings> JsonSerializerSettings { get; set; } = () => new JsonSerializerSettings () {
			Converters = new List<JsonConverter> () { new StringEnumConverter () { AllowIntegerValues = true } },
			ContractResolver = new CamelCasePropertyNamesContractResolver (),
			ObjectCreationHandling = ObjectCreationHandling.Replace,
			DateTimeZoneHandling = DateTimeZoneHandling.Local,
			DateFormatString = DateTimeIsoString
		};

		static FieldInfo JsonSchemaLicenseGenerationCountFieldInfo;
		static FieldInfo JsonSchemaLicenseValidationCountFieldInfo;
		static readonly ConcurrentDictionary<Type, JSchema> TypeJsonSchemas = new ConcurrentDictionary<Type, JSchema> ();

		public static void SetJsonSchemaLicenseUsedCount (int count) {
			if (JsonSchemaLicenseGenerationCountFieldInfo == null) {
				var type = AppDomain.CurrentDomain.GetAssemblies ().First (item => item.FullName.StartsWith ("Newtonsoft.Json.Schema"))
					.GetType ("Newtonsoft.Json.Schema.Infrastructure.Licensing.LicenseHelpers");
				JsonSchemaLicenseGenerationCountFieldInfo = type.GetField ("_generationCount", BindingFlags.NonPublic | BindingFlags.Static);
				JsonSchemaLicenseValidationCountFieldInfo = type.GetField ("_validationCount", BindingFlags.NonPublic | BindingFlags.Static);
			}
			JsonSchemaLicenseGenerationCountFieldInfo.SetValue (null, count);
			JsonSchemaLicenseValidationCountFieldInfo.SetValue (null, count);
		}

		public static JSchema ToJsonSchema (Type type) {
			if (TypeJsonSchemas.TryGetValue (type, out var jsonSchema)) {
				return jsonSchema;
			}
			var generator = new JSchemaGenerator ();
			generator.GenerationProviders.Add (new StringEnumGenerationProvider ());
			generator.GenerationProviders.Add (new JsonSchemaDateTimeGenerationProvider ());
			generator.ContractResolver = new CamelCasePropertyNamesContractResolver ();
			SetJsonSchemaLicenseUsedCount (0);
			jsonSchema = generator.Generate (type);
			TypeJsonSchemas[type] = jsonSchema;
			return jsonSchema;
		}
		public static JSchema ToJsonSchema<T> () {
			return ToJsonSchema (typeof (T));
		}

		public static Uri AddUriParameters (Uri baseUri, string relativeUrl, IEnumerable<KeyValuePair<object, object>> parameters) {
			return new Uri (baseUri, relativeUrl).AddParameters (parameters);
		}
		public static Uri AddUriParameters (Uri baseUri, IEnumerable<KeyValuePair<object, object>> parameters) {
			return baseUri.AddParameters (parameters);
		}
		public static Uri AddUriParameters (string url, IEnumerable<KeyValuePair<object, object>> parameters) {
			return new Uri (url).AddParameters (parameters);
		}

		public static string FormatPercentage (double percentage, string format = "{0:#,0.##}%") {
			return string.Format (format, percentage * 100F);
		}
		public static string FormatPercentage (float percentage, string format = "{0:#,0.##}%") {
			return string.Format (format, percentage * 100F);
		}
		public static string FormatPercentage (long percentage, string format = "{0:#,0.##}%") {
			return string.Format (format, percentage * 100F);
		}
		public static string FormatPercentage (int percentage, string format = "{0:#,0.##}%") {
			return string.Format (format, percentage * 100F);
		}

		public static string FormatValue (object value, string format = "{0:#,0.##}") {
			return string.Format (format, value);
		}

		public static string FormatFileSize (double length, string[] units, string format = "{0:#,0.##} {1}") {
			var index = 0;
			while (length >= 1024 && index < units.Length - 1) {
				length /= 1024;
				index++;
			}
			return string.Format (format, length, units[index]);
		}
		public static string FormatFileSize (double length, string format = "{0:#,0.##} {1}") {
			return FormatFileSize (length, FileSizeUnits, format);
		}

		public static string FormatFileSizeFixed (double length, string format = "{0:f2} {1}") {
			return FormatFileSize (length, FileSizeUnits, format);
		}

		public static int ToHexInt (char hex) {
			if (hex >= '0' && hex <= '9') {
				return hex - '0';
			}
			hex = char.ToUpperInvariant (hex);
			if (hex >= 'A' && hex <= 'F') {
				return hex - 'A' + 10;
			}
			throw new ArgumentException ($"非Hex字符 {hex}");
		}

		public static char ToHexCharacter (int hex) {
			const string hexString = "0123456789ABCDEF";
			if (hex < 0 || hex >= hexString.Length) {
				throw new ArgumentException ($"Hex值应为0-15 {hex}");
			}
			return hexString[hex];
		}

		public static byte[] ToHexBytes (string hex) {
			if (hex.Length % 2 != 0) {
				throw new ArgumentException ($"非偶数长度Hex字符串 {hex}");
			}
			var bytes = new byte[hex.Length / 2];
			for (var i = 0; i < hex.Length; i += 2) {
				bytes[i / 2] = (byte)((ToHexInt (hex[i]) << 4) + ToHexInt (hex[i + 1]));
			}
			return bytes;
		}

		public static string ToHexString (byte[] bytes) {
			var characters = new char[bytes.Length * 2];
			for (var i = 0; i < bytes.Length; i++) {
				characters[i * 2] = ToHexCharacter (bytes[i] >> 4);
				characters[(i * 2) + 1] = ToHexCharacter (bytes[i] & 0b00001111);
			}
			return new string (characters);
		}

		public static object CreateInstance (Type type) {
			if (type.IsArray) {
				return Array.CreateInstance (type.GetElementType (), 0);
			}
			if (type.GetConstructor (Type.EmptyTypes) != null) {
				return Activator.CreateInstance (type);
			}
			return FormatterServices.GetUninitializedObject (type);
		}

		public static object ChangeType (object value, Type type, bool ignoreCase = false, IFormatProvider provider = null) {
			if (IsAssignableFrom (ref value, type, out var valueType)) {
				return value;
			}
			if (type.IsEnum) {
				return ToEnum (value, type, ignoreCase);
			}
			var typeCode = Type.GetTypeCode (type);
			var valueCode = Type.GetTypeCode (value?.GetType ());
			switch (typeCode) {
				case TypeCode.Byte:
				case TypeCode.UInt16:
				case TypeCode.UInt32:
				case TypeCode.UInt64:
				case TypeCode.SByte:
				case TypeCode.Int16:
				case TypeCode.Int32:
				case TypeCode.Int64:
					if (valueType.IsEnum) {
						return Convert.ChangeType (value, type, provider);
					}
					switch (typeCode) {
						case TypeCode.UInt32:
						case TypeCode.Int32:
						case TypeCode.Int64:
							switch (valueCode) {
								case TypeCode.DateTime:
									return Convert.ChangeType (Convert.ToDateTime (value).ToUnixTimeSeconds (), type, provider);
							}
							break;
					}
					break;
				case TypeCode.String:
					return ToString (value);
				case TypeCode.DateTime:
					return ToDateTime (value);
			}
			return Convert.ChangeType (value, type, provider);
		}

		public static string ToString (object value, IFormatProvider provider = null) {
			if (IsAssignableFrom (ref value, typeof (string), out var valueType)) {
				return Convert.ToString (value, provider);
			}
			if (valueType.IsEnum) {
				return Convert.ToString (value, provider);
			}
			switch (Type.GetTypeCode (valueType)) {
				case TypeCode.DateTime:
					return Convert.ToDateTime (value, provider).ToIsoString ();
				default:
					return Convert.ToString (value, provider);
			}
		}

		public static object ToEnum (object value, Type type, bool ignoreCase = false, IFormatProvider provider = null) {
			if (IsAssignableFrom (ref value, type, out var valueType)) {
				return Convert.ChangeType (value, type, provider);
			}
			switch (Type.GetTypeCode (valueType)) {
				case TypeCode.String: {
					var text = Convert.ToString (value, provider);
					try {
						return Enum.Parse (type, text, ignoreCase);
					} catch {
						if (long.TryParse (text, out var longValue)) {
							return Enum.ToObject (type, longValue);
						}
						if (ulong.TryParse (text, out var ulongValue)) {
							return Enum.ToObject (type, ulongValue);
						}
						throw;
					}
				}
				default:
					return Enum.ToObject (type, value);
			}
		}
		public static T ToEnum<T> (object value, bool ignoreCase = false) {
			return (T)ToEnum (value, typeof (T), ignoreCase);
		}

		public static DateTime ToDateTime (object value, bool isUnixMilliseconds = false, IFormatProvider provider = null) {
			if (IsAssignableFrom (ref value, typeof (DateTime), out var valueType)) {
				return Convert.ToDateTime (value, provider);
			}
			switch (Type.GetTypeCode (valueType)) {
				case TypeCode.UInt32:
				case TypeCode.Int32:
				case TypeCode.Int64: {
					var timestamp = Convert.ToInt64 (value, provider);
					if (isUnixMilliseconds) {
						return DateTimeOffset.FromUnixTimeMilliseconds (timestamp).LocalDateTime;
					}
					return DateTimeOffset.FromUnixTimeSeconds (timestamp).LocalDateTime;
				}
				default: {
					try {
						return Convert.ToDateTime (value, provider).ToLocalDateTime ();
					} catch {
						if (value is string text && long.TryParse (text, out var timestamp)) {
							return ToDateTime (timestamp, isUnixMilliseconds);
						}
						throw;
					}
				}
			}
		}
		public static DateTime ToDateTime (object value) {
			return ToDateTime (value, false);
		}

		static bool IsAssignableFrom (ref object value, Type type, out Type valueType, IFormatProvider provider = null) {
			if (type == null) {
				valueType = null;
				return true;
			}
			if (value == null) {
				value = Convert.ChangeType (value, type, provider);
				valueType = value?.GetType ();
				return true;
			}
			valueType = value.GetType ();
			return type.IsAssignableFrom (valueType);
		}

		public static bool IsDirectory (string outputPath) {
			return Directory.Exists (outputPath) || !Path.HasExtension (outputPath);
		}

	}

}