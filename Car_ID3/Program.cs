using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Car_ID3
{
	class Program
	{
		static void Main(string[] args)
		{
			var metadata = ReadMetaData("car_data\\car.c45-names");
			var instances = ReadInstances(metadata, "car_data\\car.data");
		}

		static (string[] classValues, TrainAttribute[] attributes) ReadMetaData(string filename)
		{
			using (var file = File.OpenRead(filename))
			using (var reader = new StreamReader(file))
			{
				var classValues = new string[0];
				var attributes = new List<TrainAttribute>();
				while (!reader.EndOfStream)
				{
					var line = reader.ReadLine();
					if (line.StartsWith("| class values"))
					{
						while (reader.Peek() != '|' && !reader.EndOfStream)
						{
							line = reader.ReadLine().Trim();
							if (line.Length > 0) { classValues = line.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries).Select(s => s.Trim()).ToArray(); }
						}
					}
					else if (line.StartsWith("| attributes"))
					{
						while (reader.Peek() != '|' && !reader.EndOfStream)
						{
							var attr = TrainAttribute.ReadFromStream(reader);
							if (attr != null) attributes.Add(attr);
						}
					}
				}
				return (classValues, attributes.ToArray());
			}
		}

		static int[][] ReadInstances((string[] classValues, TrainAttribute[] attributes) metadata, string filename)
		{
			var valueCount = metadata.attributes.Length + 1;
			using (var file = File.OpenRead(filename))
			using (var reader = new StreamReader(file))
			{
				var instances = new List<int[]>();
				while (!reader.EndOfStream)
				{
					var values = reader.ReadLine().Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries).Select(s => s.Trim()).ToArray();
					if (values.Length != valueCount) continue;
					instances.Add(Enumerable.Range(0, valueCount).Select(i => Array.IndexOf((i < valueCount - 1 ? metadata.attributes[i].PossibleValues : metadata.classValues), values[i])).ToArray());
				}
				return instances.ToArray();
			}
		}
	}
}
