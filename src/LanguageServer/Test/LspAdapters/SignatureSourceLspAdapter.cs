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
using System.Threading.Tasks;
using Microsoft.Python.Analysis;
using Microsoft.Python.Analysis.Analyzer;
using Microsoft.Python.Core.Text;
using Microsoft.Python.LanguageServer.Protocol;
using Microsoft.Python.Core;
using System.Threading;
using UnitTests.LanguageServerClient;
using LSP = Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.Python.LanguageServer.Tests.LspAdapters {
    internal sealed class SignatureSourceLspAdapter {
        private readonly IDocumentationSource _docSource;
        private readonly bool _labelOffsetSupport;

        public SignatureSourceLspAdapter(IDocumentationSource docSource, bool labelOffsetSupport = true) {
            _docSource = docSource;
            // TODO: deprecate eventually.
            _labelOffsetSupport = labelOffsetSupport; // LSP 3.14.0+
        }

        public SignatureHelp GetSignature(IDocumentAnalysis analysis, SourceLocation location) {
            if (analysis is EmptyAnalysis) {
                return null;
            }

            var cb = PythonLanguageServiceProviderCallback.CreateTestInstance();

            var client = PythonLanguageClient.FindLanguageClient("PythonFile");
            if (client == null) {
                throw new NullReferenceException("PythonLanguageClient not found");
            }

            var uri = analysis.Document.Uri;
            cb.SetClient(uri, client);
            SignatureHelp res = GetDocumentSignatureHelpAsync(analysis, location, cb, client, uri).WaitAndUnwrapExceptions();

            return res;
        }

        private static async Task<SignatureHelp> GetDocumentSignatureHelpAsync(IDocumentAnalysis analysis, SourceLocation location, PythonLanguageServiceProviderCallback cb, PythonLanguageClient client, Uri uri) {
            await RunningDocumentTableLspAdapter.OpenDocumentLspAsync(client, uri.ToAbsolutePath(), analysis.Document.Content);

            // hack to wait for server analysis 
            await Task.Delay(1000);

            // convert SourceLocation to Position
            Position postion = location;

            // note: CompletionList is from  Microsoft.Python.LanguageServer.Protocol and not the LSP.CompletionList version
            var res = await cb.RequestAsync(
                new LSP.LspRequest<LSP.TextDocumentPositionParams, SignatureHelp>(LSP.Methods.TextDocumentSignatureHelpName),
                new LSP.TextDocumentPositionParams {
                    TextDocument = new LSP.TextDocumentIdentifier { Uri = uri },
                    Position = new LSP.Position { Line = postion.line, Character = postion.character },
                },
                CancellationToken.None
            );
            return res;
        }
    }
}
