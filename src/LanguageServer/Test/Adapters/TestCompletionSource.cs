using Microsoft.Python.Analysis;
using Microsoft.Python.Analysis.Modules;
using Microsoft.Python.Core;
using Microsoft.Python.Core.Text;
using Microsoft.Python.LanguageServer.Completion;
using Microsoft.Python.LanguageServer.Tests.LanguageServer;
using System.Threading.Tasks;
using UnitTests.LanguageServerClient;

namespace Microsoft.Python.LanguageServer.Tests.Adapters {
    internal class TestCompletionSource {
        private readonly CompletionItemSource _itemSource;
        private readonly IServiceContainer _services;

        public TestCompletionSource(IDocumentationSource docSource, ServerSettings.PythonCompletionOptions completionSettings, IServiceContainer services) {
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

            using (var client = await CreateClientAsync()) {

            }

            return null;
        }


        private static async Task<PythonLanguageClient> CreateClientAsync() {

            var contentTypeName = "PYTHONFILE";

            var clientContext = new PythonLanguageClientContextFixed(
                contentTypeName,
                version.Configuration,
                null,
                Enumerable.Empty<string>()
            );

            var broker = new MockLanguageClientBroker();
            await PythonLanguageClient.EnsureLanguageClientAsync(
                sp,
                new JoinableTaskContext(),
                clientContext,
                broker
            );

            return PythonLanguageClient.FindLanguageClient(contentTypeName);
        }
    }
}
