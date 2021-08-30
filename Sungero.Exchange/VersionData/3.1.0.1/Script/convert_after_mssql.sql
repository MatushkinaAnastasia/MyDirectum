if exists(select 1 from INFORMATION_SCHEMA.TABLES where TABLE_NAME = 'Sungero_Core_Job')
begin
  
  -- [RU] BodyConverterJob
  update Sungero_Core_Job
  set
    Name = 'Преобразование документов электронного обмена в PDF',
    Description = 'Преобразование документов электронного обмена в PDF с отметкой об электронной подписи'
  where JobId = convert(uniqueidentifier, '65FA7815-170D-489C-B613-C2C2366161A4')
    and Name like 'Преобразование документов электронного обмена'
  
  -- [EN] BodyConverterJob
  update Sungero_Core_Job
  set
    Name = 'Convert electronic exchange documents to PDF',
    Description = 'Convert electronic exchange documents to PDF with an electronic signature mark'
  where JobId = convert(uniqueidentifier, '65FA7815-170D-489C-B613-C2C2366161A4')
    and Name like 'Convert electronic exchange documents'
  
end