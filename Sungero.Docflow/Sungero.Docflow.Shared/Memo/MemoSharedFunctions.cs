using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow.Memo;

namespace Sungero.Docflow.Shared
{
  partial class MemoFunctions
  {
    /// <summary>
    /// Сменить доступность реквизитов документа.
    /// </summary>
    /// <param name="isEnabled">True, если свойства должны быть доступны.</param>
    /// <param name="isRepeatRegister">Перерегистрация.</param>
    public override void ChangeDocumentPropertiesAccess(bool isEnabled, bool isRepeatRegister)
    {
      base.ChangeDocumentPropertiesAccess(isEnabled, isRepeatRegister);
      
      var enabledState = !(_obj.InternalApprovalState == Docflow.OfficialDocument.InternalApprovalState.OnApproval ||
                           _obj.InternalApprovalState == Docflow.OfficialDocument.InternalApprovalState.PendingSign ||
                           _obj.InternalApprovalState == Docflow.OfficialDocument.InternalApprovalState.Signed ||
                           _obj.InternalApprovalState == Docflow.Memo.InternalApprovalState.PendingReview ||
                           _obj.InternalApprovalState == Docflow.Memo.InternalApprovalState.Reviewed);
      
      _obj.State.Properties.Addressee.IsEnabled = enabledState || _obj.Addressee == null;
    }
  }
}