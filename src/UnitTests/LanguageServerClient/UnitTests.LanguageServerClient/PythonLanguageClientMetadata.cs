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

using Microsoft.VisualStudio.LanguageServer.Client;
using System.Collections.Generic;

namespace UnitTests.LanguageServerClient {
    internal class PythonLanguageClientMetadata : ILanguageClientMetadata {
        public PythonLanguageClientMetadata(string clientName, string contentType) {
            ClientName = clientName;
            ContentType = contentType;
        }

        public string ClientName { get; }

        public string ContentType { get; }

        public IEnumerable<string> ContentTypes => new[] { ContentType };
    }
}
