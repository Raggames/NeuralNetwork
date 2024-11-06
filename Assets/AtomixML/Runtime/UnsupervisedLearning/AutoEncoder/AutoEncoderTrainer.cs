﻿using Atom.MachineLearning.Core;
using Atom.MachineLearning.Core.Maths;
using Atom.MachineLearning.Core.Training;
using Atom.MachineLearning.IO;
using NeuralNetwork;
using Sirenix.OdinInspector;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using static Atom.MachineLearning.Unsupervised.AutoEncoder.AutoEncoderModel;

namespace Atom.MachineLearning.Unsupervised.AutoEncoder
{
    [ExecuteInEditMode]
    public class AutoEncoderTrainer : MonoBehaviour, IMLTrainer<AutoEncoderModel, NVector, NVector>
    {
        public AutoEncoderModel trainedModel { get; set; }

        [HyperParameter, SerializeField] private int _epochs = 1000;
        [HyperParameter, SerializeField] private float _learningRate = .05f;
        [HyperParameter, SerializeField] private float _momentum = .01f;
        [HyperParameter, SerializeField] private float _weightDecay = .0001f;
        [HyperParameter, SerializeField] private AnimationCurve _learningRateCurve;

        [SerializeField] private bool _normalizeDataSet;

        [ShowInInspector, ReadOnly] private int _currentEpoch;
        [ShowInInspector, ReadOnly] private float _currentLearningRate;
        [ShowInInspector, ReadOnly] private float _currentLoss;

        private NVector[] _x_datas;
        private List<NVector> _x_datas_buffer;
        private EpochSupervisorAsync _epochSupervisor;
        private AutoEncoderModel _model;

        [ShowInInspector, ReadOnly] private Texture2D _outputVisualization;
        [SerializeField] private RawImage _outputRawImage;
        [ShowInInspector, ReadOnly] private Texture2D _inputVisualization;
        [SerializeField] private RawImage _inputRawImage;

        [Button]
        private void VisualizeRandomMnist()
        {
            var mnist = Datasets.Mnist_8x8_TexturePooled_All();
            var input = mnist[MLRandom.Shared.Range(0, _x_datas.Length - 1)];
            _inputRawImage.texture = input;
        }


        [Button]
        private void LoadMnist()
        {
            var mnist = Datasets.Mnist_8x8_Vectorized_All();

            if (_normalizeDataSet)
                _x_datas = NVector.Normalize(mnist);
            else
                _x_datas = mnist;
        }

        [Button]
        private void LoadRndbw()
        {
            var mnist = Datasets.Rnd_bw_2x2_Vectorized_All();

            _x_datas = mnist;
        }

        [Button]
        private void LoadRndbw8x8()
        {
            var mnist = Datasets.Rnd_bw_8x8_Vectorized_All();

            _x_datas = mnist;
        }

        [Button]
        private void CheckInOutRndbw()
        {
            var datas = Datasets.Rnd_bw_2x2_Texture_All();

            var input = datas[MLRandom.Shared.Range(0, _x_datas.Length - 1)];
            _inputRawImage.texture = input;

            var array = TransformationUtils.Texture2DToArray(input);

            _outputRawImage.texture = TransformationUtils.MatrixToTexture2D(TransformationUtils.ArrayToMatrix(array));
        }

        [Button]
        private async void FitMnist()
        {
            /*var autoEncoder = new AutoEncoderModel(
                new int[] { 64, 32, 16, 8 },
                new int[] { 8, 16, 32, 64 } );*/

            var autoEncoder = new AutoEncoderModel(
                new int[] { 64, 32, 8 },
                new int[] { 8, 32, 64 },
                (r) =>
                {
                    for (int i = 0; i < r.Length; ++i)
                        r[i] = MLActivationFunctions.Sigmoid(r[i]);

                    return r;
                },
                (r) =>
                {
                    for (int i = 0; i < r.Length; ++i)
                        r[i] = MLActivationFunctions.DSigmoid(r[i]);

                    return r;
                });

            autoEncoder.ModelName = "auto-encoder-mnist";

            LoadMnist();

            await Fit(autoEncoder, _x_datas);

            Debug.Log("End fit");
        }

        [Button]
        private async void FitRndbw()
        {
            var autoEncoder = new AutoEncoderModel(
                new int[] { 4, 2, 1 },
                new int[] { 1, 2, 4 },
                (r) =>
                {
                    for (int i = 0; i < r.Length; ++i)
                        r[i] = MLActivationFunctions.Sigmoid(r[i]);

                    return r;
                },
                (r) =>
                {
                    for (int i = 0; i < r.Length; ++i)
                        r[i] = MLActivationFunctions.DSigmoid(r[i]);

                    return r;
                });

            autoEncoder.ModelName = "auto-encoder-mnist";

            LoadRndbw();

            await Fit(autoEncoder, _x_datas);

            Debug.Log("End fit");
        }

