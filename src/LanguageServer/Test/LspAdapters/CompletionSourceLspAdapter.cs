using Microsoft.Python.Analysis;
using Microsoft.Python.Analysis.Modules;
using Microsoft.Python.Core;
using Microsoft.Python.Core.Text;
using Microsoft.Python.LanguageServer.Completion;
using Microsoft.Python.LanguageServer.Protocol;
using System.Threading;
using UnitTests.LanguageServerClient;
using LSP = Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.Python.LanguageServer.Tests.LspAdapters {
    internal class CompletionSourceLspAdapter {
        private readonly CompletionItemSource _itemSource;
        private readonly IServiceContainer _services;
        public static string RootPath;

        public CompletionSourceLspAdapter(IDocumentationSource docSource, ServerSettings.PythonCompletionOptions completionSettings, IServiceContainer services) {
            _itemSource = new CompletionItemSource(docSource, completionSettings);
            _services = services;
        }

        public ServerSettings.PythonCompletionOptions Options {
            get => _itemSource.Options;
            set => _itemSource.Options = value;
        }

        public CompletionResult GetCompletions(IDocumentAnalysis analysis, SourceLocation location) {
            if (analysis.Document.ModuleType != ModuleType.User) {
                return CompletionResult.Empty;
            }

            var cb = PythonLanguageServiceProviderCallback.CreateTestInstance();

            //using (var client = CreateClientAsync(analysis.Document.Interpreter?.Configuration).WaitAndUnwrapExceptions()) {
            var client = _services.GetService<PythonLanguageClient>();
            var uri = analysis.Document.Uri;
            cb.SetClient(uri, client);

            //File.WriteAllText(uri.ToAbsolutePath(), analysis.Document.Content);
            RunningDocumentTableLspAdapter.OpenDocumentLspAsync(client, uri.ToAbsolutePath(), analysis.Document.Content).WaitAndUnwrapExceptions();

            // convert SourceLocation to Position
            Position postion = location;

            // note: CompletionList is from  Microsoft.Python.LanguageServer.Protocol and not the LSP.CompletionList version
            var res = cb.RequestAsync(
                new LSP.LspRequest<LSP.CompletionParams, CompletionList>(LSP.Methods.TextDocumentCompletionName),
                new LSP.CompletionParams {
                    TextDocument = new LSP.TextDocumentIdentifier { Uri = uri },
                    Position = new LSP.Position { Line = postion.line, Character = postion.character },
                    Context = new LSP.CompletionContext {
                        TriggerCharacter = "",
                        TriggerKind = LSP.CompletionTriggerKind.Invoked
                    }
                },
                CancellationToken.None
            ).WaitAndUnwrapExceptions();

            return new CompletionResult(res.items);
        }
    }
}
