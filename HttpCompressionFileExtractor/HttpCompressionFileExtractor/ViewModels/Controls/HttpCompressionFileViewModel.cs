using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reactive;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Platform.Storage;
using Eruru.CSharp.Api;
using Eruru.CSharp.Api.FluentAvalonia;
using FluentAvalonia.UI.Controls;
using HttpCompressionFileExtractor.Core;
using Microsoft.Extensions.DependencyInjection;
using ReactiveUI;

namespace HttpCompressionFileExtractor {

	public partial class HttpCompressionFileViewModel : ViewModelBase, IDisposable {

		public HttpCompressionFileStream Stream { get => _Stream; set => this.RaiseAndSetIfChanged (ref _Stream, value); }
		public ZipArchive ZipArchive { get => _ZipArchive; set => this.RaiseAndSetIfChanged (ref _ZipArchive, value); }
		public CancellationTokenSource CancellationTokenSource { get; set; } = new ();
		public string FileName { get => _FileName; set => this.RaiseAndSetIfChanged (ref _FileName, value); }
		public CompressionFileViewModel[] Files { get => _Files; set => this.RaiseAndSetIfChanged (ref _Files, value); }
		public string State { get => _State; set => this.RaiseAndSetIfChanged (ref _State, value); }
		public Stopwatch OpenStopwatch { get; set; } = new ();
		public Stopwatch LoadStopwatch { get; set; } = new ();
		public ReactiveCommand<Unit, Unit> OnExtractCommand { get; set; }
		public ReactiveCommand<Unit, Unit> OnSearchCommand { get; set; }
		public string SearchText { get; set; }
		public bool IsUseRegex { get; set; }
		public ObservableCollection<CompressionFileViewModel> SearchedFiles { get => _SearchedFiles; set => this.RaiseAndSetIfChanged (ref _SearchedFiles, value); }

		readonly ApiService ApiService = App.Current.Services.GetService<ApiService> ();
		readonly FileService FileService = App.Current.Services.GetService<FileService> ();
		HttpCompressionFileStream _Stream;
		ZipArchive _ZipArchive;
		string _FileName = string.Empty;
		string _State = string.Empty;
		CompressionFileViewModel[] _Files = [];
		ObservableCollection<CompressionFileViewModel> _SearchedFiles;

		public HttpCompressionFileViewModel () {
			OnExtractCommand = ReactiveCommand.CreateFromTask (OnExtractAsync);
			OnSearchCommand = ReactiveCommand.CreateFromTask (OnSearchAsync);
		}

		public void Dispose () {
			CancellationTokenSource.Cancel ();
			CancellationTokenSource.Dispose ();
			ZipArchive?.Dispose ();
			Stream?.Dispose ();
			GC.SuppressFinalize (this);
		}

		public async Task<bool> ConnectAsync (string url) {
			FileName = Path.GetFileName (new Uri (url).LocalPath);
			Stream = new HttpCompressionFileStream (url);
			State = $"请求信息中 文件：{FileName}";
			if (await ApiService.ShowWaitDialogAsync (dialog => {
				dialog.Title = State;
				dialog.SecondaryButtonText = "取消";
			}, async (dialog, token) => {
				OpenStopwatch.Restart ();
				await Stream.HeadAsync (token);
				OpenStopwatch.Stop ();
			}) == ContentDialogResult.Primary) {
				State = $"请求信息成功 耗时：{OpenStopwatch.Elapsed.ToLocalString ()} 文件：{FileName}";
				return true;
			}
			return false;
		}

