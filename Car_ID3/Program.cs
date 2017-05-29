using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Car_ID3
{
	class Program
	{
		static void Main(string[] args)
		{
			var metadata = ReadMetaData("car_data\\car.c45-names");
			var instances = ReadInstances(metadata, "car_data\\car.data");
			Console.WriteLine("Read input");
			int split = instances.Length * 7 / 10;
			var trainer = new ID3Training<byte>(metadata.attributes, metadata.classValues, instances.Take(split).ToArray());
			var classificator = trainer.Train();
			Console.WriteLine("Generated tree");
			OutputTree(metadata, instances, trainer.GetRootNode(classificator), true);
			WriteStatistics(metadata, instances.Skip(split).ToArray(), classificator);
			Console.ReadLine();
		}

		static void WriteStatistics<T>((string[] classValues, TrainAttribute[] attributes) metadata, T[][] instances, IClassificator<T> classificator) where T : struct, IEquatable<T>
		{
			var test = instances.Select(i => (i, classificator.Classify(i)));
			int totalCorrect = 0;
			double weightedMacroPrec = 0;
			double weightedMacroRec = 0;
			double weightedMacroF1 = 0;
			double macroPrec = 0;
			double macroRec = 0;
			double macroF1 = 0;
			var cv = ValueHelper<T>.Convert;
			for (int c = 0; c < metadata.classValues.Length; c++)
			{
				T ct = cv(c);
				int correct = test.Count(i => i.Item1.Last().Equals(ct) && i.Item2.Equals(ct));
				int actual = test.Count(i => i.Item1.Last().Equals(ct));
				int predicted = test.Count(i => i.Item2.Equals(ct));
				double precision = correct == 0 ? 0 : (double)correct / predicted;
				double recall = correct == 0 ? 0 : (double)correct / actual;
				double f1 = correct == 0 ? 0 : 2 * precision * recall / (precision + recall);
				double macroWeight = (double)actual / instances.Length;

				totalCorrect += correct;
				macroPrec += precision;
				macroRec += recall;
				macroF1 += f1;
				weightedMacroPrec += macroWeight * precision;
				weightedMacroRec += macroWeight * recall;
				weightedMacroF1 += macroWeight * f1;

				Console.WriteLine($"class {metadata.classValues[c]} precision: {precision} recall: {recall} f1 measure: {f1}");
			}
			macroPrec /= metadata.classValues.Length;
			macroRec /= metadata.classValues.Length;
			macroF1 /= metadata.classValues.Length;
			double weightedMacroPrecRecF1 = 2 * weightedMacroPrec * weightedMacroRec / (weightedMacroPrec + weightedMacroRec);
			double macroPrecRecF1 = 2 * macroPrec * macroRec / (macroPrec + macroRec);
			Console.WriteLine($"weighted macro precision: {weightedMacroPrec} weighted macro recall: {weightedMacroRec} corresponding f1 measure: {weightedMacroPrecRecF1}");
			Console.WriteLine($"macro precision: {macroPrec} macro recall: {macroRec} corresponding f1 measure: {macroPrecRecF1}");
			Console.WriteLine($"weighted macro f1 measure: {weightedMacroF1} macro f1 measure: {macroF1}");
			Console.WriteLine($"accuracy: {(double)totalCorrect / test.Count()}");
		}

		static void OutputTree<T>((string[] classValues, TrainAttribute[] attributes) metadata, T[][] instances, ID3Training<T>.Node rootNode, bool includeInstanceDistribution) where T : struct, IEquatable<T>
		{
			var C = ValueHelper<T>.ConvertBack;
			JObject jsonRoot = ConvertNode(rootNode);
			var serializer = new JsonSerializer();
			serializer.Formatting = Formatting.Indented;
			using (var stream = File.Open("tree.json", FileMode.Create, FileAccess.Write))
			using (var writer = new StreamWriter(stream))
			{
				serializer.Serialize(writer, jsonRoot);
			}

			JObject ConvertNode(ID3Training<T>.Node node)
			{
				var o = new JObject();
				if (includeInstanceDistribution)
				{
					o.Add("instances", new JArray(node.GetClasses(instances).Select(g => new JArray(new JValue(g.Count()), new JValue(metadata.classValues[C(g.Key)])))));
					o.Add("entropy", new JValue(node.Entropy));
				}
				if (node.TrainAttribute.HasValue)
				{
					var attr = metadata.attributes[node.TrainAttribute.Value];
					o.Add("attribute", new JValue(attr.Name));
					o.Add("children", new JArray(node.Children.Select(c => new JObject { { "attributeValue", attr.PossibleValues[C(c.attributeValue)] }, { "node", ConvertNode(c.node) } })));
				}
				else if (node.Class.HasValue)
				{
					o.Add("class", new JValue(metadata.classValues[C(node.Class.Value)]));
				}
				return o;
			}
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

		static byte[][] ReadInstances((string[] classValues, TrainAttribute[] attributes) metadata, string filename)
		{
			var valueCount = metadata.attributes.Length + 1;
			using (var file = File.OpenRead(filename))
			using (var reader = new StreamReader(file))
			{
				var instances = new List<byte[]>();
				while (!reader.EndOfStream)
				{
					var values = reader.ReadLine().Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries).Select(s => s.Trim()).ToArray();
					if (values.Length != valueCount) continue;
					instances.Add(Enumerable.Range(0, valueCount).Select(i => (byte)Array.IndexOf((i < valueCount - 1 ? metadata.attributes[i].PossibleValues : metadata.classValues), values[i])).ToArray());
				}
				return instances.ToArray();
			}
		}
	}
}
