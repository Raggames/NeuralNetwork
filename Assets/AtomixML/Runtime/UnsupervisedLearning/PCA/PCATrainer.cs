﻿using Atom.MachineLearning.Core;
using Atom.MachineLearning.IO;
using MathNet.Numerics.LinearAlgebra;
using Sirenix.OdinInspector;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using Unity.Plastic.Newtonsoft.Json;
using UnityEngine;
using UnityEngine.UI;

namespace Atom.MachineLearning.Unsupervised.PCA
{

    public class PCATrainer : MonoBehaviour, IMLTrainer<PCAModel, NVector, NVector>
    {
        /// <summary>
        /// Valus in 0-1 range will be understood as a total explained variance threshold
        /// Integer values above or equal to 1 will be understood as a given count of dimensions
        /// </summary>
        [SerializeField] private float _componentSelectionThreshold;

        [SerializeField] private float _scale = 3f;

        private NVector _meanVector;
        private NVector _stdDeviationVector;
        private NVector[] _test_results;
        private Color[] _labelColors;

        private PCAModel _model;

        [Button]
        private async void TestMNISTFit(string texturesPath = "mnist")
        {
            var model = new PCAModel();
            var textures = DatasetReader.ReadTextures(texturesPath);

            var vectorized = new NVector[textures.Count];
            for (int i = 0; i < textures.Count; ++i)
            {
                var matrix = VectorizationUtils.Texture2DToMatrix(textures[i]);
                matrix = VectorizationUtils.PoolAverage(matrix, 4, 2);
                var array = VectorizationUtils.MatrixToArray(matrix);
                vectorized[i] = new NVector(array);
            }

            var result = await Fit(model, vectorized);

            Debug.Log($"End fitting, accuracy (kept variance) => {result.Accuracy}");
        }

        [SerializeField] private RawImage _rawImage;

        [Button]
        private Texture2D TestMNISTCompressionOutput(string texturesPath = "mnist", int imageIndex = 0)
        {
            var textures = DatasetReader.ReadTextures(texturesPath);

            var matrix = VectorizationUtils.Texture2DToMatrix(textures[imageIndex]);
            matrix = VectorizationUtils.PoolAverage(matrix, 4, 2);
            var array = VectorizationUtils.MatrixToArray(matrix);
            var inputVector = new NVector(array);

            var output_vector = _model.Predict(inputVector);
            output_vector = _model.Decompress(output_vector);
            var output_to_matrix = VectorizationUtils.ArrayToMatrix(output_vector.Data);
            var texture = VectorizationUtils.MatrixToTexture2D(output_to_matrix);

            _rawImage.texture = texture;

            return texture;
        }

        [Button]
        private Texture2D TestMatrixToTexture(Texture2D input, int filterSize = 2, int padding = 2)
        {
            var model = new PCAModel();

            var array = VectorizationUtils.Texture2DToArray(input);
            var matrix = VectorizationUtils.ArrayToMatrix(array); // pour tester les conversions
            matrix = VectorizationUtils.PoolAverage(matrix, filterSize, padding);
            var texture = VectorizationUtils.MatrixToTexture2D(matrix);

            return texture;
        }

        [Button]
        private async void TestFitFlowers(string csvpaath = "Assets/AtomixML/Runtime/UnsupervisedLearning/PCA/Resources/flowers/iris.data.txt", int maximumSetSize = 50)
        {
            var model = new PCAModel();
            var datas = DatasetReader.ReadCSV(csvpaath, ',');

            DatasetReader.SplitLastColumn(datas, out var features, out var labels);

            var vectorized_labels = VectorizationUtils.RuledVectorization(labels, 3, new Dictionary<string, double[]>()
            {
                { "Iris-setosa", new double[] { 0, 0, 1 } },
                { "Iris-versicolor", new double[] { 0, 1, 0 } },
                { "Iris-virginica", new double[] { 1, 0, 0 } },
            });

            _labelColors = new Color[vectorized_labels.GetLength(0)];

            for (int i = 0; i < vectorized_labels.GetLength(0); ++i)
                _labelColors[i] = new Color((float)vectorized_labels[i, 0], (float)vectorized_labels[i, 1], (float)vectorized_labels[i, 2], 1);

            var vectorized_features = VectorizationUtils.StringMatrix2DToDoubleMatrix2D(features).ToNVectorArray();

            var result = await Fit(model, vectorized_features);

            Debug.Log($"End fitting, accuracy (kept variance) => {result.Accuracy}");

            _test_results = new NVector[vectorized_features.Length];
            for (int i = 0; i < vectorized_features.Length; ++i)
            {
                _test_results[i] = model.Predict(vectorized_features[i]);
            }
        }

