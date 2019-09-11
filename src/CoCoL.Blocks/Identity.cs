using System;
using System.Threading.Tasks;

namespace CoCoL.Blocks
{
	/// <summary>
	/// Implementation of the identity process,
	/// a process that copies input to its output
	/// </summary>
	public class Identity<T> : BlockBase
	{
        /// <summary>
        /// The input channel
        /// </summary>
		private readonly IReadChannel<T> m_input;
        /// <summary>
        /// The output channel
        /// </summary>
		private readonly IWriteChannel<T> m_output;

        /// <summary>
        /// Initializes a new instance of the <see cref="T:CoCoL.Blocks.Identity`1"/> process.
        /// </summary>
        /// <param name="input">The input channel.</param>
        /// <param name="output">The output channel.</param>
		public Identity(IReadChannel<T> input, IWriteChannel<T> output)
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
					await m_output.WriteAsync(await m_input.ReadAsync());
			}
			catch (RetiredException)
			{
				m_input.Retire();
				m_output.Retire();
			}
		}
	}
}

