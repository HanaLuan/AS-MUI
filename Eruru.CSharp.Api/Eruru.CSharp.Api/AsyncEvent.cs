using System;
using System.Threading.Tasks;

namespace Eruru.CSharp.Api {

	public struct AsyncEvent {

		event Func<Task> Items;

		public async Task InvokeAsync () {
			await Items.InvokeAsync ();
		}

		public static AsyncEvent operator + (AsyncEvent asyncEvent, Func<Task> action) {
			asyncEvent.Items += action;
			return asyncEvent;
		}

		public static AsyncEvent operator - (AsyncEvent asyncEvent, Func<Task> action) {
			asyncEvent.Items -= action;
			return asyncEvent;
		}

	}
	public struct AsyncEvent<T1> {

		event Func<T1, Task> Items;

		public async Task InvokeAsync (T1 arg1) {
			await Items.InvokeAsync (arg1);
		}

		public static AsyncEvent<T1> operator + (AsyncEvent<T1> asyncEvent, Func<T1, Task> action) {
			asyncEvent.Items += action;
			return asyncEvent;
		}

		public static AsyncEvent<T1> operator - (AsyncEvent<T1> asyncEvent, Func<T1, Task> action) {
			asyncEvent.Items -= action;
			return asyncEvent;
		}

	}
	public struct AsyncEvent<T1, T2> {

		event Func<T1, T2, Task> Items;

		public async Task InvokeAsync (T1 arg1, T2 arg2) {
			await Items.InvokeAsync (arg1, arg2);
		}

		public static AsyncEvent<T1, T2> operator + (AsyncEvent<T1, T2> asyncEvent, Func<T1, T2, Task> action) {
			asyncEvent.Items += action;
			return asyncEvent;
		}

		public static AsyncEvent<T1, T2> operator - (AsyncEvent<T1, T2> asyncEvent, Func<T1, T2, Task> action) {
			asyncEvent.Items -= action;
			return asyncEvent;
		}

	}
	public struct AsyncEvent<T1, T2, T3> {

		event Func<T1, T2, T3, Task> Items;

		public async Task InvokeAsync (T1 arg1, T2 arg2, T3 arg3) {
			await Items.InvokeAsync (arg1, arg2, arg3);
		}

		public static AsyncEvent<T1, T2, T3> operator + (AsyncEvent<T1, T2, T3> asyncEvent, Func<T1, T2, T3, Task> action) {
			asyncEvent.Items += action;
			return asyncEvent;
		}

		public static AsyncEvent<T1, T2, T3> operator - (AsyncEvent<T1, T2, T3> asyncEvent, Func<T1, T2, T3, Task> action) {
			asyncEvent.Items -= action;
			return asyncEvent;
		}

	}
	public struct AsyncEvent<T1, T2, T3, T4> {

		event Func<T1, T2, T3, T4, Task> Items;

		public async Task InvokeAsync (T1 arg1, T2 arg2, T3 arg3, T4 arg4) {
			await Items.InvokeAsync (arg1, arg2, arg3, arg4);
		}

		public static AsyncEvent<T1, T2, T3, T4> operator + (AsyncEvent<T1, T2, T3, T4> asyncEvent, Func<T1, T2, T3, T4, Task> action) {
			asyncEvent.Items += action;
			return asyncEvent;
		}

		public static AsyncEvent<T1, T2, T3, T4> operator - (AsyncEvent<T1, T2, T3, T4> asyncEvent, Func<T1, T2, T3, T4, Task> action) {
			asyncEvent.Items -= action;
			return asyncEvent;
		}

	}
	public struct AsyncEvent<T1, T2, T3, T4, T5> {

		event Func<T1, T2, T3, T4, T5, Task> Items;

		public async Task InvokeAsync (T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5) {
			await Items.InvokeAsync (arg1, arg2, arg3, arg4, arg5);
		}

		public static AsyncEvent<T1, T2, T3, T4, T5> operator + (AsyncEvent<T1, T2, T3, T4, T5> asyncEvent, Func<T1, T2, T3, T4, T5, Task> action) {
			asyncEvent.Items += action;
			return asyncEvent;
		}

		public static AsyncEvent<T1, T2, T3, T4, T5> operator - (AsyncEvent<T1, T2, T3, T4, T5> asyncEvent, Func<T1, T2, T3, T4, T5, Task> action) {
			asyncEvent.Items -= action;
			return asyncEvent;
		}

	}
	public struct AsyncEvent<T1, T2, T3, T4, T5, T6> {

		event Func<T1, T2, T3, T4, T5, T6, Task> Items;

		public async Task InvokeAsync (T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6) {
			await Items.InvokeAsync (arg1, arg2, arg3, arg4, arg5, arg6);
		}

		public static AsyncEvent<T1, T2, T3, T4, T5, T6> operator + (AsyncEvent<T1, T2, T3, T4, T5, T6> asyncEvent, Func<T1, T2, T3, T4, T5, T6, Task> action) {
			asyncEvent.Items += action;
			return asyncEvent;
		}

		public static AsyncEvent<T1, T2, T3, T4, T5, T6> operator - (AsyncEvent<T1, T2, T3, T4, T5, T6> asyncEvent, Func<T1, T2, T3, T4, T5, T6, Task> action) {
			asyncEvent.Items -= action;
			return asyncEvent;
		}

	}

}