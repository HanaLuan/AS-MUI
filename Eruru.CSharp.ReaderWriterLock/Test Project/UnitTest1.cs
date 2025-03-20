using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Eruru.CSharp.ReaderWriterLock;

namespace TestProject {

	[TestClass]
	public class UnitTest1 {

		[TestMethod]
		public async Task TestMethod1 () {
			var readerWriterLock = new ReaderWriterLock ();
			var counter = 0;
			var categoryCount = 4;
			var tasks = new Task[categoryCount * 100];
			var count = 20000;
			var stopWatch = Stopwatch.StartNew ();
			var refreshTime = 100L;
			for (var i = 0; i < tasks.Length; i++) {
				tasks[i] = Task.Factory.StartNew (state => {
					var id = (int)state % categoryCount;
					for (var n = 0; n < count; n++) {
						switch (id) {
							case 0:
								readerWriterLock.TryRead (() => {
									readerWriterLock.Read (() => {
										using (readerWriterLock.Read ()) {
											return counter;
										}
									});
								});
								break;
							case 1:
								readerWriterLock.TryWrite (() => {
									readerWriterLock.Write (() => {
										using (readerWriterLock.Write ()) {
											counter++;
										}
									});
								});
								break;
							case 2:
								readerWriterLock.TryUpgradeableRead (() => {
									readerWriterLock.UpgradeableRead (() => {
										using (readerWriterLock.UpgradeableRead ()) {
											return counter;
										}
									});
								});
								break;
							case 3:
								readerWriterLock.TryUpgradeableRead (() => {
									readerWriterLock.Write (() => {
										using (readerWriterLock.Write ()) {
											counter--;
										}
									});
								});
								break;
						}
						if (refreshTime < stopWatch.ElapsedMilliseconds) {
							refreshTime = stopWatch.ElapsedMilliseconds + 100;
							Console.WriteLine (counter);
						}
					}
				}, i);
			}
			await Task.WhenAll (tasks);
			Console.WriteLine (counter);
			Assert.AreEqual (0, counter);
		}

	}

}