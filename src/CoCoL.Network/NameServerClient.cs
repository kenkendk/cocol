using System;
using System.Threading.Tasks;
using System.Net;

namespace CoCoL.Network
{
	/// <summary>
	/// Implementation of a nameserver
	/// </summary>
	public class NameServerClient : SingleThreadedWorker
	{
		/// <summary>
		/// The log instance
		/// </summary>
		private static readonly log4net.ILog LOG = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

		/// <summary>
		/// The hostname returned by this instance
		/// </summary>
		private string m_hostname;
		/// <summary>
		/// The port returned by this instance
		/// </summary>
		private int m_port;

		/// <summary>
		/// Initializes a new instance of the <see cref="CoCoL.Network.NameServerClient"/> class.
		/// </summary>
		/// <param name="hostname">The hostname to return.</param>
		/// <param name="port">The port to return.</param>
		public NameServerClient(string hostname, int port)
		{
			m_hostname = hostname;
			m_port = port;
		}

		/// <summary>
		/// Locates the server that handles a specific channel ID
		/// </summary>
		/// <returns>The channel home details.</returns>
		/// <param name="channelid">The channelid to locate.</param>
		public Task<Tuple<string, int>> GetChannelHomeAsync(string channelid)
		{
			LOG.DebugFormat("Looking up remote end for {0}", channelid);
			return Task.FromResult(new Tuple<string, int>(m_hostname, m_port));
		}
	}
}

