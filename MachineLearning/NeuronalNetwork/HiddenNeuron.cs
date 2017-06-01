using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MachineLearning.NeuronalNetwork
{
	class HiddenNeuron : INeuron
	{
		internal (double weight, INeuron neuron)[] inputs;
		internal TransferFunction transferFunction;
		private double value;

		public void Recalculate() => value = transferFunction.Calculate(inputs.Aggregate(0.0, (s, t) => s + t.weight * t.neuron.GetOutputValue()));
		public double GetOutputValue() => value;
	}
}
