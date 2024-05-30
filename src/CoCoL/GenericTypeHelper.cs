using System;
using System.Threading.Tasks;
using System.Linq;
using System.Reflection;
using System.Threading;

namespace CoCoL
{
	/// <summary>
	/// Interface to avoid certain reflection methods
	/// that are not found in the PCL
	/// </summary>
	internal interface IGenericTypeHelper
	{
		/// <summary>
		/// Reads from the channel and returns the task
		/// </summary>
		/// <returns>The async task.</returns>
		/// <param name="channel">The channel to read from.</param>
		/// <param name="offer">The two-phase offer.</param>
		Task<object> ReadAsync(IUntypedChannel channel, ITwoPhaseOffer offer);

		/// <summary>
		/// Writes the channel and returns the task
		/// </summary>
		/// <returns>The async task.</returns>
		/// <param name="channel">The channel to write to.</param>
		/// <param name="value">The value to write.</param>
		/// <param name="offer">The two-phase offer.</param>
		Task WriteAsync(IUntypedChannel channel, object value, ITwoPhaseOffer offer);

		/// <summary>
		/// Requests a read on the channel.
		/// </summary>
		/// <returns>The read request.</returns>
		/// <param name="channel">The channel to read from.</param>
		IMultisetRequestUntyped RequestRead(IUntypedChannel channel);

		/// <summary>
		/// Requests a write on the channel.
		/// </summary>
		/// <returns>The write request.</returns>
		/// <param name="value">The value to write.</param>
		/// <param name="channel">The channel to write to.</param>
		IMultisetRequestUntyped RequestWrite(object value, IUntypedChannel channel);

	}

	/// <summary>
	/// Implementation of the generic type helper
	/// </summary>
	internal struct GenericTypeHelper<T> : IGenericTypeHelper, IEquatable<GenericTypeHelper<T>>
	{
		/// <summary>
		/// Reads from the channel and returns the task
		/// </summary>
		/// <returns>The async task.</returns>
		/// <param name="channel">The channel to read from.</param>
		/// <param name="offer">The two-phase offer.</param>
		public async Task<object> ReadAsync(IUntypedChannel channel, ITwoPhaseOffer offer)
		{
			return (await (channel as IReadChannel<T>).ReadAsync(offer));
		}

		/// <summary>
		/// Writes the channel and returns the task
		/// </summary>
		/// <returns>The async task.</returns>
		/// <param name="channel">The channel to write to.</param>
		/// <param name="value">The value to write.</param>
		/// <param name="offer">The two-phase offer.</param>
		public Task WriteAsync(IUntypedChannel channel, object value, ITwoPhaseOffer offer)
		{
			return (channel as IWriteChannel<T>).WriteAsync((T)value, offer);
		}

		/// <summary>
		/// Requests a read on the channel.
		/// </summary>
		/// <returns>The read request.</returns>
		/// <param name="channel">The channel to read from.</param>
		public IMultisetRequestUntyped RequestRead(IUntypedChannel channel)
		{
			return (channel as IReadChannel<T>).RequestRead();
		}

		/// <summary>
		/// Requests a write on the channel.
		/// </summary>
		/// <returns>The write request.</returns>
		/// <param name="value">The value to write.</param>
		/// <param name="channel">The channel to write to.</param>
		public IMultisetRequestUntyped RequestWrite(object value, IUntypedChannel channel)
		{
			return (channel as IWriteChannel<T>).RequestWrite((T)value);
		}

		/// <summary>
		/// Explicit disabling of compares
		/// </summary>
		/// <param name="other">The item to compare to</param>
		/// <returns>Always throws an exception</returns>
		bool IEquatable<GenericTypeHelper<T>>.Equals(GenericTypeHelper<T> other)
		{
			throw new NotImplementedException();
		}
	}

	/// <summary>
	/// Class for helping with access to untyped methods through reflection
	/// </summary>
	internal static class UntypedAccessMethods
	{
		/// <summary>
		/// Gets the IReadChannel&lt;&gt; interface from an untyped channel instance
		/// </summary>
		/// <returns>The IReadChannel&lt;&gt; interface.</returns>
		/// <param name="self">The channel to get the interface from</param>
		public static Type ReadInterface(this IUntypedChannel self)
		{
			return GetImplementedGenericInterface(self, typeof(IReadChannel<>));
		}

		/// <summary>
		/// Gets the IWriteChannel&lt;&gt; interface from an untyped channel instance
		/// </summary>
		/// <returns>The IWriteChannel&lt;&gt; interface.</returns>
		/// <param name="self">The channel to get the interface from</param>
		public static Type WriteInterface(this IUntypedChannel self)
		{
			return GetImplementedGenericInterface(self, typeof(IWriteChannel<>));
		}


		/// <summary>
		/// Creates an acessor from the given itemtype
		/// </summary>
		/// <returns>The accessor.</returns>
		/// <param name="item">The item to create the interface for.</param>
		public static IGenericTypeHelper CreateReadAccessor(this IUntypedChannel item)
		{
			var readinterface = ReadInterface(item);
			return (IGenericTypeHelper)Activator.CreateInstance(typeof(GenericTypeHelper<>).MakeGenericType(readinterface.GenericTypeArguments));
		}

		/// <summary>
		/// Creates an acessor to an IWriteChannel&lt;&gt;
		/// </summary>
		/// <returns>The accessor.</returns>
		/// <param name="item">The item to create the interface for.</param>
		public static IGenericTypeHelper CreateWriteAccessor(this IUntypedChannel item)
		{
			var writeinterface = WriteInterface(item);
			return (IGenericTypeHelper)Activator.CreateInstance(typeof(GenericTypeHelper<>).MakeGenericType(writeinterface.GenericTypeArguments));
		}

		/// <summary>
		/// Creates an acessor from the given itemtype, that is the T in IChannel&lt;T&gt;
		/// </summary>
		/// <returns>The accessor type.</returns>
		/// <param name="itemtype">The item type, that is the T in IChannel&lt;T&gt;.</param>
		public static IGenericTypeHelper CreateAccessor(Type itemtype)
		{
			return (IGenericTypeHelper)Activator.CreateInstance(typeof(GenericTypeHelper<>).MakeGenericType(itemtype));
		}

		/// <summary>
		/// Gets the implemented generic interface from an instance.
		/// </summary>
		/// <returns>The implemented generic interface type.</returns>
		/// <param name="item">The item to examine.</param>
		/// <param name="interface">The interface type definition.</param>
		private static Type GetImplementedGenericInterface(object item, Type @interface)
		{
			if (item == null)
				throw new ArgumentNullException(nameof(item));

			var implementedinterface = item.GetType().GetInterfaces().Where(x => x.IsGenericType && !x.IsGenericTypeDefinition).Where(x => x.GetGenericTypeDefinition() == @interface).FirstOrDefault();
			if (implementedinterface == null)
				throw new ArgumentException(string.Format("Given type {0} does not implement interface {1}", item.GetType(), @interface));

			return implementedinterface;
		}
	}
}

