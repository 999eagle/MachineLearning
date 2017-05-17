using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Car_ID3
{
	class TrainAttribute
	{
		public string Name { get; private set; }
		public string[] PossibleValues { get; private set; }

		public TrainAttribute(string name, string[] possibleValues)
		{
			Name = name;
			PossibleValues = possibleValues;
		}

		public static TrainAttribute ReadFromStream(StreamReader reader)
		{
			var line = reader.ReadLine();
			int idx;
			if ((idx = line.IndexOf(':')) == -1) return null;
			var name = line.Substring(0, idx);
			line = line.Substring(idx + 1);
			if ((idx = line.IndexOf('.')) == -1) return null;
			var values = line.Substring(0, idx).Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries).Select(s => s.Trim()).ToArray();
			return new TrainAttribute(name, values);
		}
	}
}
