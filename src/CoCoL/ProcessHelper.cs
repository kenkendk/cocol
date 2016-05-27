using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace CoCoL
{
	/// <summary>
	/// Base class for implementing a communicating process as a class.
	/// </summary>
	public abstract class ProcessHelper : IAsyncProcess, IDisposable
	{
		/// <summary>
		/// The method that implements this process
		/// </summary>
		protected abstract Task Start();

		/// <summary>
		/// The task if it has been invoked
		/// </summary>
		protected Task m_started = null;

		/// <summary>
		/// The locking object
		/// </summary>
		protected object m_lock = new object();

		/// <summary>
		/// Initializes a new instance of the <see cref="CoCoL.ProcessHelper"/> class.
		/// </summary>
		protected ProcessHelper()
		{
			AutomationExtensions.AutoWireChannelsDirect(this);
		}

		/// <summary>
		/// The method invoked to run the process blocking
		/// </summary>
		public void Run()
		{
			RunAsync().WaitForTask();
		}

		/// <summary>
		/// Runs the process asynchronously.
		/// </summary>
		/// <returns>The task.</returns>
		public Task RunAsync()
		{
			return SingleRun();
		}

		/// <summary>
		/// Runs the process as a one-shot.
		/// </summary>
		/// <returns>The task.</returns>
		protected Task SingleRun()
		{
			if (m_started != null)
				return m_started;

			lock (m_lock)
			{
				if (m_started == null)
					m_started = AutomationExtensions.RunProtected(this, Start);
			}

			return m_started;
		}

		/// <summary>
		/// Releases all resource used by the <see cref="CoCoL.ProcessHelper"/> object.
		/// </summary>
		/// <remarks>Call <see cref="Dispose"/> when you are finished using the <see cref="CoCoL.ProcessHelper"/>. The
		/// <see cref="Dispose"/> method leaves the <see cref="CoCoL.ProcessHelper"/> in an unusable state. After calling
		/// <see cref="Dispose"/>, you must release all references to the <see cref="CoCoL.ProcessHelper"/> so the garbage
		/// collector can reclaim the memory that the <see cref="CoCoL.ProcessHelper"/> was occupying.</remarks>
		public void Dispose()
		{
			AutomationExtensions.RetireAllChannels(this);
		}

		/// <summary>
		/// Gets the awaiter.
		/// </summary>
		/// <returns>The awaiter.</returns>
		public TaskAwaiter GetAwaiter()
		{
			return SingleRun().GetAwaiter();
		}

	}
}
