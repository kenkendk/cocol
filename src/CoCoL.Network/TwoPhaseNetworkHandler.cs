using System;
using System.Threading.Tasks;

namespace CoCoL.Network
{
	/// <summary>
	/// A handler for Two-Phase commit over a network connection
	/// </summary>
	public class TwoPhaseNetworkHandler : ITwoPhaseOffer
	{
		/// <summary>
		/// The pending request this instances is connected to
		/// </summary>
		private PendingNetworkRequest m_pnr;
		/// <summary>
		/// The network client used for communication
		/// </summary>
		private NetworkClient m_nwc;
		/// <summary>
		/// A task source to return an accept or deny response for an offer
		/// </summary>
		private TaskCompletionSource<bool> m_offercallback;

		/// <summary>
		/// Lock for granting exclusive access
		/// </summary>
		private readonly AsyncLock m_lock = new AsyncLock();

		/// <summary>
		/// Initializes a new instance of the <see cref="CoCoL.Network.TwoPhaseNetworkHandler"/> class.
		/// </summary>
		/// <param name="pnr">The pending network request.</param>
		/// <param name="nwc">The network client.</param>
		public TwoPhaseNetworkHandler(PendingNetworkRequest pnr, NetworkClient nwc)
		{
			m_pnr = pnr;
			m_nwc = nwc;
		}

		/// <summary>
		/// Method to call when accepting an offer
		/// </summary>
		internal void Accepted()
		{
			m_offercallback.SetResult(true);
		}

		/// <summary>
		/// Method to call when denying an offer
		/// </summary>
		internal void Denied()
		{
			m_offercallback.SetResult(false);
		}

		#region ITwoPhaseOffer implementation
		/// <summary>
		/// Starts the two-phase sequence
		/// </summary>
		/// <param name="caller">The offer initiator.</param>
		public async Task<bool> OfferAsync(object caller)
		{
			using (await m_lock.LockAsync())
			{
				if (m_offercallback != null)
					throw new InvalidProgramException("Offer callback was already set?");
				
				m_offercallback = new TaskCompletionSource<bool>();

				await m_nwc.WriteAsync(new PendingNetworkRequest(
						m_pnr.ChannelID, 
						m_pnr.ChannelDataType, 
						m_pnr.RequestID, 
						m_pnr.SourceID,
						new DateTime(0),
						NetworkMessageType.OfferRequest,
						null,
						true
					));

				var res = await m_offercallback.Task.ConfigureAwait(false);
				m_offercallback = null;

				return res;
			}
		}
		/// <summary>
		/// Commits the two-phase sequence
		/// </summary>
		/// <param name="caller">The offer initiator.</param>
		public async Task CommitAsync(object caller)
		{
			using (await m_lock.LockAsync())
				await m_nwc.WriteAsync(new PendingNetworkRequest(
					m_pnr.ChannelID, 
					m_pnr.ChannelDataType, 
					m_pnr.RequestID, 
					m_pnr.SourceID,
					new DateTime(0),
					NetworkMessageType.OfferCommitRequest,
					null,
					true
				));
		}
		/// <summary>
		/// Cancels the two-phase sequence
		/// </summary>
		/// <param name="caller">The offer initiator.</param>
		public async Task WithdrawAsync(object caller)
		{
			using (await m_lock.LockAsync())
				await m_nwc.WriteAsync(new PendingNetworkRequest(
					m_pnr.ChannelID, 
					m_pnr.ChannelDataType, 
					m_pnr.RequestID, 
					m_pnr.SourceID,
					new DateTime(0),
					NetworkMessageType.OfferWithdrawRequest,
					null,
					true
				));
		}
		#endregion
	}
}

