using System;

namespace CoCoL.Blocks
{
	/// <summary>
	/// Increments the input before sending it to the output.
	/// Note that this block only supports the long datatype
	/// </summary>
	public class Successor : BlockBase
	{
		private IChannel<long> m_input;
		private IChannel<long> m_output;

		public Successor(IChannel<long> input, IChannel<long> output)
		{
			if (input == null)
				throw new ArgumentNullException("input");
			if (output == null)
				throw new ArgumentNullException("output");

			m_input = input;
			m_output = output;
		}

		public async override void Run()
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

