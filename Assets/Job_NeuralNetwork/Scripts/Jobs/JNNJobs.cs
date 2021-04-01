﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
  
namespace Assets.Job_NeuralNetwork.Scripts
{
    [BurstCompile] 
    public struct ComputeWeightsJob : IJobParallelFor
    {
        [ReadOnly] public NativeArray<double> Weights;
        [ReadOnly] public NativeArray<double> Inputs;
        public NativeArray<double> Outputs;
        
        public void Execute(int index) 
        {
            Outputs[index] = Weights[index] * Inputs[index];            
        } 
    }
       
    /*[BurstCompile]
    public struct LinearJob : IJobParallelFor
    {
        public NativeArray<JNNNeuron.NeuronData> dataArray;

        [ReadOnly] public NativeArray<double> inputs;
        public NativeArray<double> outputs;

        public void Execute(int index)
        {
            double value = 0;
            for (int i = 0; i < inputs.Length; ++i)
            {
                value += inputs[i] * dataArray[index].Weights;
            }
            value += dataArray[index].Bias;
            value /= inputs.Length;

            outputs[index] = value;
        }
    }

    [BurstCompile]
    public struct SigmoidJob : IJobParallelFor
    {
        public NativeArray<JNNNeuron.NeuronData> dataArray;

        [ReadOnly] public NativeArray<double> inputs;
        public NativeArray<double> outputs;

        public void Execute(int index)
        {
            double value = 0;
            for (int i = 0; i < inputs.Length; ++i)
            {
                value += inputs[i] * dataArray[index].Weights;
            }
            value += dataArray[index].Bias;
            value /= inputs.Length;

            value = JNNMath.Logistic(value);
            outputs[index] = value;
        }
    }

    [BurstCompile]
    public struct TanhJob : IJobParallelFor
    {
        public NativeArray<JNNNeuron.NeuronData> dataArray;

        [ReadOnly] public NativeArray<double> inputs;
        public NativeArray<double> outputs;

        public void Execute(int index)
        {
            double value = 0;
            for (int i = 0; i < inputs.Length; ++i)
            {
                value += inputs[i] * dataArray[index].Weights;
            }
            value += dataArray[index].Bias;
            value /= inputs.Length;

            value = JNNMath.Tanh(value);
            outputs[index] = value;
        }
    }

    [BurstCompile]
    public struct SinusoidJob : IJobParallelFor
    {
        public NativeArray<JNNNeuron.NeuronData> dataArray;

        [ReadOnly] public NativeArray<double> inputs;
        public NativeArray<double> outputs;

        public void Execute(int index)
        {
            double value = 0;
            for (int i = 0; i < inputs.Length; ++i)
            {
                value += inputs[i] * dataArray[index].Weights;
            }
            value += dataArray[index].Bias;
            value /= inputs.Length;

            value = JNNMath.Sinusoid(value);
            outputs[index] = value;
        }
    }

    [BurstCompile]
    public struct ReLUJob : IJobParallelFor
    {
        public NativeArray<JNNNeuron.NeuronData> dataArray;

        [ReadOnly] public NativeArray<double> inputs;
        public NativeArray<double> outputs;

        public void Execute(int index)
        {
            double value = 0;
            for (int i = 0; i < inputs.Length; ++i)
            {
                value += inputs[i] * dataArray[index].Weights;
            }
            value += dataArray[index].Bias;
            value /= inputs.Length;

            value = JNNMath.ReLU(value);
            outputs[index] = value;
        }
    }

    [BurstCompile]
    public struct PReLUJob : IJobParallelFor
    {
        public NativeArray<JNNNeuron.NeuronData> dataArray;

        [ReadOnly] public NativeArray<double> inputs;
        public NativeArray<double> outputs;

        public void Execute(int index)
        {
            double value = 0;
            for (int i = 0; i < inputs.Length; ++i)
            {
                value += inputs[i] * dataArray[index].Weights;
            }
            value += dataArray[index].Bias;
            value /= inputs.Length;

            value = JNNMath.PReLU(value, 0.1f);
            outputs[index] = value;
        }
    }*/
}