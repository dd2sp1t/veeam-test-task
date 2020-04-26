using System;
using System.Configuration;
using FileConverter;

namespace VeeamTest
{
	internal class Program
	{
		private static Int32 Main()
		{
			Console.WriteLine("To compress/decompress file type command:");
			Console.WriteLine("[action] [source file] [result file]\n");

			String command = Console.ReadLine();

			try
			{
				ParseAndExecute(command);
			}
			catch (Exception exception)
			{
				Console.WriteLine($"\n{exception}");
				return 1;
			}

			return 0;
		}

		private static void ParseAndExecute(String command)
		{
			String[] words = command.Split(" ");

			String action;
			String sourceFile;
			String resultFile;

			if (words == null || words.Length != 3)
				throw new ArgumentException("Command does not match pattern.");

			action = words[0];
			sourceFile = words[1];
			resultFile = words[2];

			if (action != "compress" && action != "decompress")
				throw new ArgumentException($"Unknown [action] parameter - {action}.");

			Int32 blockSize;

			try
			{
				blockSize = Int32.Parse(ConfigurationManager.AppSettings["blockSize"]);
			}
			catch (ArgumentNullException exception)
			{
				throw new Exception("Could not find size data block parameter in config file.", exception);
			}
			catch (Exception excetpion)
			{
				throw new Exception("Could not parse size data block parameter. Check config file.", excetpion);
			}

			if (blockSize <= 0)
				throw new Exception($"Invalid value of size data block parameter - {blockSize}. Check config file.");

			using (CFileConverter converter = CFileConverter.Create(sourceFile, resultFile, blockSize))
			{
				switch (action)
				{
					case "compress":
						converter.Compress();
						break;
					case "decompress":
						converter.Decompress();
						break;
				}
			}
		}
	}
}