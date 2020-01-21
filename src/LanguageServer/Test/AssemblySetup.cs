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

using Microsoft.Python.Core.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.VisualStudio.Threading;
using System;
using System.Collections.Generic;
using TestUtilities;
using UnitTests.LanguageServerClient;

namespace Microsoft.Python.LanguageServer.Tests {
    [TestClass]
    public sealed class AssemblySetup {
        [AssemblyInitialize]
        public static void Initialize(TestContext testContext) => LanguageServerTestEnvironment.Initialize(testContext);

        private class AnalysisTestEnvironment : TestEnvironmentImpl, ITestEnvironment {
            public static void Initialize() {
                var instance = new AnalysisTestEnvironment();
                Instance = instance;
                TestEnvironment.Current = instance;
            }
        }

        private class LanguageServerTestEnvironment : TestEnvironmentImpl, ITestEnvironment {
            public static JoinableTaskContext JoinableTaskContext;

            public static void Initialize(TestContext testContext) {
                var instance = new LanguageServerTestEnvironment();
                Instance = instance;
                TestEnvironment.Current = instance;

                var serverFolderPath = testContext.Properties["TestDeploymentDir"].ToString();

                //PythonLanguageServerDotNetCore.ExtractToFolder(serverFolderPath);

                Environment.SetEnvironmentVariable("PTVS_NODE_SERVER_ENABLED", "0");
                Environment.SetEnvironmentVariable("PTVS_DOTNETCORE_SERVER_LOCATION", serverFolderPath);
            }
        }
    }
}
