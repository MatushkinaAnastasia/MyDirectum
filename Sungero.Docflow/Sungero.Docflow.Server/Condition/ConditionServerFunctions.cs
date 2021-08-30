using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow.Condition;
using Sungero.Docflow.ConditionBase;

namespace Sungero.Docflow.Server
{
  partial class ConditionFunctions
  {
    /// <summary>
    /// Создание условия.
    /// </summary>
    /// <returns>Условие.</returns>
    [Remote]
    public static ICondition CreateCondition()
    {
      return Conditions.Create();
    }
    
    public override string GetConditionName()
    {
      using (TenantInfo.Culture.SwitchTo())
      {
        if (_obj.ConditionType == Sungero.Docflow.Condition.ConditionType.Addressee)
        {
          var addressees = _obj.Addressees.Select(a => a.Addressee.Person.ShortName).ToList();
          var conditionName = Functions.ConditionBase.ConditionMultiSelectNameBuilder(addressees);
          return Conditions.Resources.TitleAddresseeFormat(conditionName);
        }
      }
      return base.GetConditionName();
    }
  }
}