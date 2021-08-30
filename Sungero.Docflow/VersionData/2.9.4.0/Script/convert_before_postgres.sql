-- Перенос во временную таблицу значения Document у элемента очереди выдачи прав
do $$
begin
if exists(select *
          from information_schema.tables
          where table_name = 'temp_dgarqueueitem')
then
  drop table temp_dgarqueueitem;
end if;

if exists(select *
          from information_schema.columns
          where table_name = 'sungero_excore_queueitem' and column_name = 'document_docflow_sungero')
then
  create table temp_dgarqueueitem (
    QueueItemId integer,
    DocumentId  integer
  );
  insert into temp_dgarqueueitem
    select
      Id,
      Document_Docflow_Sungero
    from Sungero_ExCore_QueueItem;
end if;
end$$;