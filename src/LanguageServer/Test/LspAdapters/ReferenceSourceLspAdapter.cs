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

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Python.Core;
using Microsoft.Python.Core.Text;
using Microsoft.Python.LanguageServer.Protocol;
using Microsoft.Python.LanguageServer.Tests.LanguageServer;
using Microsoft.Python.LanguageServer.Sources;
using UnitTests.LanguageServerClient;
using LSP = Microsoft.VisualStudio.LanguageServer.Protocol;


namespace Microsoft.Python.LanguageServer.Tests.LspAdapters {

    internal sealed class ReferenceSourceLspAdapter {
        private readonly IServiceContainer _services;

        public ReferenceSourceLspAdapter(IServiceContainer services) {
            _services = services;
        }

        public async Task<Reference[]> FindAllReferencesAsync(Uri uri, SourceLocation location, ReferenceSearchOptions options, CancellationToken cancellationToken = default) {
            if (uri == null) {
                return Array.Empty<Reference>();
            }

            var cb = PythonLanguageServiceProviderCallback.CreateTestInstance();

            var client = _services.GetService<PythonLanguageClient>();
            if (client == null) {
                throw new NullReferenceException("PythonLanguageClient not found");
            }

            cb.SetClient(uri, client);

            // convert SourceLocation to Position
            Position postion = location;

            // note: CompletionList is from  Microsoft.Python.LanguageServer.Protocol and not the LSP.CompletionList version
            var res = await cb.RequestAsync(
                new LSP.LspRequest<LSP.ReferenceParams, Reference[]>(LSP.Methods.TextDocumentReferencesName),
                new LSP.ReferenceParams {
                    TextDocument = new LSP.TextDocumentIdentifier { Uri = uri },
                    Position = new LSP.Position { Line = postion.line, Character = postion.character },
                },
                CancellationToken.None
            );

            //fixup uri paths
            foreach(var reference in res) {
                reference.uri = new Uri(reference.uri.ToString().Replace("%3A", ":"));
            }

            return res;
        }
    }
}
