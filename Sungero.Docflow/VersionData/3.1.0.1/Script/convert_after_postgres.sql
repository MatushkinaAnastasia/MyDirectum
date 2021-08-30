do $$
begin
if exists(select 1 from INFORMATION_SCHEMA.TABLES where TABLE_NAME = 'sungero_core_job')
then
  -- [RU] ConvertDocumentToPdf
  update sungero_core_job
  set (name, description) = ('Преобразование документов в PDF',
                             'Преобразование документов (исключая документы электронного обмена) в PDF с отметкой об электронной подписи')
  where jobid = uuid('133F7487-283E-476C-8774-8C87F4BC892A')
    and Name = 'Преобразование документов в PDF';
  -- [EN] ConvertDocumentToPdf
  update sungero_core_job
  set (name, description) = ('Convert documents to PDF',
                             'Convert documents (excluding electronic exchange documents) to PDF with an electronic signature mark')
  where jobid = uuid('133F7487-283E-476C-8774-8C87F4BC892A')
    and Name = 'TODO';
end if;
end$$;