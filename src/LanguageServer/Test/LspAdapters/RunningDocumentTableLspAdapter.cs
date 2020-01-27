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
using System.Diagnostics;
using System.IO;
using System.Linq;
using Microsoft.Python.Analysis.Analyzer;
using Microsoft.Python.Analysis.Modules;
using Microsoft.Python.Core;
using Microsoft.Python.Core.Collections;
using Microsoft.Python.Core.Logging;
using Microsoft.Python.Analysis.Documents;
using Microsoft.Python.Analysis;
using UnitTests.LanguageServerClient;
using System.Threading.Tasks;
using LSP = Microsoft.VisualStudio.LanguageServer.Protocol;
using NSubstitute.Exceptions;

namespace Microsoft.Python.LanguageServer.Tests.LspAdapters {
    /// <summary>
    /// Represents set of files either opened in the editor or imported
    /// in order to provide analysis in open file. Rough equivalent of
    /// the running document table in Visual Studio, see
    /// "https://docs.microsoft.com/en-us/visualstudio/extensibility/internals/running-document-table"/>
    /// </summary>
    public sealed class RunningDocumentTableLspAdapter : IRunningDocumentTable, IDisposable {
        private readonly IServiceContainer _services;
        private readonly PythonLanguageClient _client;
        private readonly IRunningDocumentTable _rdt;

        private IModuleManagement _moduleManagement;
        private IModuleManagement ModuleManagement => _moduleManagement ?? (_moduleManagement = _services.GetService<IPythonInterpreter>().ModuleResolution);

        public RunningDocumentTableLspAdapter(IServiceContainer services, IRunningDocumentTable rdt, PythonLanguageClient client) {
            _services = services;
            _client = client ?? throw new ArgumentNotFoundException(nameof(PythonLanguageClient));
            _rdt = rdt ?? throw new ArgumentNotFoundException(nameof(IRunningDocumentTable)); ;
        }

        public event EventHandler<DocumentEventArgs> Opened {
            add { throw new NotImplementedException(); }
            remove { throw new NotImplementedException(); }
        }
        public event EventHandler<DocumentEventArgs> Closed {
            add { throw new NotImplementedException(); }
            remove { throw new NotImplementedException(); }
        }
        public event EventHandler<DocumentEventArgs> Removed {
            add { throw new NotImplementedException(); }
            remove { throw new NotImplementedException(); }
        }

        /// <summary>
        /// Returns collection of currently open or loaded modules.
        /// Does not include stubs or compiled/scraped modules.
        /// </summary>
        public IEnumerable<IDocument> GetDocuments() {
            return _rdt.GetDocuments();
        }

        public int DocumentCount {
            get {
                return _rdt.DocumentCount;
            }
        }

        /// <summary>
        /// Adds file to the list of available documents.
        /// </summary>
        /// <param name="uri">Document URI.</param>
        /// <param name="content">Document content</param>
        /// <param name="filePath">Optional file path, if different from the URI.</param>
        public IDocument OpenDocument(Uri uri, string content, string filePath = null) {
            var document = _rdt.OpenDocument(uri, content, filePath);
            OpenDocumentLspAsync(_client, document.Uri.AbsolutePath, content).WaitAndUnwrapExceptions();
            return document;
        }


        public static async Task<Uri> OpenDocumentLspAsync(PythonLanguageClient client, string sourcePath, string content) {
            var uri = new Uri(sourcePath, UriKind.Absolute);

            //var dir = Path.GetDirectoryName(uri.ToAbsolutePath());

            //if (!Directory.Exists(dir)) {
            //    Directory.CreateDirectory(dir);
            //}

            //File.WriteAllText(uri.ToAbsolutePath(), content);

            var openDocParams = new LSP.DidOpenTextDocumentParams() {
                TextDocument = new LSP.TextDocumentItem() {
                    Uri = uri,
                    Text = content,
                    Version = 0,
                }
            };

            await client.InvokeTextDocumentDidOpenAsync(openDocParams);

            return uri;
        }

        /// <summary>
        /// Adds library module to the list of available documents.
        /// </summary>
        public IDocument AddModule(ModuleCreationOptions mco) {
            var document = _rdt.AddModule(mco);
            return document;
        }

        public IDocument GetDocument(Uri documentUri) {
            return _rdt.GetDocument(documentUri);
        }

        public int LockDocument(Uri uri) {
            return _rdt.LockDocument(uri);
        }

        public int UnlockDocument(Uri uri) {
            return _rdt.UnlockDocument(uri);
        }

        public void CloseDocument(Uri documentUri) {
            _rdt.CloseDocument(documentUri);
        }

        public void ReloadAll() {
            _rdt.ReloadAll();
        }

        public void Dispose() {
        }
    }
}

