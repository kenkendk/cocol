using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using CoCoL;
using CoCoL.Network;
using System.Collections.Generic;

namespace Concordance
{
	/// <summary>
	/// Static helper class for choosing configuration
	/// </summary>
	public class Config
	{
		/// <summary>
		/// The file to read
		/// </summary>
		[CommandlineOption("The file to read")]
		public static string Filename = "input.txt";

		/// <summary>
		/// The file to write
		/// </summary>
		[CommandlineOption("The file to write")]
		public static string Output = null;

		/// <summary>
		/// The file to read
		/// </summary>
		[CommandlineOption("The maximum length of words to process")]
		public static int MaxLength = 6;

		/// <summary>
		/// The file to read
		/// </summary>
		[CommandlineOption("The minimum number of times a word must be present")]
		public static int MinOccurrence = 2;

		/// <summary>
		/// Set to use the Groovy version instead
		/// </summary>
		[CommandlineOption("Set this flag to use the sequential Groovy version", longname: "groovy")]
		public static bool UseGroovyVersion = false;

		/// <summary>
		/// Parses the commandline args
		/// </summary>
		/// <param name="args">The commandline arguments.</param>
		public static bool Parse(string[] args)
		{
			return SettingsHelper.Parse<Config>(args.ToList(), null);
		}

		/// <summary>
		/// Returns the config object as a human readable string
		/// </summary>
		/// <returns>The string.</returns>
		public static string AsString()
		{
			return SettingsHelper.AsString<Config>(null);
		}
	}

	public struct WordLocation
	{
		public long Line;
		public long Pos;
	}

	public struct WordEntry
	{
		public long Line;
		public long Pos;
		public string Word;
	}

	public class GroovyConcordance
	{
		/// <summary>
		/// Implementation of the Tokenization as used in the Groovy Concordance benchmark
		/// </summary>
		/// <param name="filename">The file to read from.</param>
		public static IEnumerable<WordEntry> TokenizeGroovy(string filename)
		{
			var EndTrim = new [] { ',', '.', ';', ':', '?', '!', '\'', '"', '_', '}', ')' };
			var StartTrim = new [] { '\'', '"', '_', '\t', '{', '(' };

			var lineno = 0L;
			string line;
			using (var rd = new StreamReader(filename, System.Text.Encoding.UTF8, true))
				while ((line = rd.ReadLine()) != null)
				{
					lineno++;
					foreach (var word in line.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries))
					{
						var cleaned = word.TrimStart(StartTrim).TrimEnd(EndTrim).ToLowerInvariant();
						if (!string.IsNullOrWhiteSpace(cleaned))
							yield return new WordEntry()
							{
								Line = lineno,
								Word = cleaned
							};
					}
				}
		}