		public async Task<bool> OpenAsync () {
			State = $"读取中央目录中 文件：{FileName}";
			var viewModel = new ProgressDialogViewModel () { Title = State };
			if (await ApiService.ShowProgressDialogAsync (viewModel, async (dialog, token) => {
				var stopwatch = Stopwatch.StartNew ();
				var refreshTime = 0L;
				var readLength = 0;
				OpenStopwatch.Start ();
				await Stream.CentralDirectoryAsync (token, (length, current, total) => {
					readLength += length;
					if (stopwatch.ElapsedMilliseconds >= refreshTime) {
						refreshTime = stopwatch.ElapsedMilliseconds + 1000;
						var progress = (int)(current.Divide (total) * 100);
						var stringBuilder = new StringBuilder ();
						stringBuilder.AppendLine ($"速度：{EruruApi.FormatFileSize (readLength)}/S");
						stringBuilder.AppendLine ($"已读取：{EruruApi.FormatFileSize (current)}");
						stringBuilder.Append ($"总大小：{EruruApi.FormatFileSize (total)}");
						readLength = 0;
						ApiService.PostUi (() => {
							viewModel.Text = stringBuilder.ToString ();
							viewModel.Value = progress;
						});
					}
					CancellationTokenSource.Token.ThrowIfCancellationRequested ();
					token.ThrowIfCancellationRequested ();
					return true;
				});
				OpenStopwatch.Stop ();
			}) == ContentDialogResult.Primary) {
				State = $"读取中央目录成功 耗时：{OpenStopwatch.Elapsed.ToLocalString ()} 文件：{FileName}";
				return true;
			}
			return false;
		}

		public void Load () {
			State = $"加载文件列表中 文件：{FileName}";
			LoadStopwatch.Restart ();
			ZipArchive = new ZipArchive (Stream, ZipArchiveMode.Read);
			LoadStopwatch.Stop ();
			Files = ZipArchive?.Entries.Where (item => !item.FullName.EndsWith ('/'))
				.Select (item => new CompressionFileViewModel (item)).ToArray ();
			SearchedFiles = [.. Files];
			var stringBuilder = new StringBuilder ();
			stringBuilder.Append ($"{FileName}");
			stringBuilder.Append ($" 文件数：{EruruApi.FormatValue (Files.Length)}");
			stringBuilder.Append ($" 总大小：{EruruApi.FormatFileSize (Files.Sum (item => item.Length))}");
			stringBuilder.Append ($" 压缩后：{EruruApi.FormatFileSize (Files.Sum (item => item.CompressedLength))}");
			stringBuilder.Append ($" 总耗时：{(LoadStopwatch.Elapsed + OpenStopwatch.Elapsed).ToLocalString ()}");
			stringBuilder.Append ($" 所耗流量：{EruruApi.FormatFileSize (Stream.TotalReadLength)}");
			State = stringBuilder.ToString ();
			Stream.Trim ();
		}

		async Task OnSearchAsync () {
			try {
				if (string.IsNullOrEmpty (SearchText)) {
					SearchedFiles = [.. Files];
					return;
				}
				if (IsUseRegex) {
					var regex = new Regex (SearchText, RegexOptions.IgnoreCase | RegexOptions.Compiled);
					SearchedFiles = [.. Files.Where (item => item.IsSelected || regex.IsMatch (item.FullName))];
				} else {
					SearchedFiles = [.. Files.Where (item => item.IsSelected || item.FullName.Contains (
					SearchText, StringComparison.OrdinalIgnoreCase
				))];
				}
			} catch (Exception exception) {
				await ApiService.ShowExceptionDialogAsync (exception);
			}
		}

		async Task OnExtractAsync () {
			var doneFiles = new List<CompressionFileViewModel> ();
			try {
				var files = Files.Where (item => item.IsSelected).ToArray ();
				if (files.Length == 0) {
					await ApiService.ShowMessageDialogAsync ("请勾选需要提取的文件");
					return;
				}
				var totalLength = files.Sum (item => item.Length);
				var totalCompressedLength = files.Sum (item => item.CompressedLength);
				var outputPath = string.Empty;
				Stream.TotalReadLength = 0;
				if (files.Length == 1) {
					using var file = await FileService.ShowSaveFileDialogAsync (options => options.SuggestedFileName = files[0].Name);
					if (file == null) {
						return;
					}
					outputPath = file.Path.ToString ();
					await OnExtractAsync (files[0], file, OnDownloadedAsync, doneFiles);
					return;
				}
				using var folder = await FileService.ShowOpenFolderDialogAsync (options => options.Title = $"准备下载{files.Length}个文件");
				if (folder == null) {
					return;
				}
				outputPath = folder.Path.ToString ();
				await OnExtractAsync (files, folder, OnDownloadedAsync, totalLength, totalCompressedLength, doneFiles);
				async Task OnDownloadedAsync () {
					var stringBuilder = new StringBuilder ();
					stringBuilder.AppendLine ("下载文件完成");
					stringBuilder.AppendLine ($"共消耗流量：{EruruApi.FormatFileSize (Stream.TotalReadLength)}");
					stringBuilder.AppendLine ($"总大小：{EruruApi.FormatFileSize (totalCompressedLength)}");
					stringBuilder.AppendLine ($"解压后总大小：{EruruApi.FormatFileSize (totalLength)}");
					stringBuilder.Append ($"已保存到：{outputPath}");
					await ApiService.ShowMessageDialogAsync (stringBuilder);
				}
			} catch (Exception exception) {
				await ApiService.ShowExceptionDialogAsync (exception);
			} finally {
				foreach (var file in doneFiles) {
					file.IsSelected = false;
				}
				Stream.Trim ();
			}
		}

