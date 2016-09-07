using System;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CoCoL
{
	/// <summary>
	/// Functionality for automatically wiring up channels
	/// </summary>
	public static class AutomationExtensions
	{
		/// <summary>
		/// Wires up all named channels using the supplied scope
		/// </summary>
		/// <param name="items">The processes to wire up.</param>
		/// <param name="scope">The current scope.</param>
		public static ChannelScope AutoWireChannels<T>(IEnumerable<T> items, ChannelScope scope = null)
		{
			scope = scope ?? ChannelScope.Current;
			foreach (var p in items)
				AutoWireChannelsDirect(p, scope);

			return scope;
		}

		/// <summary>
		/// Wires up all named channels using the supplied scope
		/// </summary>
		/// <param name="items">The processes to wire up.</param>
		/// <param name="scope">The current scope.</param>
		public static ChannelScope AutoWireChannels(this IEnumerable<IProcess> items, ChannelScope scope = null)
		{
			return AutoWireChannels<IProcess>(items, scope);
		}

		/// <summary>
		/// Wires up all named channels using the supplied scope
		/// </summary>
		/// <param name="item">The item to wire up.</param>
		/// <param name="scope">The current scope.</param>
		public static IProcess AutoWireChannels(this IProcess item, ChannelScope scope = null)
		{
			return AutoWireChannelsDirect<IProcess>(item, scope);
		}

		/// <summary>
		/// Wires up all named channels using the supplied scope.
		/// Checks if the supplied item is an IEnumerable and iterates it
		/// if possible
		/// </summary>
		/// <param name="item">The item to wire up.</param>
		/// <param name="scope">The current scope.</param>
		public static T AutoWireChannels<T>(T item, ChannelScope scope = null)
		{
			// Due to the type matching, we can end up here when
			// the user supplies an array or similar

			// The encapsulation ensures that we only expand the single 
			// outer most instance
			if (typeof(System.Collections.IEnumerable).IsAssignableFrom(typeof(T)))
			{
				var en = item as System.Collections.IEnumerable;
				foreach (var x in en)
					AutoWireChannelsDirect(x, scope);
			}

			return AutoWireChannelsDirect(item);
		}

		private static void JoinChannel(object item, Type definedtype)
		{
			var readInterface = new Type[] { definedtype }.Union(definedtype.GetInterfaces()).Where(x => x.IsConstructedGenericType && x.GetGenericTypeDefinition() == (typeof(IReadChannel<>))).FirstOrDefault();
			var writeInterface = new Type[] { definedtype }.Union(definedtype.GetInterfaces()).Where(x => x.IsConstructedGenericType && x.GetGenericTypeDefinition() == (typeof(IWriteChannel<>))).FirstOrDefault();
			var isOnlyReadOrWrite = (readInterface == null) != (writeInterface == null);

			var isRetired = item is IRetireAbleChannel ? (item as IRetireAbleChannel).IsRetiredAsync.WaitForTask().Result : false;

			if (item is IJoinAbleChannelEnd && !isRetired)
				((IJoinAbleChannelEnd)item).Join();
			else if (item is IJoinAbleChannel && !isRetired)
			{
				// If the type is both read and write, we cannot use join semantics
				if (isOnlyReadOrWrite)
				{
					if (readInterface != null)
						((IJoinAbleChannel)item).Join(true);
					if (writeInterface != null)
						((IJoinAbleChannel)item).Join(false);
				}
			}
		}

		/// <summary>
		/// Wires up all named channels using the supplied scope for the given element.
		/// Does not check if the given item is an IEnumerable
		/// </summary>
		/// <param name="item">The item to wire up.</param>
		/// <param name="scope">The current scope.</param>
		public static T AutoWireChannelsDirect<T>(T item, ChannelScope scope = null)
		{
			scope = scope ?? ChannelScope.Current;

			foreach (var c in GetAllFieldAndPropertyValuesOfType<IRetireAbleChannel>(item))
			{
				try
				{
					var marker = c.Value as ChannelNameMarker;

					// Make sure we do not continue with a marker class instance
					if (marker != null)
						if (c.Key is FieldInfo)
							((FieldInfo)c.Key).SetValue(item, null);
						else
							((PropertyInfo)c.Key).SetValue(item, null);

					// Skip if already assigned
					if (c.Value != null && !(c.Value is ChannelNameMarker))
						continue;

					var attr = c.Key.GetCustomAttribute(typeof(ChannelNameAttribute), true) as ChannelNameAttribute;

					// Override if we get a marker
					if (attr == null && marker != null)
						attr = marker.Attribute;

					// Skip if the channel does not have a name
					if (attr == null || string.IsNullOrWhiteSpace(attr.Name))
						continue;

					// Figure out what type of channel we expect
					var channelType = c.Key is FieldInfo ? ((FieldInfo)c.Key).FieldType : ((PropertyInfo)c.Key).PropertyType;
					var readInterface = new Type[] { channelType }.Union(channelType.GetInterfaces()).Where(x => x.IsConstructedGenericType && x.GetGenericTypeDefinition() == (typeof(IReadChannel<>))).FirstOrDefault();
					var writeInterface = new Type[] { channelType }.Union(channelType.GetInterfaces()).Where(x => x.IsConstructedGenericType && x.GetGenericTypeDefinition() == (typeof(IWriteChannel<>))).FirstOrDefault();

					if (readInterface == null && writeInterface == null)
						throw new Exception(string.Format("Item {0} had a channelname attribute but is not of the channel type", c.Key.Name));

					var isOnlyReadOrWrite = (readInterface == null) != (writeInterface == null);

					// Extract the channel data type
					Type dataType = (readInterface ?? writeInterface).GenericTypeArguments[0];

					// Honor scope requirements
					var curscope = scope;
					if (attr.TargetScope == ChannelNameScope.Parent)
						curscope = scope.ParentScope ?? curscope;
					else if (attr.TargetScope == ChannelNameScope.Global)
						curscope = ChannelScope.Root;

					// Instantiate or fetch the channel
					var chan = curscope.GetOrCreate(attr, dataType);

					if (isOnlyReadOrWrite)
					{
						if (readInterface != null && channelType.IsAssignableFrom(typeof(IReadChannelEnd<>).MakeGenericType(dataType)))
						{
							chan = (IRetireAbleChannel)typeof(ChannelExtensions).GetMethod("AsReadOnly").MakeGenericMethod(dataType).Invoke(null, new object[] { chan });
						}
						else if (writeInterface != null && channelType.IsAssignableFrom(typeof(IWriteChannelEnd<>).MakeGenericType(dataType)))
						{
							chan = (IRetireAbleChannel)typeof(ChannelExtensions).GetMethod("AsWriteOnly").MakeGenericMethod(dataType).Invoke(null, new object[] { chan });
						}
					}
						
					// Assign the channel to the field or property
						
					if (c.Key is FieldInfo)
						((FieldInfo)c.Key).SetValue(item, chan);
					else
						((PropertyInfo)c.Key).SetValue(item, chan);

					JoinChannel(chan, channelType);
				}
				catch(Exception ex)
				{
					System.Diagnostics.Debug.WriteLine("Failed to set channel: {1}, message: {0}", ex, c.Key.Name);
				}
			}

			//TODO: Support Enumerables too?
			foreach (var c in GetAllFieldAndPropertyValuesOfType<Array>(item))
				for (var i = 0; i < c.Value.Length; i++)
					try
					{
						JoinChannel(c.Value.GetValue(i), (c.Key is FieldInfo ? ((FieldInfo)c.Key).FieldType : ((PropertyInfo)c.Key).PropertyType).GetElementType());
					}
					catch
					{
					}

			return item;
		}

		/// <summary>
		/// Gets all fields and properties with a type that is assignable to T
		/// </summary>
		/// <returns>All field and property values of type.</returns>
		/// <param name="item">The item to examine.</param>
		/// <typeparam name="T">The type of the items to return.</typeparam>
		internal static IEnumerable<KeyValuePair<MemberInfo, T>> GetAllFieldAndPropertyValuesOfType<T>(object item)
			where T : class
		{
			if (item != null)
			{
				foreach (var f in item.GetType().GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy | BindingFlags.Instance))
					if (typeof(T).IsAssignableFrom(f.FieldType))
					{
						var val = default(T);
						try
						{
							val = f.GetValue(item) as T;
						}
						catch
						{
						}

						yield return new KeyValuePair<MemberInfo, T>(f, val);
					}


				foreach (var f in item.GetType().GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy | BindingFlags.Instance))
					if (typeof(T).IsAssignableFrom(f.PropertyType))
					{
						var val = default(T);
						try
						{
							val = f.GetValue(item) as T;
						}
						catch
						{
						}

						yield return new KeyValuePair<MemberInfo, T>(f, val);
					}           
			}
		}

		/// <summary>
		/// Retires a channel
		/// </summary>
		/// <param name="value">The channel to retire.</param>
		/// <param name="definedtype">The type obtained from the declaring field.</param>
		private static void RetireChannel(object value, Type definedtype)
		{
			if (value == null)
				return;
			
			if (value is IJoinAbleChannelEnd)
				((IJoinAbleChannelEnd)value).Dispose();
			else if (value is IJoinAbleChannel)
			{
				// Figure out what type of channel we expect
				var channelType = definedtype;
				var readInterface = new Type[] { definedtype }.Union(definedtype.GetInterfaces()).Where(x => x.IsConstructedGenericType && x.GetGenericTypeDefinition() == (typeof(IReadChannel<>))).FirstOrDefault();
				var writeInterface = new Type[] { definedtype }.Union(definedtype.GetInterfaces()).Where(x => x.IsConstructedGenericType && x.GetGenericTypeDefinition() == (typeof(IWriteChannel<>))).FirstOrDefault();

				// If the channel is read-write, we do not use join semantics, but just retire the channel
				if ((readInterface == null) == (writeInterface == null))
				{
					if (value as IRetireAbleChannel != null)
						((IRetireAbleChannel)value).Retire();
				}

				// Otherwise use the correct interface
				else
				{
					if (readInterface != null)
						((IJoinAbleChannel)value).Leave(true);
					if (writeInterface != null)
						((IJoinAbleChannel)value).Leave(false);
				}
			}
			else if (value as IRetireAbleChannel != null)
				((IRetireAbleChannel)value).Retire();		
		}

		/// <summary>
		/// Uses reflection to find all properties and fields that are of type IRetireAbleChannel
		/// and calls the Retire method on them
		/// </summary>
		/// <param name="item">The instance to examine.</param>
		public static void RetireAllChannels(object item)
		{
			foreach (var c in GetAllFieldAndPropertyValuesOfType<IRetireAbleChannel>(item))
				try
				{
					RetireChannel(c.Value, c.Key is FieldInfo ? ((FieldInfo)c.Key).FieldType : ((PropertyInfo)c.Key).PropertyType);
				}
				catch
				{
				}

			foreach (var c in GetAllFieldAndPropertyValuesOfType<Array>(item))
				for (var i = 0; i < c.Value.Length; i++)
					try
					{
						RetireChannel(c.Value.GetValue(i), (c.Key is FieldInfo ? ((FieldInfo)c.Key).FieldType : ((PropertyInfo)c.Key).PropertyType).GetElementType());
					}
					catch
					{
					}
		}

		/// <summary>
		/// Runs a method and disposes this instance afterwards
		/// </summary>
		/// <returns>The task for completion.</returns>
		/// <param name="instance">The instance to dispose after running</param>
		/// <param name="method">The callback method that does the actual work.</param>
		/// <param name="catchRetiredExceptions">If set to <c>true</c> any RetiredExceptions are caught and ignored.</param>
		public static async Task RunProtected(this IDisposable instance, Func<Task> method, bool catchRetiredExceptions = true)
		{
			try
			{
				using(instance)
					await method();
			}
			catch(Exception ex)
			{
				if (catchRetiredExceptions && IsRetiredException(ex))
					return;

				// Unwrap
				if (ex is AggregateException && ((AggregateException)ex).Flatten().InnerExceptions.Count == 1)
					throw ((AggregateException)ex).Flatten().InnerExceptions.First();

				throw;
			}
		}

		/// <summary>
		/// Helper method for providing channels in an external object, 
		/// such that simple processes do not need to define a class instance
		/// </summary>
		/// <returns>The awaitable task for the process</returns>
		/// <param name="channels">The channel object to use. Accepts anonymous types.</param>
		/// <param name="method">The process method.</param>
		/// <typeparam name="T">The type of the channel object parameter.</typeparam>
		/// <param name="catchRetiredExceptions">If set to <c>true</c> any RetiredExceptions are caught and ignored.</param>
		public static async Task RunTask<T>(this T channels, Func<T, Task> method, bool catchRetiredExceptions = true)
		{
			AutoWireChannelsDirect(channels);
			try
			{
				await method(channels);
			}
			catch(Exception ex)
			{
				if (catchRetiredExceptions && IsRetiredException(ex))
					return;

				// Unwrap
				if (ex is AggregateException && ((AggregateException)ex).Flatten().InnerExceptions.Count == 1)
					throw ((AggregateException)ex).Flatten().InnerExceptions.First();

				throw;
			}
			finally
			{
				RetireAllChannels(channels);
			}
		}

		/// <summary>
		/// Determines if is the exception is a RetiredException.
		/// </summary>
		/// <returns><c>true</c> the exception is a RetiredException; otherwise, <c>false</c>.</returns>
		/// <param name="self">The exception to investigate.</param>
		public static bool IsRetiredException(this Exception self)
		{
			if (self is RetiredException)
				return true;
			else if (self is AggregateException && ((AggregateException)self).Flatten().InnerExceptions.First() is RetiredException)
				return true;

			return false;
		}

		/// <summary>
		/// Determines if is the exception is a TimeoutException.
		/// </summary>
		/// <returns><c>true</c> the exception is a TimeoutException; otherwise, <c>false</c>.</returns>
		/// <param name="self">The exception to investigate.</param>
		public static bool IsTimeoutException(this Exception self)
		{
			if (self is RetiredException)
				return true;
			else if (self is AggregateException && ((AggregateException)self).Flatten().InnerExceptions.First() is TimeoutException)
				return true;

			return false;
		}
	}
}

