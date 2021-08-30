-- Значение по умолчанию признака Типовой для доп. соглашений, созданных до переноса признака в ContractualDocument.
if exists (select * from information_schema.COLUMNS where TABLE_NAME = 'Sungero_Content_EDoc' and COLUMN_NAME = 'IsStandard_Contrac_Sungero')
begin

update [dbo].[Sungero_Content_EDoc]
  set [IsStandard_Contrac_Sungero] = 0
  where [IsStandard_Contrac_Sungero] is null

end