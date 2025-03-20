using System;

namespace Eruru.CSharp.ReaderWriterLock {

	internal struct ReaderWriteLockReadLock : IDisposable {

		readonly ReaderWriterLock ReaderWriterLock;

		public ReaderWriteLockReadLock (ReaderWriterLock readerWriterLock, int timeoutMilliseconds) {
			ReaderWriterLock = readerWriterLock;
			ReaderWriterLock.EnterReadLock (timeoutMilliseconds);
		}
		public ReaderWriteLockReadLock (ReaderWriterLock readerWriterLock) : this (readerWriterLock, readerWriterLock.TimeoutMilliseconds) {

		}

		public void Dispose () {
			ReaderWriterLock.ExitReadLock ();
			GC.SuppressFinalize (this);
		}

	}

}