if exists(select 1 from INFORMATION_SCHEMA.TABLES where TABLE_NAME = 'Sungero_Docflow_Condition')
begin
  -- Переименование типа условия
  UPDATE Sungero_Docflow_Condition
  SET Name = 'Вложено приложение -'+SUBSTRING(Name, 7, len(Name)-6),
      NegationName = 'Не вложено приложение -'+SUBSTRING(Name, 7, len(Name)-6)
  WHERE ConditionType = 'HasAddenda'
    and SUBSTRING(Name, 1, 7) = 'Вложен '
end