        public async Task<ITrainingResult> Fit(PCAModel model, NVector[] trainingDatas)
        {
            _model = model;
            var standardizedDatas = NVector.Standardize(trainingDatas, out _meanVector, out _stdDeviationVector);
            var covariance_matrix = NVector.CovarianceMatrix(standardizedDatas);

            var matrix = Matrix<double>.Build.DenseOfArray(covariance_matrix);
            var evd = matrix.Evd();  // Eigenvalue decomposition

            var eigenvalues = evd.EigenValues.AsArray();
            var eigenvectors = evd.EigenVectors;

            var eigen_datas = new EigenPair[eigenvalues.Length];
            var eigen_sum = 0.0;

            for (int i = 0; i < eigenvalues.Length; ++i)
            {
                eigen_datas[i] = new EigenPair(eigenvalues[i].Real, eigenvectors.Column(i).AsArray());
                eigen_sum += eigen_datas[i].EigenValue;
            }

            eigen_datas = eigen_datas.OrderByDescending(t => t.EigenValue).ToArray();

            var selected_components = new List<EigenPair>();

            var kept_variance = 0.0;
            var tot_variance = 0.0;

            if (_componentSelectionThreshold >= 0f && _componentSelectionThreshold < 1f)
            {
                // deciding how much dimensions we need
                var desired_variance_threshold = _componentSelectionThreshold * 100f; // a purcentage of the total variance
                var threshold_reached = false;

                for (int i = 0; i < eigen_datas.Length; ++i)
                {
                    var explained_variance = (eigen_datas[i].EigenValue / eigen_sum) * 100f;
                    tot_variance += explained_variance;

                    if (!threshold_reached)
                    {
                        selected_components.Add(eigen_datas[i]);
                        kept_variance = tot_variance;
                    }

                    // We add component until we reach the minimal variance threshold we want
                    // the count of components will be the dimensions of our projection matrix
                    if (tot_variance > desired_variance_threshold)
                    {
                        threshold_reached = true;
                    }

                    Debug.Log($"Eigen value : {eigen_datas[i].EigenValue}. Explained variance : {explained_variance} / {tot_variance} %");
                }
            }
            else if (_componentSelectionThreshold < eigen_datas.Length)
            {
                int comp = (int)Math.Round(_componentSelectionThreshold);
                for (int i = 0; i < comp; ++i)
                {
                    var explained_variance = (eigen_datas[i].EigenValue / eigen_sum) * 100f;
                    tot_variance += explained_variance;
                    kept_variance = tot_variance;
                    selected_components.Add(eigen_datas[i]);
                }
            }
            else throw new Exception($"The component selection value can't be superior as the total number of dimensions of the input features");

            Debug.Log($"Selected components count : {selected_components.Count}");
            // projection matrix : this will be what the algorithm has learned
            // each feature will then be multiplied by the matrix 

            var projectionMatrix = NMatrix.DenseOfColumnVectors(selected_components.Select(t => t.EigenVector).ToArray());

            model.Initialize(projectionMatrix, _meanVector, _stdDeviationVector);

            return new TrainingResult()
            {
                Accuracy = (float)kept_variance,
            };
        }

        void OnDrawGizmos()
        {
            if (_test_results == null)
                return;

            for (int i = 0; i < _test_results.Length; ++i)
            {
                Gizmos.color = _labelColors[i];
                Gizmos.DrawSphere(new UnityEngine.Vector3((float)_test_results[i].Data[0] * _scale, (float)_test_results[i].Data[1], 0) * _scale, .15f);

            }
        }
    }
}
