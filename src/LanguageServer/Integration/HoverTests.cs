﻿// Copyright(c) Microsoft Corporation
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

using System.Runtime.InteropServices;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Python.Analysis;
using Microsoft.Python.Analysis.Core.Interpreter;
using Microsoft.Python.Core;
using Microsoft.Python.Core.Services;
using Microsoft.Python.Core.Text;
using Microsoft.Python.LanguageServer.IntegrationTests.LspAdapters;
using Microsoft.Python.LanguageServer.Sources;
using Microsoft.Python.Parsing.Tests;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.VisualStudio.Threading;
using TestUtilities;
using UnitTests.LanguageServerClient;

namespace Microsoft.Python.LanguageServer.IntegrationTests {
    [TestClass]
    public class HoverTests : LanguageServerTestBase {
        public TestContext TestContext { get; set; }

        [TestInitialize]
        public void TestInitialize()
            => TestEnvironmentImpl.TestInitialize($"{TestContext.FullyQualifiedTestClassName}.{TestContext.TestName}");

        [TestCleanup]
        public void Cleanup() => TestEnvironmentImpl.TestCleanup();

        protected override async Task<IServiceManager> CreateServicesAsync(string root, InterpreterConfiguration configuration, string stubCacheFolderPath = null, IServiceManager sm = null, string[] searchPaths = null) {
            var services = await base.CreateServicesAsync(root, configuration, stubCacheFolderPath, sm, searchPaths);
            return await ServicesLspAdapter.CreateServicesAsync(root, configuration, stubCacheFolderPath, services, searchPaths);
        }

        [TestCategory("MPLS_LSP_INT")]
        [TestCategory("PYRIGHT_LSP_INT")]
        [TestMethod, Priority(0)]
        public async Task BasicTypes() {
            const string code = @"
x = 'str'

class C:
    '''Class C is awesome'''
    def method(self, a:int, b) -> float:
        '''Returns a float!!!'''
        return 1.0

def func(a, b):
    '''Does nothing useful'''
    return 1

y = func(1, 2)
string = str
";
            var analysis = await GetAnalysisAsync(code);
            var hs = new HoverSourceLSPAdapter(new PlainTextDocumentationSource());

            var hover = hs.GetHover(analysis, new SourceLocation(2, 2));
            hover.Contents.Value.Should().Be("x: str");
            AssertHover(hs, analysis, new SourceLocation(2, 2), "module datetime*", new SourceSpan(3, 1, 3, 9));

            hover = hs.GetHover(analysis, new SourceLocation(2, 7));
            hover.Should().BeNull();

            hover = hs.GetHover(analysis, new SourceLocation(4, 7));
            hover.Contents.Value.Should().Be("class C\n\nClass C is awesome");

            hover = hs.GetHover(analysis, new SourceLocation(6, 9));
            hover.Contents.Value.Should().Be("C.method(a: int, b) -> float\n\nReturns a float!!!");

            hover = hs.GetHover(analysis, new SourceLocation(6, 22));
            hover.Contents.Value.Should().Be("a: int");

            hover = hs.GetHover(analysis, new SourceLocation(10, 7));
            hover.Contents.Value.Should().Be("func(a, b)\n\nDoes nothing useful");

            hover = hs.GetHover(analysis, new SourceLocation(14, 2));
            hover.Contents.Value.Should().Be("y: int");

            //hover = hs.GetHover(analysis, new SourceLocation(15, 2));
            //hover.Contents.Value.Should().StartWith("class str\n\nstr(object='') -> str");
        }

