using System;

namespace Eruru.CSharp.ReaderWriterLock {

	internal struct ReaderWriteLockWriteLock : IDisposable {

		readonly ReaderWriterLock ReaderWriterLock;

		public ReaderWriteLockWriteLock (ReaderWriterLock readerWriterLock, int timeoutMilliseconds) {
			ReaderWriterLock = readerWriterLock;
			ReaderWriterLock.EnterWriteLock (timeoutMilliseconds);
		}
		public ReaderWriteLockWriteLock (ReaderWriterLock readerWriterLock) : this (readerWriterLock, readerWriterLock.TimeoutMilliseconds) {

		}

		public void Dispose () {
			ReaderWriterLock.ExitWriteLock ();
			GC.SuppressFinalize (this);
		}

	}

}