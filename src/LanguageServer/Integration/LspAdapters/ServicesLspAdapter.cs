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
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Python.Analysis;
using Microsoft.Python.Analysis.Core.Interpreter;
using Microsoft.Python.Analysis.Documents;
using Microsoft.Python.Core;
using Microsoft.Python.Core.Services;
using Microsoft.Python.Core.Text;
using Microsoft.Python.LanguageServer.Indexing;
using Microsoft.Python.LanguageServer.Protocol;
using Microsoft.Python.Parsing;
using Microsoft.Python.Parsing.Tests;
using Microsoft.VisualStudio.Threading;
using TestUtilities;
using UnitTests.LanguageServerClient;
using UnitTests.LanguageServerClient.Mocks;
using LSP = Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.Python.LanguageServer.IntegrationTests.LspAdapters {
    public class ServicesLspAdapter {
        public static async Task<IServiceManager> CreateServicesAsync(string root, InterpreterConfiguration configuration, string stubCacheFolderPath = null, IServiceManager sm = null, string[] searchPaths = null) {
            var interpreter = sm.GetService<IPythonInterpreter>();
            var client = await CreateClientAsync(interpreter.Configuration);
            sm.AddService(client);

            //Replace regular rdt with Lsp version
            var rdt = sm.GetService<IRunningDocumentTable>();
            sm.RemoveService(rdt);
            sm.AddService(new RunningDocumentTableLspAdapter(sm, rdt, client));

            return sm;
        }

        static public async Task<PythonLanguageClient> CreateClientAsync(InterpreterConfiguration configuration) {
            var contentTypeName = "PythonFile";
            configuration = configuration ?? PythonVersions.LatestAvailable;
            var rootPath = TestData.GetTestSpecificRootPath();

            var clientContext = new PythonLanguageClientContextFixed(
                contentTypeName,
                configuration,
                rootPath,
                Enumerable.Empty<string>()
            );

            var broker = new MockLanguageClientBroker(rootPath);
            await PythonLanguageClient.EnsureLanguageClientAsync(
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
    }
}

