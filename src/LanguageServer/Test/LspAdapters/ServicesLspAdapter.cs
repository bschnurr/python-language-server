using Microsoft.Python.Analysis.Core.Interpreter;
using Microsoft.Python.LanguageServer.Protocol;
using Microsoft.Python.LanguageServer.Tests.LanguageServer;
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

    }
}

