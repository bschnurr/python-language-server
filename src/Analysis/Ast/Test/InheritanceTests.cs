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

using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Python.Analysis.Tests.FluentAssertions;
using Microsoft.Python.Analysis.Types;
using Microsoft.Python.Analysis.Values;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TestUtilities;

namespace Microsoft.Python.Analysis.Tests {
    [TestClass]
    public class InheritanceTests : AnalysisTestBase {
        public TestContext TestContext { get; set; }

        [TestInitialize]
        public void TestInitialize() => TestEnvironmentImpl.TestInitialize($"{TestContext.FullyQualifiedTestClassName}.{TestContext.TestName}");

        [TestCleanup]
        public void TestCleanup() => TestEnvironmentImpl.TestCleanup();

        [TestMethod, Priority(0)]
        public async Task BaseFunctionCall() {
            const string code = @"
class Baze:
  def foo(self, x):
    return 'base'

class Derived(Baze):
  def foo(self, x):
    return x

y = Baze().foo(42.0)
";

            var analysis = await GetAnalysisAsync(code);
            // the class, for which we know parameter type initially
            analysis.Should().HaveClass(@"Baze")
                    .Which.Should().HaveMethod("foo")
                    .Which.Should().HaveSingleOverload()
                    .Which.Should().HaveParameterAt(1)
                    .Which.Should().HaveName("x");

            // its derived class
            analysis.Should().HaveClass("Derived")
                .Which.Should().HaveMethod("foo")
                .Which.Should().HaveSingleOverload()
                .Which.Should().HaveParameterAt(1)
                .Which.Should().HaveName("x");

            analysis.Should().HaveVariable("y").OfType(BuiltinTypeId.Str);
        }

        [TestMethod, Priority(0)]
        public async Task DerivedFunctionCall() {
            const string code = @"
class Baze:
  def foo(self, x):
    return 'base'

class Derived(Baze):
  def foo(self, x):
    return x

y = Derived().foo(42)
";

            var analysis = await GetAnalysisAsync(code);

            // the class, for which we know parameter type initially
            analysis.Should().HaveClass("Derived").Which.Should().HaveMethod("foo")
                .Which.Should().HaveSingleOverload()
                .Which.Should().HaveParameterAt(1)
                .Which.Should().HaveName("x");

            // its base class
            analysis.Should().HaveClass(@"Baze").Which.Should().HaveMethod("foo")
                .Which.Should().HaveSingleOverload()
                .Which.Should().HaveParameterAt(1)
                .Which.Should().HaveName("x");

            analysis.Should().HaveVariable("y").OfType(BuiltinTypeId.Int);
        }


        [TestMethod, Priority(0)]
        public async Task NamedTupleSubclass() {
            const string code = @"
import collections

class A(collections.namedtuple('A', [])):
    def __new__(cls):
        return super(A, cls).__new__(cls)

a = A()
";
            var analysis = await GetAnalysisAsync(code);
            analysis.Should().HaveVariable("a")
                .Which.Value.Should().BeAssignableTo<IPythonInstance>()
                .Which.Type.Name.Should().Be("A");
        }

        [TestMethod, Priority(0)]
        public async Task SingleInheritanceSuperShouldReturnBaseClassFunctions() {
            const string code = @"
class Baze:
    def base_func(self):
        return 1234

class Derived(Baze):
    def foo(self):
        x = super()
";

            var analysis = await GetAnalysisAsync(code);

            // the class, for which we know parameter type initially
            analysis.Should().HaveClass("Derived").Which.Should().HaveMethod("foo")
                .Which.Should().HaveVariable("x");

            analysis.Should().HaveClass("Derived").Which.Should().HaveMethod("foo")
                .Which.Should().HaveVariable("x")
                .Which.Value.Should().HaveMemberName("base_func");
        }


        [TestMethod, Priority(0)]
        public async Task SingleInheritanceSuperShouldReturnBaseClassFunctionsPython27() {
            const string code = @"
class Baze:
    def baze_foo(self):
        pass

class Derived(Baze):
    def foo(self):
        pass

d = Derived()

x = super(Derived, d)
";

            var analysis = await GetAnalysisAsync(code);

            // the class, for which we know parameter type initially
            analysis.Should().HaveVariable("x")
                .Which.Value.Should().HaveMemberName("baze_foo");
        }

        [TestMethod, Priority(0)]
        public async Task SingleInheritanceSuperWithNoBaseShouldReturnObject() {
            const string code = @"
class A():
    def foo(self):
        x = super()
";
            var analysis = await GetAnalysisAsync(code);

            // the class, for which we know parameter type initially
            analysis.Should().HaveClass("A").Which.Should().HaveMethod("foo")
                .Which.Should().HaveVariable("x");

            analysis.Should().HaveClass("A").Which.Should().HaveMethod("foo")
                .Which.Should().HaveVariable("x");
        }

        [TestMethod, Priority(0)]
        public async Task FunctionWithNoClassCallingSuperShouldFail() {
            const string code = @"
def foo(self):
    x = super()
";

            var analysis = await GetAnalysisAsync(code);
          
            analysis.Should().HaveFunction("foo")
                .Which.Should().HaveVariable("x")
                .Which.Name.Should().Be("x");
        }

        [TestMethod, Priority(0)]
        public async Task FunctionAssigningIntToSuperShouldBeInt() {
            const string code = @"
def foo(self):
    super = 1
";

            var analysis = await GetAnalysisAsync(code);

            analysis.Should().HaveFunction("foo")
                .Which.Should().HaveVariable("super")
                .Which.Value.IsOfType(BuiltinTypeId.Int);
        }

        [TestMethod, Priority(0)]
        public async Task SingleInheritanceSuperShouldReturnAllBaseClassMembers() {
            const string code = @"
class GrandParent:
    def grand_func(self):
        return 1

class Parent(GrandParent):
    def parent_func(self):
        return 2

class Child(Parent):
    def child_func(self):
        x = super()
";

            var analysis = await GetAnalysisAsync(code);

            var x = analysis.Should().HaveClass("Child").Which.Should().HaveMethod("child_func")
                .Which.Should().HaveVariable("x")
                .Which;

            x.Value.Should().HaveMemberName("grand_func");
            x.Value.Should().HaveMemberName("parent_func");
        }


        [TestMethod, Priority(0)]
        public async Task MultipleInheritanceSuperShould() {
            const string code = @"
class GrandParent:
    def dowork(self):
        return 1

class Dad(GrandParent):
    def dowork(self):
        return super().dowork()

class Mom():
    def dowork(self):
        return 2

class Child(Dad, Mom):
    def child_func(self):
        x = super()

";
            var analysis = await GetAnalysisAsync(code);

            analysis.Should().HaveClass("Child")
                .Which.Should().HaveMethod("child_func")
                .Which.Should().HaveVariable("x")
                .Which.Value.Should().BeAssignableTo<IPythonInstance>()
                .Which.Type.Name.Should().Be("Mom");
        }
    }
}
