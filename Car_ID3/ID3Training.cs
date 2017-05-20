﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Car_ID3
{
	class ID3Training
	{
		TrainAttribute[] attributes;
		string[] classValues;
		byte[][] instances;

		public ID3Training(TrainAttribute[] attributes, string[] classValues, byte[][] instances)
		{
			this.attributes = attributes;
			this.classValues = classValues;
			this.instances = instances;
		}

		public class Node
		{
			public byte? TrainAttribute { get; set; }
			public ushort[] InstanceIds { get; set; }
			public byte? Class { get; set; }
			public (byte attributeValue, Node node)[] Children { get; set; }
			public Node Parent { get; set; }

			public double Entropy { get; private set; }
			public IEnumerable<IGrouping<byte, ushort>> GetClasses(byte[][] allInstances) => InstanceIds.GroupBy(i => allInstances[i].Last());
			public void CalcEntropy(int numClasses, byte[][] allInstances)
			{
				Entropy = GetClasses(allInstances).Select(g => g.Count() / (double)InstanceIds.Length).Aggregate((e, p) => e - (p == 0 ? 0 : p * Math.Log(p, numClasses)));
			}

			public int Depth { get => Parent == null ? 0 : Parent.Depth + 1; }

			public override string ToString() => $"{{attribute: {TrainAttribute}, numInstances: {InstanceIds.Length}, depth: {Depth}, entropy: {Entropy}}}";
		}

		public Node Train()
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
					IEnumerable<(byte, Node)> bestChildren = null;
					foreach (var attr in possibleAttributes)
					{
						// generate child nodes for the current attribute
						var children = Enumerable.Range(0, attributes[attr].PossibleValues.Length).Select(v =>
						{
							var child = new Node
							{
								Parent = node,
								InstanceIds = node.InstanceIds.Where(i => instances[i][attr] == v).ToArray()
							};
							child.CalcEntropy(classValues.Length, instances);
							return ((byte)v, child);
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
