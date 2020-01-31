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

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Python.Analysis;
using Microsoft.Python.Core;
using Microsoft.Python.Core.Collections;
using Microsoft.Python.Core.Text;
using Microsoft.Python.LanguageServer.CodeActions;
using Microsoft.Python.LanguageServer.Protocol;
using Microsoft.Python.Parsing.Tests;
using LSP = Microsoft.VisualStudio.LanguageServer.Protocol;


namespace Microsoft.Python.LanguageServer.Tests.LspAdapters {
    internal sealed partial class QuickFixCodeActionSourceLspAdapter {
        private static readonly ImmutableArray<IQuickFixCodeActionProvider> _codeActionProviders =
            ImmutableArray<IQuickFixCodeActionProvider>.Create(MissingImportCodeActionProvider.Instance);

        private readonly IServiceContainer _services;

        public QuickFixCodeActionSourceLspAdapter(IServiceContainer services) {
            _services = services;
        }

        public async Task<IEnumerable<CodeAction>> GetCodeActionsAsync(IDocumentAnalysis analysis, CodeActionSettings settings, Diagnostic[] diagnostics, CancellationToken cancellation) {

            var cb = PythonLanguageServiceProviderCallback.CreateTestInstance();

            var configuration = PythonVersions.LatestAvailable;

            using (var client = await ServicesLspAdapter.CreateClientAsync(configuration)) {
                var uri = analysis.Document.Uri;
                cb.SetClient(uri, client);

                Position start = diagnostics[0].range.start;
                Position end = diagnostics[0].range.end;

                var dir = Path.GetDirectoryName(uri.ToAbsolutePath());
                if (!Directory.Exists(dir)) {
                    Directory.CreateDirectory(dir);
                }
                File.WriteAllText(uri.ToAbsolutePath(), analysis.Document.Content);

                await ServicesLspAdapter.ConfigurationChangeAsync(client, cb);

                var res = await cb.RequestAsync(
                       new LSP.LspRequest<LSP.CodeActionParams, LSP.SumType<LSP.Command, CodeAction>[]>(LSP.Methods.TextDocumentCodeActionName),
                       new LSP.CodeActionParams {
                           TextDocument = new LSP.TextDocumentIdentifier { Uri = uri },
                           Range = new LSP.Range { Start = new LSP.Position(start.line, start.character), End = new LSP.Position(end.line, end.character) },
                           Context = new LSP.CodeActionContext {
                               Diagnostics = new LSP.Diagnostic[] { new LSP.Diagnostic { Code = analysis.Document.Content } },
                               Only = new LSP.CodeActionKind[] { LSP.CodeActionKind.QuickFix }
                           }
                       },
                       CancellationToken.None
                   );

                if (res.Count() > 0) {
                    var actions = new CodeAction[] { (CodeAction)res[1] };

                    return actions.ToList();
                }

                return null;
            }
        }
    }
}
