using System;

namespace CoCoL
{
	/// <summary>
	/// The communication mode
	/// </summary>
	public enum CommunicationMode
	{
		/// <summary>
		/// The request is a read
		/// </summary>
		Read,
		/// <summary>
		/// The request is a write
		/// </summary>
		Write
	}

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
	/// The result of a continuation
	/// </summary>
	public interface ICallbackResult<T>
	{
		/// <summary>
		/// Gets the value written to a channel, or throws the exception
		/// </summary>
		/// <value>The result.</value>
		T Result { get; }
		/// <summary>
		/// Gets the exception found on a channel, or null
		/// </summary>
		/// <value>The exception on the channel, or null.</value>
		Exception Exception { get; }
		/// <summary>
		/// Gets the channel.
		/// </summary>
		/// <value>The channel that was read or written.</value>
		IContinuationChannel<T> Channel { get; }
	}

	/// <summary>
	/// Interface for a blocking synchronous communication channel
	/// </summary>
	public interface IBlockingChannel<T>
	{
		/// <summary>
		/// Perform a blocking read
		/// </summary>
		T Read();

		/// <summary>
		/// Perform a blocking write
		/// </summary>
		/// <param name="value">The value to write</param>
		void Write(T value);
	}

	/// <summary>
	/// The delegate for reporting a channel operation
	/// </summary>
	public delegate void ChannelCallback<T>(ICallbackResult<T> result);

	/// <summary>
	/// Interface for a communication channel the supports continuation
	/// </summary>
	public interface IContinuationChannel<T>
	{
		/// <summary>
		/// Registers a desire to read from the channel
		/// </summary>
		/// <param name="offer">A callback method for offering an item, use null to unconditionally accept</param>
		/// <param name="callback">A callback method that is called with the result of the operation</param>
		/// <param name="timeout">The time to wait for the operation, use zero to return a timeout immediately if no items can be read. Use a negative span to wait forever.</param>
		void RegisterRead(ITwoPhaseOffer offer, ChannelCallback<T> commitCallback, TimeSpan timeout);

		/// <summary>
		/// Registers a desire to write to the channel
		/// </summary>
		/// <param name="offer">A callback method for offering an item, use null to unconditionally accept</param>
		/// <param name="callback">A callback method that is called with the result of the operation</param>
		/// <param name="value">The value to write to the channel.</param>
		/// <param name="timeout">The time to wait for the operation, use zero to return a timeout immediately if no items can be read. Use a negative span to wait forever.</param>
		void RegisterWrite(ITwoPhaseOffer offer, ChannelCallback<T> commitCallback, T value, TimeSpan timeout);

		/// <summary>
		/// Registers a desire to read from the channel
		/// </summary>
		/// <param name="callback">A callback method that is called with the result of the operation</param>
		void RegisterRead(ChannelCallback<T> commitCallback);
		/// <summary>
		/// Registers a desire to read from the channel
		/// </summary>
		/// <param name="callback">A callback method that is called with the result of the operation</param>
		/// <param name="timeout">The time to wait for the operation, use zero to return a timeout immediately if no items can be read. Use a negative span to wait forever.</param>
		void RegisterRead(ChannelCallback<T> commitCallback, TimeSpan timeout);
		/// <summary>
		/// Registers a desire to read from the channel
		/// </summary>
		/// <param name="offer">A callback method for offering an item, use null to unconditionally accept</param>
		/// <param name="callback">A callback method that is called with the result of the operation</param>
		void RegisterRead(ITwoPhaseOffer offer, ChannelCallback<T> commitCallback);

		/// <summary>
		/// Registers a desire to write to the channel
		/// </summary>
		/// <param name="callback">A callback method that is called with the result of the operation</param>
		/// <param name="value">The value to write to the channel.</param>
		void RegisterWrite(ChannelCallback<T> commitCallback, T value);

		/// <summary>
		/// Registers a desire to write to the channel
		/// </summary>
		/// <param name="offer">A callback method for offering an item, use null to unconditionally accept</param>
		/// <param name="callback">A callback method that is called with the result of the operation</param>
		/// <param name="value">The value to write to the channel.</param>
		/// <param name="timeout">The time to wait for the operation, use zero to return a timeout immediately if no items can be read. Use a negative span to wait forever.</param>
		void RegisterWrite(ChannelCallback<T> commitCallback, T value, TimeSpan timeout);

		/// <summary>
		/// Registers a desire to write to the channel
		/// </summary>
		/// <param name="offer">A callback method for offering an item, use null to unconditionally accept</param>
		/// <param name="callback">A callback method that is called with the result of the operation</param>
		/// <param name="value">The value to write to the channel.</param>
		void RegisterWrite(ITwoPhaseOffer offer, ChannelCallback<T> commitCallback, T value);

		/// <summary>
		/// Registers a desire to write to the channel
		/// </summary>
		/// <param name="value">The value to write to the channel.</param>
		/// <param name="timeout">The time to wait for the operation, use zero to return a timeout immediately if no items can be read. Use a negative span to wait forever.</param>
		void RegisterWrite(T value);

		/// <summary>
		/// Registers a desire to write to the channel
		/// </summary>
		/// <param name="value">The value to write to the channel.</param>
		/// <param name="timeout">The time to wait for the operation, use zero to return a timeout immediately if no items can be read. Use a negative span to wait forever.</param>
		void RegisterWrite(T value, TimeSpan timeout);

		/// <summary>
		/// Registers a desire to write to the channel
		/// </summary>
		/// <param name="offer">A callback method for offering an item, use null to unconditionally accept</param>
		/// <param name="value">The value to write to the channel.</param>
		/// <param name="timeout">The time to wait for the operation, use zero to return a timeout immediately if no items can be read. Use a negative span to wait forever.</param>
		void RegisterWrite(ITwoPhaseOffer offer, T value, TimeSpan timeout);

		/// <summary>
		/// Stops this channel from processing messages
		/// </summary>
		void Retire();

		/// <summary>
		/// Gets a value indicating whether this <see cref="CoCoL.IContinuationChannel`1"/> is retired.
		/// </summary>
		/// <value><c>true</c> if is retired; otherwise, <c>false</c>.</value>
		bool IsRetired { get; }
	}

	/// <summary>
	/// Marker interface to work with mixed-type channels
	/// </summary>
	public interface IUntypedContinuationChannel
	{
	}

	/// <summary>
	/// Interface for a process
	/// </summary>
	public interface IProcess
	{
		/// <summary>
		/// The method invoked to run the process
		/// </summary>
		void Run();
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
		bool Offer(object caller);
		/// <summary>
		/// Commits the two-phase sequence
		/// </summary>
		/// <param name="caller">The offer initiator.</param>
		void Commit(object caller);
		/// <summary>
		/// Cancels the two-phase sequence
		/// </summary>
		/// <param name="caller">The offer initiator.</param>
		void Withdraw(object caller);
	}


	/// <summary>
	/// An untyped communication intent
	/// </summary>
	public interface ICommunicationIntent
	{
		/// <summary>
		/// The direction of the communication
		/// </summary>
		CommunicationMode Mode { get; }

		/// <summary>
		/// The two-phase offer handler or null
		/// </summary>
		ITwoPhaseOffer Offer { get; }

		/// <summary>
		/// The channel to which the communication should be performed
		/// </summary>
		IUntypedContinuationChannel Channel { get; }

		/// <summary>
		/// The value being written, or null
		/// </summary>
		object Value { get; }

		/// <summary>
		/// The method to call after a successfull read or write
		/// </summary>
		/// <value>The callback method.</value>
		Delegate CallbackMethod { get; }
	}
}