		async Task<bool> OnExtractAsync (
			CompressionFileViewModel file, IStorageFile targetFile, Func<Task> onDone, List<CompressionFileViewModel> doneFiles
		) {
			var viewModel = new ProgressDialogViewModel () { Title = $"下载文件中 文件：{file.FullName}" };
			if (await ApiService.ShowProgressDialogAsync (viewModel, async (dialog, token) => {
				if (!ZipArchive.TryGetEntryFullLength (Stream.FileAreaSize, file.Entry, out var fileLength)) {
					fileLength = file.CompressedLength + 1024;
				}
				Stream.MaxFileSize = fileLength;
				Stream.BufferSize = (int)Stream.MaxFileSize;
				var stopwatch = Stopwatch.StartNew ();
				var refreshTime = 0L;
				var lastSecondTotalReadLength = 0L;
				var decompressedLengthSecond = 0;
				var downloadProgress = new ProgressInformation ();
				var decompressionProgress = new ProgressInformation ();
				bool OnProgress (bool isDownload) {
					if (isDownload) {
						Stream.MaxFileSize = Math.Min (1024 * 1024, Stream.MaxFileSize);
					} else {
						decompressedLengthSecond += decompressionProgress.Length;
					}
					if (stopwatch.ElapsedMilliseconds >= refreshTime) {
						refreshTime = stopwatch.ElapsedMilliseconds + 1000;
						var downloadSpeedText = EruruApi.FormatFileSize (Stream.TotalReadLength - lastSecondTotalReadLength);
						var decompressedProgressValue = (int)(decompressionProgress.Current.Divide (file.Length) * 100);
						var stringBuilder = new StringBuilder ();
						stringBuilder.AppendLine ($"下载速度：{downloadSpeedText}/S");
						stringBuilder.AppendLine ($"已下载：{EruruApi.FormatFileSize (Stream.TotalReadLength)}");
						stringBuilder.AppendLine ($"总大小：{EruruApi.FormatFileSize (file.CompressedLength)}");
						stringBuilder.AppendLine ($"解压速度：{EruruApi.FormatFileSize (decompressedLengthSecond)}/S");
						stringBuilder.AppendLine ($"已解压：{EruruApi.FormatFileSize (decompressionProgress.Current)}");
						stringBuilder.Append ($"总大小：{EruruApi.FormatFileSize (file.Length)}");
						lastSecondTotalReadLength = Stream.TotalReadLength;
						decompressedLengthSecond = 0;
						ApiService.PostUi (() => {
							viewModel.Value = decompressedProgressValue;
							viewModel.Text = stringBuilder.ToString ();
						});
					}
					CancellationTokenSource.Token.ThrowIfCancellationRequested ();
					token.ThrowIfCancellationRequested ();
					return true;
				}
				Stream.OnProgress = (length, current, total) => {
					downloadProgress.Length = length;
					downloadProgress.Current = current;
					downloadProgress.Total = total;
					return OnProgress (true);
				};
				using var stream = file.Entry.Open ();
				try {
					using var fileStream = App.Current.OnCreateFileStream (targetFile.Path);
					await stream.CopyToAsync (fileStream, (length, current, total) => {
						decompressionProgress.Length = length;
						decompressionProgress.Current = current;
						decompressionProgress.Total = total;
						return OnProgress (false);
					});
					doneFiles.Add (file);
				} catch {
					File.Delete (targetFile.Path.ToString ());
					throw;
				}
			}) == ContentDialogResult.Primary) {
				await onDone ();
				return true;
			}
			return false;
		}

