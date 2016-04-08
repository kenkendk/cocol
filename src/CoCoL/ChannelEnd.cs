using System;
using System.Threading.Tasks;

namespace CoCoL
{
	public abstract class ChannelEndBase : IJoinAbleChannelEnd, IDisposable
	{
		/// <summary>
		/// The read channel target
		/// </summary>
		protected IRetireAbleChannel m_target;

		/// <summary>
		/// <c>1</c> if leave has been called, <c>0</c> otherwise
		/// </summary>
		private int m_hasLeft = 1;

		/// <summary>
		/// <c>true</c> if the request is a reader, <c>false</c> otherwise
		/// </summary>
		private readonly bool m_isReader = false;

		/// <summary>
		/// Initializes a new instance of the <see cref="CoCoL.ChannelReadEnd`1"/> class.
		/// </summary>
		public ChannelEndBase(IRetireAbleChannel target, bool isReader)
		{
			if (target == null)
				throw new ArgumentNullException("target");
			m_isReader = isReader;
			m_target = target;
			Join();
		}

		/// <summary>
		/// Releases unmanaged resources and performs other cleanup operations before the <see cref="CoCoL.ChannelEndBase"/>
		/// is reclaimed by garbage collection.
		/// </summary>
		~ChannelEndBase()
		{
			Dispose(false);
		}

		/// <summary>
		/// Releases all resource used by the <see cref="CoCoL.ChannelReadEnd`1"/> object.
		/// </summary>
		/// <param name="disposing"><c>true</c> if disposing, <c>false</c> otherwise</param>
		/// <remarks>Call <see cref="Dispose"/> when you are finished using the <see cref="CoCoL.ChannelReadEnd`1"/>. The
		/// <see cref="Dispose"/> method leaves the <see cref="CoCoL.ChannelReadEnd`1"/> in an unusable state. After calling
		/// <see cref="Dispose"/>, you must release all references to the <see cref="CoCoL.ChannelReadEnd`1"/> so the garbage
		/// collector can reclaim the memory that the <see cref="CoCoL.ChannelReadEnd`1"/> was occupying.</remarks>
		public void Dispose(bool disposing)
		{
			if (!disposing)
				GC.SuppressFinalize(this);
			
			if (m_target != null)
				Leave();
			m_target = null;
		}


		#region IRetireAbleChannel implementation
		/// <summary>
		/// Stops this channel from processing messages
		/// </summary>
		public void Retire()
		{
			if (m_target == null)
				throw new ObjectDisposedException(this.GetType().FullName);
			m_target.Retire();
		}
		/// <summary>
		/// Stops this channel from processing messages
		/// </summary>
		/// <param name="immediate">Retires the channel without processing the queue, which may cause lost messages</param>
		public void Retire(bool immediate)
		{
			if (m_target == null)
				throw new ObjectDisposedException(this.GetType().FullName);
			m_target.Retire(immediate);
		}
		/// <summary>
		/// Gets a value indicating whether this instance is retired.
		/// </summary>
		/// <value><c>true</c> if this instance is retired; otherwise, <c>false</c>.</value>
		public bool IsRetired 
		{ 
			get 
			{ 
				if (m_target == null)
					throw new ObjectDisposedException(this.GetType().FullName);
				return m_target.IsRetired; 
			} 
		}
		#endregion

		#region IJoinAbleChannelEnd implementation
		/// <summary>
		/// Join the channel
		/// </summary>
		public void Join()
		{
			if (m_target == null)
				throw new ObjectDisposedException(this.GetType().FullName);

			if (m_target is IJoinAbleChannel && !m_target.IsRetired)
			{
				if (System.Threading.Interlocked.Exchange(ref m_hasLeft, 0) == 1)
					((IJoinAbleChannel)m_target).Join(m_isReader);
			}
		}

		/// <summary>
		/// Leave the channel.
		/// </summary>
		public void Leave()
		{
			if (m_target == null)
				throw new ObjectDisposedException(this.GetType().FullName);

			if (m_target is IJoinAbleChannel && !m_target.IsRetired)
			{
				if (System.Threading.Interlocked.Exchange(ref m_hasLeft, 1) == 0)
					((IJoinAbleChannel)m_target).Leave(m_isReader);
			}
		}
		#endregion

		#region IDisposable implementation
		/// <summary>
		/// Releases all resource used by the <see cref="CoCoL.ChannelReadEnd`1"/> object.
		/// </summary>
		/// <remarks>Call <see cref="Dispose"/> when you are finished using the <see cref="CoCoL.ChannelReadEnd`1"/>. The
		/// <see cref="Dispose"/> method leaves the <see cref="CoCoL.ChannelReadEnd`1"/> in an unusable state. After calling
		/// <see cref="Dispose"/>, you must release all references to the <see cref="CoCoL.ChannelReadEnd`1"/> so the garbage
		/// collector can reclaim the memory that the <see cref="CoCoL.ChannelReadEnd`1"/> was occupying.</remarks>
		public void Dispose()
		{
			Dispose(true);
		}
		#endregion
	}

	/// <summary>
	/// Wrapper class that prevents write access to a channel
	/// </summary>
	public class ChannelReadEnd<T> : ChannelEndBase, IReadChannelEnd<T>
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="CoCoL.ChannelReadEnd`1"/> class.
		/// </summary>
		public ChannelReadEnd(IReadChannel<T> target)
			: base(target, true)
		{
		}

		#region IReadChannel implementation
		/// <summary>
		/// Registers a desire to read from the channel
		/// </summary>
		/// <param name="offer">A callback method for offering an item, use null to unconditionally accept</param>
		/// <param name="callback">A callback method that is called with the result of the operation</param>
		/// <param name="timeout">The time to wait for the operation, use zero to return a timeout immediately if no items can be read. Use a
		/// negative span to wait forever.</param>
		/// <returns>The async.</returns>
		public Task<T> ReadAsync(TimeSpan timeout, ITwoPhaseOffer offer = null)
		{
			if (m_target == null)
				throw new ObjectDisposedException(this.GetType().FullName);
			return ((IReadChannel<T>)m_target).ReadAsync(timeout, offer);
		}
		#endregion
	}


	/// <summary>
	/// Wrapper class that prevents write access to a channel
	/// </summary>
	public class ChannelWriteEnd<T> : ChannelEndBase, IWriteChannelEnd<T>
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="CoCoL.ChannelReadEnd`1"/> class.
		/// </summary>
		public ChannelWriteEnd(IWriteChannel<T> target)
			: base(target, false)
		{
		}

		#region IWriteChannel implementation

		/// <summary>
		/// Registers a desire to write to the channel
		/// </summary>
		/// <param name="offer">A callback method for offering an item, use null to unconditionally accept</param>
		/// <param name="value">The value to write to the channel.</param>
		/// <param name="timeout">The time to wait for the operation, use zero to return a timeout immediately if no items can be read. Use a
		/// negative span to wait forever.</param>
		/// <returns>The async.</returns>
		public Task WriteAsync(T value, TimeSpan timeout, ITwoPhaseOffer offer = null)
		{
			return ((IWriteChannel<T>)m_target).WriteAsync(value, timeout, offer);
		}

		#endregion
	}

}

