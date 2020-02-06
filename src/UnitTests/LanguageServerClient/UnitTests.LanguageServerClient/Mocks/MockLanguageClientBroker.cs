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
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.LanguageServer.Client;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using StreamJsonRpc;

namespace UnitTests.LanguageServerClient.Mocks {
    /// <summary>
    /// Minimal version of the broker used by VS
    /// (which is internal and so we can't instantiate).
    /// Note that this requires StreamJsonRpc to run, which in turn depends on
    /// a lot of other dlls that are normally available in VS install, but that
    /// need to be referenced by the test project in order to run outside VS.
    /// </summary>
    public class MockLanguageClientBroker : ILanguageClientBroker {
        private JsonRpc _rpc;
        private readonly string _rootPath;

        public MockLanguageClientBroker(string rootPath) {
            _rootPath = rootPath;
        }

        public async Task LoadAsync(ILanguageClientMetadata metadata, ILanguageClient client) {
            if (client == null) {
                throw new ArgumentNullException(nameof(client));
            }

            client.StartAsync += Client_StartAsync;
            await client.OnLoadedAsync();
        }

        private async Task Client_StartAsync(object sender, EventArgs args) {
            var client = (ILanguageClient)sender;
            var connection = await client.ActivateAsync(new CancellationTokenSource(TimeSpan.FromSeconds(15)).Token);
            await InitializeAsync(client, connection);
        }

