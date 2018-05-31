using System;
using System.Collections.Generic;
using System.Linq;

namespace CoCoL
{
	/// <summary>
	/// Implementation of a nested scope for assigning channel names
	/// </summary>
	public class ChannelScope : IDisposable
	{
		/// <summary>
		/// The root scope, where all other scopes descend from
		/// </summary>
		public static readonly ChannelScope Root;

		/// <summary>
		/// The lock object
		/// </summary>
		protected static readonly object __lock;

		/// <summary>
		/// Lookup table for scopes
		/// </summary>
		protected static readonly Dictionary<string, ChannelScope> __scopes = new Dictionary<string, ChannelScope>();

		/// <summary>
		/// Static initializer to control the creation order
		/// </summary>
		static ChannelScope()
		{
			__lock = new object();
			Root = new ChannelScope(null, true);
		}
			

		/// <summary>
		/// True if this instance is disposed, false otherwise
		/// </summary>
		protected bool m_isDisposed = false;

		/// <summary>
		/// The parent scope, or null if this is the root scope
		/// </summary>
		/// <value>The parent scope.</value>
		public ChannelScope ParentScope { get; private set; }

		/// <summary>
		/// Gets a value indicating whether this scope is isolated, meaning that it does not inherit from the parent scope.
		/// </summary>
		/// <value><c>true</c> if this instance isolated; otherwise, <c>false</c>.</value>
		public bool Isolated { get; private set; }

		/// <summary>
		/// The key for this instance
		/// </summary>
		private readonly string m_instancekey = Guid.NewGuid().ToString("N");

		/// <summary>
		/// The local storage for channels
		/// </summary>
		protected Dictionary<string, IRetireAbleChannel> m_lookup = new Dictionary<string, IRetireAbleChannel>();

		/// <summary>
		/// The key used to assign the current scope into the current call-context
		/// </summary>
		protected const string LOGICAL_CONTEXT_KEY = "CoCoL:AutoWireScope";

		/// <summary>
		/// Initializes a new instance of the <see cref="CoCoL.ChannelScope"/> class that derives from the current scope.
		/// </summary>
		public ChannelScope()
			: this(ChannelScope.Current, false)
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="CoCoL.ChannelScope"/> class that derives from the current scope.
		/// </summary>
		/// <param name="isolated"><c>True</c> if this is an isolated scope, <c>false</c> otherwise</param>
		protected ChannelScope(bool isolated)
			: this(ChannelScope.Current, isolated)
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="CoCoL.ChannelScope"/> class that derives from a parent scope.
		/// </summary>
		/// <param name="parent">The parent scope.</param>
		/// <param name="isolated"><c>True</c> if this is an isolated scope, <c>false</c> otherwise</param>
		private ChannelScope(ChannelScope parent, bool isolated)
		{
			ParentScope = parent;
			Isolated = isolated;
			Current = this;
			lock (__lock)
				__scopes[m_instancekey] = this;
		}

		/// <summary>
		/// Gets or creates a channel
		/// </summary>
		/// <returns>The or create.</returns>
		/// <param name="attribute">The attribute describing the channel to create.</param>
		/// <param name="datatype">The type of data communicated through the channel.</param>
		public IRetireAbleChannel GetOrCreate(ChannelNameAttribute attribute, Type datatype)
		{
			return (IRetireAbleChannel)typeof(ChannelScope).GetMethod("GetOrCreate", new Type[] { typeof(ChannelNameAttribute) })
			   .MakeGenericMethod(datatype)
               .Invoke(this, new object[] { attribute });
		}

