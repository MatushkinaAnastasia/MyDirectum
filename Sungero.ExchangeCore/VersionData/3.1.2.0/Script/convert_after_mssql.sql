  -- Заполнение свойства Routing в абонентском ящике.
if exists(select *
          from information_schema.COLUMNS
          where TABLE_NAME = 'Sungero_ExCore_BoxBase' and COLUMN_NAME = 'Routing')
begin
  UPDATE Sungero_ExCore_BoxBase
  SET Routing = 'CPResponsible'
  WHERE Routing = 'PartyRespons'
end