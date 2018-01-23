using System;
using System.Threading.Tasks;

namespace CoCoL.Blocks
{
	/// <summary>
	/// The prefix process outputs a value a number of times,
	/// and then becomes an identity process
	/// </summary>
	public class Prefix<T> : BlockBase
	{
		private readonly IReadChannel<T> m_input;
		private readonly IWriteChannel<T> m_output;
		private readonly T m_value;
		private long m_repeat;

        /// <summary>
        /// Initializes a new instance of the <see cref="T:CoCoL.Blocks.Prefix`1"/> class.
        /// </summary>
        /// <param name="input">The input channel.</param>
        /// <param name="output">The output channel.</param>
        /// <param name="value">The initial value to emit.</param>
        /// <param name="repeat">The number of copies to initially emit.</param>
		public Prefix(IReadChannel<T> input, IWriteChannel<T> output, T value, long repeat = 1)
		{
            m_input = input ?? throw new ArgumentNullException(nameof(input));
			m_output = output ?? throw new ArgumentNullException(nameof(output));
			m_value = value;
			m_repeat = repeat;
		}

        /// <summary>
        /// Runs the process.
        /// </summary>
        /// <returns>An awaitable task.</returns>
		public async override Task RunAsync()
		{
			try
			{
				while(m_repeat-- > 0)
					await m_output.WriteAsync(m_value);
					
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

