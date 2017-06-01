using System;
using System.Collections.Generic;
using System.Text;

namespace MachineLearning.NeuronalNetwork
{
	class InputNeuron : INeuron
	{
		public double Value { get; set; }

		public double GetOutputValue() => Value;
		public void Recalculate() { }
	}
}
