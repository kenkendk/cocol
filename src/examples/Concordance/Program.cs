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
		/// Value used to delimit sentences
		/// </summary>
		public const string STREAM_TERMINATOR = "!";

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
								Word = word.Value.ToLowerInvariant()
							};

							hassentpunctuation = false;
						}
					}
				}

			yield return new WordEntry()
			{
				Line = lineno,
				Pos = 0,
				Word = STREAM_TERMINATOR
			};
		}

		/// <summary>
		/// Container class for keeping the state of the emiter class
		/// </summary>
		public class EmitNWordSentences
		{
			/// <summary>
			/// The current collected sequence of words
			/// </summary>
			private readonly List<WordEntry> buffer = new List<WordEntry>();

			/// <summary>
			/// The word sequence length
			/// </summary>
			private readonly int m_wordlength;

			/// <summary>
			/// Initializes a new instance of the <see cref="T:Concordance.MainClass.EmitNWordSentences"/> class.
			/// </summary>
			/// <param name="length">The word sequence length.</param>
			public EmitNWordSentences(int length)
			{
				m_wordlength = length;
			}

			/// <summary>
			/// Reads in words one at a time, and outputs sentences of the desired word length
			/// </summary>
			/// <returns>The awaitable task, with a value indicating if the output is useable.</returns>
			/// <param name="data">The input data word.</param>
			public Task<KeyValuePair<bool, WordEntry>> RunAsync(WordEntry data)
			{
				var res = new KeyValuePair<bool, WordEntry>();

				// If sentence ends, reset buffer state
				if (data.Word == SENTENCE_TERMINATOR)
				{
					buffer.Clear();
				}
				// If stream ends, send terminator
				else if (data.Word == STREAM_TERMINATOR)
				{
					buffer.Clear();
					res = new KeyValuePair<bool, WordEntry>(true, new WordEntry()
					{
						Line = data.Line,
						Pos = data.Pos,
						Word = STREAM_TERMINATOR
					});
				}
				else
				{
					// First word copies data
					buffer.Add(data);

					// Check to see if we have enough to emit a sentence
					if (buffer.Count == m_wordlength)
					{
						res = new KeyValuePair<bool, WordEntry>(true, new WordEntry()
						{
							Line = buffer.First().Line,
							Pos = buffer.First().Pos,
							Word = string.Join(" ", buffer.Select(x => x.Word))
						});

						// Prepare for next word
						buffer.RemoveAt(0);
					}
				}

				return Task.FromResult(res);
			}
		}

		/// <summary>
		/// The counter class that emits the collected counts
		/// </summary>
		public class Counter
		{
			private readonly Dictionary<string, List<WordLocation>> m_map = new Dictionary<string, List<WordLocation>>();

			public Task<KeyValuePair<bool, Dictionary<string, List<WordLocation>>>> RunAsync(WordEntry entry)
			{
				if (entry.Word == STREAM_TERMINATOR)
					return Task.FromResult(new KeyValuePair<bool, Dictionary<string, List<WordLocation>>>(true, m_map));

				List<WordLocation> lst;
				if (!m_map.TryGetValue(entry.Word, out lst))
					lst = m_map[entry.Word] = new List<WordLocation>();

				lst.Add(new WordLocation() { Line = entry.Line, Pos = entry.Pos });

				return Task.FromResult(new KeyValuePair<bool, Dictionary<string, List<WordLocation>>>(false, null));
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
		public static Task<IEnumerable<KeyValuePair<string, List<WordLocation>>>> Sorter(Dictionary<string, List<WordLocation>> input)
		{
			return Task.FromResult(
				input.Where(x => x.Value.Count > Config.MinOccurrence).OrderByDescending(x => x.Value.Count).AsEnumerable()
			);
		}

		/// <summary>
		/// Writes all output to a file or the console
		/// </summary>
		/// <returns>The to output.</returns>
		/// <param name="input">Input.</param>
		public static async Task DumpToOutput(IEnumerable<KeyValuePair<string, List<WordLocation>>> input)
		{
			using (var o = string.IsNullOrWhiteSpace(Config.Output) ? Console.Out : new StreamWriter(Config.Output))
				foreach(var line in input)
					await o.WriteLineAsync(string.Format("{0}, {1}, {2}", line.Key, line.Value.Count, string.Join(", ", line.Value.Select(x => string.Format("{0}:{1}", x.Line, x.Pos)))));
		}

		/// <summary>
		/// Explicitly instanciate all channels and wire them up
		/// </summary>
		/// <returns>The channels.</returns>
		public static Task ExplicitChannels()
		{
			var sentenceemitters = Enumerable.Range(0, Config.MaxLength).Select(x => (Func<WordEntry, Task<KeyValuePair<bool, WordEntry>>>)new EmitNWordSentences(x + 1).RunAsync).ToArray();
			var counteremitters = Enumerable.Range(0, Config.MaxLength).Select(x => (Func<WordEntry, Task<KeyValuePair<bool, Dictionary<string, List<WordLocation>>>>>)new Counter().RunAsync).ToArray();

			var wordstreamsource = ChannelManager.CreateChannel<WordEntry>();
			var wordstreamtargets = Enumerable.Range(0, Config.MaxLength).Select(x => ChannelManager.CreateChannel<WordEntry>()).ToArray();
			var nwordsources = Enumerable.Range(0, Config.MaxLength).Select(x => ChannelManager.CreateChannel<WordEntry>()).ToArray();
			var wordmapsources = Enumerable.Range(0, Config.MaxLength).Select(x => ChannelManager.CreateChannel<Dictionary<string, List<WordLocation>>>()).ToArray();
			var wordmaptarget = ChannelManager.CreateChannel<Dictionary<string, List<WordLocation>>[]>();
			var unsortedlist = ChannelManager.CreateChannel<Dictionary<string, List<WordLocation>>>();
			var sortedlist = ChannelManager.CreateChannel<IEnumerable<KeyValuePair<string, List<WordLocation>>>>();

			return Task.WhenAll(
				// Wire up input data
				Skeletons.DataSourceAsync(Tokenize(Config.Filename), wordstreamsource),

				// Send to each n-word generator
				Skeletons.BroadcastAsync(wordstreamsource, wordstreamtargets),

				// Start the n-word generators
				Skeletons.ParallelAsync(sentenceemitters, wordstreamtargets, nwordsources),

				// Start the collection workers
				Skeletons.ParallelAsync(counteremitters, nwordsources, wordmapsources),

				// Join the results
				Skeletons.GatherAllAsync(wordmapsources, wordmaptarget),

				// Combine all maps into a single map
				Skeletons.WrapperAsync(Combiner, wordmaptarget, unsortedlist),

				// Sort the list based on usage
				Skeletons.WrapperAsync(Sorter, unsortedlist, sortedlist),

				// Write the list to a file
				Skeletons.DataSinkAsync(DumpToOutput, sortedlist)

			);
		}

		/// <summary>
		/// Pass channels forward
		/// </summary>
		/// <returns>The channels.</returns>
		public static Task SkeletonCallback()
		{
			var sentenceemitters = Enumerable.Range(0, Config.MaxLength).Select(x => (Func<WordEntry, Task<KeyValuePair<bool, WordEntry>>>)new EmitNWordSentences(x + 1).RunAsync).ToArray();
			var counteremitters = Enumerable.Range(0, Config.MaxLength).Select(x => (Func<WordEntry, Task<KeyValuePair<bool, Dictionary<string, List<WordLocation>>>>>)new Counter().RunAsync).ToArray();

			return Task.WhenAll(
				SkeletonCallbacks.DataSourceAsync(
					Tokenize(Config.Filename),
					a => SkeletonCallbacks.BroadcastAsync(
						a,
						Config.MaxLength,
						b => SkeletonCallbacks.ParallelAsync(
							sentenceemitters,
							b,
							cx => SkeletonCallbacks.ParallelAsync(
								counteremitters,
								cx,
								d => SkeletonCallbacks.GatherAllAsync(
									d,
									e => SkeletonCallbacks.WrapperAsync(
										Combiner,
										e,
										f => SkeletonCallbacks.WrapperAsync(
											Sorter,
											f,
											g => Skeletons.DataSinkAsync(
												DumpToOutput,
												g
											)
										)
									)
								)
							)
						)
					)
				)
			);
		}

		public static Task SkeletonWithReturn()
		{
			var sentenceemitters = Enumerable.Range(0, Config.MaxLength).Select(x => (Func<WordEntry, Task<KeyValuePair<bool, WordEntry>>>)new EmitNWordSentences(x + 1).RunAsync).ToArray();
			var counteremitters = Enumerable.Range(0, Config.MaxLength).Select(x => (Func<WordEntry, Task<KeyValuePair<bool, Dictionary<string, List<WordLocation>>>>>)new Counter().RunAsync).ToArray();

			return
				Skeletons.DataSinkAsync(DumpToOutput,
						SkeletonReturn.WrapperAsync(Sorter,
							SkeletonReturn.WrapperAsync(Combiner,
								SkeletonReturn.GatherAllAsync(
									SkeletonReturn.ParallelAsync(counteremitters,
										 SkeletonReturn.ParallelAsync(sentenceemitters,
											SkeletonReturn.BroadcastAsync(Config.MaxLength,
											  SkeletonReturn.DataSourceAsync(Tokenize(Config.Filename))
											 )
										)
									)
							   )
						   )
					   )
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

			//ExplicitChannels().WaitForTaskOrThrow();

			//SkeletonCallback().WaitForTaskOrThrow();

			SkeletonWithReturn().WaitForTaskOrThrow();
		}
	}
}
