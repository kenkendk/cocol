using System;
using System.Threading.Tasks;

namespace CoCoL.Blocks
{
	/// <summary>
	/// Increments the input before sending it to the output.
	/// Note that this block only supports the long datatype
	/// </summary>
	public class Successor : BlockBase
	{
		private readonly IReadChannel<long> m_input;
		private readonly IWriteChannel<long> m_output;

        /// <summary>
        /// Initializes a new instance of the <see cref="T:CoCoL.Blocks.Successor"/> process.
        /// </summary>
        /// <param name="input">The input channel.</param>
        /// <param name="output">The output channel.</param>
		public Successor(IReadChannel<long> input, IWriteChannel<long> output)
		{
            m_input = input ?? throw new ArgumentNullException(nameof(input));
			m_output = output ?? throw new ArgumentNullException(nameof(output));
		}

        /// <summary>
        /// Runs the process.
        /// </summary>
        /// <returns>An awaitable task.</returns>
		public async override Task RunAsync()
		{
			try
			{
				while (true)
					await m_output.WriteAsync(await m_input.ReadAsync() + 1);
			}
			catch (RetiredException)
			{
				m_input.Retire();
				m_output.Retire();
			}
		}
	}
}

