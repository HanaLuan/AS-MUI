using System;
using System.IO;
using Newtonsoft.Json;

namespace Eruru.CSharp.Api {

	public class CommandLineWriter : IDisposable {

		public Func<JsonSerializerSettings> JsonSerializerSettings { get; set; } = EruruApi.JsonSerializerSettings;
		public bool IsWebApi { get; set; }
		public Formatting Formatting { get => JsonTextWriter.Formatting; set => JsonTextWriter.Formatting = value; }

		readonly JsonTextWriter JsonTextWriter;
		bool WritedHeader = true;
		bool WritedResult;

		public CommandLineWriter (bool isWebApi = false, TextWriter textWriter = null) {
			IsWebApi = isWebApi;
			JsonTextWriter = new JsonTextWriter (textWriter ?? Console.Out);
		}

		public void Dispose () {
			SetResult (0, string.Empty, null);
			GC.SuppressFinalize (this);
		}

		public void WriteLine (string text) {
			if (IsWebApi) {
				Header ();
				JsonTextWriter.WriteValue (text);
				return;
			}
			Console.WriteLine (text);
		}
		public void WriteLine (object value) {
			WriteLine (EruruApi.ToString (value));
		}
		public void WriteLine () {
			if (IsWebApi) {
				return;
			}
			Console.WriteLine ();
		}

		public void SetResult (int code = 0, string message = "", object data = default (object)) {
			if (!IsWebApi || WritedResult) {
				return;
			}
			WritedResult = true;
			Header ();
			JsonTextWriter.WriteEndArray ();
			JsonTextWriter.WritePropertyName ("data");
			JsonSerializer.CreateDefault (JsonSerializerSettings ()).Serialize (JsonTextWriter, data);
			JsonTextWriter.WritePropertyName ("code");
			JsonTextWriter.WriteValue (code);
			JsonTextWriter.WritePropertyName ("message");
			JsonTextWriter.WriteValue (message);
			JsonTextWriter.WriteEndObject ();
		}

		public void SetData (object data) {
			SetResult (data: data);
		}
		public void SetData (Func<object> data) {
			if (!IsWebApi) {
				return;
			}
			SetResult (data: data ());
		}

		public void SetException (Exception exception) {
			if (!IsWebApi) {
				throw exception;
			}
			SetResult (-1, exception.Message, exception);
		}

		void Header () {
			if (!WritedHeader) {
				return;
			}
			WritedHeader = true;
			JsonTextWriter.WriteStartObject ();
			JsonTextWriter.WritePropertyName ("messages");
			JsonTextWriter.WriteStartArray ();
		}

	}

}