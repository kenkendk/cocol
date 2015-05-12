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
		private IReadChannel<T> m_input;
		private IWriteChannel<T> m_output;

		public Identity(IReadChannel<T> input, IWriteChannel<T> output)
		{
			if (input == null)
				throw new ArgumentNullException("input");
			if (output == null)
				throw new ArgumentNullException("output");

			m_input = input;
			m_output = output;
		}

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

