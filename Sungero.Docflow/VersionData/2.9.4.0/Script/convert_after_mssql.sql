-- Восстановление значений для атрибута DocumentId у элемента очереди выдачи прав
if EXISTS(SELECT *
          FROM information_schema.tables
          WHERE TABLE_NAME = 'temp_DGARQueueItem')
  begin
    execute ('
      update [Sungero_ExCore_QueueItem]
      set DocumentId_Docflow_Sungero = temp.DocumentId
      from temp_DGARQueueItem temp
      where [Sungero_ExCore_QueueItem].Id = temp.QueueItemId
    ')
    DROP TABLE temp_DGARQueueItem
  end