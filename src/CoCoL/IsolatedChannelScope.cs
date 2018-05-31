using System;
using System.Collections.Generic;
using System.Linq;

namespace CoCoL
{
	/// <summary>
	/// A scope that does not inherit channels from its parent scope
	/// </summary>
	public class IsolatedChannelScope : ChannelScope
	{
		/// <summary>
		/// Constructs a new isolated channel scope
		/// </summary>
		public IsolatedChannelScope()
			: base(true)
		{
		}

		/// <summary>
		/// Constructs a new isolated channel scope
		/// </summary>
		/// <param name="inheritedChannelNames">List of channels to inherit from the parent scope.</param>
		public IsolatedChannelScope(IEnumerable<string> inheritedChannelNames)
			: base(true)
		{
			SetupInheritedChannels(inheritedChannelNames);
		}

		/// <summary>
		/// Constructs a new isolated channel scope
		/// </summary>
		/// <param name="inheritedChannelNames">List of channels to inherit from the parent scope.</param>
		public IsolatedChannelScope(params string[] inheritedChannelNames)
			: base(true)
		{
			SetupInheritedChannels(inheritedChannelNames);
		}

		/// <summary>
		/// Constructs a new isolated channel scope
		/// </summary>
		/// <param name="inheritedChannelNames">List of channels to inherit from the parent scope.</param>
		public IsolatedChannelScope(IEnumerable<INamedItem> inheritedChannelNames)
			: base(true)
		{
			if (inheritedChannelNames != null)
				SetupInheritedChannels(from n in inheritedChannelNames where n != null select n.Name);
		}

		/// <summary>
		/// Constructs a new isolated channel scope
		/// </summary>
		/// <param name="inheritedChannelNames">List of channels to inherit from the parent scope.</param>
		public IsolatedChannelScope(params INamedItem[] inheritedChannelNames)
			: base(true)
		{
			if (inheritedChannelNames != null)
				SetupInheritedChannels(from n in inheritedChannelNames where n != null select n.Name);
		}

		/// <summary>
		/// Adds all inherited channels to the current scope,
		/// and disposes this instance if an exception is thrown
		/// </summary>
		/// <param name="names">List of channels to inherit from the parent scope.</param>
		protected void SetupInheritedChannels(IEnumerable<string> names)
		{
			try
			{
				if (names != null)
					foreach (var n in names)
						InjectChannelFromParent(n);
			}
			catch
			{
				this.Dispose();
				throw;
			}
		}

		/// <summary>
		/// Injects a channel into the current scope.
		/// </summary>
		/// <param name="name">The name of the channel to create.</param>
		/// <param name="channel">The channel to inject.</param>
		public void InjectChannel(string name, IRetireAbleChannel channel)
		{
			if (string.IsNullOrWhiteSpace(name))
				throw new ArgumentNullException(nameof(name));
			if (channel == null)
				throw new ArgumentNullException(nameof(channel));

			lock (__lock)
				m_lookup[name] = channel;
		}

		/// <summary>
		/// Injects a channel into the current scope, by looking in the parent scope.
		/// This is particularly useful in isolated scopes, to selectively forward channels
		/// </summary>
		/// <param name="names">The names of the channel to create.</param>
		/// <param name="parent">The scope to look in, <code>null</code> means the current parent</param>
		public void InjectChannelsFromParent(IEnumerable<string> names, ChannelScope parent = null)
		{
			foreach (var n in names)
				InjectChannelFromParent(n, parent);
		}

		/// <summary>
		/// Injects a channel into the current scope, by looking in the parent scope.
		/// This is particularly useful in isolated scopes, to selectively forward channels
		/// </summary>
		/// <param name="names">The name of the channel to create.</param>
		public void InjectChannelsFromParent(params string[] names)
		{
			foreach (var n in names)
				InjectChannelFromParent(n);
		}

		/// <summary>
		/// Injects a channel into the current scope, by looking in the parent scope.
		/// This is particularly useful in isolated scopes, to selectively forward channels
		/// </summary>
		/// <param name="name">The name of the channel to create.</param>
		/// <param name="parent">The scope to look in, <code>null</code> means the current parent</param>
		public void InjectChannelFromParent(string name, ChannelScope parent = null)
		{
			if (string.IsNullOrWhiteSpace(name))
				throw new ArgumentNullException(nameof(name));
			parent = parent ?? this.ParentScope;

			lock (__lock)
			{
				var c = parent.RecursiveLookup(name);
                m_lookup[name] = c ?? throw new Exception($"No channel with the name \"{name}\" was found in the parent scope");
			}
		}
	}

    /// <summary>
    /// Creates a scope that disables custom channel creation
    /// </summary>
    public class CustomCreationIsolationScope : ChannelScope
    {
        /// <summary>
        /// Starts a new instance of the <see cref="T:CoCoL.CustomCreationIsolationScope"/> class.
        /// </summary>
        public CustomCreationIsolationScope()
            : base(true)
        {
        }

        /// <summary>
        /// Returns the default channel type
        /// </summary>
        /// <returns>The created channel.</returns>
        /// <param name="attribute">The channel attribute.</param>
        /// <typeparam name="T">The channel type parameter.</typeparam>
        protected override IChannel<T> TryCreateChannel<T>(ChannelNameAttribute attribute)
        {
            return ChannelManager.CreateChannelForScope<T>(attribute);
        }
    }
}

