﻿// Python Tools for Visual Studio
// Copyright(c) Microsoft Corporation
// All rights reserved.
//
// Licensed under the Apache License, Version 2.0 (the License); you may not use
// this file except in compliance with the License. You may obtain a copy of the
// License at http://www.apache.org/licenses/LICENSE-2.0
//
// THIS CODE IS PROVIDED ON AN  *AS IS* BASIS, WITHOUT WARRANTIES OR CONDITIONS
// OF ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING WITHOUT LIMITATION ANY
// IMPLIED WARRANTIES OR CONDITIONS OF TITLE, FITNESS FOR A PARTICULAR PURPOSE,
// MERCHANTABILITY OR NON-INFRINGEMENT.
//
// See the Apache Version 2.0 License for specific language governing
// permissions and limitations under the License.

using Microsoft.Python.Analysis.Core.Interpreter;
using System;
using System.Collections.Generic;

namespace UnitTests.LanguageServerClient {
    public class PythonLanguageClientContextFixed : IPythonLanguageClientContext {
        public event EventHandler InterpreterChanged { add { } remove { } }
        public event EventHandler SearchPathsChanged { add { } remove { } }
        public event EventHandler Closed { add { } remove { } }

        public PythonLanguageClientContextFixed(
            string contentTypeName,
            InterpreterConfiguration configuration,
            string rootPath,
            IEnumerable<string> searchPaths
        ) {
            ContentTypeName = contentTypeName ?? throw new ArgumentNullException(nameof(contentTypeName));
            InterpreterConfiguration = configuration;
            RootPath = rootPath;
            SearchPaths = searchPaths;
        }

        public string ContentTypeName { get; }

        public InterpreterConfiguration InterpreterConfiguration { get; }

        public string RootPath { get; }

        public IEnumerable<string> SearchPaths { get; }

        public object Clone() {
            return new PythonLanguageClientContextFixed(
                ContentTypeName,
                InterpreterConfiguration,
                RootPath,
                SearchPaths
            );
        }
    }
}
