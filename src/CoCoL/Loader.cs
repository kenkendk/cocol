using System;
using System.Reflection;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CoCoL
{
	/// <summary>
	/// Helper class for loading statically denfined processes
	/// </summary>
	public static class Loader
	{
		/// <summary>
		/// Finds all classes marked as Process and launches them
		/// </summary>
		/// <param name="asm">The assemblies to examine.</param>
		public static void StartFromAssembly(params Assembly[] asm)
		{
			StartFromAssembly(asm.AsEnumerable());
		}

		/// <summary>
		/// Finds all classes marked as Process and launches them
		/// </summary>
		/// <param name="asm">The assemblies to examine.</param>
		public static void StartFromAssembly(IEnumerable<Assembly> asm)
		{
			var c = (from a in asm
			         select StartFromTypes(a.GetTypes())).Sum();

			if (c == 0)
				throw new Exception("No process found in given assemblies");
		}

		/// <summary>
		/// Helper iterator to repeatedly call a function, like Enumerator.Range, but for Int64
		/// </summary>
		/// <param name="count">The number of repetitions to perform</param>
		/// <param name="op">The method to call, which receives the index</param>
		/// <typeparam name="T">The return type parameter.</typeparam>
		public static IEnumerable<T> Each<T>(long count, Func<long, T> op)
		{
			for (var i = 0L; i < count; i++)
				yield return op(i);
		}

		/// <summary>
		/// Starts all process found in the given types
		/// </summary>
		/// <returns>The number of processes started</returns>
		/// <param name="types">The types to examine</param>
		public static int StartFromTypes(params Type[] types)
		{
			return StartFromTypes(types.AsEnumerable());
		}

		/// <summary>
		/// Starts all process found in the given types
		/// </summary>
		/// <returns>The number of processes started</returns>
		/// <param name="types">The types to examine</param>
		public static int StartFromTypes(IEnumerable<Type> types)
		{
			var count = 0;
			foreach (var c in 
				from n in types
				let isRunable = typeof(IProcess).IsAssignableFrom(n)
				let decorator = n.GetCustomAttributes(typeof(ProcessAttribute), true).FirstOrDefault() as ProcessAttribute
				where n.IsClass && isRunable && n.GetConstructor(new Type[0]) != null
				select new { Class = n, Decorator = decorator ?? new ProcessAttribute() })
			{
				if (typeof(IAsyncProcess).IsAssignableFrom(c.Class)) 
					count += StartFromProcesses(Each(c.Decorator.ProcessCount, x => ((IAsyncProcess)Activator.CreateInstance(c.Class))));
				else
					count += StartFromProcesses(Each(c.Decorator.ProcessCount, x => ((IProcess)Activator.CreateInstance(c.Class))));
			}	
				
			return count;
		}

		/// <summary>
		/// Starts processes by scheduling their run method for execution
		/// </summary>
		/// <returns>The number of processes started</returns>
		/// <param name="processes">The list of process instances to start</param>
		public static int StartFromProcesses(IEnumerable<IProcess> processes)
		{
			var count = 0;
			foreach (var p in processes)
			{
				count++;
				ThreadPool.QueueItem(p.Run);
			}

			return count;
		}

		/// <summary>
		/// Starts processes by scheduling their run method for execution
		/// </summary>
		/// <returns>The number of processes started</returns>
		/// <param name="processes">The list of process instances to start</param>
		public static int StartFromProcesses(IEnumerable<IAsyncProcess> processes)
		{
			var count = 0;
			foreach (var p in processes)
			{
				count++;
				ThreadPool.QueueItem(() => { p.RunAsync(); });
			}

			return count;
		}

		/// <summary>
		/// Starts processes by scheduling their run method for execution
		/// </summary>
		/// <returns>The number of processes started</returns>
		/// <param name="processes">The list of process instances to start</param>
		public static Task<int> StartAsync(this IEnumerable<IProcess> processes)
		{
			return System.Threading.Tasks.Task.Run(() => StartFromProcesses(processes));
		}

		/// <summary>
		/// Starts processes by scheduling their run method for execution
		/// </summary>
		/// <returns>The number of processes started</returns>
		/// <param name="processes">The list of process instances to start</param>
		public static Task<int> StartAsync(this IEnumerable<IAsyncProcess> processes)
		{
			return System.Threading.Tasks.Task.Run(() => StartFromProcesses(processes));
		}

		/// <summary>
		/// Starts processes by scheduling their run method for execution
		/// </summary>
		/// <returns>The number of processes started</returns>
		/// <param name="processes">The list of process instances to start</param>
		public static int Start(this IEnumerable<IProcess> processes)
		{
			return StartFromProcesses(processes);
		}

		/// <summary>
		/// Starts processes by scheduling their run method for execution
		/// </summary>
		/// <returns>The number of processes started</returns>
		/// <param name="processes">The list of process instances to start</param>
		public static int Start(this IEnumerable<IAsyncProcess> processes)
		{
			return StartFromProcesses(processes);
		}
	}
}

