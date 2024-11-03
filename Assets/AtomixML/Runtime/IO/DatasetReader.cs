﻿using Atom.MachineLearning.Core.Maths;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace Atom.MachineLearning.IO
{
    /// <summary>
    /// Base class for reading datas from a dataset and feed training algorithm
    /// </summary>
    public static class DatasetReader
    {
        /// <summary>
        /// Reads a csv file at path, and split it with separator char
        /// </summary>
        /// <param name="filepath"></param>
        /// <param name="separator"></param>
        /// <param name="startIndex"> Skipping first rows (header rows ?) </param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public static string[,] ReadCSV(string filepath, char separator, int startIndex = 1)
        {
            //read
            var lines = File.ReadAllLines(filepath).ToList();
            //clean empty
            lines.RemoveAll(t => t == string.Empty);

            string[] text = lines.ToArray();

            var columns = text.First().Split(separator, StringSplitOptions.RemoveEmptyEntries);
            string[,] datas = new string[text.Length, columns.Length];

            for (int i = startIndex; i < text.Length; ++i)
            {
                var row = text[i].Split(separator, StringSplitOptions.RemoveEmptyEntries);

                for (int j = 0; j < row.Length; ++j)
                    datas[i, j] = row[j];
            }

            return datas;
        }

        /// <summary>
        /// Returns a subselection of columns by their names as a new matrice
        /// </summary>
        /// <param name="selectedColumnNames"></param>
        /// <returns></returns>
        public static string[,] GetSubCSVByHeaderColumnNames(string[] selectedColumnNames)
        {
            return null;
        }

        public static string[,] ColumnReplace(int columnIndex, Dictionary<string, string> rules)
        {
            return null;
        }

        /// <summary>
        /// Allow, for instance, subselection of a set into a training rows and test rows
        /// </summary>
        /// <param name="datas"></param>
        /// <param name="splitIndex"></param>
        /// <param name="right"></param>
        /// <param name="left"></param>
        public static void SplitRows(string[,] datas, int splitIndex, out string[,] before, out string[,] after)
        {
            before = new string[splitIndex, datas.GetLength(1)];
            after = new string[datas.GetLength(0) - splitIndex, datas.GetLength(1)];

            for (int i = 0; i < splitIndex; ++i)
            {
                for(int j = 0; j < datas.GetLength(1); ++j)
                {
                    before[i, j] = datas[i, j];
                }
            }

            int index = 0;
            for (int i = splitIndex; i < datas.GetLength(0); ++i)
            {
                for (int j = 0; j < datas.GetLength(1); ++j)
                {
                    after[index, j] = datas[i, j];
                }

                index++;
            }
        }

        /// <summary>
        /// Allows, for instance, to split data columns from label columns
        /// </summary>
        /// <param name="datas"></param>
        /// <param name="splitIndex"></param>
        /// <param name="right"></param>
        /// <param name="left"></param>
        public static void SplitColumn(string[,] datas, int splitIndex, out string[,] right, out string[,] left)
        {
            right = new string[datas.GetLength(0), splitIndex + 1];
            left = new string[datas.GetLength(0), datas.GetLength(1) - splitIndex - 1];
            int left_start_index = splitIndex + 1;
            int total_lenght = datas.GetLength(1);

            for (int i = 0; i < datas.GetLength(0); ++i)
            {
                for (int j = 0; j < left_start_index; ++j)
                    right[i, j] = datas[i, j];

                for (int k = left_start_index; k < total_lenght; ++k)
                    left[i, k - left_start_index] = datas[i, k];
            }
        }

        /// <summary>
        /// Allows, for instance, to split data columns from label unique column
        /// </summary>
        /// <param name="datas"></param>
        /// <param name="splitIndex"></param>
        /// <param name="right"></param>
        /// <param name="left"></param>
        public static void SplitLastColumn(string[,] datas, out string[,] right, out string[] left)
        {
            int total_lenght = datas.GetLength(1);
            int splitIndex = total_lenght - 2;

            right = new string[datas.GetLength(0), total_lenght - 1];
            left = new string[datas.GetLength(0)];

            int left_start_index = splitIndex + 1;

            for (int i = 0; i < datas.GetLength(0); ++i)
            {
                for (int j = 0; j < left_start_index; ++j)
                    right[i, j] = datas[i, j];

                left[i] = datas[i, total_lenght - 1];
            }
        }

        public static void ShuffleRows(string[,] datas)
        {
            int rowCount = datas.GetLength(0); // Number of rows
            int colCount = datas.GetLength(1); // Number of columns

            for (int i = 0; i < rowCount; i++)
            {
                // Pick a random row to swap with
                int j = MLRandom.Shared.Next(i, rowCount);

                // Swap row i with row j
                for (int k = 0; k < colCount; k++)
                {
                    string temp = datas[i, k];
                    datas[i, k] = datas[j, k];
                    datas[j, k] = temp;
                }
            }
        }

        /// <summary>
        /// Returns textures from Unity's resource folder
        /// </summary>
        /// <param name="folderpath"></param>
        /// <returns></returns>
        public static List<Texture2D> ReadTextures(string folderpath)
        {
            return Resources.LoadAll<Texture2D>(folderpath).ToList();
        }
    }
}