        [Button]
        private void LoadLast()
        {
            trainedModel = ModelSerializationService.LoadModel<AutoEncoderModel>("auto-encoder-mnist");
        }

        [Button]
        private void Visualize()
        {
            var input = _x_datas[MLRandom.Shared.Range(0, _x_datas.Length - 1)];

            _inputVisualization = TransformationUtils.MatrixToTexture2D(TransformationUtils.ArrayToMatrix(input.Data));
            _inputRawImage.texture = _inputVisualization;

            var output = trainedModel.Predict(input);

            // visualize each epoch the output of the last run
            _outputVisualization = TransformationUtils.MatrixToTexture2D(TransformationUtils.ArrayToMatrix(output.Data));
            _outputRawImage.texture = _outputVisualization;
        }

        [Button]
        private void Cancel()
        {
            _epochSupervisor?.Cancel();
        }

        [Button]
        private async void TestBothNetworksWithRnd_bw(int iterations = 50)
        {

            MLRandom.SeedShared(0);
            var nn_1 = new NeuralNetwork.NeuralNetwork();
            nn_1.AddDenseLayer(4, 2, ActivationFunctions.Sigmoid);
            nn_1.AddOutputLayer(4, ActivationFunctions.Sigmoid);
            nn_1.SeedRandomWeights(-1, 1);

            var nn_2 = new AutoEncoderModel(new int[] { 4, 2 }, new int[] { 2, 4 });
            MLRandom.SeedShared(0);
            nn_2.SeedWeigths(-1, 1);                      

            LoadRndbw();

            for (int i = 0; i < iterations; ++i)
            {
                var _x_input = _x_datas[MLRandom.Shared.Range(0, _x_datas.Length - 1)];
                nn_1.FeedForward(_x_input.Data, out var nn1_result);

                var nn2_result = nn_2.Predict(_x_input);

                var error_1 = MSE_Error(new NVector(nn1_result), _x_input);
                var error_2 = MSE_Error(nn2_result, _x_input);

                nn_1.ComputeDenseGradients(_x_input.Data, nn1_result);
                nn_1.UpdateDenseWeights(_learningRate, _momentum, _weightDecay, _learningRate);

                nn_2.Backpropagate(error_2);
                nn_2.UpdateWeights(_learningRate, _momentum, _weightDecay);
            }

        }


        private NeuralNetwork.NeuralNetwork _neuralNetwork;


        [Button]
        private void VisualizeOld()
        {
            var input = _x_datas[MLRandom.Shared.Range(0, _x_datas.Length - 1)];

            _inputVisualization = TransformationUtils.MatrixToTexture2D(TransformationUtils.ArrayToMatrix(input.Data));
            _inputRawImage.texture = _inputVisualization;

            _neuralNetwork.FeedForward(input.Data, out var output);

            // visualize each epoch the output of the last run
            _outputVisualization = TransformationUtils.MatrixToTexture2D(TransformationUtils.ArrayToMatrix(output));
            _outputRawImage.texture = _outputVisualization;
        }

        [Button]
        public async void TestFitOldNetworkRnwbw()
        {
            _neuralNetwork = new NeuralNetwork.NeuralNetwork();
            _neuralNetwork.AddDenseLayer(64, 16, ActivationFunctions.Sigmoid);
            _neuralNetwork.AddDenseLayer(8, ActivationFunctions.Sigmoid);
            _neuralNetwork.AddDenseLayer(16, ActivationFunctions.Sigmoid);
            _neuralNetwork.AddOutputLayer(64, ActivationFunctions.Sigmoid);

            LoadRndbw8x8();

            _x_datas_buffer = new List<NVector>();
            _currentLearningRate = _learningRate;

            for (int i = 0; i < _epochs; ++i)
            {
                _currentEpoch = i;
                _x_datas_buffer.AddRange(_x_datas);

                double error_sum = 0.0;
                double[] output = new double[_neuralNetwork.DenseLayers[0].NeuronsCount];

                while (_x_datas_buffer.Count > 0)
                {
                    var index = MLRandom.Shared.Range(0, _x_datas_buffer.Count - 1);
                    var input = _x_datas_buffer[index];
                    _x_datas_buffer.RemoveAt(index);

                    _neuralNetwork.FeedForward(input.Data, out output);

                    // we try to reconstruct the input while autoencoding
                    var error = MSE_Error(new NVector(output), input);
                    error_sum += MSE_Loss(error);

                    _neuralNetwork.BackPropagate(output, input.Data, _currentLearningRate, _momentum, _weightDecay, _learningRate);
                }


                _currentLoss = (float)error_sum / _x_datas.Length;

                _currentLearningRate = _learningRateCurve.Evaluate(((float)i / (float)_epochs)) * _learningRate;
            }
        }

