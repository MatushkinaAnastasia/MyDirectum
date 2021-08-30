using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Company;
using Sungero.Content;
using Sungero.Core;
using Sungero.CoreEntities;
using Sungero.Docflow.DocumentRegister;
using Sungero.Docflow.OfficialDocument;

namespace Sungero.Docflow
{
  partial class SkippedNumbersReportServerHandlers
  {
    
    public override void AfterExecute(Sungero.Reporting.Server.AfterExecuteEventArgs e)
    {
      Docflow.PublicFunctions.Module.DeleteReportData(SkippedNumbersReport.SkipsTableName, SkippedNumbersReport.ReportSessionId);
      Docflow.PublicFunctions.Module.DeleteReportData(SkippedNumbersReport.AvailableDocumentsTableName, SkippedNumbersReport.ReportSessionId);
    }
    
    public override void BeforeExecute(Sungero.Reporting.Server.BeforeExecuteEventArgs e)
    {
      if (SkippedNumbersReport.DocumentRegisterId.HasValue)
        SkippedNumbersReport.DocumentRegister = DocumentRegisters.Get(SkippedNumbersReport.DocumentRegisterId.Value);
      
      SkippedNumbersReport.CurrentDate = Calendar.Now;

      var documentRegister = SkippedNumbersReport.DocumentRegister;
      
      var documents = Enumerable.Empty<IOfficialDocument>().AsQueryable();
      AccessRights.AllowRead(() =>
                             { documents = Docflow.OfficialDocuments.GetAll()
                                 .Where(d => d.DocumentRegister == SkippedNumbersReport.DocumentRegister)
                                 .Where(d => d.RegistrationState == RegistrationState.Registered || d.RegistrationState == RegistrationState.Reserved); });
      
      #region Период формирования отчета и разрезы
      
      var baseDate = Calendar.UserNow;
      var periodOffset = SkippedNumbersReport.PeriodOffset.HasValue
        ? SkippedNumbersReport.PeriodOffset.Value
        : 0;
      
      // Признак того, что отчет запущен из диалога регистрации.
      var launchedFromDialog = SkippedNumbersReport.RegistrationDate.HasValue;
      
      if (launchedFromDialog)
      {
        baseDate = SkippedNumbersReport.RegistrationDate.Value;
        // По умолчанию для отчета из диалога регистрации берем данные за последний месяц.
        SkippedNumbersReport.Period = Constants.SkippedNumbersReport.Month;
        SkippedNumbersReport.PeriodOffset = 0;
      }
      
      if (SkippedNumbersReport.Period.Equals(Constants.SkippedNumbersReport.Year))
      {
        SkippedNumbersReport.PeriodBegin = Calendar.BeginningOfYear(baseDate.AddYears(periodOffset));
        SkippedNumbersReport.PeriodEnd = periodOffset == 0 ? Calendar.EndOfYear(baseDate) :
          Calendar.EndOfYear(baseDate.AddYears(periodOffset));
      }
      if (SkippedNumbersReport.Period.Equals(Constants.SkippedNumbersReport.Quarter))
      {
        SkippedNumbersReport.PeriodBegin = Docflow.PublicFunctions.AccountingDocumentBase.BeginningOfQuarter(baseDate.AddMonths(3 * periodOffset));
        SkippedNumbersReport.PeriodEnd = periodOffset == 0 ? Docflow.PublicFunctions.AccountingDocumentBase.EndOfQuarter(baseDate) :
          Docflow.PublicFunctions.AccountingDocumentBase.EndOfQuarter(baseDate.AddMonths(3 * periodOffset));
      }
      if (SkippedNumbersReport.Period.Equals(Constants.SkippedNumbersReport.Month))
      {
        SkippedNumbersReport.PeriodBegin = Calendar.BeginningOfMonth(baseDate.AddMonths(periodOffset));
        SkippedNumbersReport.PeriodEnd = periodOffset == 0 ? Calendar.EndOfMonth(baseDate) :
          Calendar.EndOfMonth(baseDate.AddMonths(periodOffset));
      }
      if (SkippedNumbersReport.Period.Equals(Constants.SkippedNumbersReport.Week))
      {
        SkippedNumbersReport.PeriodBegin = Calendar.BeginningOfWeek(baseDate.AddDays(7 * periodOffset));
        SkippedNumbersReport.PeriodEnd = periodOffset == 0 ? Calendar.EndOfWeek(baseDate) :
          Calendar.EndOfWeek(baseDate.AddDays(7 * periodOffset));
      }
      
      // Получить границы периода журнала регистрации.
      var registrationDate = launchedFromDialog ? SkippedNumbersReport.RegistrationDate.Value : SkippedNumbersReport.PeriodEnd.Value;
      DateTime? documentRegisterPeriodBegin = Functions.DocumentRegister.GetBeginPeriod(documentRegister, registrationDate);
      DateTime? documentRegisterPeriodEnd = Functions.DocumentRegister.GetEndPeriod(documentRegister, registrationDate) ?? SkippedNumbersReport.PeriodEnd.Value;
      
      // Начало расчетного периода.
      var periodBegin = SkippedNumbersReport.PeriodBegin;
      
      // Если отчет вызван из диалога регистрации взять "месяц назад" от даты регистрации.
      if (launchedFromDialog)
        periodBegin = registrationDate.AddMonths(-1);
      
      // Если начало указанного периода раньше начала периода журнала, то считать от последнего.
      if (documentRegisterPeriodBegin.HasValue && documentRegisterPeriodBegin > periodBegin)
        periodBegin = documentRegisterPeriodBegin;
      else if (!documentRegisterPeriodBegin.HasValue)
        documentRegisterPeriodBegin = Calendar.SqlMinValue;
      
      SkippedNumbersReport.PeriodBegin = periodBegin;
      
      // Конец расчетного периода.
      var periodEnd = launchedFromDialog ? SkippedNumbersReport.RegistrationDate.Value.EndOfDay() : SkippedNumbersReport.PeriodEnd;
      SkippedNumbersReport.PeriodEnd = periodEnd;
      
      var hasLeadingDocument = SkippedNumbersReport.LeadingDocument != null;
      var hasDepartment = SkippedNumbersReport.Department != null;
      var hasBusinessUnit = SkippedNumbersReport.BusinessUnit != null;
      
      // Отфильтровать документы по разрезам.
      if (hasLeadingDocument)
        documents = documents.Where(d => Equals(d.LeadingDocument, SkippedNumbersReport.LeadingDocument));
      
      if (hasDepartment)
        documents = documents.Where(d => Equals(d.Department, SkippedNumbersReport.Department));
      
      if (hasBusinessUnit)
        documents = documents.Where(d => Equals(d.BusinessUnit, SkippedNumbersReport.BusinessUnit));
      
      #endregion
      
      #region Генерация формата номера
      
      var numberFormat = string.Empty;
      foreach (var item in documentRegister.NumberFormatItems.OrderBy(x => x.Number))
      {
        var elementName = string.Empty;
        if (item.Element == DocumentRegisterNumberFormatItems.Element.Number)
          elementName = DocumentRegisters.Resources.NumberFormatNumber;
        else if (item.Element == DocumentRegisterNumberFormatItems.Element.Year2Place || item.Element == DocumentRegisterNumberFormatItems.Element.Year4Place)
          elementName = DocumentRegisters.Resources.NumberFormatYear;
        else if (item.Element == DocumentRegisterNumberFormatItems.Element.Quarter)
          elementName = DocumentRegisters.Resources.NumberFormatQuarter;
        else if (item.Element == DocumentRegisterNumberFormatItems.Element.Month)
          elementName = DocumentRegisters.Resources.NumberFormatMonth;
        else if (item.Element == DocumentRegisterNumberFormatItems.Element.LeadingNumber)
          elementName = DocumentRegisters.Resources.NumberFormatLeadingNumber;
        else if (item.Element == DocumentRegisterNumberFormatItems.Element.Log)
          elementName = DocumentRegisters.Resources.NumberFormatLog;
        else if (item.Element == DocumentRegisterNumberFormatItems.Element.RegistrPlace)
          elementName = DocumentRegisters.Resources.NumberFormatRegistrPlace;
        else if (item.Element == DocumentRegisterNumberFormatItems.Element.CaseFile)
          elementName = DocumentRegisters.Resources.NumberFormatCaseFile;
        else if (item.Element == DocumentRegisterNumberFormatItems.Element.DepartmentCode)
          elementName = DocumentRegisters.Resources.NumberFormatDepartmentCode;
        else if (item.Element == DocumentRegisterNumberFormatItems.Element.BUCode)
          elementName = DocumentRegisters.Resources.NumberFormatBUCode;
        else if (item.Element == DocumentRegisterNumberFormatItems.Element.DocKindCode)
          elementName = DocumentRegisters.Resources.NumberFormatDocKindCode;
        else if (item.Element == DocumentRegisterNumberFormatItems.Element.CPartyCode)
          elementName = DocumentRegisters.Resources.NumberFormatCounterpartyCode;
        
        numberFormat += elementName + item.Separator;
      }
      SkippedNumbersReport.NumberFormat = numberFormat;
      
      #endregion
      
      #region Границы индексов в выбранном периоде

      // Получить минимальный индекс по документам в периоде (при ручной регистрации мб нарушение следования индексов).
      var firstDocumentIndex = Functions.DocumentRegister.GetIndex(documents, periodBegin, periodEnd, false);
      
      // Получить индекс документа из предыдущего периода.
      var previousIndex = 0;
      if (periodBegin != documentRegisterPeriodBegin)
        previousIndex = Functions.DocumentRegister.FilterDocumentsByPeriod(documents, documentRegisterPeriodBegin,
                                                                           periodBegin.Value.AddDays(-1).EndOfDay())
          .Where(d => !firstDocumentIndex.HasValue || d.Index < firstDocumentIndex).Select(d => d.Index).OrderByDescending(a => a).FirstOrDefault() ?? 0;
      
      if (firstDocumentIndex == null)
        firstDocumentIndex = previousIndex + 1;
      
      var firstIndex = firstDocumentIndex < previousIndex ? firstDocumentIndex : previousIndex + 1;
      
      // Получить первый индекс документа следующего периода.
      var nextIndex = periodEnd != documentRegisterPeriodEnd ?
        Functions.DocumentRegister.GetIndex(documents, periodEnd.Value.AddDays(1).BeginningOfDay(), documentRegisterPeriodEnd, false) : null;
      
      // Если в следующем периоде ещё нет документов, то взять текущий индекс журнала.
      var leadingDocumentId = hasLeadingDocument
        ? SkippedNumbersReport.LeadingDocument.Id
        : 0;
      var departmentId = hasDepartment
        ? SkippedNumbersReport.Department.Id
        : 0;
      var businessUnitId = hasBusinessUnit
        ? SkippedNumbersReport.BusinessUnit.Id
        : 0;
      if (nextIndex == null)
      {
        nextIndex = Functions.DocumentRegister.GetCurrentNumber(documentRegister, registrationDate, leadingDocumentId, departmentId, businessUnitId) + 1;
      }
      
      // Получить индекс по зарегистрированным документам (при ручной регистрации мб нарушение следования индексов).
      var lastDocumentIndex = Functions.DocumentRegister.GetIndex(documents, periodBegin, periodEnd, true) ?? nextIndex - 1;
      var lastIndex = lastDocumentIndex >= nextIndex ? lastDocumentIndex : nextIndex - 1;
      
      // Для случая когда нет документов в периоде.
      if (lastIndex < firstIndex)
        lastIndex = firstIndex - 1;
      
      #endregion
      
      // Отфильтровать документы по найденным границам индексов и по периоду журнала регистрации.
      // Допускать документы с номером не соответствующим формату (Index = 0).
      documents = documents
        .Where(d => !documentRegisterPeriodBegin.HasValue || d.RegistrationDate >= documentRegisterPeriodBegin)
        .Where(d => !documentRegisterPeriodEnd.HasValue || d.RegistrationDate <= documentRegisterPeriodEnd)
        .Where(l => l.Index >= firstIndex && l.Index <= lastIndex ||
               (l.Index == 0 && l.RegistrationDate <= SkippedNumbersReport.PeriodEnd && l.RegistrationDate >= SkippedNumbersReport.PeriodBegin));
      
      // Заполнить маску для гиперссылки.
      if (documents.Count() > 0)
      {
        var link = Hyperlinks.Get(documents.First());
        var index = link.IndexOf("?type=");
        SkippedNumbersReport.hyperlinkMask = link.Substring(0, index) + "?type=DocGUID&id=DocId";
      }
      else
      {
        SkippedNumbersReport.hyperlinkMask = string.Empty;
      }
      
      #region Вычислить пропущенные индексы
      
      // Создать временную таблицу для списка номеров "подряд".
      var skipsTableName = Constants.SkippedNumbersReport.SkipsTableName;
      SkippedNumbersReport.SkipsTableName = skipsTableName;
      var skipedNumberList = new List<string>();
      var skipedNumbers = new List<Structures.SkippedNumbersReport.SkippedNumber>();
      var reportSessionId = Guid.NewGuid().ToString();
      SkippedNumbersReport.ReportSessionId = reportSessionId;
      
      // Заполнить таблицу номеров.
      var month = documentRegisterPeriodBegin.Value.Month < 10 ? string.Format("0{0}", documentRegisterPeriodBegin.Value.Month) : documentRegisterPeriodBegin.Value.Month.ToString();
      var day = documentRegisterPeriodBegin.Value.Day < 10 ? string.Format("0{0}", documentRegisterPeriodBegin.Value.Day) : documentRegisterPeriodBegin.Value.Day.ToString();
      var startDate = string.Format("{0}{1}{2}", documentRegisterPeriodBegin.Value.Year, month, day);
      
      month = documentRegisterPeriodEnd.Value.Month < 10 ? string.Format("0{0}", documentRegisterPeriodEnd.Value.Month) : documentRegisterPeriodEnd.Value.Month.ToString();
      day = documentRegisterPeriodEnd.Value.Day < 10 ? string.Format("0{0}", documentRegisterPeriodEnd.Value.Day) : documentRegisterPeriodEnd.Value.Day.ToString();
      var endDate = string.Format("{0}{1}{2}", documentRegisterPeriodEnd.Value.Year, month, day);
      
      var queryText = string.Format(Queries.SkippedNumbersReport.GetSkippedIndexes,
                                    SkippedNumbersReport.DocumentRegister.Id.ToString(),
                                    (firstIndex - 1).ToString(),
                                    (lastIndex + 1).ToString(),
                                    hasBusinessUnit.ToString(),
                                    businessUnitId.ToString(),
                                    hasDepartment.ToString(),
                                    departmentId.ToString(),
                                    hasLeadingDocument.ToString(),
                                    leadingDocumentId.ToString(),
                                    documentRegisterPeriodBegin.HasValue.ToString(),
                                    startDate,
                                    endDate);
      
      // Получить интервалы пропущеных индексов журнала в периоде.
      // Key - начало интервала, Value - окончиние интервала.
      var skippedIndexIntervals = new Dictionary<int, int>();
      using (var command = SQL.GetCurrentConnection().CreateCommand())
      {
        command.CommandText = queryText;
        var result = command.ExecuteReader();
        while (result.Read())
        {
          skippedIndexIntervals.Add((int)result[1], (int)result[0]);
        }
        result.Close();
      }
      
      // Заполнить отчет данными для пропущенных индексов.
      foreach (var interval in skippedIndexIntervals)
      {
        var intervalStart = interval.Key;
        var intervalEnd = interval.Value;

        // Три и более подряд идущих пропущеных индексов должны быть собраны в одну строку.
        var intervalLength = intervalEnd - intervalStart + 1;
        if (intervalLength >= 3)
        {
          skipedNumbers.Add(Structures.SkippedNumbersReport.SkippedNumber.Create(Docflow.Reports.Resources.SkippedNumbersReport.NumbersAreSkipped,
                                                                                 string.Format("{0}-{1}", intervalStart.ToString(), intervalEnd.ToString()),
                                                                                 intervalStart,
                                                                                 reportSessionId));
          skipedNumberList.Add(string.Format("{0}-{1}",
                                             intervalStart.ToString(),
                                             intervalEnd.ToString()));
          
          continue;
        }
        
        for (var i = intervalStart; i <= intervalEnd; i++)
        {
          skipedNumbers.Add(Structures.SkippedNumbersReport.SkippedNumber.Create(Docflow.Reports.Resources.SkippedNumbersReport.NumberIsSkipped,
                                                                                 i.ToString(),
                                                                                 i,
                                                                                 reportSessionId));
          skipedNumberList.Add(i.ToString());
        }
      }
      
      #endregion
      
      Functions.Module.WriteStructuresToTable(skipsTableName, skipedNumbers);
      
      // Получить 8-10 первых пропущенных номеров строкой. Для остальных указать общее количество.
      var skipedNumberCount = skipedNumberList.Count;
      var maxDisplayedNumberCount = 10;
      var minHiddenNumberCount = 3;
      var displayedValuesCount = skipedNumberCount;
      if (skipedNumberCount >= (maxDisplayedNumberCount + minHiddenNumberCount))
        displayedValuesCount = maxDisplayedNumberCount;
      else if (skipedNumberCount > maxDisplayedNumberCount)
        displayedValuesCount = skipedNumberCount - minHiddenNumberCount;
      
      SkippedNumbersReport.SkipedNumberList = string.Join(", ", skipedNumberList.ToArray(), 0, displayedValuesCount);
      var hiddenSkipedNumberCount = skipedNumberCount - displayedValuesCount;
      if (hiddenSkipedNumberCount > 0)
      {
        var numberLabel = Functions.Module.GetNumberDeclination(hiddenSkipedNumberCount,
                                                                Resources.SkippedNumbersReportNumber,
                                                                Resources.SkippedNumbersReportNumberGenetive,
                                                                Resources.SkippedNumbersReportNumberPlural);
        
        SkippedNumbersReport.SkipedNumberList += string.Format(Sungero.Docflow.Reports.Resources.SkippedNumbersReport.And, hiddenSkipedNumberCount, numberLabel);
      }
      
      // Создать таблицу для доступных пользователю документов.
      var availableDocuments = new List<Structures.SkippedNumbersReport.AvailableDocument>();
      var previousDocDate = Calendar.SqlMinValue;
      foreach (var document in documents.ToList().OrderBy(x => x.Index))
      {
        var numberOnFormat = document.Index != null && document.Index != 0;
        var canRead = document.AccessRights.CanRead();
        var inCorrectOrder = (previousDocDate <= document.RegistrationDate || !numberOnFormat) &&
          (document.RegistrationDate >= SkippedNumbersReport.PeriodBegin && document.RegistrationDate <= SkippedNumbersReport.PeriodEnd);
        availableDocuments.Add(Structures.SkippedNumbersReport.AvailableDocument.Create(document.Id, numberOnFormat, canRead, inCorrectOrder, reportSessionId));
        
        if (numberOnFormat && inCorrectOrder)
          previousDocDate = document.RegistrationDate.Value;
      }
      
      SkippedNumbersReport.AvailableDocumentsTableName = Constants.SkippedNumbersReport.AvailableDocumentsTableName;
      Functions.Module.WriteStructuresToTable(SkippedNumbersReport.AvailableDocumentsTableName, availableDocuments);
    }
  }
}