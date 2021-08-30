-- Восстановление значений для атрибута DocumentId у элемента очереди выдачи прав
DO $$
begin
  if EXISTS(SELECT *
            FROM information_schema.tables
            WHERE TABLE_NAME = 'temp_dgarqueueitem')
  then
    update Sungero_ExCore_QueueItem
    set documentid_docflow_sungero = temp.documentid
    from temp_dgarqueueitem temp
    where sungero_excore_queueitem.id = temp.queueitemid;

    DROP TABLE temp_dgarqueueitem;
  end if;
end $$;