		/// <summary>
		/// Gets or creates a channel
		/// </summary>
		/// <returns>The or create.</returns>
		/// <param name="name">The name of the channel to create.</param>
		/// <param name="datatype">The type of data communicated through the channel.</param>
		/// <param name="buffersize">The size of the channel buffer.</param>
		/// <param name="maxPendingReaders">The maximum number of pending readers. A negative value indicates infinite</param>
		/// <param name="maxPendingWriters">The maximum number of pending writers. A negative value indicates infinite</param>
		/// <param name="pendingReadersOverflowStrategy">The strategy for dealing with overflow for read requests</param>
		/// <param name="pendingWritersOverflowStrategy">The strategy for dealing with overflow for write requests</param>
		/// <param name="broadcast"><c>True</c> will create the channel as a broadcast channel, the default <c>false</c> will create a normal channel</param>
		/// <param name="initialBroadcastBarrier">The number of readers required on the channel before sending the first broadcast, can only be used with broadcast channels</param>
		/// <param name="broadcastMinimum">The minimum number of readers required on the channel, before a broadcast can be performed, can only be used with broadcast channels</param>
		public IRetireAbleChannel GetOrCreate(string name, Type datatype, int buffersize = 0, int maxPendingReaders = -1, int maxPendingWriters = -1, QueueOverflowStrategy pendingReadersOverflowStrategy = QueueOverflowStrategy.Reject, QueueOverflowStrategy pendingWritersOverflowStrategy = QueueOverflowStrategy.Reject, bool broadcast = false, int initialBroadcastBarrier = -1, int broadcastMinimum = -1)
		{
			if (!broadcast && (initialBroadcastBarrier >= 0 || broadcastMinimum >= 0))
                throw new ArgumentException(string.Format("Cannot set \"{0}\" or \"{1}\" unless the channel is a broadcast channel", "initialBroadcastBarrier", "broadcastMinimum"), nameof(broadcast));

			var attr = 
				broadcast
				? new BroadcastChannelNameAttribute(name, buffersize, ChannelNameScope.Local, maxPendingReaders, maxPendingWriters, pendingReadersOverflowStrategy, pendingWritersOverflowStrategy, initialBroadcastBarrier, broadcastMinimum)
				: new ChannelNameAttribute(name, buffersize, ChannelNameScope.Local, maxPendingReaders, maxPendingWriters, pendingReadersOverflowStrategy, pendingWritersOverflowStrategy);

			return GetOrCreate(
				attr, 
				datatype
			);
		}


		/// <summary>
		/// Gets or creates a channel
		/// </summary>
		/// <returns>The channel with the given name.</returns>
		/// <param name="marker">The <see cref="ChannelNameMarker"/> of the channel to create.</param>
		/// <typeparam name="T">The type of data in the channel.</typeparam>
		public IChannel<T> GetOrCreate<T>(ChannelNameMarker marker)
		{
			return this.GetOrCreate<T>(marker.Attribute);
		}

		/// <summary>
		/// Gets or creates a channel
		/// </summary>
		/// <returns>The channel with the given name.</returns>
		/// <param name="marker">The <see cref="ChannelNameMarker"/> of the channel to create.</param>
		/// <typeparam name="T">The type of data in the channel.</typeparam>
		public IChannel<T> GetOrCreate<T>(ChannelMarkerWrapper<T> marker)
		{
			return this.GetOrCreate<T>(marker.Attribute);
		}

		/// <summary>
		/// Gets or creates a channel
		/// </summary>
		/// <returns>The channel with the given name.</returns>
		/// <param name="attribute">The attribute describing the channel.</param>
		/// <typeparam name="T">The type of data in the channel.</typeparam>
		public IChannel<T> GetOrCreate<T>(ChannelNameAttribute attribute)
		{
			if (attribute == null)
				throw new ArgumentNullException(nameof(attribute));

			lock (__lock)
			{
				var res = RecursiveLookup(attribute.Name);
				if (res != null)
					return (IChannel<T>)res;
				else
				{
					var chan = DoCreateChannel<T>(attribute);
					if (!string.IsNullOrWhiteSpace(attribute.Name))
						m_lookup.Add(attribute.Name, chan);
					return chan;
				}
			}
		}

