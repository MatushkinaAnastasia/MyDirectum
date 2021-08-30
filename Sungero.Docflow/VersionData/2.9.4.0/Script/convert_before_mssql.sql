-- Перенос во временную таблицу значения Document у элемента очереди выдачи прав
if exists(select *
          from information_schema.tables
          where TABLE_NAME = 'temp_DGARQueueItem')
  drop table temp_DGARQueueItem

if exists(select *
          from information_schema.COLUMNS
          where TABLE_NAME = 'Sungero_ExCore_QueueItem' and COLUMN_NAME = 'Document_Docflow_Sungero')
  begin
    execute ('
      create table temp_DGARQueueItem (
        QueueItemId integer,
        DocumentId  integer
      );
      insert into temp_DGARQueueItem
        select
          [Id],
          [Document_Docflow_Sungero]
        from [Sungero_ExCore_QueueItem]
  ')
  end