        [TestCategory("MPLS_LSP_INT")]
        [TestCategory("PYRIGHT_LSP_INT")]
        [DataRow(false)]
        [DataRow(true)]
        [DataTestMethod]
        public async Task HoverSpanCheck(bool is3x) {
            const string code = @"
import datetime
datetime.datetime.now().day
";
            var analysis = await GetAnalysisAsync(code, is3x ? PythonVersions.LatestAvailable3X : PythonVersions.LatestAvailable2X);
            var hs = new HoverSourceLSPAdapter(new PlainTextDocumentationSource());

            AssertHover(hs, analysis, new SourceLocation(3, 2), "module datetime*", new SourceSpan(3, 1, 3, 9));
            AssertHover(hs, analysis, new SourceLocation(3, 11), "class datetime*", new SourceSpan(3, 9, 3, 18));
            AssertHover(hs, analysis, new SourceLocation(3, 20), @"datetime.now(tz: tzinfo) -> datetime*", new SourceSpan(3, 18, 3, 22));
        }

        [TestCategory("MPLS_LSP_INT")]
        [TestCategory("PYRIGHT_LSP_INT")]
        [TestMethod, Priority(0)]
        public async Task FromImportHover() {
            const string code = @"
from os import path as p
";
            var analysis = await GetAnalysisAsync(code, PythonVersions.LatestAvailable3X);
            var hs = new HoverSourceLSPAdapter(new PlainTextDocumentationSource());

            AssertHover(hs, analysis, new SourceLocation(2, 6), "module os*", new SourceSpan(2, 6, 2, 8));
            AssertHover(hs, analysis, new SourceLocation(2, 16), "module*", new SourceSpan(2, 16, 2, 20));
            AssertHover(hs, analysis, new SourceLocation(2, 24), "module*", new SourceSpan(2, 24, 2, 25));
        }

        [TestCategory("MPLS_LSP_INT")]
        [TestCategory("PYRIGHT_LSP_INT")]
        [TestMethod, Priority(0)]
        public async Task ImportAsNameHover() {
            const string code = @"
import datetime as d123
";
            var analysis = await GetAnalysisAsync(code, PythonVersions.LatestAvailable3X);
            var hs = new HoverSourceLSPAdapter(new PlainTextDocumentationSource());

            AssertHover(hs, analysis, new SourceLocation(2, 11), "module datetime*", new SourceSpan(2, 8, 2, 16));
            AssertHover(hs, analysis, new SourceLocation(2, 21), "module datetime*", new SourceSpan(2, 20, 2, 24));
        }

        [TestCategory("MPLS_LSP_INT")]
        [TestCategory("PYRIGHT_LSP_INT")]
        [TestMethod, Priority(0)]
        public async Task SelfHover() {
            const string code = @"
class Base(object):
    def fob_base(self):
       pass

class Derived(Base):
    def fob_derived(self):
       self.fob_base()
       pass
";
            var analysis = await GetAnalysisAsync(code, PythonVersions.LatestAvailable3X);
            var hs = new HoverSourceLSPAdapter(new PlainTextDocumentationSource());
            AssertHover(hs, analysis, new SourceLocation(3, 19), "class Base*", new SourceSpan(3, 18, 3, 22));
            AssertHover(hs, analysis, new SourceLocation(8, 8), "class Derived*", new SourceSpan(8, 8, 8, 12));
        }

        [TestCategory("MPLS_LSP_INT")]
        [TestCategory("PYRIGHT_LSP_INT")]
        [TestMethod, Priority(0)]
        public async Task HoverGenericClass() {
            const string code = @"
from typing import TypeVar, Generic

_T = TypeVar('_T')

class Box(Generic[_T]):
    def __init__(self, v: _T):
        self.v = v

    def get(self) -> _T:
        return self.v

boxedint = Box(1234)
x = boxedint.get()

boxedstr = Box('str')
y = boxedstr.get()
";
            var analysis = await GetAnalysisAsync(code, PythonVersions.LatestAvailable3X);
            var hs = new HoverSourceLSPAdapter(new PlainTextDocumentationSource());
            AssertHover(hs, analysis, new SourceLocation(14, 15), "Box.get() -> int", new SourceSpan(14, 13, 14, 17));
            AssertHover(hs, analysis, new SourceLocation(17, 15), "Box.get() -> str", new SourceSpan(17, 13, 17, 17));
        }

