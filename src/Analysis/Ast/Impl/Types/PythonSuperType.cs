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

using System.Collections.Generic;
using System.Linq;
using Microsoft.Python.Core;

namespace Microsoft.Python.Analysis.Types {
    internal sealed class PythonSuperType : PythonType, IPythonSuperType {
        /// <summary>
        /// more info at https://docs.python.org/3/library/functions.html#super
        /// </summary>
        /// <param name="location"></param>
        /// <param name="mro">Should be a list of IPythonType</param>
        public PythonSuperType(Location location, IReadOnlyList<IPythonType> mro)
            : base("super", location, string.Empty, BuiltinTypeId.Type) {
            Mro = mro;
        }

        public override string QualifiedName {
            get {
                return $":SuperType[{string.Join(",", Mro.Select(t => t.QualifiedName))}]";
            }
        }

        public IReadOnlyList<IPythonType> Mro { get; }

        public override IMember GetMember(string name) => Mro.MaybeEnumerate().Select(c => c.GetMember(name)).ExcludeDefault().FirstOrDefault();

        public override IEnumerable<string> GetMemberNames() => Mro.MaybeEnumerate().SelectMany(cls => cls.GetMemberNames()).Distinct();
    }
}

