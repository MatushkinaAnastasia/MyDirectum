using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow.FreeApprovalTask;

namespace Sungero.Docflow
{

  partial class FreeApprovalTaskSharedHandlers
  {

    public override void SubjectChanged(Sungero.Domain.Shared.StringPropertyChangedEventArgs e)
    {
      // TODO: удалить код после исправления бага 17930 (сейчас этот баг в TFS недоступен, он про автоматическое обрезание темы).
      if (e.NewValue != null && e.NewValue.Length > FreeApprovalTasks.Info.Properties.Subject.Length)
        _obj.Subject = e.NewValue.Substring(0, FreeApprovalTasks.Info.Properties.Subject.Length);
    }

    public virtual void ForApprovalGroupDeleted(Sungero.Workflow.Interfaces.AttachmentDeletedEventArgs e)
    {
      Functions.Module.SynchronizeAddendaAndAttachmentsGroup(_obj.AddendaGroup, null);
      // Очистить группу "Дополнительно".
      var document = OfficialDocuments.As(e.Attachment);
      if (OfficialDocuments.Is(document))
        Functions.OfficialDocument.RemoveRelatedDocumentsFromAttachmentGroup(OfficialDocuments.As(document), _obj.OtherGroup);
      
      _obj.Subject = Docflow.Resources.AutoformatTaskSubject;
    }

    public virtual void ForApprovalGroupAdded(Sungero.Workflow.Interfaces.AttachmentAddedEventArgs e)
    {
      var document = _obj.ForApprovalGroup.ElectronicDocuments.First();
      
      // Получить ресурсы в культуре тенанта.
      using (TenantInfo.Culture.SwitchTo())
        _obj.Subject = Functions.Module.TrimSpecialSymbols(FreeApprovalTasks.Resources.TaskSubject, document.Name);

      if (!_obj.State.IsCopied)
      {
        Functions.Module.SynchronizeAddendaAndAttachmentsGroup(_obj.AddendaGroup, document);
        if (OfficialDocuments.Is(document))
          Functions.OfficialDocument.AddRelatedDocumentsToAttachmentGroup(OfficialDocuments.As(document), _obj.OtherGroup);
      }
      
      if (OfficialDocuments.Is(document))
        Functions.OfficialDocument.DocumentAttachedInMainGroup(OfficialDocuments.As(document), _obj);
    }

  }
}