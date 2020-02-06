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

using System.Threading;
using System.Threading.Tasks;
using Microsoft.Python.Analysis;
using Microsoft.Python.Analysis.Analyzer;
using Microsoft.Python.Core;
using Microsoft.Python.Core.Text;
using Microsoft.Python.LanguageServer.Protocol;
using UnitTests.LanguageServerClient;
using LSP = Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.Python.LanguageServer.IntegrationTests.LspAdapters {
    internal sealed class HoverSourceLSPAdapter {
        private readonly IDocumentationSource _docSource;

        public HoverSourceLSPAdapter(IDocumentationSource docSource) {
            _docSource = docSource;
        }

        public LSP.Hover GetHover(IDocumentAnalysis analysis, SourceLocation location) {
            return GetDocumentHoverNameAsync(analysis, location).WaitAndUnwrapExceptions();
        }

        internal static async Task<LSP.Hover> GetDocumentHoverNameAsync(IDocumentAnalysis analysis, SourceLocation sourceLocation) {
            var cb = PythonLanguageServiceProviderCallback.CreateTestInstance();
            var uri = analysis.Document.Uri;
            var client = PythonLanguageClient.FindLanguageClient("PythonFile");
            cb.SetClient(uri, client);

            await RunningDocumentTableLspAdapter.OpenDocumentLspAsync(client, uri.AbsolutePath, analysis.Document.Content);

            await Task.Delay(1000);

            //convert to LSP postion
            Position position = sourceLocation;

            var hover = await cb.RequestAsync(
                new LSP.LspRequest<LSP.TextDocumentPositionParams, LSP.Hover>(LSP.Methods.TextDocumentHoverName),
                new LSP.TextDocumentPositionParams {
                    TextDocument = new LSP.TextDocumentIdentifier { Uri = uri },
                    Position = new LSP.Position { Line = position.line, Character = position.character }
                },
                CancellationToken.None
            );
            return hover;
        }
    }
}
