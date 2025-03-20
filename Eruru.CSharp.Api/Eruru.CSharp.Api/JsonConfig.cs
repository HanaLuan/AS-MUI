using System;
using System.IO;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Eruru.CSharp.Api {

	public class JsonConfig<T> : IDisposable {

		public virtual Func<JsonSerializerSettings> JsonSerializerSettings { get; set; }
		public string Path { get; set; }
		public T Value { get; set; }
		public Func<T> OnCreate { get; set; }
		public Func<JsonConfig<T>, T, bool> OnCheckNeedPerformOnLoaded { get; set; }
		public Type Type { get; set; }
		public AsyncEvent<JsonConfig<T>> OnPreLoadedAsync;
		public AsyncEvent<JsonConfig<T>> OnLoadedAsync;
		public AsyncEvent<JsonConfig<T>> OnSavedAsync;
		public bool IsLoaded { get; set; }

		readonly ReaderWriterLock.ReaderWriterLock ReaderWriterLock = new ReaderWriterLock.ReaderWriterLock ();

		public JsonConfig (
			string path, Func<JsonSerializerSettings> jsonSerializerSettings, Func<T> onCreate
		) {
			Path = path;
			JsonSerializerSettings = jsonSerializerSettings;
			OnCreate = onCreate;
			Value = OnCreate ();
			Type = Value.GetType ();
		}
		public JsonConfig (string path, Func<T> onCreate) : this (path, JsonConvert.DefaultSettings, onCreate) {

		}

		~JsonConfig () {
			Dispose ();
		}

		public void Dispose () {
			ReaderWriterLock.Dispose ();
			GC.SuppressFinalize (this);
		}

		public async Task LoadAsync () {
			IsLoaded = false;
			var fileInfo = new FileInfo (Path);
			var config = default (T);
			var needSave = false;
			var serializer = JsonSerializer.CreateDefault (JsonSerializerSettings ());
			using (ReaderWriterLock.Write ()) {
				if (fileInfo.Exists) {
					using (var reader = File.OpenText (fileInfo.FullName)) {
						config = (T)serializer.Deserialize (reader, Type);
					}
					if (config == null) {
						throw new FileLoadException ($"配置文件已损坏 {fileInfo.FullName}");
					}
				}
				if (config == null) {
					config = OnCreate ();
					needSave = true;
				}
				Value = config;
			}
			IsLoaded = true;
			if (needSave) {
				await SaveAsync ();
			}
			await OnPreLoadedAsync.InvokeAsync (this);
			await OnLoadedAsync.InvokeAsync (this);
		}

		public async Task SaveAsync () {
			if (!IsLoaded) {
				throw new Exception ("配置文件加载失败，为保护配置文件禁止保存");
			}
			var fileInfo = new FileInfo (Path);
			fileInfo.Directory.Create ();
			var tempFileInfo = new FileInfo (System.IO.Path.Combine (fileInfo.Directory.FullName, $"{fileInfo.Name}.cache"));
			var serializer = JsonSerializer.CreateDefault (JsonSerializerSettings ());
			serializer.Formatting = Formatting.Indented;
			using (ReaderWriterLock.Write ()) {
				using (var writer = tempFileInfo.CreateText ()) {
					serializer.Serialize (writer, Value);
				}
				File.Copy (tempFileInfo.FullName, fileInfo.FullName, true);
			}
			await OnSavedAsync.InvokeAsync (this);
		}

		public IDisposable Read () {
			return ReaderWriterLock.Read ();
		}

		public IDisposable UpgradeableRead () {
			return ReaderWriterLock.UpgradeableRead ();
		}

		public IDisposable Write (bool performOnLoaded = true) {
			return new WriteLock (this, performOnLoaded);
		}

		public bool CheckNeedPerformOnLoaded (T newConfig) {
			return OnCheckNeedPerformOnLoaded == null || OnCheckNeedPerformOnLoaded (this, newConfig);
		}

		public T Copy () {
			var serializer = JsonSerializer.CreateDefault (JsonSerializerSettings ());
			JToken token;
			using (ReaderWriterLock.Read ()) {
				token = JToken.FromObject (Value, serializer);
			}
			return (T)serializer.Deserialize (token.CreateReader (), Type);
		}

		public async Task SetAsync (T value, bool performOnLoaded = true) {
			using (ReaderWriterLock.Write ()) {
				Value = value;
			}
			await SaveAsync ();
			await OnPreLoadedAsync.InvokeAsync (this);
			if (performOnLoaded) {
				await OnLoadedAsync.InvokeAsync (this);
			}
		}

		public struct WriteLock : IDisposable {

			readonly JsonConfig<T> Config;
			readonly bool PerformOnLoaded;

			public WriteLock (JsonConfig<T> config, bool performOnLoaded = true) {
				Config = config;
				PerformOnLoaded = performOnLoaded;
				Config.ReaderWriterLock.EnterWriteLock ();
			}

			public void Dispose () {
				Config.ReaderWriterLock.ExitWriteLock ();
				Config.SaveAsync ().Wait ();
				Config.OnPreLoadedAsync.InvokeAsync (Config).Wait ();
				if (PerformOnLoaded) {
					Config.OnLoadedAsync.InvokeAsync (Config).Wait ();
				}
			}

		}

	}

}