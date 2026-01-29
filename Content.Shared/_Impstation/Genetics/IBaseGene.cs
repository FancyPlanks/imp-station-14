using System;
using System.Collections.Generic;
using System.Text;

namespace Content.Shared._Impstation.Genetics;

[NotContentImplementable]
public partial interface IBaseGene
{
    public virtual void OnGeneAdded(IEntityManager entManager, EntityUid host) { }

    public virtual void OnGeneRemoved() { }

    public virtual void ApplyPositiveEffect() { }
    public virtual void ApplyNegativeEffect() { }
    public virtual void ActivateEffect() { }

}
