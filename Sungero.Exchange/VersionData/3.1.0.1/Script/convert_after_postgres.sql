do $$
begin
if exists(select 1 from INFORMATION_SCHEMA.TABLES where TABLE_NAME = 'sungero_core_job')
then
  -- [RU] BodyConverterJob
  update sungero_core_job
  set (name, description) = ('Преобразование документов электронного обмена в PDF',
                             'Преобразование документов электронного обмена в PDF с отметкой об электронной подписи')
  where jobid = uuid('65FA7815-170D-489C-B613-C2C2366161A4')
    and name = 'Преобразование документов электронного обмена';
  -- [EN] BodyConverterJob
  update sungero_core_job
  set (name, description) = ('Convert electronic exchange documents to PDF',
                             'Convert electronic exchange documents to PDF with an electronic signature mark')
  where jobid = uuid('65FA7815-170D-489C-B613-C2C2366161A4')
    and name = 'Convert electronic exchange documents';
end if;
end$$;