using System;
using System.CommandLine;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Eruru.CSharp.Api;
using HttpCompressionFileExtractor.Core;
using Newtonsoft.Json.Linq;

namespace HttpCompressionFileExtractor.Cli {

	internal class Program {

		static readonly CommandLineWriter CommandLineWriter = new ();

		static async Task Main (string[] args) {
			var urlOption = new Option<string> ("--url", "URL");
			var searchTextOption = new Option<string> ("--search", () => string.Empty, "搜索文本");
			var isRegexOption = new Option<bool> ("--isRegex", () => false, "正则");
			var pathOption = new Option<string> ("--path", "路径");
			var outputPathOption = new Option<string> ("--output", "输出路径");
			var isWebApiOption = new Option<bool> ("--isWebApi", () => false, "WebApi方式打印结果");
			var rootCommand = new RootCommand ();
			var listCommand = new Command ("list", "查看HTTP压缩包文件列表") {
				urlOption, searchTextOption, isRegexOption, isWebApiOption
			};
			rootCommand.AddCommand (listCommand);
			listCommand.SetHandler (ListAsync, urlOption, searchTextOption, isRegexOption, isWebApiOption);
			var downloadCommand = new Command ("download", "下载HTTP压缩包内文件") {
				urlOption, pathOption, outputPathOption, isWebApiOption
			};
			rootCommand.AddCommand (downloadCommand);
			downloadCommand.SetHandler (DownloadAsync, urlOption, pathOption, outputPathOption, isWebApiOption);
			try {
				await rootCommand.InvokeAsync (args);
			} catch (Exception exception) {
				CommandLineWriter.SetException (exception);
			} finally {
				CommandLineWriter.Dispose ();
			}
		}

		static async Task<HttpCompressionFileStream> OpenAsync (string url, Stopwatch stopwatch) {
			var fileName = Path.GetFileName (new Uri (url).LocalPath);
			var stream = new HttpCompressionFileStream (url);
			CommandLineWriter.WriteLine ($"请求信息中 文件：{fileName}");
			stopwatch.Restart ();
			await stream.HeadAsync (CancellationToken.None);
			stopwatch.Stop ();
			CommandLineWriter.WriteLine ($"请求信息成功 耗时：{stopwatch.Elapsed.ToLocalString ()} 文件：{fileName}");
			CommandLineWriter.WriteLine ($"读取中央目录中 文件：{fileName}");
			var refreshTime = 0L;
			var readLength = 0;
			stopwatch.Start ();
			await stream.CentralDirectoryAsync (CancellationToken.None, (length, current, total) => {
				readLength += length;
				if (stopwatch.ElapsedMilliseconds >= refreshTime) {
					refreshTime = stopwatch.ElapsedMilliseconds + 1000;
					var stringBuilder = new StringBuilder ();
					stringBuilder.AppendLine ($"速度：{EruruApi.FormatFileSize (readLength)}/S");
					stringBuilder.AppendLine ($"已读取：{EruruApi.FormatFileSize (current)}");
					stringBuilder.Append ($"总大小：{EruruApi.FormatFileSize (total)}");
					readLength = 0;
					CommandLineWriter.WriteLine (stringBuilder);
				}
				return true;
			});
			stopwatch.Stop ();
			CommandLineWriter.WriteLine ($"读取中央目录成功 耗时：{stopwatch.Elapsed.ToLocalString ()} 文件：{fileName}");
			return stream;
		}

		static ZipArchive Get (HttpCompressionFileStream stream) {
			try {
				return new ZipArchive (stream, ZipArchiveMode.Read);
			} finally {
				stream.Trim ();
			}
		}

