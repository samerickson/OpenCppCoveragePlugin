// OpenCppCoverage is an open source code coverage for C++.
// Copyright (C) 2016 OpenCppCoverage
//
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// any later version.
//
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with this program.  If not, see <http://www.gnu.org/licenses/>.

using OpenCppCoverage.VSPackage.CoverageRateBuilder;
using System;
using System.Collections.Generic;
using System.Linq;

namespace OpenCppCoverage.VSPackage.CoverageTree
{
    internal class FileCoverageAggregator
    {
        //---------------------------------------------------------------------
        public Dictionary<string, FileCoverage> Aggregate(
            CoverageRate coverageRate,
            Func<string, string> normalizePath)
        {
            var fileCoverages = coverageRate.Children.SelectMany(module => module.Children);

            return CreateDictionary(fileCoverages, fileCoverage => normalizePath(fileCoverage.Path), MergeFileCoverage);
        }

        //---------------------------------------------------------------------
        private static FileCoverage MergeFileCoverage(FileCoverage fileCoverage, FileCoverage fileCoverage2)
        {
            var lineCoverages = fileCoverage.LineCoverages.Concat(fileCoverage2.LineCoverages);

            var lineCoverageByLine = CreateDictionary(lineCoverages, lineCoverage => lineCoverage.LineNumber, MergeLineCoverage);
            var mergedLineCoverages = lineCoverageByLine.Select(kvp => kvp.Value).ToList();

            return new FileCoverage(fileCoverage.Path, mergedLineCoverages);
        }

        //---------------------------------------------------------------------
        private static LineCoverage MergeLineCoverage(LineCoverage lineCoverage, LineCoverage lineCoverage2)
        {
            if (lineCoverage.LineNumber != lineCoverage2.LineNumber) throw new InvalidOperationException("Line numbers are not the same.");
            return new LineCoverage(lineCoverage.LineNumber, lineCoverage.HasBeenExecuted || lineCoverage2.HasBeenExecuted);
        }

        //---------------------------------------------------------------------
        private static Dictionary<TKey, TValue> CreateDictionary<TKey, TValue>(IEnumerable<TValue> collection, Func<TValue, TKey> getKey, Func<TValue, TValue, TValue> merge)
        {
            var dictionary = new Dictionary<TKey, TValue>();

            foreach (var value in collection)
            {
                var key = getKey(value);
                if (dictionary.TryGetValue(key, out var existingValue))
                    dictionary[key] = merge(existingValue, value);
                else
                    dictionary.Add(key, value);
            }

            return dictionary;
        }
    }
}
