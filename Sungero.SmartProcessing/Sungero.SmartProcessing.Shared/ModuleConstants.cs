using System;
using Sungero.Core;

namespace Sungero.SmartProcessing.Constants
{
  public static class Module
  {
    // Типы источников захвата.
    public static class CaptureSourceType
    {
      [Sungero.Core.Public]
      public const string Folder = "folder";
      
      [Sungero.Core.Public]
      public const string Mail = "mail";
    }
    
    // Названия тегов файла DeviceInfo.xml с информацией об устройствах ввода.
    public static class DcsDeviceInfoTagNames
    {
      // Корневой узел для электронной почты.
      [Sungero.Core.Public]
      public const string MailSourceInfo = "MailSourceInfo";
      
      // Узел c информацией об отправляемых в конечную систему файлах.
      [Sungero.Core.Public]
      public const string Files = "Files";
      
      // Узел c именем файла без пути.
      [Sungero.Core.Public]
      public const string FileDescription = "FileDescription";
      
      // Узел c именем захваченного файла относительно папки InputFiles.
      [Sungero.Core.Public]
      public const string FileName = "FileName";
    }
    
    // Названия тегов файла InputFiles.xml с информацией об отправляемых в систему файлах службой DCS.
    public static class DcsInputFilesTagNames
    {
      // Корневой узел.
      [Sungero.Core.Public]
      public const string InputFilesSection = "InputFilesSection";
      
      // Узел c информацией об отправляемых в конечную систему файлах.
      [Sungero.Core.Public]
      public const string Files = "Files";
      
      // Узел c именем файла без пути.
      [Sungero.Core.Public]
      public const string FileDescription = "FileDescription";
      
      // Узел c именем захваченного файла относительно папки InputFiles.
      [Sungero.Core.Public]
      public const string FileName = "FileName";
    }
    
    // Наименования для тела письма с электронной почты.
    public static class DcsMailBodyName
    {
      [Sungero.Core.Public]
      public const string Html = "body.html";
      
      [Sungero.Core.Public]
      public const string Txt = "body.txt";
    }
    
    // Названия тегов файла InstanceInfos.xml с информацией об экземплярах ввода и о захваченных файлах службой DCS.
    public static class DcsInstanceInfosTagNames
    {
      // Корневой узел для ввода из файловой системы.
      [Sungero.Core.Public]
      public const string CaptureInstanceInfoList = "CaptureInstanceInfoList";
      
      // Корневой узел для ввода с почтового сервера.
      [Sungero.Core.Public]
      public const string MailCaptureInstanceInfo = "MailCaptureInstanceInfo";
      
      // Узел c темой почтового сообщения, полученного по электронной почте.
      [Sungero.Core.Public]
      public const string Subject = "Subject";
      
      // Узел c информацией об отправителе письма.
      [Sungero.Core.Public]
      public const string From = "From";
      
      // Узел c информацией о получателях письма.
      [Sungero.Core.Public]
      public const string To = "To";
      
      // Узел c информацией о получателях копии письма.
      [Sungero.Core.Public]
      public const string CC = "CC";
      
      // Узел c информацией об одном получателе.
      [Sungero.Core.Public]
      public const string Recipient = "Recipient";
      
      // Атрибуты с информацией о контакте.
      public static class ContactAttributes
      {
        // Узел c адресом контакта.
        [Sungero.Core.Public]
        public const string Address = "Address";
        
        // Узел c именем контакта.
        [Sungero.Core.Public]
        public const string Name = "Name";
      }
    }
    
    /// <summary>
    /// Наименования фактов и полей фактов в правилах извлечения фактов Ario.
    /// </summary>
    /// <remarks>Составлен для версии Ario 1.7.</remarks>
    public static class ArioGrammars
    {
      /// <summary>
      /// Факт "Письмо".
      /// </summary>
      public static class LetterFact
      {
        /// <summary>
        /// Наименование факта.
        /// </summary>
        [Sungero.Core.Public]
        public const string Name = "Letter";
        
        /// <summary>
        /// Адресат письма.
        /// </summary>
        /// <remarks>Содержит информацию в формате "Фамилия И.О." или "Фамилия Имя Отчество".</remarks>
        [Sungero.Core.Public]
        public const string AddresseeField = "Addressee";
        
