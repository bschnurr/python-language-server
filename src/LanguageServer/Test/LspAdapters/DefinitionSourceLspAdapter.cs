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
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Python.Analysis;
using Microsoft.Python.Analysis.Types;
using Microsoft.Python.Core;
using Microsoft.Python.Core.Text;
using Microsoft.Python.LanguageServer.Protocol;
using UnitTests.LanguageServerClient;
using LSP = Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.Python.LanguageServer.Tests.LspAdapters {
    internal class DefinitionSourceLspAdapter {
        private readonly IServiceContainer _services;
        public DefinitionSourceLspAdapter(IServiceContainer services){
            _services = services;
        }

        public Reference FindDefinition(IDocumentAnalysis analysis, SourceLocation location, out ILocatedMember definingMember) {
            definingMember = null;

            var cb = PythonLanguageServiceProviderCallback.CreateTestInstance();

            var client = _services.GetService<PythonLanguageClient>();
            var uri = analysis.Document.Uri;
            cb.SetClient(uri, client);

            Reference[] res = FindDefintionAsync(analysis, location, cb, client, uri).WaitAndUnwrapExceptions();

            //fixup uri paths
            foreach (var reference in res) {
                reference.uri = new Uri(reference.uri.ToString().Replace("%3A", ":"));
            }

            return res.FirstOrDefault();
        }

        private static async Task<Reference[]> FindDefintionAsync(IDocumentAnalysis analysis, SourceLocation location, PythonLanguageServiceProviderCallback cb, PythonLanguageClient client, Uri uri) {
            
            await RunningDocumentTableLspAdapter.OpenDocumentLspAsync(client, uri.ToAbsolutePath(), analysis.Document.Content);

            await Task.Delay(1000);

            // convert SourceLocation to Position
            Position postion = location;

            var res = await cb.RequestAsync(
                   new LSP.LspRequest<LSP.TextDocumentPositionParams, Reference[]>(LSP.Methods.TextDocumentDefinitionName),
                   new LSP.TextDocumentPositionParams {
                       TextDocument = new LSP.TextDocumentIdentifier { Uri = uri },
                       Position = new LSP.Position { Line = postion.line, Character = postion.character }
                   },
                   CancellationToken.None
               );
            return res;
        }
    }
}
