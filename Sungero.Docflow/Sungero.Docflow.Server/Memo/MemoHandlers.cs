using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow.Memo;

namespace Sungero.Docflow
{
  partial class MemoOurSignatoryPropertyFilteringServerHandler<T>
  {

    public override IQueryable<T> OurSignatoryFiltering(IQueryable<T> query, Sungero.Domain.PropertyFilteringEventArgs e) 
    {
      return query;
    }
  }

  partial class MemoServerHandlers
  {

    public override void Created(Sungero.Domain.CreatedEventArgs e)
    {
      base.Created(e);
      
      // Заполнить "Подписал".
      var employee = Company.Employees.Current;
      if (_obj.OurSignatory == null)
        _obj.OurSignatory = employee;
    }
  }
}