        /// <summary>
        /// Гриф доступа.
        /// </summary>
        /// <remarks>Гриф "Конфиденциально", "Для служебного пользования", "Коммерческая тайна".</remarks>
        [Sungero.Core.Public]
        public const string ConfidentialField = "Confidential";
        
        /// <summary>
        /// Организационно-правовая форма корреспондента.
        /// </summary>
        [Sungero.Core.Public]
        public const string CorrespondentLegalFormField = "CorrespondentLegalForm";
        
        /// <summary>
        /// Наименование корреспондента.
        /// </summary>
        [Sungero.Core.Public]
        public const string CorrespondentNameField = "CorrespondentName";
        
        /// <summary>
        /// Дата документа.
        /// </summary>
        [Sungero.Core.Public]
        public const string DateField = "Date";
        
        /// <summary>
        /// Номер документа.
        /// </summary>
        [Sungero.Core.Public]
        public const string NumberField = "Number";
        
        /// <summary>
        /// В ответ на дату документа.
        /// </summary>
        [Sungero.Core.Public]
        public const string ResponseToDateField = "ResponseToDate";
        
        /// <summary>
        /// В ответ на номер документа.
        /// </summary>
        [Sungero.Core.Public]
        public const string ResponseToNumberField = "ResponseToNumber";
        
        /// <summary>
        /// Тема документа.
        /// </summary>
        [Sungero.Core.Public]
        public const string SubjectField = "Subject";
      }
      
      /// <summary>
      /// Факт "Персона письма".
      /// </summary>
      public static class LetterPersonFact
      {
        /// <summary>
        /// Наименование факта.
        /// </summary>
        [Sungero.Core.Public]
        public const string Name = "LetterPerson";
        
        /// <summary>
        /// Имя.
        /// </summary>
        [Sungero.Core.Public]
        public const string NameField = "Name";
        
        /// <summary>
        /// Отчество.
        /// </summary>
        [Sungero.Core.Public]
        public const string PatrnField = "Patrn";
        
        /// <summary>
        /// Фамилия.
        /// </summary>
        [Sungero.Core.Public]
        public const string SurnameField = "Surname";
        
        /// <summary>
        /// Тип персоны.
        /// </summary>
        [Sungero.Core.Public]
        public const string TypeField = "Type";
        
        /// <summary>
        /// Типы персоны: "Подписант", "Исполнитель".
        /// </summary>
        public static class PersonTypes
        {
          [Sungero.Core.Public]
          public const string Signatory = "SIGNATORY";
          
          [Sungero.Core.Public]
          public const string Responsible = "RESPONSIBLE";
        }
      }
      
      /// <summary>
      /// Факт "Контрагент".
      /// </summary>
      public static class CounterpartyFact
      {
        /// <summary>
        /// Наименование факта.
        /// </summary>
        [Sungero.Core.Public]
        public const string Name = "Counterparty";
        
        /// <summary>
        /// Расчетный счет.
        /// </summary>
        [Sungero.Core.Public]
        public const string BankAccountField = "BankAccount";
        
        /// <summary>
        /// БИК.
        /// </summary>
        [Sungero.Core.Public]
        public const string BinField = "BIN";
        
        /// <summary>
        /// Тип контрагента.
        /// </summary>
        [Sungero.Core.Public]
        public const string CounterpartyTypeField = "CounterpartyType";
        
        /// <summary>
        /// Типы контрагента.
        /// </summary>
        public static class CounterpartyTypes
        {
          [Sungero.Core.Public]
          public const string Consignee = "CONSIGNEE";
          
          [Sungero.Core.Public]
          public const string Payer = "PAYER";
          
          [Sungero.Core.Public]
          public const string Shipper = "SHIPPER";
          
          [Sungero.Core.Public]
          public const string Supplier = "SUPPLIER";
          
          [Sungero.Core.Public]
          public const string Buyer = "BUYER";
          
          [Sungero.Core.Public]
          public const string Seller = "SELLER";
        }
        
