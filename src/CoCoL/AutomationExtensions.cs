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
		public static ChannelScope AutoWireChannels<T>(T[] items, ChannelScope scope = null)
		{
			return AutoWireChannels(items.AsEnumerable(), scope);
		}

		/// <summary>
		/// Wires up all named channels using the supplied scope
		/// </summary>
		/// <param name="items">The processes to wire up.</param>
		/// <param name="scope">The current scope.</param>
		public static ChannelScope AutoWireChannels<T>(IEnumerable<T> items, ChannelScope scope = null)
		{
			scope = scope ?? ChannelScope.Current;
			foreach (var p in items)
				AutoWireChannels(p, scope);

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
			return AutoWireChannels<IProcess>(item, scope);
		}

		/// <summary>
		/// Wires up all named channels using the supplied scope
		/// </summary>
		/// <param name="item">The item to wire up.</param>
		/// <param name="scope">The current scope.</param>
		public static T AutoWireChannels<T>(T item, ChannelScope scope = null)
		{
			scope = scope ?? ChannelScope.Current;

			foreach (var c in GetAllFieldAndPropertyValuesOfType<IRetireAbleChannel>(item))
			{
				try
				{
					// Skip if already assigned
					if (c.Value != null)
						continue;

					var attr = c.Key.GetCustomAttribute(typeof(ChannelNameAttribute), true) as ChannelNameAttribute;

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
					var chan = curscope.GetOrCreate(attr.Name, dataType, attr.BufferSize);

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

					if (chan is IJoinAbleChannelEnd)
						((IJoinAbleChannelEnd)chan).Join();
					else if (chan is IJoinAbleChannel)
					{
						// If the type is both read and write, we cannot use join semantics
						if (isOnlyReadOrWrite)
						{
							if (readInterface != null)
								((IJoinAbleChannel)chan).Join(true);
							if (writeInterface != null)
								((IJoinAbleChannel)chan).Join(false);
						}
					}
				}
				catch(Exception ex)
				{
					System.Diagnostics.Debug.WriteLine("Failed to set channel: {1}, message: {0}", ex, c.Key.Name);
				}
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
		/// Uses reflection to find all properties and fields that are of type IRetireAbleChannel
		/// and calls the Retire method on them
		/// </summary>
		/// <param name="item">The instance to examine.</param>
		public static void RetireAllChannels(object item)
		{
			foreach (var c in GetAllFieldAndPropertyValuesOfType<IRetireAbleChannel>(item))
				try
			{
				if (c.Value is IJoinAbleChannelEnd)
					((IJoinAbleChannelEnd)c.Value).Dispose();
				else if (c.Value is IJoinAbleChannel)
				{
					// Figure out what type of channel we expect
					var channelType = c.Key is FieldInfo ? ((FieldInfo)c.Key).FieldType : ((PropertyInfo)c.Key).PropertyType;
					var readInterface = new Type[] { channelType }.Union(channelType.GetInterfaces()).Where(x => x.IsConstructedGenericType && x.GetGenericTypeDefinition() == (typeof(IReadChannel<>))).FirstOrDefault();
					var writeInterface = new Type[] { channelType }.Union(channelType.GetInterfaces()).Where(x => x.IsConstructedGenericType && x.GetGenericTypeDefinition() == (typeof(IWriteChannel<>))).FirstOrDefault();

					// If the channel is read-write, we do not use join semantics, but just retire the channel
					if ((readInterface == null) == (writeInterface == null))
					{
						if (c.Value != null)
							c.Value.Retire();
					}

					// Otherwise use the correct interface
					else
					{
						if (readInterface != null)
							((IJoinAbleChannel)c.Value).Leave(true);
						if (writeInterface != null)
							((IJoinAbleChannel)c.Value).Leave(false);
					}
				}
				else if (c.Value != null)
					c.Value.Retire();
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
		public static async Task RunProtected(IDisposable instance, Func<Task> method, bool catchRetiredExceptions = true)
		{
			try
			{
				using(instance)
					await method();
			}
			catch(AggregateException ex)
			{
				if (catchRetiredExceptions)
				{
					var lst = 
						from n in ex.Flatten().InnerExceptions
							where !(n is RetiredException)
						select n;

					if (lst.Count() == 0)
						return;
					else if (lst.Count() == 1)
						throw lst.First();
					else
						throw new AggregateException(lst);
				}

				if (ex.Flatten().InnerExceptions.Count == 1)
					throw ex.Flatten().InnerExceptions.First();

				throw;
			}
			catch(RetiredException)
			{
				if (!catchRetiredExceptions)
					throw;
			}
		}
	}
}

