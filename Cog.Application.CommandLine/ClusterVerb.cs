﻿using System;
using System.Collections.Generic;
using System.IO;
using CommandLine;
using SIL.Collections;
using SIL.Machine.Clusterers;

namespace SIL.Cog.Application.CommandLine
{
	[Verb("cluster", HelpText = "Cluster words")]
	public class ClusterVerb : CommonOptions
	{
		[Option('t', "threshhold", Default = 0.2, HelpText = "Distance threshhold for a cluster (between 0.0 and 1.0, higher for easier clustering)")]
		public double Threshhold { get; set; }

		public override int DoWork(TextReader inputStream, TextWriter outputStream, TextWriter errorStream)
		{
			int retcode = (int)ReturnCodes.Okay;
			SetUpProject();

			var distances = new Dictionary<UnorderedTuple<string, string>, double>();
			var allWords = new HashSet<string>();
			var errors = new Errors();

			foreach (string line in inputStream.ReadLines())
			{
				string[] words = line.Split(' '); // Format: word1 word2 score (where score is a floating-point number with 1.0 = 100% similarity)
				if (words.Length < 3)
				{
					errors.Add(line, "Each line should contain two words and one score, separated by spaces.");
					continue;
				}
				double score = 0;
				if (!Double.TryParse(words[2], out score))
				{
					errors.Add(line, "Could not parse score \"{0}\". Scores should be a number between 0 and 1.", words[2]);
					continue;
				}
					
				distances.Add(new UnorderedTuple<string, string>(words[0], words[1]), 1.0 - score);
				allWords.Add(words[0]);
				allWords.Add(words[1]);
			}
			var clusterer = new FlatUpgmaClusterer<string>((w1, w2) => distances[new UnorderedTuple<string, string>(w1, w2)], Threshhold);
			IEnumerable<Cluster<string>> clusters = clusterer.GenerateClusters(allWords);
			PrintResults(outputStream, clusters);
			if (!errors.OK)
			{
				errors.DumpToStream(errorStream);
				retcode = (int) ReturnCodes.InputError;
			}
			return retcode;
		}

		private static void PrintResults(TextWriter output, IEnumerable<Cluster<string>> clusters)
		{
			int groupnum = 0;
			foreach (var item in clusters)
			{
				groupnum++;
				output.WriteLine("{0} {1}", groupnum, String.Join(" ", item.DataObjects));
			}
		}
	}
}