		/// <summary>
		/// Gets or creates a channel
		/// </summary>
		/// <returns>The channel with the given name.</returns>
		/// <param name="name">The name of the channel to create.</param>
		/// <param name="buffersize">The size of the channel buffer.</param>
		/// <param name="maxPendingReaders">The maximum number of pending readers. A negative value indicates infinite</param>
		/// <param name="maxPendingWriters">The maximum number of pending writers. A negative value indicates infinite</param>
		/// <param name="pendingReadersOverflowStrategy">The strategy for dealing with overflow for read requests</param>
		/// <param name="pendingWritersOverflowStrategy">The strategy for dealing with overflow for write requests</param>
		/// <param name="broadcast"><c>True</c> will create the channel as a broadcast channel, the default <c>false</c> will create a normal channel</param>
		/// <param name="initialBroadcastBarrier">The number of readers required on the channel before sending the first broadcast, can only be used with broadcast channels</param>
		/// <param name="broadcastMinimum">The minimum number of readers required on the channel, before a broadcast can be performed, can only be used with broadcast channels</param>
		/// <typeparam name="T">The type of data in the channel.</typeparam>
		public IChannel<T> GetOrCreate<T>(string name, int buffersize = 0, int maxPendingReaders = -1, int maxPendingWriters = -1, QueueOverflowStrategy pendingReadersOverflowStrategy = QueueOverflowStrategy.Reject, QueueOverflowStrategy pendingWritersOverflowStrategy = QueueOverflowStrategy.Reject, bool broadcast = false, int initialBroadcastBarrier = -1, int broadcastMinimum = -1)
		{
			if (!broadcast && (initialBroadcastBarrier >= 0 || broadcastMinimum >= 0))
				throw new ArgumentException(string.Format("Cannot set \"{0}\" or \"{1}\" unless the channel is a broadcast channel", "initialBroadcastBarrier", "broadcastMinimum"));

			var attr =
				broadcast
				? new BroadcastChannelNameAttribute(name, buffersize, ChannelNameScope.Local, maxPendingReaders, maxPendingWriters, pendingReadersOverflowStrategy, pendingWritersOverflowStrategy, initialBroadcastBarrier, broadcastMinimum)
				: new ChannelNameAttribute(name, buffersize, ChannelNameScope.Local, maxPendingReaders, maxPendingWriters, pendingReadersOverflowStrategy, pendingWritersOverflowStrategy);

			return GetOrCreate<T>(attr);
		}

        /// <summary>
        /// Hook method that allows custom channel creation
        /// </summary>
        /// <returns>The created channel or null, if there is no special handler.</returns>
        /// <param name="attribute">The attribute describing the channel to create.</param>
        /// <typeparam name="T">The type of data in the channel.</typeparam>
        protected virtual IChannel<T> TryCreateChannel<T>(ChannelNameAttribute attribute)
        {
            return null;
        }

		/// <summary>
		/// Creates the channel by calling the ChannelManager.
		/// </summary>
		/// <returns>The channel with the given name.</returns>
		/// <param name="attribute">The attribute describing the channel to create.</param>
		/// <typeparam name="T">The type of data in the channel.</typeparam>
		protected virtual IChannel<T> DoCreateChannel<T>(ChannelNameAttribute attribute)
		{
            var cur = this;
            while (cur != null && cur != Root)
            {
                var res = cur.TryCreateChannel<T>(attribute);
                if (res != null)
                    return res;

                cur = cur.ParentScope;
            }

            return ChannelManager.CreateChannelForScope<T>(attribute);
		}

		/// <summary>
		/// Performs a recursive lookup to find the specified channel.
		/// Returns null if no such channel was found
		/// </summary>
		/// <returns>The channel.</returns>
		/// <param name="name">The name to look for.</param>
		internal IRetireAbleChannel RecursiveLookup(string name)
		{
			if (string.IsNullOrWhiteSpace(name))
				return null;

			IRetireAbleChannel res;
			if (m_lookup.TryGetValue(name, out res))
				return res;
			
			lock (__lock)
			{
				var cur = this;
				while (cur != null)
				{
					if (cur.m_lookup.TryGetValue(name, out res))
						return res;

                    if (cur.Isolated)
						cur = null;
					else
						cur = cur.ParentScope;
				}

				return null;
			}		
		}