        /// <summary>
        /// Организационно-правовая форма.
        /// </summary>
        [Sungero.Core.Public]
        public const string LegalFormField = "LegalForm";
        
        /// <summary>
        /// Наименование.
        /// </summary>
        [Sungero.Core.Public]
        public const string NameField = "Name";
        
        /// <summary>
        /// Имя подписанта документа.
        /// </summary>
        [Sungero.Core.Public]
        public const string SignatoryNameField = "SignatoryName";
        
        /// <summary>
        /// Отчество подписанта документа.
        /// </summary>
        [Sungero.Core.Public]
        public const string SignatoryPatrnField = "SignatoryPatrn";
        
        /// <summary>
        /// Фамилия подписанта документа.
        /// </summary>
        [Sungero.Core.Public]
        public const string SignatorySurnameField = "SignatorySurname";
        
        /// <summary>
        /// ИНН.
        /// </summary>
        [Sungero.Core.Public]
        public const string TinField = "TIN";
        
        /// <summary>
        /// Признак "ИНН корректен".
        /// </summary>
        [Sungero.Core.Public]
        public const string TinIsValidField = "TinIsValid";
        
        /// <summary>
        /// КПП.
        /// </summary>
        [Sungero.Core.Public]
        public const string TrrcField = "TRRC";
      }
      
      /// <summary>
      /// Факт "Документ".
      /// </summary>
      /// <remarks>Используется в договорах, актах.</remarks>
      public static class DocumentFact
      {
        /// <summary>
        /// Наименование факта.
        /// </summary>
        [Sungero.Core.Public]
        public const string Name = "Document";
        
        /// <summary>
        /// Дата документа.
        /// </summary>
        [Sungero.Core.Public]
        public const string DateField = "Date";
        
        /// <summary>
        /// Номер документа.
        /// </summary>
        [Sungero.Core.Public]
        public const string NumberField = "Number";
      }
      
      /// <summary>
      /// Факт "Дополнительное соглашение".
      /// </summary>
      public static class SupAgreementFact
      {
        /// <summary>
        /// Наименование факта.
        /// </summary>
        [Sungero.Core.Public]
        public const string Name = "SupAgreement";
        
        /// <summary>
        /// Номер ведущего документа (договора).
        /// </summary>
        [Sungero.Core.Public]
        public const string DocumentBaseNumberField = "DocumentBaseNumber";
        
        /// <summary>
        /// Дата ведущего документа (договора).
        /// </summary>
        [Sungero.Core.Public]
        public const string DocumentBaseDateField = "DocumentBaseDate";
        
        /// <summary>
        /// Дата.
        /// </summary>
        [Sungero.Core.Public]
        public const string DateField = "Date";
        
        /// <summary>
        /// Номер.
        /// </summary>
        [Sungero.Core.Public]
        public const string NumberField = "Number";
      }
      
      /// <summary>
      /// Факт "Сумма документа".
      /// </summary>
      public static class DocumentAmountFact
      {
        /// <summary>
        /// Наименование факта.
        /// </summary>
        [Sungero.Core.Public]
        public const string Name = "DocumentAmount";
        
        /// <summary>
        /// Целая часть.
        /// </summary>
        [Sungero.Core.Public]
        public const string AmountField = "Amount";
        
        /// <summary>
        /// Дробная часть.
        /// </summary>
        [Sungero.Core.Public]
        public const string AmountCentsField = "AmountCents";
        
        /// <summary>
        /// Валюта.
        /// </summary>
        [Sungero.Core.Public]
        public const string CurrencyField = "Currency";
        
        /// <summary>
        /// Целая часть суммы НДС.
        /// </summary>
        [Sungero.Core.Public]
        public const string VatAmountField = "VatAmount";
        
        /// <summary>
        /// Дробная часть суммы НДС.
        /// </summary>
        [Sungero.Core.Public]
        public const string VatAmountCentsField = "VatAmountCents";
      }
      
      /// <summary>
      /// Факт "Финансовый документ".
      /// </summary>
      public static class FinancialDocumentFact
      {
        /// <summary>
        /// Наименование факта.
        /// </summary>
        [Sungero.Core.Public]
        public const string Name = "FinancialDocument";
        
