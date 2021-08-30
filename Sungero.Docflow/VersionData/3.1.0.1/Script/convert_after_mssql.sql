if exists(select 1 from INFORMATION_SCHEMA.TABLES where TABLE_NAME = 'Sungero_Core_Job')
begin
  
  -- [RU] ConvertDocumentToPdf
  update Sungero_Core_Job
  set
    Name = 'Преобразование документов в PDF',
    Description = 'Преобразование документов (исключая документы электронного обмена) в PDF с отметкой об электронной подписи'
  where JobId = convert(uniqueidentifier, '133F7487-283E-476C-8774-8C87F4BC892A')
    and Name like 'Преобразование документов в PDF'
  
  -- [EN] ConvertDocumentToPdf
  update Sungero_Core_Job
  set
    Name = 'Convert documents to PDF',
    Description = 'Convert documents (excluding electronic exchange documents) to PDF with an electronic signature mark'
  where JobId = convert(uniqueidentifier, '133F7487-283E-476C-8774-8C87F4BC892A')
    and Name like 'TODO'
  
end