// Python Tools for Visual Studio
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
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnitTests.LanguageServerClient;
using LSP = Microsoft.VisualStudio.LanguageServer.Protocol;

namespace Microsoft.Python.LanguageServer.IntegrationTests.LspAdapters {

    internal class PythonLanguageServiceProviderCallback {
        private readonly IServiceProvider _serviceProvider;

        // Cache clients for the session to avoid UI thread marshalling
        private readonly ConcurrentDictionary<Uri, PythonLanguageClient> _clientCache;

        public PythonLanguageServiceProviderCallback(IServiceProvider serviceProvider) {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            _clientCache = new ConcurrentDictionary<Uri, PythonLanguageClient>(UriEqualityComparer.Default);
        }

        private PythonLanguageServiceProviderCallback() {
            _clientCache = new ConcurrentDictionary<Uri, PythonLanguageClient>(UriEqualityComparer.Default);
        }

        /// <summary>
        /// Test helper factory for creating an instance with no service provider.
        /// </summary>
        internal static PythonLanguageServiceProviderCallback CreateTestInstance() => new PythonLanguageServiceProviderCallback();

        public async Task<TOut> RequestAsync<TIn, TOut>(LspRequest<TIn, TOut> method, TIn param, CancellationToken cancellationToken) {
            // Note that the LSP types TIn and TOut are defined in an assembly
            // (Microsoft.VisualStudio.LanguageServer.Protocol) referenced by
            // LiveShare and that assembly may be from a different version than
            // the one referenced in PTVS. There is no binding redirect since
            // backwards compatibility is not guaranteed in that assembly, so
            // we cannot cast between TIn/TOut and our referenced types.
            // We can convert between our types and the ones passed in TIn and TOut
            // via the intermediate JSON format which is backwards compatible.
            switch (method.Name) {
                case Methods.InitializeName:
                    return Initialize<TOut>();
                case Methods.TextDocumentCompletionName:
                case Methods.TextDocumentHoverName:
                case Methods.TextDocumentSignatureHelpName:
                case Methods.TextDocumentDefinitionName:
                case Methods.TextDocumentReferencesName:
                case Methods.TextDocumentOnTypeFormattingName:
                case Methods.TextDocumentCodeActionName:
                case Methods.TextDocumentDocumentSymbolName:
                    return await DispatchToLanguageServer(method, param, cancellationToken);
                default:
                    throw new ArgumentException(method.Name);
            }
        }

        private PythonLanguageClient FindClientAsync(Uri uri) {
            if (uri == null) {
                return null;
            }

            if (!_clientCache.TryGetValue(uri, out var client)) {
                client = GetOrCreateClientAsync(uri);
                if (client != null) {
                    client = _clientCache.GetOrAdd(uri, client);
                }
            }

            return client;
        }

        private PythonLanguageClient GetOrCreateClientAsync(Uri uri) {

            var filePath = uri.LocalPath;
            if (string.IsNullOrEmpty(filePath)) {
                return null;
            }

            return PythonLanguageClient.FindLanguageClient("PythonFile");
        }

        /// <summary>
        /// Helper function for tests, enabling this class to be tested without needing
        /// a service provider or UI thread.
        /// </summary>
        internal void SetClient(Uri documentUri, PythonLanguageClient client) {
            _clientCache[documentUri] = client;
        }

        private static TOut Initialize<TOut>() {
            var capabilities = new ServerCapabilities {
                CompletionProvider = new LSP.CompletionOptions {
                    TriggerCharacters = new[] { "." }
                },
                SignatureHelpProvider = new SignatureHelpOptions {
                    TriggerCharacters = new[] { "(", ",", ")" }
                },
                HoverProvider = true,
                DefinitionProvider = true,
                ReferencesProvider = true
            };
            var result = new InitializeResult { Capabilities = capabilities };

            // Convert between our type and TOut via a JSON object
            try {
                var jsonObj = JObject.FromObject(result);
                return jsonObj.ToObject<TOut>();
            } catch (JsonException) {
                return default(TOut);
            }
        }
        private async Task<TOut> DispatchToLanguageServer<TIn, TOut>(LspRequest<TIn, TOut> method, TIn param, CancellationToken cancellationToken) {
            var uri = GetDocumentUri(param);
            var client = FindClientAsync(uri);
            if (client == null) {
                return default(TOut);
            }

            var result = await client.InvokeWithParameterObjectAsync<TOut>(method.Name, param, cancellationToken);
            return result;
        }

        public async Task TextDocumentDidChangeAsync(DidChangeTextDocumentParams param, CancellationToken cancellationToken) {
            var uri = GetDocumentUri(param);
            var client = FindClientAsync(uri);
            if (client == null) {
                return;
            }
            await client.InvokeTextDocumentDidChangeAsync(param);
            return;
        }

        public async Task ConfigurationChangeAsync(PythonLanguageClient client, DidChangeConfigurationParams param, CancellationToken cancellationToken) {
            if (client == null) {
                return;
            }
            await client.InvokeConfigurationChangeAsync(param);
            return;
        }


        private Uri GetDocumentUri<TIn>(TIn param) {
            Uri uri = null;

            // Convert to JSON object to get document URI
            try {
                var paramObj = JObject.FromObject(param);
                var uriObj = paramObj.SelectToken("textDocument.uri");
                if (uriObj is JValue uriVal && uriVal.Type == JTokenType.String) {
                    uri = new Uri((string)uriVal.Value);
                }
            } catch (JsonException) {
                return null;
            }

            return uri;
        }
    }
}
