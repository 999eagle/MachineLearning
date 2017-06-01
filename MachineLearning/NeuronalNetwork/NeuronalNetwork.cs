using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MachineLearning.NeuronalNetwork
{
	public class NeuronalNetwork
	{
		INeuron[][] layers;

		private NeuronalNetwork() { }

		public static NeuronalNetwork CreateNeuronalNetwork(params int[] neuronCount)
		{
			if (neuronCount.Length < 2) throw new ArgumentException("Need at least two layers.");
			var layers = new List<INeuron[]>();
			layers.Add(Enumerable.Range(0, neuronCount[0]).Select(i => new InputNeuron()).ToArray());
			foreach (var count in neuronCount.Skip(1).Take(neuronCount.Length - 2))
			{
				var inputs = layers.Last().Select(n => (1.0, n)).ToArray();
				layers.Add(Enumerable.Range(0, count).Select(i => new HiddenNeuron() { transferFunction = TransferFunction.Step, inputs = inputs }).ToArray());
			}
			var lastInputs = layers.Last().Select(n => (1.0, n)).ToArray();
			layers.Add(Enumerable.Range(0, neuronCount.Last()).Select(i => new OutputNeuron() { transferFunction = TransferFunction.Step, inputs = lastInputs }).ToArray());

			return new NeuronalNetwork() { layers = layers.ToArray() };
		}

		public double[] CalculateFor(double[] inputs)
		{
			if (inputs.Length != layers[0].Length) throw new ArgumentException("Number of inputs must be equal to number of neurons in first layer.");
			Enumerable.Range(0, inputs.Length).Select(i => (layers[0][i] as InputNeuron).Value = inputs[i]);
			foreach (var layer in layers.Skip(1))
			{
				foreach (var neuron in layer)
				{
					neuron.Recalculate();
				}
			}
			return layers.Last().Select(n => n.GetOutputValue()).ToArray();
		}
	}
}
