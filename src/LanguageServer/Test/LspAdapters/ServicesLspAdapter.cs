using Microsoft.Python.Analysis.Core.Interpreter;
using Microsoft.Python.LanguageServer.Protocol;
using Microsoft.Python.Parsing.Tests;
using Microsoft.Python.Core;
using Microsoft.Python.Core.Text;
using Microsoft.VisualStudio.Threading;
using System.Linq;
using System.Threading.Tasks;
using UnitTests.LanguageServerClient;
using UnitTests.LanguageServerClient.Mocks;
using TestUtilities;
using System.IO;
using System;
using System.Threading;
using Microsoft.Python.Parsing;
using System.Collections.Generic;
using Microsoft.Python.LanguageServer.Indexing;
using Microsoft.Python.Core.Services;
using Microsoft.Python.Analysis;
using Microsoft.Python.Analysis.Documents;
using LSP = Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.Python.LanguageServer.Tests.LspAdapters {
    public class ServicesLspAdapter {
        static public async Task<PythonLanguageClient> CreateClientAsync(InterpreterConfiguration configuration) {

            var contentTypeName = "PythonFile";

            configuration = configuration ?? PythonVersions.LatestAvailable;

            var clientContext = new PythonLanguageClientContextFixed(
                contentTypeName,
                configuration,
                TestData.GetTestSpecificRootPath(),
                Enumerable.Empty<string>()
            );

            var broker = new MockLanguageClientBroker();
            await PythonLanguageClient.EnsureLanguageClientAsync(
                null,
                new JoinableTaskContext(),
                clientContext,
                broker
            );

            return PythonLanguageClient.FindLanguageClient(contentTypeName);
        }

        static public TextEdit[] FormatLine(string text, int line, PythonLanguageVersion languageVersion) { 
            var cb = PythonLanguageServiceProviderCallback.CreateTestInstance();

            var configuration = PythonVersions.GetRequiredCPythonConfiguration(languageVersion);

            using (var client = CreateClientAsync(configuration).WaitAndUnwrapExceptions()) {

                var modulePath = TestData.GetDefaultModulePath();
                var moduleDirectory = Path.GetDirectoryName(modulePath);

                var uri = new Uri(modulePath);
                cb.SetClient(uri, client);

             //   File.WriteAllText(uri.ToAbsolutePath(), text);
                RunningDocumentTableLspAdapter.OpenDocumentLspAsync(client, uri.ToAbsolutePath(), text).WaitAndUnwrapExceptions();

                var res = cb.RequestAsync(
                       new LSP.LspRequest<LSP.DocumentOnTypeFormattingParams, TextEdit[]>(LSP.Methods.TextDocumentOnTypeFormattingName),
                       new LSP.DocumentOnTypeFormattingParams {
                           TextDocument = new LSP.TextDocumentIdentifier { Uri = uri },
                           Position = new LSP.Position { Line = line, Character = 0 },
                           Options = new LSP.FormattingOptions() {
                              InsertSpaces = false,
                              TabSize = 0,
                           },
                           Character = ";"
                       },
                       CancellationToken.None
                   ).WaitAndUnwrapExceptions();

                return res;
            }
        }

        static public async Task<TextEdit[]> BlockFormat(string text, Position position, FormattingOptions options) {
            var cb = PythonLanguageServiceProviderCallback.CreateTestInstance();

            var configuration = PythonVersions.LatestAvailable;

            using (var client = await CreateClientAsync(configuration)) {

                var modulePath = TestData.GetDefaultModulePath();
                var moduleDirectory = Path.GetDirectoryName(modulePath);

                var uri = new Uri(modulePath);
                cb.SetClient(uri, client);

                //   File.WriteAllText(uri.ToAbsolutePath(), text);
                await RunningDocumentTableLspAdapter.OpenDocumentLspAsync(client, uri.ToAbsolutePath(), text);

                var res = await cb.RequestAsync(
                       new LSP.LspRequest<LSP.DocumentOnTypeFormattingParams, TextEdit[]>(LSP.Methods.TextDocumentOnTypeFormattingName),
                       new LSP.DocumentOnTypeFormattingParams {
                           TextDocument = new LSP.TextDocumentIdentifier { Uri = uri },
                           Position = new LSP.Position { Line = position.line, Character = position.character },
                           Options = new LSP.FormattingOptions() {
                               InsertSpaces = options.insertSpaces,
                               TabSize = options.tabSize,
                           },
                           Character = ":"
                       },
                       CancellationToken.None
                   );

                return res;
            }
        }


        static internal async Task ConfigurationChangeAsync(PythonLanguageClient client, PythonLanguageServiceProviderCallback cb) {
            var linting = new Dictionary<string, object> {
                { "linting", new Dictionary<string, object> {{"enabeld", true }} },
                { "memory", new Dictionary<string, object> {} }
            };

            Dictionary<string, object> settings = new Dictionary<string, object> {
                { "python",  linting}
            };

            await cb.ConfigurationChangeAsync(
                client,
                new LSP.DidChangeConfigurationParams { 
                    Settings = settings 
                }, 
                CancellationToken.None);

            await Task.Delay(1000);
        }


        static internal DocumentSymbol[] GetDocumentSymbol(string text, PythonLanguageVersion languageVersion) {
            var cb = PythonLanguageServiceProviderCallback.CreateTestInstance();

            var configuration = PythonVersions.GetRequiredCPythonConfiguration(languageVersion);

            using (var client = CreateClientAsync(configuration).WaitAndUnwrapExceptions()) {

                var modulePath = TestData.GetDefaultModulePath();
                var moduleDirectory = Path.GetDirectoryName(modulePath);

                var uri = new Uri(modulePath);
                cb.SetClient(uri, client);
                
                RunningDocumentTableLspAdapter.OpenDocumentLspAsync(client, uri.ToAbsolutePath(), text).WaitAndUnwrapExceptions();

                var res = cb.RequestAsync(
                       new LSP.LspRequest<LSP.DocumentSymbolParams, DocumentSymbol[]>(LSP.Methods.TextDocumentDocumentSymbolName),
                       new LSP.DocumentSymbolParams {
                           TextDocument = new LSP.TextDocumentIdentifier { Uri = uri },
                       },
                       CancellationToken.None
                   ).WaitAndUnwrapExceptions();

                return res;
            }
        }

        public static async Task<IServiceManager> CreateServicesAsync(string root, InterpreterConfiguration configuration, string stubCacheFolderPath = null, IServiceManager sm = null, string[] searchPaths = null) {
            var interpreter = sm.GetService<IPythonInterpreter>();
            var client = await ServicesLspAdapter.CreateClientAsync(interpreter.Configuration);
            sm.AddService(client);

            //Replace regular rdt with Lsp version
            var rdt = sm.GetService<IRunningDocumentTable>();
            sm.RemoveService(rdt);
            sm.AddService(new RunningDocumentTableLspAdapter(sm, rdt, client));

            return sm;
        }

        private static HierarchicalSymbol MakeHierarchicalSymbol(DocumentSymbol dSym) {
            return new HierarchicalSymbol(
                dSym.name,
                ToSymbolKind(dSym.kind),
                dSym.range,
                dSym.selectionRange,
                dSym.children.Length > 0 ? dSym.children.Select(MakeHierarchicalSymbol).ToList() : null);
        }

        private static Indexing.SymbolKind ToSymbolKind(Protocol.SymbolKind kind) {
            switch (kind) {
                case Protocol.SymbolKind.None:
                    return Indexing.SymbolKind.None;
                case Protocol.SymbolKind.File:
                    return Indexing.SymbolKind.File;
                case Protocol.SymbolKind.Module:
                    return Indexing.SymbolKind.Module;
                case Protocol.SymbolKind.Namespace:
                    return Indexing.SymbolKind.Namespace;
                case Protocol.SymbolKind.Package:
                    return Indexing.SymbolKind.Package;
                case Protocol.SymbolKind.Class:
                    return Indexing.SymbolKind.Class;
                case Protocol.SymbolKind.Method:
                    return Indexing.SymbolKind.Method;
                case Protocol.SymbolKind.Property:
                    return Indexing.SymbolKind.Property;
                case Protocol.SymbolKind.Field:
                    return Indexing.SymbolKind.Field;
                case Protocol.SymbolKind.Constructor:
                    return Indexing.SymbolKind.Constructor;
                case Protocol.SymbolKind.Enum:
                    return Indexing.SymbolKind.Enum;
                case Protocol.SymbolKind.Interface:
                    return Indexing.SymbolKind.Interface;
                case Protocol.SymbolKind.Function:
                    return Indexing.SymbolKind.Function;
                case Protocol.SymbolKind.Variable:
                    return Indexing.SymbolKind.Variable;
                case Protocol.SymbolKind.Constant:
                    return Indexing.SymbolKind.Constant;
                case Protocol.SymbolKind.String:
                    return Indexing.SymbolKind.String;
                case Protocol.SymbolKind.Number:
                    return Indexing.SymbolKind.Number;
                case Protocol.SymbolKind.Boolean:
                    return Indexing.SymbolKind.Boolean;
                case Protocol.SymbolKind.Array:
                    return Indexing.SymbolKind.Array;
                case Protocol.SymbolKind.Object:
                    return Indexing.SymbolKind.Object;
                case Protocol.SymbolKind.Key:
                    return Indexing.SymbolKind.Key;
                case Protocol.SymbolKind.Null:
                    return Indexing.SymbolKind.Null;
                case Protocol.SymbolKind.EnumMember:
                    return Indexing.SymbolKind.EnumMember;
                case Protocol.SymbolKind.Struct:
                    return Indexing.SymbolKind.Struct;
                case Protocol.SymbolKind.Event:
                    return Indexing.SymbolKind.Event;
                case Protocol.SymbolKind.Operator:
                    return Indexing.SymbolKind.Operator;
                case Protocol.SymbolKind.TypeParameter:
                    return Indexing.SymbolKind.TypeParameter;
                default:
                    throw new NotImplementedException($"{kind} is not a LSP's SymbolKind");
            }
        }
    }
}

