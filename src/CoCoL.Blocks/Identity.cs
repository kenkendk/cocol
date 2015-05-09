using System;

namespace CoCoL.Blocks
{
	/// <summary>
	/// Implementation of the identity process,
	/// a process that copies input to its output
	/// </summary>
	public class Identity<T> : BlockBase
	{
		private IChannel<T> m_input;
		private IChannel<T> m_output;

		public Identity(IChannel<T> input, IChannel<T> output)
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
					m_output.WriteAsync(await m_input.ReadAsync());
			}
			catch (RetiredException)
			{
				m_input.Retire();
				m_output.Retire();
			}
		}
	}
}

