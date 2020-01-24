using Microsoft.Python.Analysis;
using Microsoft.Python.Analysis.Types;
using Microsoft.Python.Core;
using Microsoft.Python.Core.Text;
using Microsoft.Python.LanguageServer.Protocol;
using Microsoft.Python.LanguageServer.Sources;
using Microsoft.Python.LanguageServer.Tests.LanguageServer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using UnitTests.LanguageServerClient;
using LSP = Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.Python.LanguageServer.Tests.LspAdapters {
    internal class DefinitionSourceLspAdapter {
        private readonly IServiceContainer _services;
        public DefinitionSourceLspAdapter(IServiceContainer services){
            _services = services;
        }

        public Reference FindDefinition(IDocumentAnalysis analysis, SourceLocation location, out ILocatedMember definingMember) {
            var cb = PythonLanguageServiceProviderCallback.CreateTestInstance();

            //using (var client = CreateClientAsync(analysis.Document.Interpreter?.Configuration).WaitAndUnwrapExceptions()) {
            var client = _services.GetService<PythonLanguageClient>();
            var uri = analysis.Document.Uri;
            cb.SetClient(uri, client);

            //File.WriteAllText(uri.ToAbsolutePath(), analysis.Document.Content);
            RunningDocumentTableLspAdapter.OpenDocumentLspAsync(client, uri.ToAbsolutePath(), analysis.Document.Content).WaitAndUnwrapExceptions();

            // convert SourceLocation to Position
            Position postion = location;

            var res = cb.RequestAsync(
                   new LSP.LspRequest<LSP.TextDocumentPositionParams, Reference[]>(LSP.Methods.TextDocumentDefinitionName),
                   new LSP.TextDocumentPositionParams {
                       TextDocument = new LSP.TextDocumentIdentifier { Uri = uri },
                       Position = new LSP.Position { Line = postion.line, Character = postion.character }
                   },
                   CancellationToken.None
               ).WaitAndUnwrapExceptions();

            definingMember = null;
            return res.FirstOrDefault();
        }
    }
}