        /// <summary>
        /// Наименование ведущего документа.
        /// </summary>
        [Sungero.Core.Public]
        public const string DocumentBaseNameField = "DocumentBaseName";
        
        /// <summary>
        /// Номер ведущего документа.
        /// </summary>
        [Sungero.Core.Public]
        public const string DocumentBaseNumberField = "DocumentBaseNumber";
        
        /// <summary>
        /// Дата ведущего документа.
        /// </summary>
        [Sungero.Core.Public]
        public const string DocumentBaseDateField = "DocumentBaseDate";
        
        /// <summary>
        /// Дата.
        /// </summary>
        [Sungero.Core.Public]
        public const string DateField = "Date";
        
        /// <summary>
        /// Номер.
        /// </summary>
        [Sungero.Core.Public]
        public const string NumberField = "Number";
        
        /// <summary>
        /// Дата корректируемого документа.
        /// </summary>
        [Sungero.Core.Public]
        public const string CorrectionDateField = "CorrectionDate";
        
        /// <summary>
        /// Номер корректируемого документа.
        /// </summary>
        [Sungero.Core.Public]
        public const string CorrectionNumberField = "CorrectionNumber";
        
        /// <summary>
        /// Номер исправления.
        /// </summary>
        [Sungero.Core.Public]
        public const string RevisionNumberField = "RevisionNumber";
        
        /// <summary>
        /// Дата исправления.
        /// </summary>
        [Sungero.Core.Public]
        public const string RevisionDateField = "RevisionDate";
        
        /// <summary>
        /// Номер исправления корректировки.
        /// </summary>
        [Sungero.Core.Public]
        public const string CorrectionRevisionNumberField = "CorrectionRevisionNumber";
        
        /// <summary>
        /// Дата исправления корректировки.
        /// </summary>
        [Sungero.Core.Public]
        public const string CorrectionRevisionDateField = "CorrectionRevisionDate";
      }
      
      /// <summary>
      /// Факт "Номенклатура".
      /// </summary>
      public static class GoodsFact
      {
        /// <summary>
        /// Наименование факта.
        /// </summary>
        [Sungero.Core.Public]
        public const string Name = "Goods";
        
        /// <summary>
        /// Наименование товара.
        /// </summary>
        [Sungero.Core.Public]
        public const string NameField = "Name";
        
        /// <summary>
        /// Количество (объем) товара.
        /// </summary>
        [Sungero.Core.Public]
        public const string CountField = "Count";
        
        /// <summary>
        /// Наименование, условное обозначение единицы измерения.
        /// </summary>
        [Sungero.Core.Public]
        public const string UnitNameField = "UnitName";
        
        /// <summary>
        /// Цена за единицу измерения.
        /// </summary>
        [Sungero.Core.Public]
        public const string PriceField = "Price";
        
        /// <summary>
        /// Сумма НДС, потовару.
        /// </summary>
        [Sungero.Core.Public]
        public const string VatAmountField = "VatAmount";
        
        /// <summary>
        /// Стоимость с НДС, товара.
        /// </summary>
        [Sungero.Core.Public]
        public const string AmountField = "Amount";
      }
    }
    
    // Уровни вероятности.
    public static class PropertyProbabilityLevels
    {
      [Sungero.Core.Public]
      public const double Max = 90;

      [Sungero.Core.Public]
      public const double UpperMiddle = 75;
      
      [Sungero.Core.Public]
      public const double Middle = 50;
      
      [Sungero.Core.Public]
      public const double LowerMiddle = 25;
      
      [Sungero.Core.Public]
      public const double Min = 5;
    }
    
    // Html расширение.
    public static class HtmlExtension
    {
      [Sungero.Core.Public]
      public const string WithDot = ".html";
      
      [Sungero.Core.Public]
      public const string WithoutDot = "html";
    }
    
    // Html теги.
    public static class HtmlTags
    {
      [Sungero.Core.Public]
      public const string MaskForSearch = "<html";
      
      [Sungero.Core.Public]
      public const string StartTag = "<html>";
      
      [Sungero.Core.Public]
      public const string EndTag = "</html>";
    }
  }
}