		/// <summary>
		/// Implementation that follows the Groovy demo
		/// </summary>
		public static void SequentialGroovy()
		{
			var list = TokenizeGroovy(Config.Filename).ToArray();
			var wordBuffer = list.Select(x => x.Word).ToArray();
			var intValueList = wordBuffer.Select(x => x.Sum(y => y)).ToArray();

			var sequenceLists = Enumerable.Range(0, Config.MaxLength).Select(x => new List<int>()).ToArray();
			var valueIndicesMaps = Enumerable.Range(0, Config.MaxLength).Select(x => new Dictionary<int, List<int>>()).ToArray();
			var wordMaps = Enumerable.Range(0, Config.MaxLength).Select(x => new Dictionary<string, List<int>>()).ToArray();

			for (var strLen = 0; strLen < Config.MaxLength; strLen++)
			{
				var sequenceList = sequenceLists[strLen];
				var valueIndicesMap = valueIndicesMaps[strLen];
				var wordMap = wordMaps[strLen];

				for (var w = 0; w < intValueList.Length; w++)
				{
					var sum = intValueList.Skip(w).Take(strLen + 1).Sum();
					sequenceList.Add(sum);
				}

				var index = 0;
				foreach (var v in sequenceList)
				{
					List<int> indexList;
					if (!valueIndicesMap.TryGetValue(v, out indexList))
						indexList = valueIndicesMap[v] = new List<int>();
					indexList.Add(index);
					index++;
				}

				var sequenceValues = valueIndicesMap.Keys;
				foreach (var sv in sequenceValues)
				{
					var indexList = valueIndicesMap[sv];
					foreach (var il in indexList)
					{
						var wordKeyList = string.Join(" ", wordBuffer.Skip(il).Take(strLen + 1));
						List<int> wordMapEntry;
						if (!wordMap.TryGetValue(wordKeyList, out wordMapEntry))
							wordMapEntry = wordMap[wordKeyList] = new List<int>();
						wordMapEntry.Add(il);
					}
				}

				using (var o = string.IsNullOrWhiteSpace(Config.Output) ? Console.Out : new StreamWriter(Config.Output))
					foreach (var k in wordMap)
					{
						if (k.Value.Count > Config.MinOccurrence)
							o.WriteLine("{0}, {1}, {2}", k.Key, k.Value.Count, string.Join(";", k.Value.Select(x => x.ToString())));
					}

			}
		}
	}

	class MainClass
	{
		/// <summary>
		/// Value used to delimit sentences
		/// </summary>
		public const string SENTENCE_TERMINATOR = ".";
		
		/// <summary>
		/// Tokenizes the input and outputs words and punctuations
		/// </summary>
		/// <param name="filename">The file to read from.</param>
		public static IEnumerable<WordEntry> Tokenize(string filename)
		{
			var lineno = 0L;
			var hassentpunctuation = false;
			var re = new Regex("(?<word>[a-z\']+)|(?<sentencedelimiter>[,\\.;:\\?!\"_\\{\\}\\(\\)])", RegexOptions.IgnoreCase);
			string line;
			using (var rd = new StreamReader(filename, System.Text.Encoding.UTF8, true))
				while ((line = rd.ReadLine()) != null)
				{
					lineno++;
					foreach (Match word in re.Matches(line))
					{
						if (word.Groups["sentencedelimiter"].Success)
						{
							if (!hassentpunctuation)
							{
								hassentpunctuation = true;
								yield return new WordEntry()
								{
									Line = lineno,
									Pos = word.Index,
									Word = SENTENCE_TERMINATOR
								};
							}
						}
						else 
						{
							yield return new WordEntry()
							{
								Line = lineno,
								Pos = word.Index,
								Word = word.Value
							};

							hassentpunctuation = false;
						}
					}
				}

			Console.WriteLine("Completed enumerator");
		}

		/// <summary>
		/// Reads in words one at a time, and outputs sentences of the desired word length
		/// </summary>
		/// <returns>The awaitable task.</returns>
		/// <param name="wordlength">The length of the words to emit.</param>
		/// <param name="input">The input channel where words are read from.</param>
		/// <param name="output">The output channel where words are written to.</param>
		public static async Task EmitNWordSentences(int wordlength, IReadChannel<WordEntry> input, IWriteChannel<WordEntry> output)
		{
			var buffer = new List<WordEntry>();

			while (true)
			{
				var data = await input.ReadAsync();

				// If sentence ends, reset buffer state
				if (data.Word == SENTENCE_TERMINATOR)
				{
					buffer.Clear();
				}
				else
				{
					// First word copies data
					buffer.Add(data);

					// Check to see if we have enough to emit a sentence
					if (buffer.Count == wordlength)
					{
						await output.WriteAsync(new WordEntry() {
							Line = buffer.First().Line,
							Pos = buffer.First().Pos,
							Word = string.Join(" ", buffer.Select(x => x.Word))
						});
						
						// Prepare for next word
						buffer.RemoveAt(0);
					}
				}
			}
		}

		/// <summary>
		/// The counter method that counts the occurence of each string
		/// </summary>
		/// <param name="input">The input channel.</param>
		/// <param name="output">The output channel.</param>
		public static async Task Counter(IReadChannel<WordEntry> input, IWriteChannel<Dictionary<string, List<WordLocation>>> output)
		{
			var map = new Dictionary<string, List<WordLocation>>();
			try
			{
				while (true)
				{
					List<WordLocation> lst;
					var data = await input.ReadAsync();
					if (!map.TryGetValue(data.Word, out lst))
						lst = map[data.Word] = new List<WordLocation>();
					
					lst.Add(new WordLocation() { Line = data.Line, Pos = data.Pos });
				}
			}
			catch (Exception ex)
			{
				if (ex.IsRetiredException())
					await output.WriteAsync(map);
			}
		}

		/// <summary>
		/// Combines the input maps into a single map
		/// </summary>
		/// <returns>The combined map</returns>
		/// <param name="input">The input data.</param>
		public static Task<Dictionary<string, List<WordLocation>>> Combiner(Dictionary<string, List<WordLocation>>[] input)
		{
			var map = new Dictionary<string, List<WordLocation>>();
			foreach (var m in input)
				foreach(var item in m)
				{
					List<WordLocation> lst;
					if (map.TryGetValue(item.Key, out lst))
						lst.AddRange(item.Value);
					else
						map[item.Key] = item.Value;
				}

			return Task.FromResult(map);
		}

		/// <summary>
		/// Sorts the input list based on occurrence count
		/// </summary>
		/// <returns>A soted output list</returns>
		/// <param name="input">The unsorted input list.</param>
		public static Task<KeyValuePair<string, List<WordLocation>>[]> Sorter(Dictionary<string, List<WordLocation>> input)
		{
			return Task.FromResult(
				input.Where(x => x.Value.Count > Config.MinOccurrence).OrderByDescending(x => x.Value.Count).ToArray()
			);
		}

		/// <summary>
		/// Writes all output to a file or the console
		/// </summary>
		/// <returns>The to output.</returns>
		/// <param name="input">Input.</param>
		public static Task DumpToOutput(IReadChannel<KeyValuePair<string, List<WordLocation>>[]> input)
		{
			return AutomationExtensions.RunTask(
				new { input = input },
				async (self) =>
				{
					using (var o = string.IsNullOrWhiteSpace(Config.Output) ? Console.Out : new StreamWriter(Config.Output))
						while (true)
						{
							var data = await self.input.ReadAsync();
							foreach(var line in data)
								o.WriteLine("{0}, {1}, {2}", line.Key, line.Value.Count, string.Join(", ", line.Value.Select(x => string.Format("{0}:{1}", x.Line, x.Pos))));
						}
				}
			);
		}

		public static void Main(string[] args)
		{
			if (!Config.Parse(args))
				return;

			if (string.IsNullOrWhiteSpace(Config.Filename))
			{
				Console.WriteLine("No filename specified, please use --filename=<filename.txt>");
				return;
			}

			Config.Filename = Environment.ExpandEnvironmentVariables(Config.Filename.Replace("~", "%HOME%"));

			if (!File.Exists(Config.Filename))
			{
				Console.WriteLine("File not found: {0}", Config.Filename);
				return;
			}

			if (Config.UseGroovyVersion)
			{
				GroovyConcordance.SequentialGroovy();
				return;
			}

			var wordstreamsource = ChannelManager.CreateChannel<WordEntry>();
			var wordstreamtargets = Enumerable.Range(0, Config.MaxLength).Select(x => ChannelManager.CreateChannel<WordEntry>()).ToArray();
			var nwordsources = Enumerable.Range(0, Config.MaxLength).Select(x => ChannelManager.CreateChannel<WordEntry>()).ToArray();
			var wordmapsources = Enumerable.Range(0, Config.MaxLength).Select(x => ChannelManager.CreateChannel<Dictionary<string, List<WordLocation>>>()).ToArray();
			var wordmaptarget = ChannelManager.CreateChannel<Dictionary<string, List<WordLocation>>[]>();
			var unsortedlist = ChannelManager.CreateChannel<Dictionary<string, List<WordLocation>>>();
			var sortedlist = ChannelManager.CreateChannel<KeyValuePair<string, List<WordLocation>>[]>();
			var i = 1;


			Task.WhenAll(
				// Wire up input data
				Skeletons.GeneratorAsync(Tokenize(Config.Filename), wordstreamsource),

				// Send to each n-word generator
				Skeletons.BroadcastAsync(wordstreamsource, wordstreamtargets),

				// Start the n-word generators
				Skeletons.ParallelAsync((a, b) => EmitNWordSentences(i++, a, b), wordstreamtargets, nwordsources),

				// Start the collection workers
				Skeletons.ParallelAsync((a, b) => Counter(a, b), nwordsources, wordmapsources),

				// Join the results
				Skeletons.GatherAllAsync(wordmapsources, wordmaptarget),

				// Combine all maps into a single map
				Skeletons.WrapperAsync(Combiner, wordmaptarget, unsortedlist),

				// Sort the list based on usage
				Skeletons.WrapperAsync(Sorter, unsortedlist, sortedlist),

				// Write the list to a file
				DumpToOutput(sortedlist)

			).WaitForTaskOrThrow();
		}
	}
}