		async Task<bool> OnExtractAsync (
			CompressionFileViewModel[] files, IStorageFolder folder, Func<Task> onDone,
			long totalLength, long totalCompressionLength, List<CompressionFileViewModel> doneFiles
		) {
			var viewModel = new ProgressDialogViewModel () { Title = $"下载{files.Length}个文件中" };
			if (await ApiService.ShowProgressDialogAsync (viewModel, async (dialog, token) => {
				var directoryInfo = new DirectoryInfo (folder.Path.LocalPath);
				var stopwatch = Stopwatch.StartNew ();
				var refreshTime = 0L;
				var lastSecondTotalReadLength = 0L;
				var decompressedLengthSecond = 0;
				var decompressedLength = 0L;
				var downloadProgress = new ProgressInformation ();
				var decompressionProgress = new ProgressInformation ();
				var i = 0;
				var entries = files.Select (item => item.Entry).ToArray ();
				foreach (var file in files) {
					var fileInfo = new FileInfo (Path.Combine (directoryInfo.FullName, file.FullName));
					if (!ZipArchive.TryGetEntriesFullLength (Stream.FileAreaSize, entries, i, out var fileLength)) {
						fileLength = file.CompressedLength + 1024;
					}
					Stream.MaxFileSize = fileLength;
					Stream.BufferSize = (int)Stream.MaxFileSize;
					bool OnProgress (bool isDownload) {
						if (isDownload) {
							Stream.MaxFileSize = Math.Min (1024 * 1024, Stream.MaxFileSize);
						} else {
							decompressedLengthSecond += decompressionProgress.Length;
							decompressedLength += decompressionProgress.Length;
						}
						if (stopwatch.ElapsedMilliseconds >= refreshTime) {
							refreshTime = stopwatch.ElapsedMilliseconds + 1000;
							var downloadSpeedText = EruruApi.FormatFileSize (Stream.TotalReadLength - lastSecondTotalReadLength);
							var decompressedProgressValue = (int)(decompressedLength.Divide (totalLength) * 100);
							var stringBuilder = new StringBuilder ();
							stringBuilder.AppendLine ($"当前：{i + 1}/{files.Length}");
							stringBuilder.AppendLine ($"文件：{file.FullName}");
							stringBuilder.AppendLine ($"下载速度：{downloadSpeedText}/S");
							stringBuilder.AppendLine ($"已下载：{EruruApi.FormatFileSize (Stream.TotalReadLength)}");
							stringBuilder.AppendLine ($"总大小：{EruruApi.FormatFileSize (totalCompressionLength)}");
							stringBuilder.AppendLine ($"解压速度：{EruruApi.FormatFileSize (decompressedLengthSecond)}/S");
							stringBuilder.AppendLine ($"已解压：{EruruApi.FormatFileSize (decompressedLength)}");
							stringBuilder.AppendLine ($"总大小：{EruruApi.FormatFileSize (totalLength)}");
							lastSecondTotalReadLength = Stream.TotalReadLength;
							decompressedLengthSecond = 0;
							ApiService.PostUi (() => {
								viewModel.Value = decompressedProgressValue;
								viewModel.Text = stringBuilder.ToString ();
							});
						}
						CancellationTokenSource.Token.ThrowIfCancellationRequested ();
						token.ThrowIfCancellationRequested ();
						return true;
					}
					Stream.OnProgress = (length, current, total) => {
						decompressionProgress.Length = length;
						decompressionProgress.Current = current;
						decompressionProgress.Total = total;
						return OnProgress (true);
					};
					using var stream = file.Entry.Open ();
					fileInfo.Directory.Create ();
					try {
						using var fileStream = fileInfo.Create ();
						await stream.CopyToAsync (fileStream, (length, current, total) => {
							downloadProgress.Length = length;
							downloadProgress.Current = current;
							downloadProgress.Total = total;
							return OnProgress (false);
						});
						doneFiles.Add (file);
						i++;
					} catch {
						fileInfo.Delete ();
						throw;
					}
				}
			}) == ContentDialogResult.Primary) {
				await onDone ();
				return true;
			}
			return false;
		}

	}

}