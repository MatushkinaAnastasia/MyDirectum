using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Domain.Shared;

namespace Sungero.Commons.Shared
{
  public class ModuleFunctions
  {
    #region Интеллектуальная обработка
    
    /// <summary>
    /// Получить значение объекта в виде строки.
    /// </summary>
    /// <param name="propertyValue">Объект.</param>
    /// <returns>Значение объекта в виде строки.</returns>
    /// <remarks>Для объектов типа Sungero.Domain.Shared.IEntity будет возвращена строка с ID сущности.</remarks>
    [Public]
    public static string GetValueAsString(object propertyValue)
    {
      if (propertyValue == null)
        return string.Empty;
      
      var propertyStringValue = propertyValue.ToString();
      if (propertyValue is Sungero.Domain.Shared.IEntity)
        propertyStringValue = ((Sungero.Domain.Shared.IEntity)propertyValue).Id.ToString();
      return propertyStringValue;
    }
    
    #endregion
  }
}