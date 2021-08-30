do $$
begin
  -- Заполнение свойства Routing в абонентском ящике.
if exists(select *
          from information_schema.columns
          where table_name = 'sungero_excore_boxbase' and column_name = 'routing')
then
  UPDATE sungero_excore_boxbase
  SET routing = 'BoxResponsible'
  WHERE coalesce(receiveassignm, true) = true;

  UPDATE sungero_excore_boxbase
  SET routing = 'NoAssignments'
  WHERE receiveassignm = false;
end if;
end $$;