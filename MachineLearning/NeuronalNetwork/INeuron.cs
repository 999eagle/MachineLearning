using System;
using System.Collections.Generic;
using System.Text;

namespace MachineLearning.NeuronalNetwork
{
	interface INeuron
	{
		double GetOutputValue();
		void Recalculate();
	}
}
