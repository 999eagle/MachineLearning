using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Car_ID3
{
	class Node
	{
		public sbyte TrainAttribute { get; set; } = -1;
		public ushort[] InstanceIds { get; set; }
		public sbyte Class { get; set; } = -1;
		public (sbyte attributeValue, Node node)[] Children { get; set; }
		public Node Parent { get; set; }

		public double Entropy { get; private set; }
		public IEnumerable<IGrouping<sbyte, ushort>> GetClasses(int[][] allInstances) => InstanceIds.GroupBy(i => (sbyte)allInstances[i].Last());
		public void CalcEntropy(int numClasses, int[][] allInstances)
		{
			Entropy = GetClasses(allInstances).Select(g => g.Count() / (double)InstanceIds.Length).Aggregate((e, p) => e - (p == 0 ? 0 : p * Math.Log(p, numClasses)));
		}

		public int Depth { get => Parent == null ? 0 : Parent.Depth + 1; }

		public override string ToString() => $"{{attribute: {TrainAttribute}, numInstances: {InstanceIds.Length}, depth: {Depth}, entropy: {Entropy}}}";
	}
}
