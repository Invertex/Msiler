﻿using Mono.Cecil;
using System.Collections.Generic;
using System.Linq;

namespace Quart.Msiler.Lib
{
    public class MsilReader
    {
        private readonly ModuleDefinition _module;
        private string AssemblyName { get; set; }

        public MsilReader(string assemblyName) {
            this.AssemblyName = assemblyName;
            this._module = ModuleDefinition.ReadModule(assemblyName);
        }

        public IEnumerable<MethodEntity> EnumerateMethods() {
            var types = this._module.GetTypes();
            return
                from type in types
                from method in type.Methods
                let body = method.Body
                where method.HasBody
                let instructions = body.Instructions
                select new MethodEntity(method, instructions.ToList());
        }
    }
}