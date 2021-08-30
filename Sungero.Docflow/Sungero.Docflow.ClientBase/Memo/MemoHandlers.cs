using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow.Memo;

namespace Sungero.Docflow
{
  partial class MemoClientHandlers
  {
    public override IEnumerable<Enumeration> ExecutionStateFiltering(IEnumerable<Enumeration> query)
    {
      query = base.ExecutionStateFiltering(query);
      return query.Where(s => s != OfficialDocument.ExecutionState.OnReview);
    }
  }

}