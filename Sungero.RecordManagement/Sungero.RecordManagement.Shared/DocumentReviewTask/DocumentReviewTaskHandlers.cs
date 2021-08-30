using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Content;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow;
using Sungero.RecordManagement.DocumentReviewTask;

namespace Sungero.RecordManagement
{
  partial class DocumentReviewTaskSharedHandlers
  {

    public virtual void ResolutionGroupCreated(Sungero.Workflow.Interfaces.AttachmentCreatedEventArgs e)
    {
      var task = ActionItemExecutionTasks.As(e.Attachment);
      if (task != null)
      {
        task.IsDraftResolution = true;
        var document = _obj.DocumentForReviewGroup.OfficialDocuments.FirstOrDefault();
        if (document != null)
          task.DocumentsGroup.OfficialDocuments.Add(document);
        foreach (var otherGroupAttachment in _obj.OtherGroup.All)
          task.OtherGroup.All.Add(otherGroupAttachment);
      }
    }

    public virtual void ResolutionGroupAdded(Sungero.Workflow.Interfaces.AttachmentAddedEventArgs e)
    {
      // В качестве проектов резолюции нельзя отправить поручения-непроекты.
      if (_obj.ResolutionGroup.ActionItemExecutionTasks.Any(a => a.IsDraftResolution != true))
      {
        foreach (var actionItem in _obj.ResolutionGroup.ActionItemExecutionTasks.Where(a => a.IsDraftResolution != true))
          _obj.ResolutionGroup.ActionItemExecutionTasks.Remove(actionItem);
        throw AppliedCodeException.Create(DocumentReviewTasks.Resources.FindNotDraftResolution);
      }
    }
    
    public virtual void DocumentForReviewGroupDeleted(Sungero.Workflow.Interfaces.AttachmentDeletedEventArgs e)
    {
      // Сброс на тему по умолчанию.
      using (TenantInfo.Culture.SwitchTo())
        _obj.Subject = Docflow.Resources.AutoformatTaskSubject;
      
      Docflow.PublicFunctions.Module.SynchronizeAddendaAndAttachmentsGroup(_obj.AddendaGroup, null);
    }

    public virtual void DocumentForReviewGroupAdded(Sungero.Workflow.Interfaces.AttachmentAddedEventArgs e)
    {
      var document = OfficialDocuments.As(e.Attachment);
      
      // Задать тему.
      using (TenantInfo.Culture.SwitchTo())
        _obj.Subject = Docflow.PublicFunctions.Module.TrimSpecialSymbols(DocumentReviewTasks.Resources.Consideration, document.Name);
      
      // Задать адресата.
      if (IncomingDocumentBases.Is(document))
        _obj.Addressee = IncomingDocumentBases.As(document).Addressee;
      if (Memos.Is(document))
        _obj.Addressee = Memos.As(document).Addressee;
      
      // Задать срок рассмотрения.
      var addressee = _obj.Addressee ?? Users.Current;
      _obj.Deadline = Docflow.PublicFunctions.DocumentKind.GetConsiderationDate(document.DocumentKind, addressee) ??
        Calendar.Now.AddWorkingDays(addressee, Functions.Module.Remote.GetDocumentReviewDefaultDays());
      if (!_obj.State.IsCopied)
        Docflow.PublicFunctions.Module.SynchronizeAddendaAndAttachmentsGroup(_obj.AddendaGroup, _obj.DocumentForReviewGroup.OfficialDocuments.FirstOrDefault());
      
      Docflow.PublicFunctions.OfficialDocument.DocumentAttachedInMainGroup(document, _obj);
      
      // Добавить вложения.
      Docflow.PublicFunctions.OfficialDocument.AddRelatedDocumentsToAttachmentGroup(document, _obj.OtherGroup);
    }

    public override void SubjectChanged(Sungero.Domain.Shared.StringPropertyChangedEventArgs e)
    {
      // TODO: удалить код после исправления бага 17930 (сейчас этот баг в TFS недоступен, он про автоматическое обрезание темы).
      if (e.NewValue != null && e.NewValue.Length > DocumentReviewTasks.Info.Properties.Subject.Length)
        _obj.Subject = e.NewValue.Substring(0, DocumentReviewTasks.Info.Properties.Subject.Length);
    }

    public virtual void DeadlineChanged(Sungero.Domain.Shared.DateTimePropertyChangedEventArgs e)
    {
      // Продублировать срок в крайний срок для списков.
      _obj.MaxDeadline = e.NewValue;
    }
  }
}