		static async Task ListAsync (string url, string searchText, bool isRegex, bool isWebApi) {
			CommandLineWriter.IsWebApi = isWebApi;
			var fileName = Path.GetFileName (new Uri (url).LocalPath);
			var openStopwatch = new Stopwatch ();
			var stream = await OpenAsync (url, openStopwatch);
			CommandLineWriter.WriteLine ($"加载文件列表中 {fileName}");
			var loadStopwatch = Stopwatch.StartNew ();
			var zipArchive = Get (stream);
			loadStopwatch.Stop ();
			var files = zipArchive.Entries.Where (item => !item.FullName.EndsWith ('/')).ToArray ();
			var tempFiles = files.AsEnumerable ();
			if (!string.IsNullOrEmpty (searchText)) {
				if (isRegex) {
					var regex = new Regex (searchText, RegexOptions.Compiled | RegexOptions.IgnoreCase);
					tempFiles = tempFiles.Where (item => regex.IsMatch (item.Name));
				} else {
					tempFiles = tempFiles.Where (item => item.Name.Contains (searchText, StringComparison.OrdinalIgnoreCase));
				}
			}
			var selectedFiles = tempFiles.ToArray ();
			Array.Sort (selectedFiles, (a, b) => a.Length.CompareTo (b.Length) * -1);
			var stringBuilder = new StringBuilder ();
			for (var i = 0; i < selectedFiles.Length; i++) {
				stringBuilder.Clear ();
				stringBuilder.Append ($"{i + 1,-10}大小：{EruruApi.FormatFileSizeFixed (selectedFiles[i].Length),10}");
				stringBuilder.Append ($" 压缩后：{EruruApi.FormatFileSizeFixed (selectedFiles[i].CompressedLength),10}");
				stringBuilder.Append ($" 修改：{selectedFiles[i].LastWriteTime.LocalDateTime.ToLocalDateTimeString ()}");
				stringBuilder.Append ($" 加密：{(selectedFiles[i].IsEncrypted ? "是" : "否")}");
				stringBuilder.Append ($" 路径：{selectedFiles[i].FullName}");
				stringBuilder.Append ($" 注释：{selectedFiles[i].Comment}");
				CommandLineWriter.WriteLine (stringBuilder);
			}
			stringBuilder.Clear ();
			stringBuilder.Append ($"{fileName}");
			stringBuilder.Append ($" 文件数：{EruruApi.FormatValue (files.Length)}");
			stringBuilder.Append ($" 总大小：{EruruApi.FormatFileSize (files.Sum (item => item.Length))}");
			stringBuilder.Append ($" 压缩后：{EruruApi.FormatFileSize (files.Sum (item => item.CompressedLength))}");
			stringBuilder.Append ($" 总耗时：{(loadStopwatch.Elapsed + openStopwatch.Elapsed).ToLocalString ()}");
			stringBuilder.Append ($" 所耗流量：{EruruApi.FormatFileSize (stream.TotalReadLength)}");
			CommandLineWriter.WriteLine (stringBuilder);
			CommandLineWriter.SetData (() => selectedFiles.Select (item => new JObject () {
				{ "length", item.Length },
				{ "compressedLength", item.CompressedLength },
				{ "lastWriteTime", item.LastWriteTime },
				{ "isEncrypted", item.IsEncrypted },
				{ "fullName", item.FullName },
				{ "comment", item.Comment }
			}));
		}

