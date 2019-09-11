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
        /// <summary>
        /// The input channel
        /// </summary>
		private readonly IReadChannel<T> m_input;
        /// <summary>
        /// One output channel
        /// </summary>
		private readonly IWriteChannel<T> m_outputA;
        /// <summary>
        /// Another output channel
        /// </summary>
		private readonly IWriteChannel<T> m_outputB;

        /// <summary>
        /// Initializes a new instance of the <see cref="T:CoCoL.Blocks.Delta`1"/> process.
        /// </summary>
        /// <param name="input">The input channel.</param>
        /// <param name="outputA">Output channel A.</param>
        /// <param name="outputB">Output channel B.</param>
		public Delta(IReadChannel<T> input, IWriteChannel<T> outputA, IWriteChannel<T> outputB)
		{
            m_input = input ?? throw new ArgumentNullException(nameof(input));
			m_outputA = outputA ?? throw new ArgumentNullException(nameof(outputA));
			m_outputB = outputB ?? throw new ArgumentNullException(nameof(outputB));
		}

        /// <summary>
        /// Runs the process.
        /// </summary>
        /// <returns>An awaitable task.</returns>
		public async override Task RunAsync()
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

