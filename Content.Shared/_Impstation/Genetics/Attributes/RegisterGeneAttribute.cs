using Content.Shared._Impstation.Genetics.Genes;
using JetBrains.Annotations;
using System;
using System.Collections.Generic;
using System.Text;

namespace Content.Shared._Impstation.Genetics.Attributes;

[AttributeUsage(AttributeTargets.Class, Inherited = false)]
[BaseTypeRequired(typeof(BaseGene))]
[MeansImplicitUse]
public sealed class RegisterGeneAttribute : Attribute;