		static async Task DownloadAsync (string url, string path, string outputPath, bool isWebApi) {
			CommandLineWriter.IsWebApi = isWebApi;
			var fileName = Path.GetFileName (new Uri (url).LocalPath);
			var openStopwatch = new Stopwatch ();
			var stream = await OpenAsync (url, openStopwatch);
			CommandLineWriter.WriteLine ($"加载文件列表中 {fileName}");
			var loadStopwatch = Stopwatch.StartNew ();
			var zipArchive = Get (stream);
			loadStopwatch.Stop ();
			var files = zipArchive.Entries.Where (item => !item.FullName.EndsWith ('/')).ToArray ();
			var file = files.Where (item => item.FullName == path).FirstOrDefault ();
			if (file == null) {
				throw new FileNotFoundException ($"未找到文件 {path}");
			}
			stream.TotalReadLength = 0;
			var totalLength = file.Length;
			var totalCompressedLength = file.CompressedLength;
			CommandLineWriter.WriteLine ($"下载文件中 文件：{file.FullName}");
			if (!zipArchive.TryGetEntryFullLength (stream.FileAreaSize, file, out var fileLength)) {
				fileLength = file.CompressedLength + 1024;
			}
			stream.MaxFileSize = fileLength;
			stream.BufferSize = (int)stream.MaxFileSize;
			var stopwatch = Stopwatch.StartNew ();
			var refreshTime = 0L;
			var lastSecondTotalReadLength = 0L;
			var decompressedLengthSecond = 0;
			var downloadProgress = new ProgressInformation ();
			var decompressionProgress = new ProgressInformation ();
			bool OnProgress (bool isDownload) {
				if (isDownload) {
					stream.MaxFileSize = Math.Min (1024 * 1024, stream.MaxFileSize);
				} else {
					decompressedLengthSecond += decompressionProgress.Length;
				}
				if (stopwatch.ElapsedMilliseconds >= refreshTime) {
					refreshTime = stopwatch.ElapsedMilliseconds + 1000;
					var downloadSpeedText = EruruApi.FormatFileSize (stream.TotalReadLength - lastSecondTotalReadLength);
					var decompressedProgressValue = (int)(decompressionProgress.Current.Divide (file.Length) * 100);
					var stringBuilder = new StringBuilder ();
					stringBuilder.AppendLine ($"下载速度：{downloadSpeedText}/S");
					stringBuilder.AppendLine ($"已下载：{EruruApi.FormatFileSize (stream.TotalReadLength)}");
					stringBuilder.AppendLine ($"总大小：{EruruApi.FormatFileSize (file.CompressedLength)}");
					stringBuilder.AppendLine ($"解压速度：{EruruApi.FormatFileSize (decompressedLengthSecond)}/S");
					stringBuilder.AppendLine ($"已解压：{EruruApi.FormatFileSize (decompressionProgress.Current)}");
					stringBuilder.Append ($"总大小：{EruruApi.FormatFileSize (file.Length)}");
					lastSecondTotalReadLength = stream.TotalReadLength;
					decompressedLengthSecond = 0;
					CommandLineWriter.WriteLine (stringBuilder);
					CommandLineWriter.WriteLine ();
				}
				return true;
			}
			stream.OnProgress = (length, current, total) => {
				downloadProgress.Length = length;
				downloadProgress.Current = current;
				downloadProgress.Total = total;
				return OnProgress (true);
			};
			using var entryStream = file.Open ();
			FileInfo fileInfo;
			if (EruruApi.IsDirectory (outputPath)) {
				fileInfo = new FileInfo (Path.Combine (outputPath, file.Name));
			} else {
				fileInfo = new FileInfo (outputPath);
			}
			fileInfo.Directory.Create ();
			try {
				using var fileStream = fileInfo.Create ();
				await entryStream.CopyToAsync (fileStream, (length, current, total) => {
					decompressionProgress.Length = length;
					decompressionProgress.Current = current;
					decompressionProgress.Total = total;
					return OnProgress (false);
				});
			} catch {
				fileInfo.Delete ();
				throw;
			}
			var stringBuilder = new StringBuilder ();
			stringBuilder.AppendLine ("下载文件完成");
			stringBuilder.AppendLine ($"共消耗流量：{EruruApi.FormatFileSize (stream.TotalReadLength)}");
			stringBuilder.AppendLine ($"总大小：{EruruApi.FormatFileSize (totalCompressedLength)}");
			stringBuilder.AppendLine ($"解压后总大小：{EruruApi.FormatFileSize (totalLength)}");
			stringBuilder.Append ($"已保存到：{fileInfo}");
			CommandLineWriter.WriteLine (stringBuilder);
		}

	}

}