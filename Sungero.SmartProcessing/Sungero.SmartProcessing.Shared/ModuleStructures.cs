using System;
using System.Collections.Generic;
using System.Linq;
using Sungero.Company;
using Sungero.Core;
using Sungero.CoreEntities;

namespace Sungero.SmartProcessing.Structures.Module
{
  
  /// <summary>
  /// Пакет результатов обработки документов в Ario.
  /// </summary>
  [Public]
  partial class ArioPackage
  {
    // Результаты обработки документов в Ario.
    public List<Sungero.SmartProcessing.Structures.Module.IArioDocument> Documents { get; set; }
  }
  
  /// <summary>
  /// Распознанный в Ario документ.
  /// </summary>
  [Public]
  partial class ArioDocument
  {
    // Guid pdf версии документа.
    public string BodyGuid { get; set; }
    
    // Тело pdf версии документа из Ario.
    public byte[] BodyFromArio { get; set; }
    
    // Извлеченные из документа факты.
    public List<Sungero.Commons.Structures.Module.IArioFact> Facts { get; set; }
    
    // Запись в справочнике для сохранения результатов распознавания документа.
    public Sungero.Commons.IEntityRecognitionInfo RecognitionInfo { get; set; }
    
    // Исходный файл.
    public IBlob OriginalBlob { get; set; }
    
    // Признак обработанности документа Арио.
    public bool IsProcessedByArio { get; set; }
    
    // Признак распознанности документа Арио.
    public bool IsRecognized { get; set; }
  }
  
  /// <summary>
  /// Пакет документов.
  /// </summary>
  [Public]
  partial class DocumentPackage
  {
    /// <summary>
    /// Документ.
    /// </summary>
    public List<Sungero.SmartProcessing.Structures.Module.IDocumentInfo> DocumentInfos { get; set; }
    
    /// <summary>
    /// Пакет блобов с информацией о захваченных файлах.
    /// </summary>
    public IBlobPackage BlobPackage { get; set; }
    
    /// <summary>
    /// Ответственный за верификацию пакета документов.
    /// </summary>
    public IEmployee Responsible { get; set; }
  }
  
  /// <summary>
  /// Информация о документе.
  /// </summary>
  [Public]
  partial class DocumentInfo
  {
    // Документ.
    public Sungero.Docflow.IOfficialDocument Document { get; set; }
    
    // Распознанный в Ario документ.
    public Sungero.SmartProcessing.Structures.Module.IArioDocument ArioDocument { get; set; }
    
    // Является ли ведущим.
    public bool IsLeadingDocument { get; set; }
    
    // Не удалось зарегистрировать или пронумеровать.
    public bool RegistrationFailed { get; set; }
    
    // Признак распознанности документа Арио.
    public bool IsRecognized { get; set; }
    
    // Является телом письма.
    public bool IsEmailBody { get; set; }
    
    // Найдены по штрихкоду.
    public bool FoundByBarcode { get; set; }
    
    // Не удалось создать версию.
    public bool FailedCreateVersion { get; set; }
  }
  
  /// <summary>
  /// Полное и краткое ФИО персоны.
  /// </summary>
  [Public]
  partial class RecognizedPersonNaming
  {
    // Полное ФИО персоны.
    public string FullName { get; set; }
    
    // Фамилия И.О. персоны.
    public string ShortName { get; set; }
  }
  
  /// <summary>
  /// Результат распознавания валюты.
  /// </summary>
  [Public]
  partial class RecognizedCurrency
  {
    // Валюта.
    public Commons.ICurrency Currency { get; set; }
    
    // Признак - есть значение.
    public bool HasValue { get; set; }
    
    // Вероятность.
    public double? Probability { get; set; }
    
    // Факт
    public Sungero.Commons.Structures.Module.IArioFact Fact { get; set; }
  }

  /// <summary>
  /// Результат распознавания номера документа.
  /// </summary>
  [Public]
  partial class RecognizedDocumentNumber
  {
    public string Number { get; set; }
    
    public double? Probability { get; set; }
    
    public Sungero.Commons.Structures.Module.IArioFact Fact { get; set; }
  }
  
  /// <summary>
  /// Результат распознавания даты документа.
  /// </summary>
  [Public]
  partial class RecognizedDocumentDate
  {
    public DateTime? Date { get; set; }
    
    // Вероятность.
    public double? Probability { get; set; }
    
    public Sungero.Commons.Structures.Module.IArioFact Fact { get; set; }
  }
  
  /// <summary>
  /// Результат распознавания суммы.
  /// </summary>
  [Public]
  partial class RecognizedAmount
  {
    // Сумма.
    public double Amount { get; set; }
    
    // Признак - есть значение.
    public bool HasValue { get; set; }

    // Факт
    public Sungero.Commons.Structures.Module.IArioFact Fact { get; set; }
    
    // Вероятность.
    public double? Probability { get; set; }
  }
  
  /// <summary>
  /// Результат подбора сторон сделки для документа.
  /// </summary>
  [Public]
  partial class RecognizedDocumentParties
  {
    // НОР.
    public Sungero.SmartProcessing.Structures.Module.IRecognizedCounterparty BusinessUnit { get; set; }
    
    // Контрагент.
    public Sungero.SmartProcessing.Structures.Module.IRecognizedCounterparty Counterparty { get; set; }
    
    // НОР подобранная из ответственного сотрудника.
    public Sungero.Company.IBusinessUnit ResponsibleEmployeeBusinessUnit { get; set; }
    
    // Признак, что документ исходящий. Используется при создании счет-фактур.
    public bool? IsDocumentOutgoing { get; set; }
  }

  /// <summary>
  /// Контрагент, НОР и сопоставленный с ними факт с типом "Контрагент".
  /// </summary>
  [Public]
  partial class RecognizedCounterparty
  {
    // НОР.
    public Sungero.Company.IBusinessUnit BusinessUnit { get; set; }
    
    // Контрагент.
    public Sungero.Parties.ICounterparty Counterparty { get; set; }
    
    // Факт с типом контрагент, по полям которого осуществлялся поиск.
    public Sungero.Commons.Structures.Module.IArioFact Fact { get; set; }
    
    // Тип найденного значения (Buyer, Seller и т.д.).
    public string Type { get; set; }
    
    // Вероятность определения НОР.
    public double? BusinessUnitProbability { get; set; }
    
    // Вероятность определения КА.
    public double? CounterpartyProbability { get; set; }
  }
  
  /// <summary>
  /// Подписант (контакт или сотрудник) и сопоставленный с ним факт.
  /// </summary>
  [Public]
  partial class RecognizedOfficial
  {
    // Сотрудник.
    public Sungero.Company.IEmployee Employee { get; set; }
    
    // Контактное лицо.
    public Sungero.Parties.IContact Contact { get; set; }
    
    // Факт, по полям которого было найдено контактное лицо.
    public Sungero.Commons.Structures.Module.IArioFact Fact { get; set; }
    
    // Вероятность.
    public double? Probability { get; set; }
  }
  
  /// <summary>
  /// Договорной документ и сопоставленный с ним факт.
  /// </summary>
  [Public]
  partial class RecognizedContract
  {
    // Договорной документ.
    public Sungero.Contracts.IContractualDocument Contract { get; set; }
    
    // Факт, по полям которого был найден договорной документ.
    public Sungero.Commons.Structures.Module.IArioFact Fact { get; set; }
    
    // Вероятность.
    public double? Probability { get; set; }
  }
}