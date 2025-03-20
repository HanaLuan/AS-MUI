using System;

namespace Eruru.CSharp.ReaderWriterLock {

	internal struct ReaderWriteLockUpgradeableReadLock : IDisposable {

		readonly ReaderWriterLock ReaderWriterLock;

		public ReaderWriteLockUpgradeableReadLock (ReaderWriterLock readerWriterLock, int timeoutMilliseconds) {
			ReaderWriterLock = readerWriterLock;
			ReaderWriterLock.EnterUpgradeableReadLock (timeoutMilliseconds);
		}
		public ReaderWriteLockUpgradeableReadLock (ReaderWriterLock readerWriterLock) : this (readerWriterLock, readerWriterLock.TimeoutMilliseconds) {

		}

		public void Dispose () {
			ReaderWriterLock.ExitUpgradeableReadLock ();
			GC.SuppressFinalize (this);
		}

	}

}