        [TestCategory("MPLS_LSP_INT")]
        [TestCategory("PYRIGHT_LSP_INT")]
        [TestMethod, Priority(0)]
        public async Task FromImportParts() {
            const string code = @"
from time import time
";
            var analysis = await GetAnalysisAsync(code);
            var hs = new HoverSourceLSPAdapter(new PlainTextDocumentationSource());
            AssertHover(hs, analysis, new SourceLocation(2, 7), @"module time*", new SourceSpan(2, 6, 2, 10));
            AssertHover(hs, analysis, new SourceLocation(2, 22), @"time() -> float*", new SourceSpan(2, 18, 2, 22));
        }

        [TestCategory("MPLS_LSP_INT")]
        [TestCategory("PYRIGHT_LSP_INT")]
        [TestMethod, Priority(0)]
        public async Task FromOsPath() {
            const string code = @"
from os.path import join as JOIN
";
            var analysis = await GetAnalysisAsync(code);
            var hs = new HoverSourceLSPAdapter(new PlainTextDocumentationSource());
            AssertHover(hs, analysis, new SourceLocation(2, 7), @"module os*", new SourceSpan(2, 6, 2, 8));

            var name = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? @"ntpath" : @"posixpath";
            AssertHover(hs, analysis, new SourceLocation(2, 10), $"module {name}*", new SourceSpan(2, 9, 2, 13));

            AssertHover(hs, analysis, new SourceLocation(2, 22), @"join(path: str, *paths: str) -> str", new SourceSpan(2, 21, 2, 25));
            AssertHover(hs, analysis, new SourceLocation(2, 30), @"join(path: str, *paths: str) -> str", new SourceSpan(2, 29, 2, 33));
        }

        [TestCategory("MPLS_LSP_INT")]
        [TestCategory("PYRIGHT_LSP_INT")]
        [TestMethod, Priority(0)]
        public async Task OsPath() {
            const string code = @"
import os.path as PATH
";
            var analysis = await GetAnalysisAsync(code);
            var hs = new HoverSourceLSPAdapter(new PlainTextDocumentationSource());
            AssertHover(hs, analysis, new SourceLocation(2, 9), @"module os*", new SourceSpan(2, 8, 2, 10));
            var name = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? @"ntpath" : @"posixpath";
            AssertHover(hs, analysis, new SourceLocation(2, 12), $"module {name}*", new SourceSpan(2, 11, 2, 15));
            AssertHover(hs, analysis, new SourceLocation(2, 20), $"module {name}*", new SourceSpan(2, 19, 2, 23));
        }

        [TestCategory("MPLS_LSP_INT")]
        [TestCategory("PYRIGHT_LSP_INT")]
        [TestMethod, Priority(0)]
        public async Task FStringExpressions() {
            const string code = @"
some = ''
f'{some'
Fr'{some'
f'{some}'
f'hey {some}'
";
            var analysis = await GetAnalysisAsync(code);
            var hs = new HoverSourceLSPAdapter(new PlainTextDocumentationSource());
            AssertHover(hs, analysis, new SourceLocation(3, 4), @"some: str", new SourceSpan(3, 4, 3, 8));
            AssertHover(hs, analysis, new SourceLocation(4, 5), @"some: str", new SourceSpan(4, 5, 4, 9));
            AssertHover(hs, analysis, new SourceLocation(5, 4), @"some: str", new SourceSpan(5, 4, 5, 8));
            hs.GetHover(analysis, new SourceLocation(6, 3)).Should().BeNull();
            AssertHover(hs, analysis, new SourceLocation(6, 8), @"some: str", new SourceSpan(6, 8, 6, 12));
        }

