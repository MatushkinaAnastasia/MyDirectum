using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow.SimpleDocument;
using Sungero.Domain.Shared;
using Sungero.Metadata;

namespace Sungero.Docflow.Server
{
  partial class SimpleDocumentFunctions
  {
    /// <summary>
    /// Определить, есть ли активные задачи согласования по регламенту документа.
    /// </summary>
    /// <returns>True, если есть.</returns>
    [Remote]
    public bool AnyApprovalTasksWithCurrentDocument()
    {
      var anyTasks = false;

      AccessRights.AllowRead(
        () =>
        {
          var docGuid = _obj.GetEntityMetadata().GetOriginal().NameGuid;
          var approvalTaskDocumentGroupGuid = Constants.Module.TaskMainGroup.ApprovalTask;
          anyTasks = ApprovalTasks.GetAll()
            .Where(t => t.Status == Workflow.Task.Status.InProcess ||
                   t.Status == Workflow.Task.Status.Suspended)
            .Where(t => t.AttachmentDetails
                   .Any(att => att.AttachmentId == _obj.Id && att.EntityTypeGuid == docGuid &&
                        att.GroupId == approvalTaskDocumentGroupGuid))
            .Any();
          
        });
      
      return anyTasks;
    }    
  }
}