        /// <summary>
        /// Registers a channel in the current scope to provide a different channel for that name
        /// </summary>
        /// <param name="name">The name of the channel to register.</param>
        /// <param name="channel">The channel instance to use.</param>
        /// <typeparam name="T">The channel data type parameter.</typeparam>
        public void RegisterChannel<T>(string name, IChannel<T> channel)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Must provide the channel name", nameof(name));

            if (channel == null)
                throw new ArgumentNullException(nameof(channel));

            lock (__lock)
                m_lookup[name] = channel;
        }

		#region IDisposable implementation

		/// <summary>
		/// Releases all resource used by the <see cref="CoCoL.ChannelScope"/> object.
		/// </summary>
		/// <remarks>Call <see cref="Dispose"/> when you are finished using the <see cref="CoCoL.ChannelScope"/>. The
		/// <see cref="Dispose"/> method leaves the <see cref="CoCoL.ChannelScope"/> in an unusable state. After calling
		/// <see cref="Dispose"/>, you must release all references to the <see cref="CoCoL.ChannelScope"/> so the garbage
		/// collector can reclaim the memory that the <see cref="CoCoL.ChannelScope"/> was occupying.</remarks>
		public void Dispose()
		{
			lock (__lock)
			{
				if (this == Root)
					throw new InvalidOperationException("Cannot dispose the root scope");
				
				if (Current == this)
				{
					Current = this.ParentScope;

					// Disposal can be non-deterministic, so we walk the chain
					while (Current.m_isDisposed)
						Current = Current.ParentScope;
				}
				
                // We do not de-register or de-allocate as the scope might still be used
                __scopes.Remove(this.m_instancekey);
                m_lookup = null;
				m_isDisposed = true;
			}
		}

        #endregion

#if PCL_BUILD
		private static bool __IsFirstUsage = true;
		private static ChannelScope __Current = null;

        /// <summary>
        /// Gets the current channel scope.
        /// </summary>
        /// <value>The current scope.</value>
		public static ChannelScope Current
		{
			get
			{
				lock (__lock)
				{
					// TODO: Use AsyncLocal if targeting 4.6
					//var cur = new System.Threading.AsyncLocal<ChannelScope>();
					if (__IsFirstUsage)
					{
						__IsFirstUsage = false;
						System.Diagnostics.Debug.WriteLine("*Warning*: PCL does not provide a call context, so channel scoping does not work correctly for multithreaded use!");
					}

					var cur = __Current;
					if (cur == null)
						return Current = Root;
					else
						return cur;
				}
			}
			private set
			{
				lock (__lock)
				{
					__Current = value;
				}
			}
		}
#elif NETCOREAPP2_0 || NETSTANDARD2_0 || NETSTANDARD1_6

        /// <summary>
        /// The scope data, using AsyncLocal
        /// </summary>
        private static System.Threading.AsyncLocal<string> local_state = new System.Threading.AsyncLocal<string>();

