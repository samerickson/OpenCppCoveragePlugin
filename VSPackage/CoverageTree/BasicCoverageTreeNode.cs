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

using ICSharpCode.TreeView;
using OpenCppCoverage.VSPackage.CoverageRateBuilder;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace OpenCppCoverage.VSPackage.CoverageTree
{
    internal class BasicCoverageTreeNode : SharpTreeNode
    {
        private readonly BaseCoverage _coverage;
        private readonly string _name;
        private readonly ImageSource _icon;

        private static readonly string ImagesFolder;

        //-----------------------------------------------------------------------
        static BasicCoverageTreeNode()
        {
            var rootFolder = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            BasicCoverageTreeNode.ImagesFolder = Path.Combine(rootFolder, "CoverageTree", "Images");
        }

        //-----------------------------------------------------------------------
        public BasicCoverageTreeNode(
            string name,
            BaseCoverage coverage,
            string iconFilename,
            bool isLeaf)
        {
            this._name = name;
            this._coverage = coverage;
            this.LazyLoading = !isLeaf;
            this._icon = LoadIcon(iconFilename);
        }

        //-----------------------------------------------------------------------
        public override object Icon => _icon;

        //-----------------------------------------------------------------------
        public override object Text => this._name;

        //-----------------------------------------------------------------------
        public double? OptionalCoverageRate
        {
            get
            {
                if (this.TotalLineCount == 0)
                    return null;
                return (double)this.CoveredLineCount / this.TotalLineCount;
            }
        }

        //-----------------------------------------------------------------------
        public double? OptionalUncoverageRate
        {
            get
            {
                if (this.TotalLineCount == 0)
                    return null;
                return (double)this.UncoveredLineCount / this.TotalLineCount;
            }
        }

        //-----------------------------------------------------------------------
        public int CoveredLineCount => this._coverage.CoverLineCount;

        //-----------------------------------------------------------------------
        public int UncoveredLineCount => this._coverage.TotalLineCount - this._coverage.CoverLineCount;

        //-----------------------------------------------------------------------
        public int TotalLineCount => this._coverage.TotalLineCount;

        //-----------------------------------------------------------------------
        protected IEnumerable<TReeNode> AddChildrenNode<T, TReeNode>(
            IEnumerable<T> children,
            Func<T, TReeNode> nodeFactory) where TReeNode : BasicCoverageTreeNode
        {
            var childrenNode = children.Select(nodeFactory);

            // ToList is required to avoid calling Select during the second iteration
            // and so create new objects.
            var sortedChildrenNode = childrenNode.OrderBy(c => c.OptionalCoverageRate).ToList();
            this.Children.Clear();
            this.Children.AddRange(sortedChildrenNode);

            return sortedChildrenNode;
        }

        //-----------------------------------------------------------------------
        private static ImageSource LoadIcon(string iconFilename)
        {
            var iconPath = Path.Combine(ImagesFolder, iconFilename);
            return BitmapFrame.Create(new Uri(iconPath, UriKind.Absolute));
        }
    }
}