        [TestCategory("MPLS_LSP_INT")]
        [TestCategory("PYRIGHT_LSP_INT")]
        [TestMethod, Priority(0)]
        public async Task AssignmentExpressions() {
            const string code = @"
(a := 1)
";
            var analysis = await GetAnalysisAsync(code, PythonVersions.Required_Python38X);
            var hs = new HoverSourceLSPAdapter(new PlainTextDocumentationSource());
            AssertHover(hs, analysis, new SourceLocation(2, 2), @"a: int", new SourceSpan(2, 2, 2, 3));
        }

        [TestCategory("MPLS_LSP_INT")]
        [TestCategory("PYRIGHT_LSP_INT")]
        [TestMethod, Priority(0)]
        public async Task MissingSelf() {
            const string code = @"
class A:
    def __init__(self):
        self.instance_var = 1

    def do(self):
        y = instance_var
        y = do()
";
            var analysis = await GetAnalysisAsync(code);
            var hs = new HoverSourceLSPAdapter(new PlainTextDocumentationSource());
            AssertNoHover(hs, analysis, new SourceLocation(7, 15));
            AssertNoHover(hs, analysis, new SourceLocation(8, 14));
        }

        [TestCategory("MPLS_LSP_INT")]
        [TestCategory("PYRIGHT_LSP_INT")]
        [TestMethod, Priority(0)]
        public async Task MemberWithSelf() {
            const string code = @"
class A:
    def __init__(self):
        self.instance_var = 1

    def do(self):
        y = self.instance_var
        z1 = func()
        z2 = self.func()

    def func(self): ...
";
            var analysis = await GetAnalysisAsync(code);
            var hs = new HoverSourceLSPAdapter(new PlainTextDocumentationSource());
            AssertHover(hs, analysis, new SourceLocation(7, 19), @"instance_var: int", new SourceSpan(7, 17, 7, 30));
            AssertNoHover(hs, analysis, new SourceLocation(8, 15));
            AssertHover(hs, analysis, new SourceLocation(9, 20), @"A.func()", new SourceSpan(9, 18, 9, 23));
        }

        [TestCategory("MPLS_LSP_INT")]
        [TestCategory("PYRIGHT_LSP_INT")]
        [TestMethod, Priority(0)]
        public async Task ImmediateClassScopeVar() {
            const string code = @"
class A:
  x = 1
  y = x
";
            var analysis = await GetAnalysisAsync(code);
            var hs = new HoverSourceLSPAdapter(new PlainTextDocumentationSource());
            AssertHover(hs, analysis, new SourceLocation(4, 7), @"x: int", new SourceSpan(4, 7, 4, 8));
        }

        private static void AssertNoHover(HoverSourceLSPAdapter hs, IDocumentAnalysis analysis, SourceLocation position) {
            var hover = hs.GetHover(analysis, position);
            hover.Should().BeNull();
        }

        private static void AssertHover(HoverSourceLSPAdapter hs, IDocumentAnalysis analysis, SourceLocation sourceLocation, string hoverText, SourceSpan? span = null) {
            var hover = hs.GetHover(analysis, sourceLocation);

            if (hoverText.EndsWith("*")) {
                var obj = hover.Contents.Match(
                 firstMatch => firstMatch.Value, secondMatch => secondMatch[0].Value, thirdMatch => thirdMatch.Value);

                var hoverValue = (obj as MarkedString).Value;
                // Check prefix first, but then show usual message for mismatched value
                if (!hoverValue.StartsWith(hoverText.Remove(hoverText.Length - 1))) {
                    Assert.AreEqual(hoverText, hoverValue);
                }
            } else {
                var obj = hover.Contents.Match(
                    firstMatch => firstMatch.Value, secondMatch => secondMatch[0].Value, thirdMatch => thirdMatch.Value);

                Assert.AreEqual(hoverText, (obj as MarkedString).Value);
            }
            if (span.HasValue) {
                hover.Range.Should().Be((Core.Text.Range)span.Value);
            }
        }
    }
}