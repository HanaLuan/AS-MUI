using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Eruru.CSharp.Api {

	public static class ExtendsionMethods {

		public static readonly Dictionary<string, string> HttpHeadersEscapes = new Dictionary<string, string> () {
			{ ":\r\n", ":" },
			{ ":\n", ":" }
		};

		static readonly FieldInfo ZipArchiveEntryOffsetOfLocalHeaderFieldInfo = typeof (ZipArchiveEntry).GetField (
			"_offsetOfLocalHeader", BindingFlags.NonPublic | BindingFlags.Instance
		);

		public static bool TryGetEntryFullLength (this ZipArchive zipArchive, long fileAreaLength, ZipArchiveEntry entry, out long length) {
			var position = (long)ZipArchiveEntryOffsetOfLocalHeaderFieldInfo.GetValue (entry);
			var isFounded = false;
			var i = 0;
			foreach (var item in zipArchive.Entries) {
				if (isFounded) {
					length = (long)ZipArchiveEntryOffsetOfLocalHeaderFieldInfo.GetValue (item) - position;
					return true;
				}
				if (item == entry) {
					if (i == zipArchive.Entries.Count - 1) {
						length = fileAreaLength - position;
						return true;
					}
					isFounded = true;
				}
				i++;
			}
			length = 0;
			return false;
		}

		public static bool TryGetEntriesFullLength (
			this ZipArchive zipArchive, long fileAreaLength, IList<ZipArchiveEntry> entries, int offset, out long length
		) {
			var position = (long)ZipArchiveEntryOffsetOfLocalHeaderFieldInfo.GetValue (entries[offset]);
			var isFounded = false;
			var i = 0;
			ZipArchiveEntry endEntry = null;
			foreach (var item in zipArchive.Entries) {
				if (isFounded) {
					if (offset >= entries.Count || item != entries[offset++]) {
						endEntry = item;
						break;
					}
				} else {
					if (item == entries[offset]) {
						offset++;
						isFounded = true;
					}
				}
				i++;
			}
			if (isFounded) {
				if (endEntry != null) {
					length = (long)ZipArchiveEntryOffsetOfLocalHeaderFieldInfo.GetValue (endEntry) - position;
					return true;
				}
				length = fileAreaLength - position;
				return true;
			}
			length = 0;
			return false;
		}

		public static void CopyTo (
			this Stream stream, byte[] buffer, int offset, int length, EruruApi.OnProgress onProgress = null
		) {
			if (onProgress != null && !onProgress (0, 0, length)) {
				return;
			}
			var total = length;
			var writed = 0;
			while (true) {
				var count = stream.Read (buffer, offset, length);
				offset += count;
				length -= count;
				writed += count;
				if (count <= 0) {
					break;
				}
				if (onProgress != null && !onProgress (count, writed, total)) {
					break;
				}
			}
		}

		public static async Task CopyToAsync (
			this Stream stream, byte[] buffer, int offset, int length, EruruApi.OnProgress onProgress = null
		) {
			if (onProgress != null && !onProgress (0, 0, length)) {
				return;
			}
			var total = length;
			var writed = 0;
			while (true) {
				var count = await stream.ReadAsync (buffer, offset, length);
				offset += count;
				length -= count;
				writed += count;
				if (count <= 0) {
					break;
				}
				if (onProgress != null && !onProgress (count, writed, total)) {
					break;
				}
			}
		}

		public static async Task CopyToAsync (
			this Stream stream, Stream target, EruruApi.OnProgress onProgress = null, int bufferSize = 1024 * 1024
		) {
			if (onProgress != null && !onProgress (0, 0, 0)) {
				return;
			}
			var buffer = new byte[bufferSize];
			var writed = 0L;
			while (true) {
				var count = await stream.ReadAsync (buffer, 0, buffer.Length);
				await target.WriteAsync (buffer, 0, count);
				writed += count;
				if (count <= 0) {
					break;
				}
				if (onProgress != null && !onProgress (count, writed, 0)) {
					break;
				}
			}
		}

		public static string ToLocalString (this TimeSpan timeSpan) {
			var stringBuilder = new StringBuilder ();
			if (timeSpan.Days != 0) {
				stringBuilder.Append ($"{timeSpan.Days}天");
			}
			if (timeSpan.Hours != 0) {
				stringBuilder.Append ($"{timeSpan.Hours}时");
			}
			if (timeSpan.Minutes != 0) {
				stringBuilder.Append ($"{timeSpan.Minutes}分");
			}
			if (timeSpan.Seconds != 0) {
				stringBuilder.Append ($"{timeSpan.Seconds}秒");
			}
			if (timeSpan.Milliseconds != 0) {
				stringBuilder.Append ($"{timeSpan.Milliseconds}毫秒");
			}
			if (stringBuilder.Length == 0) {
				stringBuilder.Append ("0秒");
			}
			return stringBuilder.ToString ();
		}

		public static DateTime ToLocalDateTime (this DateTime dateTime) {
			switch (dateTime.Kind) {
				case DateTimeKind.Utc:
					return dateTime.ToLocalTime ();
				case DateTimeKind.Unspecified:
					return new DateTime (dateTime.Ticks, DateTimeKind.Local);
				case DateTimeKind.Local:
					return dateTime;
				default:
					throw new NotImplementedException ($"未支持 {dateTime.Kind}");
			}
		}

		public static string ToIsoString (this DateTime dateTime) {
			return dateTime.ToLocalDateTime ().ToString (EruruApi.DateTimeIsoString);
		}

		public static string ToLocalDateTimeString (this DateTime dateTime) {
			return dateTime.ToLocalDateTime ().ToString (EruruApi.DateTimeLocalString);
		}

		public static long ToUnixTimeSeconds (this DateTime dateTime) {
			return new DateTimeOffset (dateTime).ToUnixTimeSeconds ();
		}

		public static long ToUnixTimeMilliseconds (this DateTime dateTime) {
			return new DateTimeOffset (dateTime).ToUnixTimeMilliseconds ();
		}

		public static string ToMessage (this Exception exception) {
			var stringBuilder = new StringBuilder ();
			for (var i = 0; i < 100 && exception != null; i++) {
				if (stringBuilder.Length > 0) {
					stringBuilder.Append (" -> ");
				}
				stringBuilder.Append (exception.Message);
				exception = exception.InnerException;
			}
			return stringBuilder.ToString ();
		}

		public static float Divide (this int a, float b, float defaultValue = default (float)) {
			return b != 0 ? a / b : defaultValue;
		}
		public static double Divide (this long a, double b, double defaultValue = default (double)) {
			return b != 0 ? a / b : defaultValue;
		}
		public static float Divide (this float a, float b, float defaultValue = default (float)) {
			return b != 0 ? a / b : defaultValue;
		}

		public static T ToEnum<T> (this JToken token, JsonSerializer serializer, T defaultValue = default (T)) {
			if (token == null || token.Type == JTokenType.Null) {
				return defaultValue;
			}
			return token.ToObject<T> (serializer);
		}

		public static T ToValue<T> (this JToken token, T defaultValue = default (T)) {
			if (token == null || token.Type == JTokenType.Null) {
				if (typeof (T) == typeof (string)) {
					return defaultValue != null ? defaultValue : (T)(object)string.Empty;
				}
				if (typeof (T) == typeof (JArray)) {
					return defaultValue != null ? defaultValue : (T)(object)new JArray ();
				}
				if (typeof (T) == typeof (JObject)) {
					return defaultValue != null ? defaultValue : (T)(object)new JObject ();
				}
				if (typeof (T) == typeof (JToken)) {
					return defaultValue != null ? defaultValue : (T)(object)new JObject ();
				}
				return defaultValue;
			}
			return token.Value<T> ();
		}

		public static async Task InvokeAsync (this MulticastDelegate multicastDelegate, params object[] args) {
			if (multicastDelegate == null) {
				return;
			}
			await Task.WhenAll (multicastDelegate.GetInvocationList ().Select (async item => await (Task)item.DynamicInvoke (args)));
		}

		public static Task ContinueOnFaulted (this Task task, Action<Task> action) {
			return task.ContinueWith (action, TaskContinuationOptions.OnlyOnFaulted);
		}
		public static Task ContinueOnFaulted (this Task task, Func<Task, Task> func) {
			return task.ContinueWith (func, TaskContinuationOptions.OnlyOnFaulted);
		}

		public static Task ContinueOnFaultedConsoleWriteLine (this Task task) {
			return task.ContinueWith (item => Console.WriteLine (item.Exception), TaskContinuationOptions.OnlyOnFaulted);
		}

		public static string TrimEnd (this string text, string value, StringComparison stringComparison = StringComparison.Ordinal) {
			if (string.IsNullOrEmpty (value) || text.Length < value.Length) {
				return text;
			}
			var index = text.LastIndexOf (value, text.Length - 1, value.Length, stringComparison);
			if (index > -1) {
				return text.Substring (0, text.Length - value.Length);
			}
			return text;
		}

		public static string GetNameWithoutAsync (this MethodInfo methodInfo) {
			if (!typeof (Task).IsAssignableFrom (methodInfo.ReturnType)) {
				return methodInfo.Name;
			}
			return methodInfo.Name.TrimEnd ("Async");
		}
		public static string GetNameWithoutAsync (this MethodBase method) {
			return GetNameWithoutAsync ((MethodInfo)method);
		}

		public static bool EqualsCharacter (this char a, char b, bool ignoreCase = false) {
			if (ignoreCase) {
				return char.ToUpperInvariant (a) == char.ToUpperInvariant (b);
			}
			return a == b;
		}

		public static bool IsEndOfFileOrWhiteSpace (this char character) {
			return character == '\0' || char.IsWhiteSpace (character);
		}

		public static string Escape (this string text, IDictionary<string, string> escapes) {
			return Regex.Replace (text, string.Join ("|", escapes.Keys.Select (Regex.Escape)), match => escapes[match.Value], RegexOptions.Compiled);
		}

		public static void Set (this HttpRequestHeaders headers, string key, string value) {
			headers.Remove (key);
			headers.Add (key, value);
		}
		public static void Set (this HttpRequestHeaders headers, string headersText) {
			headersText = headersText.Escape (HttpHeadersEscapes);
			var lines = headersText.Split ('\n');
			for (var i = 0; i < lines.Length; i++) {
				var data = lines[i].TrimEnd ('\r').Split (':');
				if (data.Length == 2) {
					headers.Set (data[0], data[1]);
				}
			}
		}

		public static string GetSchemaAuthorityText (this Uri uri) {
			return $"{uri.Scheme}://{uri.Authority}";
		}
		public static string GetSchemaAuthorityPathText (this Uri uri) {
			return $"{uri.Scheme}://{uri.Authority}{uri.AbsolutePath}";
		}

		public static Uri AddParameters (this Uri uri, IEnumerable<KeyValuePair<object, object>> parameters) {
			var stringBuilder = new StringBuilder ();
			stringBuilder.Append (uri.GetSchemaAuthorityPathText ());
			if (string.IsNullOrEmpty (uri.Query)) {
				stringBuilder.Append ("?");
			} else {
				stringBuilder.Append (uri.Query);
				stringBuilder.Append ('&');
			}
			var index = 0;
			foreach (var parameter in parameters) {
				if (index > 0) {
					stringBuilder.Append ('&');
				}
				stringBuilder.Append (parameter.Key);
				stringBuilder.Append ('=');
				stringBuilder.Append (HttpUtility.UrlEncode (EruruApi.ToString (parameter.Value)));
				index++;
			}
			return new Uri (stringBuilder.ToString ());
		}
		public static Uri AddParameters (this Uri baseUri, string relativeUrl, IEnumerable<KeyValuePair<object, object>> parameters) {
			return AddParameters (new Uri (baseUri, relativeUrl), parameters);
		}

		public static IPEndPoint GetRemoteEndPoint (this HttpListenerRequest request) {
			try {
				return request.RemoteEndPoint;
			} catch {
				return new IPEndPoint (IPAddress.None, 0);
			}
		}

	}

}