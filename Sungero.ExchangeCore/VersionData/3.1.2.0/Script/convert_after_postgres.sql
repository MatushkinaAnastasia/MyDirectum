do $$
begin
-- Заполнение свойства Routing в абонентском ящике.
if exists(select *
          from information_schema.COLUMNS
          where TABLE_NAME = 'sungero_excore_boxbase' and COLUMN_NAME = 'routing')
then
  UPDATE sungero_excore_boxbase
  SET routing = 'CPResponsible'
  WHERE routing = 'PartyRespons';
end if;
end $$;