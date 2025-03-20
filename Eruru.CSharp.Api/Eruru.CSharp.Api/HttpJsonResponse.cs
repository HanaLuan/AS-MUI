using System;

namespace Eruru.CSharp.Api {

	public class HttpJsonResponse {

		public int Code { get; set; }
		public string Message { get; set; }
		public object Data { get; set; }

		public HttpJsonResponse (int code, string message, object data) {
			Code = code;
			Message = message;
			Data = data;
		}
		public HttpJsonResponse (object data) : this (0, string.Empty, data) {

		}
		public HttpJsonResponse (Exception exception) : this (-1, exception.ToMessage (), exception.ToString ()) {

		}
		public HttpJsonResponse () : this (0, string.Empty, null) {

		}

	}

}