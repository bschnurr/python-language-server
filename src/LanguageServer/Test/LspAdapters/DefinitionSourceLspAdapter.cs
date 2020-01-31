using Microsoft.Python.Analysis;
using Microsoft.Python.Analysis.Types;
using Microsoft.Python.Core;
using Microsoft.Python.Core.Text;
using Microsoft.Python.LanguageServer.Protocol;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
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
