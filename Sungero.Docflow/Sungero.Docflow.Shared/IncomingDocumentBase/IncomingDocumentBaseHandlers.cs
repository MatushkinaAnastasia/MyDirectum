using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow.IncomingDocumentBase;

namespace Sungero.Docflow
{
  partial class IncomingDocumentBaseSharedHandlers
  {
    public virtual void CorrespondentChanged(Sungero.Docflow.Shared.IncomingDocumentBaseCorrespondentChangedEventArgs e)
    {
      if (e.NewValue != null && !Equals(e.NewValue, e.OldValue) && _obj.InResponseTo != null &&
          !_obj.InResponseTo.Addressees.Any(a => Equals(a.Correspondent, _obj.Correspondent)))
        _obj.InResponseTo = null;
    }

    public virtual void InResponseToChanged(Sungero.Docflow.Shared.IncomingDocumentBaseInResponseToChangedEventArgs e)
    {
      if (Equals(e.NewValue, e.OldValue))
        return;
      
      _obj.Relations.AddFromOrUpdate(Constants.Module.ResponseRelationName, e.OldValue, e.NewValue);

      if (e.NewValue == null)
        return;
      
      var correspondents = e.NewValue.Addressees.Select(a => a.Correspondent).ToList();
      if (!correspondents.Contains(_obj.Correspondent))
        _obj.Correspondent = e.NewValue.IsManyAddressees.Value ? null : correspondents.FirstOrDefault();

      Functions.OfficialDocument.CopyProjects(e.NewValue, _obj);
    }

    public virtual void AddresseeChanged(Sungero.Docflow.Shared.IncomingDocumentBaseAddresseeChangedEventArgs e)
    {
      if (e.NewValue != null && !Equals(e.NewValue, e.OldValue) && _obj.BusinessUnit == null)
      {
        // Не чистить, если указан адресат с пустой НОР.
        if (e.NewValue.Department.BusinessUnit != null)
          _obj.BusinessUnit = e.NewValue.Department.BusinessUnit;
      }
    }
  }
}