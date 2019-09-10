using System;
using System.Threading;
using System.Threading.Tasks;

#if DISABLE_WAITCALLBACK
using WAITCALLBACK = System.Action<object>;
#else
using WAITCALLBACK = System.Threading.WaitCallback;
#endif

namespace CoCoL
{
	/// <summary>
	/// Priorities for selecting a channel when multiple are available
	/// </summary>
	public enum MultiChannelPriority
	{
		/// <summary>
		/// No preference, select any, same as First
		/// </summary>
		Any,
		/// <summary>
		/// Select the first channel in the list that matches
		/// </summary>
		First,
		/// <summary>
		/// Select a random channel
		/// </summary>
		Random,
		/// <summary>
		/// Select the least used channel
		/// </summary>
		Fair
	}

	/// <summary>
	/// The strategies for expiring pending operations on overflow
	/// </summary>
	public enum QueueOverflowStrategy
	{
		/// <summary>
		/// First in, first out.
		/// Expires the oldest entry and inserts a new request as the newest
		/// </summary>
		FIFO,
		/// <summary>
		/// Last in, first out.
		/// Expires most recent entry and inserts a request there instead
		/// </summary>
		LIFO,
		/// <summary>
		/// Keeps the current set of requests and discards the new request
		/// </summary>
		Reject
	}

	/// <summary>
	/// Interface for naming an item
	/// </summary>
	public interface INamedItem
	{
		/// <summary>
		/// Gets the name of the item
		/// </summary>
		/// <value>The name of this item</value>
		string Name { get; }
	}

	/// <summary>
	/// Represents and interface that is retire-able
	/// </summary>
    public interface IRetireAbleChannel : IUntypedChannel
	{
		/// <summary>
		/// Stops this channel from processing messages
		/// </summary>
		/// <returns>An awaitable task</returns>
		Task RetireAsync();

		/// <summary>
		/// Stops this channel from processing messages
		/// </summary>
		/// <param name="immediate">Retires the channel without processing the queue, which may cause lost messages</param>
		/// <returns>An awaitable task</returns>
		Task RetireAsync(bool immediate);

		/// <summary>
		/// Gets a value indicating whether this <see cref="CoCoL.IRetireAbleChannel"/> is retired.
		/// </summary>
		/// <value><c>true</c> if is retired; otherwise, <c>false</c>.</value>
		Task<bool> IsRetiredAsync { get; }
	}

	/// <summary>
	/// Interface for a channel that can be joined
	/// </summary>
	public interface IJoinAbleChannel
	{
		/// <summary>
		/// Join the channel
		/// </summary>
		/// <param name="asReader"><c>true</c> if joining as a reader, <c>false</c> otherwise</param>
		/// <returns>An awaitable task</returns>
		Task JoinAsync(bool asReader);

		/// <summary>
		/// Leave the channel.
		/// </summary>
		/// <param name="asReader"><c>true</c> if leaving as a reader, <c>false</c> otherwise</param>
		/// <returns>An awaitable task</returns>
		Task LeaveAsync(bool asReader);
	}

	/// <summary>
	/// Interface for a channel that can be joined
	/// </summary>
	public interface IJoinAbleChannelEnd : IDisposable
	{
		/// <summary>
		/// Join the channel
		/// </summary>
		/// <returns>An awaitable task</returns>
		Task JoinAsync();

		/// <summary>
		/// Leave the channel.
		/// </summary>
		/// <returns>An awaitable task</returns>
		Task LeaveAsync();
	}
		
	/// <summary>
	/// Interface for the read-end of a channel that supports continuation
	/// </summary>
	public interface IReadChannel<T> : IRetireAbleChannel
	{
        /// <summary>
        /// Registers a desire to read from the channel
        /// </summary>
        Task<T> ReadAsync();

        /// <summary>
        /// Registers a desire to read from the channel
        /// </summary>
        /// <param name="offer">A two-phase offer, use null to unconditionally accept</param>
        Task<T> ReadAsync(ITwoPhaseOffer offer);
	}

	/// <summary>
	/// Interface for the write-end of a channel that supports continuation
	/// </summary>
	public interface IWriteChannel<T> : IRetireAbleChannel
	{
        /// <summary>
        /// Registers a desire to write to the channel
        /// </summary>
        /// <param name="value">The value to write to the channel.</param>
        Task WriteAsync(T value);

        /// <summary>
		/// Registers a desire to write to the channel
		/// </summary>
		/// <param name="offer">A two-phase offer, use null to unconditionally accept</param>
		/// <param name="value">The value to write to the channel.</param>
        Task WriteAsync(T value, ITwoPhaseOffer offer );
	}

