using System;
using System.Threading.Tasks;

namespace CoCoL.Blocks
{
	/// <summary>
	/// Implementation of the Delta process,
	/// a process that copies its input to two outputs
	/// </summary>
	public class Delta<T> : BlockBase
	{
		private IReadChannel<T> m_input;
		private IWriteChannel<T> m_outputA;
		private IWriteChannel<T> m_outputB;

		public Delta(IReadChannel<T> input, IWriteChannel<T> outputA, IWriteChannel<T> outputB)
		{
			if (input == null)
				throw new ArgumentNullException("input");
			if (outputA == null)
				throw new ArgumentNullException("outputA");
			if (outputB == null)
				throw new ArgumentNullException("outputB");
			
			m_input = input;
			m_outputA = outputA;
			m_outputB = outputB;
		}

		public async override void Run()
		{

			try
			{
				while(true)
				{
					var r = await m_input.ReadAsync();
					await Task.WhenAll(
						m_outputA.WriteAsync(r),
						m_outputB.WriteAsync(r)
					);
				}
			}
			catch (RetiredException)
			{
				m_input.Retire();
				m_outputA.Retire();
				m_outputB.Retire();
			}
		}
	}
}

