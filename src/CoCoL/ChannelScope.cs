using System;
using System.Collections.Generic;
using System.Linq;

namespace CoCoL
{
	public class ChannelScope : IEnumerable<KeyValuePair<string, IRetireAbleChannel>>, IDisposable
	{
		public static readonly ChannelScope Root = new ChannelScope(null);

		public ChannelScope ParentScope { get; private set; }
		private Dictionary<string, IRetireAbleChannel> m_lookup = new Dictionary<string, IRetireAbleChannel>();

		private const string LOGICAL_CONTEXT_KEY = "CoCoL:AutoWireScope";

		public ChannelScope()
			: this(ChannelScope.Current)
		{
		}

		private ChannelScope(ChannelScope parent)
		{
			ParentScope = parent;
			Current = this;
		}

		public bool ContainsKey(string key)
		{
			if (m_lookup.ContainsKey(key))
				return true;
			return ParentScope == null ? false : ParentScope.ContainsKey(key);
		}

		public void Add(string key, IRetireAbleChannel value)
		{
			m_lookup.Add(key, value);
		}

		public bool Remove(string key)
		{
			throw new InvalidOperationException();
		}

		public bool TryGetValue(string key, out IRetireAbleChannel value)
		{
			if (m_lookup.TryGetValue(key, out value))
				return true;

			return ParentScope == null ? false : ParentScope.TryGetValue(key, out value);
		}

		public IRetireAbleChannel this[string index]
		{
			get
			{
				if (ParentScope == null)
					return m_lookup[index];
				else
				{
					IRetireAbleChannel value;
					if (m_lookup.TryGetValue(index, out value))
						return value;
					
					return ParentScope[index];
				}
			}
			set
			{
				if (value == null)
					throw new ArgumentNullException();
				m_lookup[index] = value;
			}
		}

		public IEnumerable<string> Keys
		{
			get
			{
				if (ParentScope == null)
					return m_lookup.Keys;
				else
					return m_lookup.Keys.Union(ParentScope.Keys);
			}
		}

		public IEnumerable<IRetireAbleChannel> Values
		{
			get
			{
				if (ParentScope == null)
					return m_lookup.Values;
				else
					return m_lookup.Values.Union(ParentScope.Values);
			}
		}


		#region IEnumerable implementation

		public IEnumerator<KeyValuePair<string, IRetireAbleChannel>> GetEnumerator()
		{
			if (ParentScope == null)
				return m_lookup.GetEnumerator();
			else
				return m_lookup.Union(ParentScope).GetEnumerator();
		}

		#endregion

		#region IEnumerable implementation

		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
		{
			return this.GetEnumerator();
		}

		#endregion

		#region IDisposable implementation

		public void Dispose()
		{
			if (Current == this)
				Current = this.ParentScope;
			m_lookup = null;
		}

		#endregion

#if PCL_BUILD
		private static bool __IsFirstUsage = true;
		private static ChannelScope __Current = null;
#endif

		public static ChannelScope Current
		{
			get 
			{ 
#if PCL_BUILD
				// TODO: Use AsyncLocal if targeting 4.6
				//var cur = new System.Threading.AsyncLocal<ChannelScope>();
				if (__IsFirstUsage)
				{
					__IsFirstUsage = false;
					System.Diagnostics.Debug.WriteLine("*Warning*: PCL does not provide a call context, so channel scoping does not work correctly for multithreaded use!");
				}

				var cur = __Current;
#else
				var cur = System.Runtime.Remoting.Messaging.CallContext.LogicalGetData(LOGICAL_CONTEXT_KEY) as ChannelScope;
#endif
				if (cur == null)
					return Current = Root;
				else
					return cur;
			}
			private set
			{
#if PCL_BUILD				
				__Current = value;
#else
				System.Runtime.Remoting.Messaging.CallContext.LogicalSetData(LOGICAL_CONTEXT_KEY, value);
#endif
			}
		}
	}
}

