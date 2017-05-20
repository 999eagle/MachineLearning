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
			var treeRoot = ID3Training(metadata, instances);
			Console.WriteLine("Generated tree");
			JObject jsonRoot = ConvertNode(treeRoot, true);
			var serializer = new JsonSerializer();
			serializer.Formatting = Formatting.Indented;
			using (var stream = File.OpenWrite("tree.json"))
			using (var writer = new StreamWriter(stream))
			{
				serializer.Serialize(writer, jsonRoot);
			}
			Console.ReadLine();

			JObject ConvertNode(Node node, bool includeInstanceDistribution)
			{
				var o = new JObject();
				if (includeInstanceDistribution)
				{
					o.Add("instances", new JArray(node.GetClasses(instances).Select(g => new JArray(new JValue(g.Count()), new JValue(metadata.classValues[g.Key])))));
				}
				if (node.TrainAttribute != -1)
				{
					var attr = metadata.attributes[node.TrainAttribute];
					o.Add("attribute", new JValue(attr.Name));
					o.Add("children", new JArray(node.Children.Select(c => new JObject { { "attributeValue", attr.PossibleValues[c.attributeValue] }, { "node", ConvertNode(c.node, includeInstanceDistribution) } })));
				}
				else if (node.Class != -1)
				{
					o.Add("class", new JValue(metadata.classValues[node.Class]));
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

		static Node ID3Training((string[] classValues, TrainAttribute[] attributes) metadata, int[][] instances)
		{
			var rootNode = new Node { InstanceIds = Enumerable.Range(0, instances.Length).Select(i => (ushort)i).ToArray() };
			rootNode.CalcEntropy(metadata.classValues.Length, instances);
			var openList = new List<Node> { rootNode };
			while (openList.Any())
			{
				var node = openList.First();
				openList.Remove(node);

				var possibleAttributes = Enumerable.Range(0, metadata.attributes.Length).Select(i => (sbyte)i).ToList();
				var parent = node.Parent;
				while (parent != null)
				{
					possibleAttributes.Remove(parent.TrainAttribute);
					parent = parent.Parent;
				}
				var classes = node.GetClasses(instances);
				if (classes.Count() == 1 || possibleAttributes.Count == 0)
				{
					// all instances in this node have the same class
					node.Class = classes.OrderByDescending(g => g.Count()).First().Key;
				}
				else
				{
					// multiple classes in this node
					// find attribute with the highest information gain
					double maxGain = double.MinValue;
					sbyte bestAttribute = 0;
					IEnumerable<(sbyte, Node)> bestChildren = null;
					foreach (var attr in possibleAttributes)
					{
						// generate child nodes for the current attribute
						var children = Enumerable.Range(0, metadata.attributes[attr].PossibleValues.Length).Select(v =>
						{
							var child = new Node
							{
								Parent = node,
								InstanceIds = node.InstanceIds.Where(i => instances[i][attr] == v).ToArray()
							};
							child.CalcEntropy(metadata.classValues.Length, instances);
							return ((sbyte)v, child);
						});
						// calculate gain
						var gain = children.Aggregate(node.Entropy, (g, c) => g - c.Item2.Entropy * c.Item2.InstanceIds.Length / node.InstanceIds.Length);
						if (gain > maxGain)
						{
							bestAttribute = attr;
							bestChildren = children;
							maxGain = gain;
						}
					}
					// set node to use the best attribute and the corresponding children
					node.TrainAttribute = bestAttribute;
					node.Children = bestChildren.ToArray();
					openList.AddRange(node.Children.Select(c => c.node));
				}
			}
			return rootNode;
		}
	}
}