        /// <summary>
        /// Gets the current channel scope.
        /// </summary>
        /// <value>The current scope.</value>
        public static ChannelScope Current
        {
            get
            {
                lock (__lock)
                {
                    var cur = local_state?.Value;
                    if (cur == null)
                        return Current = Root;
                    else
                    {
                        ChannelScope sc;
                        if (!__scopes.TryGetValue(cur, out sc))
                            throw new Exception(string.Format("Unable to find scope in lookup table, this may be caused by attempting to transport call contexts between AppDomains (eg. with remoting calls)"));

                        return sc;
                    }
                }
            }
            private set
            {
                lock (__lock)
                    local_state.Value = value.m_instancekey;
            }
        }
#else
        /// <summary>
        /// Gets the current channel scope.
        /// </summary>
        /// <value>The current scope.</value>
        public static ChannelScope Current
		{
			get 
			{
				lock (__lock)
				{
					var cur = System.Runtime.Remoting.Messaging.CallContext.LogicalGetData(LOGICAL_CONTEXT_KEY) as string;
					if (cur == null)
						return Current = Root;
					else
					{
						ChannelScope sc;
						if (!__scopes.TryGetValue(cur, out sc))
							throw new Exception(string.Format("Unable to find scope in lookup table, this may be caused by attempting to transport call contexts between AppDomains (eg. with remoting calls)"));

						return sc;
					}
				}
			}
			private set
			{
				lock (__lock)
					System.Runtime.Remoting.Messaging.CallContext.LogicalSetData(LOGICAL_CONTEXT_KEY, value.m_instancekey);
			}
		}

#endif
	}

    /// <summary>
    /// Support implementation channel scopes that support custom channel create logic based on the channel name
    /// </summary>  
    public abstract class PrefixCreateChannelScope : ChannelScope
    {
        /// <summary>
        /// The channel selector method used to choose if a channel should be custom created or not.
        /// Return <c>true</c> to create the channel as a custom channel, <c>false</c> to use a default.
        /// </summary>
        private readonly Func<string, bool> m_selector;

        /// <summary>
        /// The method used to create the channel of the given type
        /// </summary>
        private readonly Func<ChannelScope, ChannelNameAttribute, Type, IUntypedChannel> m_creator;

        /// <summary>
        /// Initializes a new instance of the <see cref="CoCoL.PrefixCreateChannelScope"/> class.
        /// </summary>
        /// <param name="creator">The creator method to use</param>
        /// <param name="prefix">The prefix for a channel name that indicates custom channels, or an empty string to make all channels custom created.</param>
        /// <param name="redirectunnamed">Set to <c>true</c> if unnamed channels should be created as custom channels.</param>
        protected PrefixCreateChannelScope(Func<ChannelScope, ChannelNameAttribute, Type, IUntypedChannel> creator, string prefix, bool redirectunnamed)
            : this(creator, name =>
                {
                    // If all channels need to be network channels, we assign a name to this channel
                    if (string.IsNullOrWhiteSpace(name) && redirectunnamed)
                        return true;

                    // Only create those with the right prefix
                    return !string.IsNullOrWhiteSpace(name) && name.StartsWith(prefix ?? string.Empty, StringComparison.OrdinalIgnoreCase);
                })
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="T:CoCoL.PrefixCreateChannelScope"/> class.
        /// </summary>
        /// <param name="creator">The creator method to use</param>
        /// <param name="names">The list of names to create with the <paramref name="creator"/>.</param>
        protected PrefixCreateChannelScope(Func<ChannelScope, ChannelNameAttribute, Type, IUntypedChannel> creator, params string[] names)
            : this(creator, name => name != null && (Array.IndexOf(names, name) >= 0))
        {
            if (names == null)
                throw new ArgumentNullException(nameof(names));
        }

        /// <summary>
        /// Starts a new <see cref="T:CoCoL.PrefixCreateChannelScope"/>.
        /// </summary>
        /// <param name="creator">The creator method to use</param>
        /// <param name="names">The names of the channels to make profiled</param>
        public PrefixCreateChannelScope(Func<ChannelScope, ChannelNameAttribute, Type, IUntypedChannel> creator, params INamedItem[] names)
            : this(creator, names.Where(x => x != null && !string.IsNullOrWhiteSpace(x.Name)).Select(x => x.Name).ToArray())
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CoCoL.PrefixCreateChannelScope"/> class.
        /// </summary>
        /// <param name="creator">The creator method to use </param>
        /// <param name="selector">The selector method used too determine if a channel should be created with the <paramref name="creator" /> function or not.</param>
        protected PrefixCreateChannelScope(Func<ChannelScope, ChannelNameAttribute, Type, IUntypedChannel> creator, Func<string, bool> selector)
        {
            m_creator = creator ?? throw new ArgumentNullException(nameof(creator));
            m_selector = selector ?? throw new ArgumentNullException(nameof(selector));
        }


        /// <summary>
        /// Hook method that allows custom channel creation
        /// </summary>
        /// <returns>The created channel or null, if there is no special handler.</returns>
        /// <param name="attribute">The attribute describing the channel to create.</param>
        /// <typeparam name="T">The type of data in the channel.</typeparam>
        protected override IChannel<T> TryCreateChannel<T>(ChannelNameAttribute attribute)
        {
            return m_selector(attribute.Name)
                ? (IChannel<T>)m_creator(this, attribute, typeof(T))
                : null;
        }

        /// <summary>
        /// Helper method to invoke the default channel instance creator
        /// </summary>
        /// <returns>The created channel.</returns>
        /// <param name="attribute">The attribute describing the channel to create.</param>
        protected IChannel<T> BaseCreateChannel<T>(ChannelNameAttribute attribute)
        {
            if (ParentScope != Root)
                return ParentScope.GetOrCreate<T>(attribute);
            
            return ChannelManager.CreateChannelForScope<T>(attribute);
        }
    }

    /// <summary>
    /// Creates a scope where some or all channels are wrapped as profiling channels
    /// </summary>
    public class ProfilerChannelScope : PrefixCreateChannelScope
    {
        /// <summary>
        /// Starts a new <see cref="T:CoCoL.ProfilerChannelScope"/>.
        /// </summary>
        /// <param name="prefix">The prefix for a channel name that indicates a profiling channel, or an empty string to make all channels custom created.</param>
        /// <param name="redirectunnamed">Set to <c>true</c> if unnamed channels should be created as custom channels.</param>
        public ProfilerChannelScope(string prefix = null, bool redirectunnamed = false)
            : base(Creator, prefix, redirectunnamed)
        {
        }

        /// <summary>
        /// Starts a new <see cref="T:CoCoL.ProfilerChannelScope"/>.
        /// </summary>
        /// <param name="names">The names of the channels to make profiled</param>
        public ProfilerChannelScope(params string[] names)
            : base(Creator, names)
        {
        }

        /// <summary>
        /// Starts a new <see cref="T:CoCoL.ProfilerChannelScope"/>.
        /// </summary>
        /// <param name="names">The names of the channels to make profiled</param>
        public ProfilerChannelScope(params INamedItem[] names)
            : base(Creator, names)
        {
        }

        /// <summary>
        /// Starts a new <see cref="T:CoCoL.ProfilerChannelScope"/>.
        /// </summary>
        /// <param name="selector">The selector method used too determine if a channel should be created as a profiling method or not.</param>
        public ProfilerChannelScope(Func<string, bool> selector)
            : base(Creator, selector)
        {
        }

        /// <summary>
        /// Handler method to create a profiling channel
        /// </summary>
        /// <returns>The profiling channel.</returns>
        /// <param name="scope">The scope calling the method.</param>
        /// <param name="attr">The channel attribute.</param>
        /// <param name="type">The type to create the channel for</param>
        private static IUntypedChannel Creator(ChannelScope scope, ChannelNameAttribute attr, Type type)
        {
            return 
                (IUntypedChannel)typeof(ProfilerChannelScope)
                .GetMethod(nameof(CreateProfilingChannel), System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.DeclaredOnly | System.Reflection.BindingFlags.NonPublic)
                .MakeGenericMethod(type)
                .Invoke(scope, new object[] { attr });
        }

        /// <summary>
        /// Creates a channel that is wrapped in a profiling channel instance
        /// </summary>
        /// <returns>The profiling-wrapped channel.</returns>
        /// <param name="attribute">The channel attribute.</param>
        /// <typeparam name="T">The channel type parameter.</typeparam>
        private IChannel<T> CreateProfilingChannel<T>(ChannelNameAttribute attribute)
        {
            return new ProfilingChannel<T>(this.BaseCreateChannel<T>(attribute));
        }
    }
}

