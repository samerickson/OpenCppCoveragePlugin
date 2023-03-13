// OpenCppCoverage is an open source code coverage for C++.
// Copyright (C) 2014 OpenCppCoverage
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

using EnvDTE;
using EnvDTE80;
using ICSharpCode.TreeView;
using OpenCppCoverage.VSPackage.CoverageRateBuilder;
using OpenCppCoverage.VSPackage.Helper;
using System;

namespace OpenCppCoverage.VSPackage.CoverageTree
{
    internal class CoverageTreeController : PropertyChangedNotifier
    {
        private RootCoverageTreeNode _rootNode;
        private string _filter;
        private string _warning;
        private DTE2 _dte;
        private ICoverageViewManager _coverageViewManager;

        private readonly TreeNodeVisibilityManager _visibilityManager;

        //-----------------------------------------------------------------------
        public static readonly string WarningMessage = "Warning: Your program has exited with error code: ";

        //-----------------------------------------------------------------------
        public CoverageTreeController()
        {
            this._visibilityManager = new TreeNodeVisibilityManager();
        }

        //-----------------------------------------------------------------------
        public void UpdateCoverageRate(CoverageRate coverageRate, DTE2 dte, ICoverageViewManager coverageViewManager)
        {
            this._dte = dte;
            this._coverageViewManager = coverageViewManager;
            this.Root = new RootCoverageTreeNode(coverageRate);
            this.Filter = "";
            this.DisplayCoverage = true;

            if (coverageRate.ExitCode == 0) this.Warning = null;
            else this.Warning = WarningMessage + coverageRate.ExitCode;
        }

        //-----------------------------------------------------------------------
        public RootCoverageTreeNode Root
        {
            get => this._rootNode; private set => this.SetField(ref this._rootNode, value);
        }

        //-----------------------------------------------------------------------
        public string Filter
        {
            get => this._filter;
            set
            {
                if (!SetField(ref this._filter, value)) return;
                if (this.Root == null || value == null) return;

                this._visibilityManager.UpdateVisibility(this.Root, value);
                NotifyPropertyChanged("Root");
            }
        }

        //-----------------------------------------------------------------------
        public string Warning
        {
            get => this._warning; set => SetField(ref this._warning, value);
        }

        //-----------------------------------------------------------------------
        private bool _displayCoverage;
        public bool DisplayCoverage
        {
            get => this._displayCoverage;
            set
            {
                if (this.SetField(ref this._displayCoverage, value))
                    this._coverageViewManager.ShowCoverage = value;
            }
        }
    }
}
