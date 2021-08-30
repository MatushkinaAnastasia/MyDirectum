using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Parties.CompanyBase;

namespace Sungero.Parties.Shared
{
  partial class CompanyBaseFunctions
  {
    #region Проверка дублей
    
    /// <summary>
    /// Предупреждение о контрагенте с аналогичным ИНН или ИНН/КПП.
    /// </summary>
    /// <returns>Текст предупреждения с наименованием контрагента.</returns>
    [Public, Obsolete]
    public override string GetCounterpartyWithSameTinWarning()
    {
      if (_obj.Status != Sungero.CoreEntities.DatabookEntry.Status.Active)
        return string.Empty;
      
      return PublicFunctions.Counterparty.GetCounterpartyWithSameTinWarning(_obj.TIN, _obj.TRRC, _obj.Id);
    }
    
    /// <summary>
    /// Получить текст ошибки о наличии дублей контрагента.
    /// </summary>
    /// <returns>Текст ошибки.</returns>
    [Public]
    public override string GetCounterpartyDuplicatesErrorText()
    {
      // Не проверять для закрытых записей.
      if (_obj.Status != Sungero.CoreEntities.DatabookEntry.Status.Active)
        return string.Empty;
      
      var duplicates = this.GetDuplicates(true);
      var errorText = GenerateCounterpartyDuplicatesErrorText(duplicates, _obj.TRRC);
      return errorText;
    }
    
    /// <summary>
    /// Получить дубли организации.
    /// </summary>
    /// <param name="excludeClosed">Исключить закрытые записи.</param>
    /// <returns>Дубли организации.</returns>
    public override List<ICounterparty> GetDuplicates(bool excludeClosed)
    {
      // TODO Dmitriev_IA: На 53202, 69259 добавить поиск по имени.
      //                   По умолчанию для CompanyBase ищем по ИНН/КПП.
      return Functions.Module.Remote.GetDuplicateCounterparties(_obj.TIN, _obj.TRRC, string.Empty, _obj.Id, excludeClosed)
        .Where(x => CompanyBases.Is(x))
        .ToList();
    }
    
    #endregion
    
    /// <summary>
    /// Проверка, что контрагент - ИП.
    /// </summary>
    /// <returns>True, если контрагент - ИП, иначе False.</returns>
    public bool IsSelfEmployed()
    {
      if (!string.IsNullOrWhiteSpace(_obj.PSRN))
        return _obj.PSRN.Length == 15;
      
      if (!string.IsNullOrWhiteSpace(_obj.TIN))
        return _obj.TIN.Length == 12;
      
      if (!string.IsNullOrWhiteSpace(_obj.Name))
        return _obj.Name.StartsWith("ИП ");
      
      return false;
    }
    
    /// <summary>
    /// Проверка введенного ОГРН по количеству символов.
    /// </summary>
    /// <param name="psrn">ОГРН.</param>
    /// <returns>Пустая строка, если длина ОГРН в порядке.
    /// Иначе текст ошибки.</returns>
    public override string CheckPsrnLength(string psrn)
    {
      if (string.IsNullOrWhiteSpace(psrn))
        return string.Empty;
      
      psrn = psrn.Trim();
      
      return System.Text.RegularExpressions.Regex.IsMatch(psrn, @"(^\d{13}$)|(^\d{15}$)") ? string.Empty : CompanyBases.Resources.IncorrecPsrnLength;
    }
    
    /// <summary>
    /// Проверка введенного ОКПО по количеству символов.
    /// </summary>
    /// <param name="nceo">ОКПО.</param>
    /// <returns>Пустая строка, если длина ОКПО в порядке.
    /// Иначе текст ошибки.</returns>
    [Public]
    public override string CheckNceoLength(string nceo)
    {
      if (string.IsNullOrWhiteSpace(nceo))
        return string.Empty;
      
      return System.Text.RegularExpressions.Regex.IsMatch(nceo, @"(^\d{8}$)|(^\d{10}$)") ? string.Empty : CompanyBases.Resources.IncorrecNceoLength;
    }
  }
}