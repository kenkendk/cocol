using System;
using System.Linq;
using NUnit.Framework;
using CoCoL;
using System.Threading.Tasks;

namespace UnitTest
{
	[TestFixture]
	public class MixedOperationTest
	{
		[Test]
		[ExpectedException(typeof(InvalidOperationException))]
		public void TestInvalidMultiAccessOperation()
		{
			try
			{
				var c1 = ChannelManager.CreateChannel<int>();
				MultiChannelAccess.ReadOrWriteAnyAsync(MultisetRequest<int>.Read(c1), MultisetRequest<int>.Write(1, c1)).WaitForTask().Wait();
			}
			catch(AggregateException aex)
			{
				if (aex.InnerExceptions.Count == 1)
					throw aex.InnerExceptions.First();
			}

		}

		[Test]
		public void TestMultiAccessOperation()
		{
			var c1 = ChannelManager.CreateChannel<int>();
			var c2 = ChannelManager.CreateChannel<int>();

			// Copy c2 + 1 => c1
			Func<Task> p1 = async() => {
				var val = await c2.ReadAsync();
				while(true) {
					var res = await MultiChannelAccess.ReadOrWriteAnyAsync(MultisetRequest<int>.Read(c2), MultisetRequest<int>.Write(val, c1));
					if (res.IsRead)
						val = res.Value + 1;
				}
			};

			// Copy c1 => c2
			Func<Task> p2 = async() => {
				var val = 1;
				for(var i = 0; i < 10; i++) {
					await c2.WriteAsync(val);
					val = await c1.ReadAsync();
				}

				c1.Retire();
				c2.Retire();

				if (val != 10)
					throw new InvalidProgramException("Bad counter!");
			};
				
			// Wait for shutdown
			try 
			{
				Task.WhenAll(p1(), p2()).WaitForTask().Wait();
			}
			catch(Exception ex)
			{
				// Filter out all ChannelRetired exceptions
				if (ex is AggregateException)
				{
					var rex = (from n in (ex as AggregateException).InnerExceptions
					       where !(n is RetiredException)
					       select n);

					if (rex.Count() == 1)
						throw rex.First();
					else if (rex.Count() != 0)
						throw new AggregateException(rex);						
				}
				else
					throw;
			}
		}

		[Test]
		public void TestMultiTypeOperation()
		{
			var c1 = ChannelManager.CreateChannel<int>();
			var c2 = ChannelManager.CreateChannel<string>();

			// Copy c2 + 1 => c1
			Func<Task> p1 = async() => {
				var val = int.Parse(await c2.ReadAsync());
				while(true) {
					var res = await UntypedMultiChannelAccess.ReadOrWriteAnyAsync(MultisetRequest<string>.Read(c2), MultisetRequest<int>.Write(val, c1));
					if (res.IsRead)
						val = int.Parse((string)res.Value) + 1;
				}
			};

			// Copy c1 => c2
			Func<Task> p2 = async() => {
				var val = 1;
				for(var i = 0; i < 10; i++) {
					await c2.WriteAsync(val.ToString());
					val = await c1.ReadAsync();
				}

				c1.Retire();
				c2.Retire();

				if (val != 10)
					throw new InvalidProgramException("Bad counter!");
			};

			// Wait for shutdown
			try 
			{
				Task.WhenAll(p1(), p2()).Wait();
			}
			catch(Exception ex)
			{
				// Filter out all ChannelRetired exceptions
				if (ex is AggregateException)
				{
					var rex = (from n in (ex as AggregateException).InnerExceptions
						where !(n is RetiredException)
						select n);

					if (rex.Count() == 1)
						throw rex.First();
					else if (rex.Count() != 0)
						throw new AggregateException(rex);						
				}
				else
					throw;
			}
		}

		[Test]
		public void TestMultiTypeReadWrite()
		{
			var c1 = ChannelManager.CreateChannel<int>();
			var c2 = ChannelManager.CreateChannel<string>();

			c1.WriteNoWait(1);
			c2.WriteNoWait("2");

			var r = UntypedMultiChannelAccess.ReadFromAnyAsync(c1.AsUntyped(), c2.AsUntyped()).WaitForTask().Result;
			if (r == null)
				throw new Exception("Unexpected null result");
			if (r.Channel != c1)
				throw new Exception("Unexpected read channel");
			
			if (!(r.Value is int))
				throw new Exception("Priority changed?");
			if ((int)r.Value != 1)
				throw new Exception("Bad value?");

			r = UntypedMultiChannelAccess.ReadFromAnyAsync(c1.RequestRead(), c2.RequestRead()).WaitForTask().Result;
			if (r == null)
				throw new Exception("Unexpected null result");
			if (r.Channel != c2)
				throw new Exception("Unexpected read channel");
			if (!(r.Value is string))
				throw new Exception("Priority changed?");
			if ((string)r.Value != "2")
				throw new Exception("Bad value?");

			var t = UntypedMultiChannelAccess.WriteToAnyAsync(c1.AsUntyped().RequestWrite(4));
			if (c1.Read() != 4)
				throw new Exception("Bad value?");

			t.WaitForTask().Wait();
				
		}

	}
}

