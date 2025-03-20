using System;
using System.Threading;

namespace Eruru.CSharp.ReaderWriterLock {

	public class ReaderWriterLock : IDisposable {

		public int TimeoutMilliseconds { get; set; }

		readonly ReaderWriterLockSlim RWLock;

		public ReaderWriterLock (int timeoutMilliseconds = 60 * 1000, LockRecursionPolicy policy = LockRecursionPolicy.SupportsRecursion) {
			TimeoutMilliseconds = timeoutMilliseconds;
			RWLock = new ReaderWriterLockSlim (policy);
		}

		~ReaderWriterLock () {
			Dispose ();
		}

		public void Dispose () {
			RWLock.Dispose ();
			GC.SuppressFinalize (this);
		}

		public bool TryEnterReadLock (int timeoutMilliseconds) {
			return RWLock.TryEnterReadLock (timeoutMilliseconds);
		}
		public bool TryEnterReadLock () {
			return TryEnterReadLock (TimeoutMilliseconds);
		}

		public bool TryEnterWriteLock (int timeoutMilliseconds) {
			return RWLock.TryEnterWriteLock (timeoutMilliseconds);
		}
		public bool TryEnterWriteLock () {
			return TryEnterWriteLock (TimeoutMilliseconds);
		}

		public bool TryEnterUpgradeableReadLock (int timeoutMilliseconds) {
			return RWLock.TryEnterUpgradeableReadLock (timeoutMilliseconds);
		}
		public bool TryEnterUpgradeableReadLock () {
			return TryEnterUpgradeableReadLock (TimeoutMilliseconds);
		}

		public void EnterReadLock (int timeoutMilliseconds) {
			if (!TryEnterReadLock (timeoutMilliseconds)) {
				throw new TimeoutException ("获取读锁超时");
			}
		}
		public void EnterReadLock () {
			EnterReadLock (TimeoutMilliseconds);
		}

		public void EnterWriteLock (int timeoutMilliseconds) {
			if (!TryEnterWriteLock (timeoutMilliseconds)) {
				throw new TimeoutException ("获取写锁超时");
			}
		}
		public void EnterWriteLock () {
			EnterWriteLock (TimeoutMilliseconds);
		}

		public void EnterUpgradeableReadLock (int timeoutMilliseconds) {
			if (!TryEnterUpgradeableReadLock (timeoutMilliseconds)) {
				throw new TimeoutException ("获取可升级锁超时");
			}
		}
		public void EnterUpgradeableReadLock () {
			EnterUpgradeableReadLock (TimeoutMilliseconds);
		}

		public void ExitReadLock () {
			RWLock.ExitReadLock ();
		}

		public void ExitWriteLock () {
			RWLock.ExitWriteLock ();
		}

		public void ExitUpgradeableReadLock () {
			RWLock.ExitUpgradeableReadLock ();
		}

		public bool TryRead (Action action, int timeoutMilliseconds) {
			if (!TryEnterReadLock (timeoutMilliseconds)) {
				return false;
			}
			try {
				action ();
				return true;
			} finally {
				ExitReadLock ();
			}
		}
		public bool TryRead (Action action) {
			return TryRead (action, TimeoutMilliseconds);
		}
		public bool TryRead<T> (Func<T> action, out T result, int timeoutMilliseconds) {
			if (!TryEnterReadLock (timeoutMilliseconds)) {
				result = default (T);
				return false;
			}
			try {
				result = action ();
				return true;
			} finally {
				ExitReadLock ();
			}
		}
		public bool TryRead<T> (Func<T> action, out T result) {
			return TryRead (action, out result, TimeoutMilliseconds);
		}

		public bool TryWrite (Action action, int timeoutMilliseconds) {
			if (!TryEnterWriteLock (timeoutMilliseconds)) {
				return false;
			}
			try {
				action ();
				return true;
			} finally {
				ExitWriteLock ();
			}
		}
		public bool TryWrite (Action action) {
			return TryWrite (action, TimeoutMilliseconds);
		}
		public bool TryWrite<T> (Func<T> action, out T result, int timeoutMilliseconds) {
			if (!TryEnterWriteLock (timeoutMilliseconds)) {
				result = default (T);
				return false;
			}
			try {
				result = action ();
				return true;
			} finally {
				ExitWriteLock ();
			}
		}
		public bool TryWrite<T> (Func<T> action, out T result) {
			return TryWrite (action, out result, TimeoutMilliseconds);
		}

