using System;
using System.Collections.Concurrent;
using System.Reflection;
using System.Threading.Tasks;

namespace Eruru.CSharp.Api {

	public class MethodInvoker {

		public string Name { get; set; }
		public MethodInfo MethodInfo { get; set; }
		public object Instance { get; set; }
		public ParameterInfo[] ParameterInfos { get; set; }
		public bool IsTask { get; set; }
		public bool IsGenericTask { get; set; }
		public Type ReturnType => MethodInfo.ReturnType;

		static readonly ConcurrentDictionary<Type, PropertyInfo> TaskResultProperties = new ConcurrentDictionary<Type, PropertyInfo> ();

		public MethodInvoker () {

		}
		public MethodInvoker (MethodInfo methodInfo, object instance) {
			MethodInfo = methodInfo;
			Name = MethodInfo.GetNameWithoutAsync ();
			Instance = MethodInfo.IsStatic ? null : instance;
			ParameterInfos = methodInfo.GetParameters ();
			IsTask = typeof (Task).IsAssignableFrom (MethodInfo.ReturnType);
			IsGenericTask = IsTask && MethodInfo.ReturnType.GenericTypeArguments.Length > 0;
		}

		public async Task<object> InvokeAsync (params object[] args) {
			var task = (Task)MethodInfo.Invoke (Instance, args);
			await task;
			if (IsGenericTask) {
				return TaskResultProperties.GetOrAdd (MethodInfo.ReturnType, type => type.GetProperty ("Result")).GetValue (task);
			}
			return null;
		}

		public object Invoke (params object[] args) {
			return MethodInfo.Invoke (Instance, args);
		}

	}

}