        private async Task InitializeAsync(ILanguageClient client, Connection connection) {
            var messageHandler = new HeaderDelimitedMessageHandler(connection.Writer, connection.Reader);
            _rpc = new JsonRpc(messageHandler, this) {
                CancelLocallyInvokedMethodsWhenConnectionIsClosed = true
            };

            _rpc.StartListening();

            (client as ILanguageClientCustomMessage2)?.AttachForCustomMessageAsync(_rpc);

            var initParam = new InitializeParams {
                ProcessId = Process.GetCurrentProcess().Id,
                InitializationOptions = client.InitializationOptions,
                RootPath = _rootPath,
                RootUri = new Uri(_rootPath),
//                Capabilities = new ClientCapabilities() { Workspace = new WorkspaceClientCapabilities()}
            };

            initParam.Capabilities = new ClientCapabilities();
            initParam.Capabilities.Workspace = new WorkspaceClientCapabilities();
            initParam.Capabilities.Workspace.ApplyEdit = true;
            initParam.Capabilities.Workspace.WorkspaceEdit = new WorkspaceEditSetting();
            initParam.Capabilities.Workspace.WorkspaceEdit.DocumentChanges = true;
            initParam.Capabilities.Workspace.DidChangeConfiguration = new DynamicRegistrationSetting(false);
            initParam.Capabilities.Workspace.DidChangeWatchedFiles = new DynamicRegistrationSetting(false);
            initParam.Capabilities.Workspace.Symbol = new SymbolSetting();
            initParam.Capabilities.Workspace.Symbol.DynamicRegistration = false;
            initParam.Capabilities.Workspace.Symbol.SymbolKind = null;
            initParam.Capabilities.Workspace.ExecuteCommand = new DynamicRegistrationSetting(false);

            initParam.Capabilities.TextDocument = new TextDocumentClientCapabilities();
            initParam.Capabilities.TextDocument.Synchronization = new SynchronizationSetting();
            initParam.Capabilities.TextDocument.Synchronization.DynamicRegistration = false;
            initParam.Capabilities.TextDocument.Synchronization.WillSave = false;
            initParam.Capabilities.TextDocument.Synchronization.WillSaveWaitUntil = false;
            initParam.Capabilities.TextDocument.Synchronization.DidSave = true;
            initParam.Capabilities.TextDocument.Completion = new CompletionSetting();
            initParam.Capabilities.TextDocument.Completion.DynamicRegistration = false;
            initParam.Capabilities.TextDocument.Completion.CompletionItem = new CompletionItemSetting();
            initParam.Capabilities.TextDocument.Completion.CompletionItem.SnippetSupport = false;
            initParam.Capabilities.TextDocument.Completion.CompletionItem.CommitCharactersSupport = true;
            initParam.Capabilities.TextDocument.Hover = new HoverSetting();
            initParam.Capabilities.TextDocument.Hover.DynamicRegistration = false;
            initParam.Capabilities.TextDocument.Hover.ContentFormat = new MarkupKind[] { MarkupKind.PlainText };
            initParam.Capabilities.TextDocument.SignatureHelp = new SignatureHelpSetting();
            initParam.Capabilities.TextDocument.SignatureHelp.DynamicRegistration = false;
            initParam.Capabilities.TextDocument.SignatureHelp.SignatureInformation = new SignatureInformationSetting();
            initParam.Capabilities.TextDocument.SignatureHelp.SignatureInformation.DocumentationFormat = new MarkupKind[] { MarkupKind.PlainText };
            initParam.Capabilities.TextDocument.SignatureHelp.SignatureInformation.ParameterInformation = new ParameterInformationSetting();
            initParam.Capabilities.TextDocument.SignatureHelp.SignatureInformation.ParameterInformation.LabelOffsetSupport = true;
            initParam.Capabilities.TextDocument.References = new DynamicRegistrationSetting(false);
            initParam.Capabilities.TextDocument.DocumentHighlight = new DynamicRegistrationSetting(false);
            initParam.Capabilities.TextDocument.DocumentSymbol = new DocumentSymbolSetting();
            initParam.Capabilities.TextDocument.DocumentSymbol.DynamicRegistration = false;
            initParam.Capabilities.TextDocument.DocumentSymbol.SymbolKind = null;
            initParam.Capabilities.TextDocument.Formatting = new DynamicRegistrationSetting(false);
            initParam.Capabilities.TextDocument.RangeFormatting = new DynamicRegistrationSetting(false);
            initParam.Capabilities.TextDocument.OnTypeFormatting = new DynamicRegistrationSetting(false);
            initParam.Capabilities.TextDocument.Definition = new DynamicRegistrationSetting(false);
            initParam.Capabilities.TextDocument.Implementation = new DynamicRegistrationSetting(false);
            initParam.Capabilities.TextDocument.TypeDefinition = new DynamicRegistrationSetting(false);
            initParam.Capabilities.TextDocument.CodeAction = new CodeActionSetting();
            initParam.Capabilities.TextDocument.CodeAction.CodeActionLiteralSupport = new CodeActionLiteralSetting();
            initParam.Capabilities.TextDocument.CodeAction.CodeActionLiteralSupport.CodeActionKind = new CodeActionKindSetting();
            initParam.Capabilities.TextDocument.CodeAction.CodeActionLiteralSupport.CodeActionKind.ValueSet = new CodeActionKind[] { CodeActionKind.QuickFix, CodeActionKind.Refactor, CodeActionKind.RefactorExtract, CodeActionKind.RefactorInline, CodeActionKind.RefactorRewrite, CodeActionKind.Source, CodeActionKind.SourceOrganizeImports };
            initParam.Capabilities.TextDocument.CodeLens = new DynamicRegistrationSetting(false);
            initParam.Capabilities.TextDocument.DocumentLink = new DynamicRegistrationSetting(false);
            initParam.Capabilities.TextDocument.Rename = new DynamicRegistrationSetting(false);
            initParam.Capabilities.TextDocument.FoldingRange = new FoldingRangeSetting() { LineFoldingOnly = false, RangeLimit = null };
            initParam.Capabilities.TextDocument.PublishDiagnostics = new PublishDiagnosticsSetting();
            initParam.Capabilities.TextDocument.PublishDiagnostics.TagSupport = true;

            await _rpc.InvokeWithParameterObjectAsync(Methods.Initialize.Name, initParam, cancellationToken: CancellationToken.None);
            await _rpc.NotifyWithParameterObjectAsync(Methods.Initialized.Name, new InitializedParams());
            await client.OnServerInitializedAsync();
        }
    }
}