		public bool TryUpgradeableRead (Action action, int timeoutMilliseconds) {
			if (!TryEnterUpgradeableReadLock (timeoutMilliseconds)) {
				return false;
			}
			try {
				action ();
				return true;
			} finally {
				ExitUpgradeableReadLock ();
			}
		}
		public bool TryUpgradeableRead (Action action) {
			return TryUpgradeableRead (action, TimeoutMilliseconds);
		}
		public bool TryUpgradeableRead<T> (Func<T> action, out T result, int timeoutMilliseconds) {
			if (!TryEnterUpgradeableReadLock (timeoutMilliseconds)) {
				result = default (T);
				return false;
			}
			try {
				result = action ();
				return true;
			} finally {
				ExitUpgradeableReadLock ();
			}
		}
		public bool TryUpgradeableRead<T> (Func<T> action, out T result) {
			return TryUpgradeableRead (action, out result, TimeoutMilliseconds);
		}

		public void Read (Action action, int timeoutMilliseconds) {
			if (!TryRead (action, timeoutMilliseconds)) {
				throw new TimeoutException ("获取读锁超时");
			}
		}
		public void Read (Action action) {
			Read (action, TimeoutMilliseconds);
		}
		public T Read<T> (Func<T> action, int timeoutMilliseconds) {
			if (!TryRead (action, out var result, timeoutMilliseconds)) {
				throw new TimeoutException ("获取读锁超时");
			}
			return result;
		}
		public T Read<T> (Func<T> action) {
			return Read (action, TimeoutMilliseconds);
		}
		public IDisposable Read (int timeoutMilliseconds) {
			return new ReaderWriteLockReadLock (this, timeoutMilliseconds);
		}
		public IDisposable Read () {
			return Read (TimeoutMilliseconds);
		}

		public void Write (Action action, int timeoutMilliseconds) {
			if (!TryWrite (action, timeoutMilliseconds)) {
				throw new TimeoutException ("获取写锁超时");
			}
		}
		public void Write (Action action) {
			Write (action, TimeoutMilliseconds);
		}
		public T Write<T> (Func<T> action, int timeoutMilliseconds) {
			if (!TryWrite (action, out var result, timeoutMilliseconds)) {
				throw new TimeoutException ("获取写锁超时");
			}
			return result;
		}
		public T Write<T> (Func<T> action) {
			return Write (action, TimeoutMilliseconds);
		}
		public IDisposable Write (int timeoutMilliseconds) {
			return new ReaderWriteLockWriteLock (this, timeoutMilliseconds);
		}
		public IDisposable Write () {
			return Write (TimeoutMilliseconds);
		}

		public void UpgradeableRead (Action action, int timeoutMilliseconds) {
			if (!TryUpgradeableRead (action, timeoutMilliseconds)) {
				throw new TimeoutException ("获取可升级锁超时");
			}
		}
		public void UpgradeableRead (Action action) {
			UpgradeableRead (action, TimeoutMilliseconds);
		}
		public T UpgradeableRead<T> (Func<T> action, int timeoutMilliseconds) {
			if (!TryUpgradeableRead (action, out var result, timeoutMilliseconds)) {
				throw new TimeoutException ("获取可升级锁超时");
			}
			return result;
		}
		public T UpgradeableRead<T> (Func<T> action) {
			return UpgradeableRead (action, TimeoutMilliseconds);
		}
		public IDisposable UpgradeableRead (int timeoutMilliseconds) {
			return new ReaderWriteLockUpgradeableReadLock (this, timeoutMilliseconds);
		}
		public IDisposable UpgradeableRead () {
			return UpgradeableRead (TimeoutMilliseconds);
		}

	}

}