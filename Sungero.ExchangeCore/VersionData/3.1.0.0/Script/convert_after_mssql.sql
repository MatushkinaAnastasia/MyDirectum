  -- Заполнение свойства Routing в абонентском ящике.
if exists(select *
          from information_schema.COLUMNS
          where TABLE_NAME = 'Sungero_ExCore_BoxBase' and COLUMN_NAME = 'Routing')
  begin
    UPDATE Sungero_ExCore_BoxBase
    SET Routing = 'BoxResponsible'
    WHERE isnull(ReceiveAssignm, 1) = 1

    UPDATE Sungero_ExCore_BoxBase
    SET Routing = 'NoAssignments'
    WHERE ReceiveAssignm = 0
  end