        [Button]
        public async void TestFitOldNetworkMnist()
        {
            _neuralNetwork = new NeuralNetwork.NeuralNetwork();
            _neuralNetwork.AddDenseLayer(64, 32, ActivationFunctions.ReLU);
            _neuralNetwork.AddDenseLayer(16, ActivationFunctions.Sigmoid);
            _neuralNetwork.AddDenseLayer(8, ActivationFunctions.Sigmoid);
            _neuralNetwork.AddDenseLayer(16, ActivationFunctions.Sigmoid);
            _neuralNetwork.AddDenseLayer(32, ActivationFunctions.Sigmoid);
            _neuralNetwork.AddOutputLayer(64, ActivationFunctions.ReLU);

            LoadMnist();

            _x_datas_buffer = new List<NVector>();
            _currentLearningRate = _learningRate;

            for (int i = 0; i < _epochs; ++i)
            {
                _currentEpoch = i;
                _x_datas_buffer.AddRange(_x_datas);

                double error_sum = 0.0;
                double[] output = new double[_neuralNetwork.DenseLayers[0].NeuronsCount];

                while (_x_datas_buffer.Count > 0)
                {
                    var index = MLRandom.Shared.Range(0, _x_datas_buffer.Count - 1);
                    var input = _x_datas_buffer[index];
                    _x_datas_buffer.RemoveAt(index);

                    _neuralNetwork.FeedForward(input.Data, out output);

                    // we try to reconstruct the input while autoencoding
                    var error = MSE_Error(new NVector(output), input);
                    error_sum += MSE_Loss(error);

                    _neuralNetwork.BackPropagate(output, input.Data, _currentLearningRate, _momentum, _weightDecay, _learningRate);
                }


                _currentLoss = (float)error_sum / _x_datas.Length;

                _currentLearningRate = _learningRateCurve.Evaluate(((float)i / (float)_epochs)) * _learningRate;

                await Task.Delay(1);
            }
        }

        public async Task<ITrainingResult> Fit(AutoEncoderModel model, NVector[] x_datas)
        {
            trainedModel = model;

            _x_datas = x_datas;
            _x_datas_buffer = new List<NVector>();
            _currentLearningRate = _learningRate;

            var _epochSupervisor = new EpochSupervisorAsync(EpochIterationCallback);
            await _epochSupervisor.Run(_epochs);

            // test train ? 
            // accuracy ?
            ModelSerializationService.SaveModel(trainedModel);


            return new TrainingResult();
        }

        private void EpochIterationCallback(int epoch)
        {
            _currentEpoch = epoch;
            _x_datas_buffer.AddRange(_x_datas);

            double error_sum = 0.0;
            NVector output = new NVector(trainedModel.tensorDimensions);

            while (_x_datas_buffer.Count > 0)
            {
                var index = MLRandom.Shared.Range(0, _x_datas_buffer.Count - 1);
                var input = _x_datas_buffer[index];
                _x_datas_buffer.RemoveAt(index);

                output = trainedModel.Predict(input);

                // we try to reconstruct the input while autoencoding
                var error = MSE_Error(output, input);
                error_sum += MSE_Loss(error);
                trainedModel.Backpropagate(error);
                trainedModel.UpdateWeights(_currentLearningRate, _momentum, _weightDecay);
            }


            _currentLoss = (float)error_sum / _x_datas.Length;

            // decay learning rate
            // decay neighboordHoodDistance
            // for instance, linear degression

            _currentLearningRate = _learningRateCurve.Evaluate(((float)epoch / (float)_epochs)) * _learningRate;
        }

        /// <summary>
        /// Mean squarred
        /// </summary>
        /// <param name="error"></param>
        /// <returns></returns>
        public double MSE_Loss(NVector error)
        {
            var result = 0.0;
            for (int i = 0; i < error.Length; ++i)
            {
                result += Math.Pow(error[i], 2);
            }

            result /= error.Length;

            return result;
        }

        public NVector MSE_Error(NVector output, NVector test)
        {
            return (test - output) * 2;
        }
    }
}