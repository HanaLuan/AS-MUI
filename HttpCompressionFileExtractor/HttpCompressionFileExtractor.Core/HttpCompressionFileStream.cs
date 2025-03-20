using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Eruru.CSharp.Api;

namespace HttpCompressionFileExtractor.Core {

	public class HttpCompressionFileStream : Stream {

		public override bool CanRead => true;
		public override bool CanSeek => true;
		public override bool CanWrite => false;
		public override long Length => _Length;
		public override long Position { get; set; }
		public string Url { get; set; }
		public uint CentralDirectorySize { get; set; }
		public uint CentralDirectoryOffset { get; set; }
		public ushort CommentSize { get; set; }
		public long FileAreaSize { get; set; }
		public int BufferSize { get; set; }
		public long MaxFileSize { get; set; }
		public long TotalReadLength { get; set; }
		public EruruApi.OnProgress OnProgress { get; set; }

		readonly HttpClient HttpClient = new HttpClient ();
		bool IsCentralDirectory;
		readonly byte[] HeadBuffer = new byte[22];
		byte[] Buffer;
		long BufferPosition;
		long BufferLength;
		long _Length;

		public HttpCompressionFileStream (string url, int bufferSize = 1024 * 1024) {
			Url = url;
			BufferSize = bufferSize;
		}

		protected override void Dispose (bool disposing) {
			base.Dispose (disposing);
			HttpClient.Dispose ();
			Buffer = null;
		}

		public async Task HeadAsync (CancellationToken cancellationToken) {
			using (var message = new HttpRequestMessage (HttpMethod.Head, Url)) {
				using (var response = await HttpClient.SendAsync (message, cancellationToken)) {
					if (!response.Headers.AcceptRanges.Contains ("bytes")) {
						//throw new ArgumentException ("Remote does not support ranges!");
					}
					if (!response.Content.Headers.ContentLength.HasValue || response.Content.Headers.ContentLength == 0) {
						throw new ArgumentException ("Remote has no content length!");
					}
					_Length = response.Content.Headers.ContentLength.Value;
					await ReadRangeAsync (Length - HeadBuffer.Length, HeadBuffer.Length, HeadBuffer, 0, cancellationToken);
					using (var reader = new BinaryReader (new MemoryStream (HeadBuffer))) {
						var signature = reader.ReadUInt32 ();
						if (signature != 0x06054b50) {
							throw new Exception ($"非ZIP文件");
						}
						reader.BaseStream.Seek (8, SeekOrigin.Current);
						CentralDirectorySize = reader.ReadUInt32 ();
						CentralDirectoryOffset = reader.ReadUInt32 ();
						CommentSize = reader.ReadUInt16 ();
						FileAreaSize = Length - HeadBuffer.Length - CommentSize - CentralDirectorySize;
						IsCentralDirectory = true;
					}
				}
			}
		}

		public async Task CentralDirectoryAsync (CancellationToken cancellationToken, EruruApi.OnProgress onProgress = null) {
			BufferPosition = CentralDirectoryOffset;
			BufferLength = (int)(CentralDirectorySize + CommentSize + HeadBuffer.Length);
			Buffer = new byte[BufferLength];
			Array.Copy (HeadBuffer, 0, Buffer, CentralDirectorySize + CommentSize, HeadBuffer.Length);
			await ReadRangeAsync (BufferPosition, BufferLength - HeadBuffer.Length, Buffer, 0, cancellationToken, onProgress);
			Position = 0;
		}

		async Task ReadRangeAsync (
			long index, long length, byte[] buffer, int offset, CancellationToken cancellationToken, EruruApi.OnProgress onProgress = null
		) {
			using (var message = new HttpRequestMessage (HttpMethod.Get, Url)) {
				message.Headers.Add ("Range", $"bytes={index}-{index + length - 1}");
				using (var response = await HttpClient.SendAsync (message, HttpCompletionOption.ResponseHeadersRead, cancellationToken)) {
					if (response.StatusCode != (HttpStatusCode)206) {
						throw new Exception ($"HTTP响应状态码 {response.StatusCode} 不是206或服务器不支持断点续传");
					}
					using (var stream = await response.Content.ReadAsStreamAsync ()) {
						await stream.CopyToAsync (buffer, offset, (int)length, (count, current, total) => {
							TotalReadLength += count;
							return onProgress == null || onProgress (count, current, total);
						});
					}
				}
			}
		}

		public void Trim () {
			Buffer = Array.Empty<byte> ();
			BufferLength = 0;
		}

		public override void Flush () {
			throw new NotImplementedException ();
		}

		public override int Read (byte[] buffer, int offset, int count) {
			return ReadAsync (buffer, offset, count, CancellationToken.None).Result;
		}

		public override async Task<int> ReadAsync (byte[] buffer, int offset, int count, CancellationToken cancellationToken) {
			if (Position >= BufferPosition && Position - BufferPosition + count <= BufferLength) {
				Array.Copy (Buffer, Position - BufferPosition, buffer, offset, count);
				Position += count;
				return count;
			}
			if (IsCentralDirectory) {
				IsCentralDirectory = false;
			}
			var minLength = Math.Max (BufferSize, count);
			if (Buffer.Length < minLength) {
				Buffer = new byte[minLength];
			}
			BufferPosition = Position;
			BufferLength = Math.Min (Buffer.Length, Length - BufferPosition);
			if (MaxFileSize > 0 && BufferLength > MaxFileSize) {
				BufferLength = MaxFileSize;
			}
			BufferLength = Math.Max (BufferLength, count);
			const int retryCount = 3;
			for (var i = 0; i < retryCount; i++) {
				try {
					await ReadRangeAsync (BufferPosition, BufferLength, Buffer, 0, cancellationToken, OnProgress);
					return await ReadAsync (buffer, offset, count, cancellationToken);
				} catch (HttpRequestException exception) {
					if (i == retryCount - 1) {
						throw;
					}
					Console.WriteLine (exception.Message);
				}
			}
			throw new NotImplementedException ();
		}

		public override long Seek (long offset, SeekOrigin origin) {
			switch (origin) {
				case SeekOrigin.Begin:
					return Position = offset;
				case SeekOrigin.Current:
					return Position += offset;
				case SeekOrigin.End:
					return Position = Length + offset;
				default:
					throw new NotSupportedException ();
			}
		}

		public override void SetLength (long value) {
			_Length = value;
		}

		public override void Write (byte[] buffer, int offset, int count) {
			throw new NotImplementedException ();
		}

	}

}