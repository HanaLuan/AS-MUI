using System;
using System.IO.Compression;
using ReactiveUI;

namespace HttpCompressionFileExtractor {

	public class CompressionFileViewModel (ZipArchiveEntry entry) : ViewModelBase {

		public ZipArchiveEntry Entry { get; set; } = entry;
		public bool IsSelected { get => _IsSelected; set => this.RaiseAndSetIfChanged (ref _IsSelected, value); }
		public long Length => Entry.Length;
		public long CompressedLength => Entry.CompressedLength;
		public string Comment => Entry.Comment;
		public string FullName => Entry.FullName;
		public bool IsEncrypted => Entry.IsEncrypted;
		public DateTimeOffset LastWriteTime => Entry.LastWriteTime;
		public string Name => Entry.Name;

		bool _IsSelected;

	}

}