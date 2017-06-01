using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MachineLearning
{
	public class ID3Training<TValueIndex> where TValueIndex : struct, IEquatable<TValueIndex>
	{
		TrainAttribute[] attributes;
		string[] classValues;
		TValueIndex[][] instances;

		private Func<int, TValueIndex> CV = ValueHelper<TValueIndex>.Convert;

		public ID3Training(TrainAttribute[] attributes, string[] classValues, TValueIndex[][] instances)
		{
			this.attributes = attributes;
			this.classValues = classValues;
			this.instances = instances;
		}

		public class Node
		{
			public byte? TrainAttribute { get; set; }
			public ushort[] InstanceIds { get; set; }
			public TValueIndex? Class { get; set; }
			public (TValueIndex attributeValue, Node node)[] Children { get; set; }
			public Node Parent { get; set; }

			public double Entropy { get; private set; }
			public IEnumerable<IGrouping<TValueIndex, ushort>> GetClasses(TValueIndex[][] allInstances) => InstanceIds.GroupBy(i => allInstances[i].Last());
			public void CalcEntropy(int numClasses, TValueIndex[][] allInstances)
			{
				Entropy = GetClasses(allInstances).Select(g => g.Count() / (double)InstanceIds.Length).Aggregate(0.0, (e, p) => e - (p == 0 ? 0 : p * Math.Log(p, numClasses)));
			}

			public int Depth { get => Parent == null ? 0 : Parent.Depth + 1; }

			public override string ToString() => $"{{attribute: {TrainAttribute}, numInstances: {InstanceIds.Length}, depth: {Depth}, entropy: {Entropy}}}";
		}

		class Classificator : IClassificator<TValueIndex>
		{
			public Node RootNode { get; }
			public Classificator(Node rootNode)
			{
				RootNode = rootNode;
			}

			private Func<int, TValueIndex> CV = ValueHelper<TValueIndex>.Convert;

			public TValueIndex Classify(TValueIndex[] instance)
			{
				var currentNode = RootNode;
				while (!currentNode.Class.HasValue)
				{
					var value = instance[currentNode.TrainAttribute.Value];
					currentNode = currentNode.Children.FirstOrDefault(c => c.attributeValue.Equals(value)).node;
					if (currentNode == null) { return CV(-1); }
				}
				return currentNode.Class.Value;
			}
		}

		public IClassificator<TValueIndex> Train()
		{
			var rootNode = new Node { InstanceIds = Enumerable.Range(0, instances.Length).Select(i => (ushort)i).ToArray() };
			rootNode.CalcEntropy(classValues.Length, instances);
			var openList = new List<Node> { rootNode };
			while (openList.Any())
			{
				var node = openList.First();
				openList.Remove(node);

				var possibleAttributes = Enumerable.Range(0, attributes.Length).Select(i => (byte)i).ToList();
				var parent = node.Parent;
				while (parent != null)
				{
					possibleAttributes.Remove(parent.TrainAttribute.Value);
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
					byte bestAttribute = 0;
					IEnumerable<(TValueIndex, Node)> bestChildren = null;
					foreach (var attr in possibleAttributes)
					{
						// generate child nodes for the current attribute
						var children = Enumerable.Range(0, attributes[attr].PossibleValues.Length).Select(v =>
						{
							var c = CV(v);
							var child = new Node
							{
								Parent = node,
								InstanceIds = node.InstanceIds.Where(i => instances[i][attr].Equals(c)).ToArray()
							};
							child.CalcEntropy(classValues.Length, instances);
							return (c, child);
						}).Where(c => c.Item2.InstanceIds.Length > 0);
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
			return new Classificator(rootNode);
		}

		public Node GetRootNode(IClassificator<TValueIndex> classificator)
		{
			if (!(classificator is Classificator cls)) { return null; }
			return cls.RootNode;
		}
	}
}