	/// <summary>
	/// Interface for the write-end of a joinable channel that supports continuation
	/// </summary>
	public interface IWriteChannelEnd<T> : IWriteChannel<T>, IJoinAbleChannelEnd
	{
	}


	/// <summary>
	/// Interface for the read-end of a joinable channel that supports continuation
	/// </summary>
	public interface IReadChannelEnd<T> : IReadChannel<T>, IJoinAbleChannelEnd
	{
	}

	/// <summary>
	/// Interface for a communication channel the supports continuation
	/// </summary>
	public interface IChannel<T> : IReadChannel<T>, IWriteChannel<T>
	{
	}

	/// <summary>
	/// Marker interface to work with mixed-type channels
	/// </summary>
	public interface IUntypedChannel
	{
	}

	/// <summary>
	/// Interface for a process
	/// </summary>
	public interface IProcess
	{
		/// <summary>
		/// The method invoked to run the process blocking
		/// </summary>
		void Run();
	}

	/// <summary>
	/// Interface for a process that runs asyncronously
	/// </summary>
	public interface IAsyncProcess : IProcess
	{
		/// <summary>
		/// Runs the process asynchronously.
		/// </summary>
		/// <returns>The task.</returns>
		Task RunAsync();
	}

	/// <summary>
	/// A two-phase model where a read or write request is offered,
	/// and either accepted by both or rejected if consensus could not
	/// be reached
	/// </summary>
	public interface ITwoPhaseOffer
	{
		/// <summary>
		/// Starts the two-phase sequence
		/// </summary>
		/// <param name="caller">The offer initiator.</param>
		Task<bool> OfferAsync(object caller);

		/// <summary>
		/// Commits the two-phase sequence
		/// </summary>
		/// <param name="caller">The offer initiator.</param>
		Task CommitAsync(object caller);
		/// <summary>
		/// Cancels the two-phase sequence
		/// </summary>
		/// <param name="caller">The offer initiator.</param>
		Task WithdrawAsync(object caller);
	}

    /// <summary>
    /// Represents an offer that needs notification once the initial match has failed
    /// </summary>
    public interface IProbeAbleOffer
    {
        /// <summary>
        /// Signals the instance that the probe phase is completed
        /// </summary>
        void ProbeComplete();
    }

    /// <summary>
    /// Represents an offer that can expire
    /// </summary>
    public interface IExpiringOffer : ITwoPhaseOffer, IProbeAbleOffer
    {
        /// <summary>
        /// The time the offer expires
        /// </summary>
        DateTime Expires { get; }

        /// <summary>
        /// Gets a value indicating whether this <see cref="T:CoCoL.ITimeoutAbleOffer"/> is expired.
        /// </summary>
        bool IsExpired { get; }
    }

    /// <summary>
    /// Represents an offer that can be cancelled
    /// </summary>
    public interface ICancelAbleOffer : ITwoPhaseOffer
    {
        /// <summary>
        /// The cancellation token
        /// </summary>
        /// <value>The cancel token.</value>
        CancellationToken CancelToken { get; }            
    }

	/// <summary>
	/// Interface for a thread pool implementation
	/// </summary>
	public interface IThreadPool : IDisposable
	{
		/// <summary>
		/// Puts an item into the work queue
		/// </summary>
		/// <param name="a">The work item.</param>
		void QueueItem(Action a);

		/// <summary>
		/// Puts an item into the work queue
		/// </summary>
		/// <param name="a">The work item.</param>
		/// <param name="item">An optional callback parameter.</param>
		void QueueItem(WAITCALLBACK a, object item);

		/// <summary>
		/// Puts an item into the work queue
		/// </summary>
		/// <param name="a">The work item.</param>
		/// <returns>The awaitable task.</returns>
		Task QueueTask(Action a);
	}

	/// <summary>
	/// Interface for a threadpool that supports finishing
	/// </summary>
	public interface IFinishAbleThreadPool : IThreadPool
	{
		/// <summary>
		/// Ensures that the threadpool is finished or throws an exception
		/// </summary>
		/// <param name="waittime">The maximum time to wait for completion.</param>
		Task EnsureFinishedAsync(TimeSpan waittime = default(TimeSpan));
	}

    /// <summary>
	/// A marker interface that signals that all continuations must be enqueued
	/// </summary>
	public interface ILimitingThreadPool : IThreadPool
	{
	}
}

