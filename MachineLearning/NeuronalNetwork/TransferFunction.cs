using System;
using System.Collections.Generic;
using System.Text;

namespace MachineLearning.NeuronalNetwork
{
	abstract class TransferFunction
	{
		public double Parameter { get; set; }

		public abstract double Calculate(double value);

		public static TransferFunction Step { get => new StepTransferFunction(); }

		class StepTransferFunction : TransferFunction
		{
			public override double Calculate(double value) => value >= Parameter ? 1 : 0;
		}
	}
}
