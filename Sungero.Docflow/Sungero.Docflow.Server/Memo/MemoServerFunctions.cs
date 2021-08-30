using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow.Memo;

namespace Sungero.Docflow.Server
{
  partial class MemoFunctions
  {
    /// <summary>
    /// Заполнить подписывающего в карточке документа.
    /// </summary>
    /// <param name="employee">Сотрудник.</param>
    [Remote]
    public override void SetDocumentSignatory(Company.IEmployee employee)
    {
      // Не перебивать подписанта при рассмотрении. US: 78188.
      if (CallContext.CalledFrom(ApprovalReviewAssignments.Info))
        return;

      base.SetDocumentSignatory(employee);
    }
    
    /// <summary>
    /// Признак того, что необходимо проверять наличие прав подписи на документ у сотрудника, указанного в качестве подписанта с нашей стороны.
    /// </summary>
    /// <returns>True - необходимо проверять, False - иначе.</returns>
    /// <remarks>Проверка прав подписи не проводится для служебной записки.</remarks>
    public override bool NeedValidateOurSignatorySignatureSetting()
    {
      return false;
    }
  }
}