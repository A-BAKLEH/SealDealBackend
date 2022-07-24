using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Clean.Architecture.SharedKernel;
public abstract class Entity<TId> : EntityBase
{
  public TId Id { get; set; }
}
