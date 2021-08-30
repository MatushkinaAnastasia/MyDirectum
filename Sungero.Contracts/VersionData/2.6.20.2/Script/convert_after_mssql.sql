-- Восстановление значений для атрибута IsStandard у договорных документов
if EXISTS (SELECT * FROM information_schema.tables WHERE TABLE_NAME = 'ContractIsStandard_Temp')
begin
  if EXISTS (SELECT * FROM information_schema.tables WHERE table_name = 'Sungero_Content_EDoc')
  begin
    execute('
      UPDATE Sungero_Content_EDoc
        SET IsStandard_Contrac_Sungero = CIS_T.IsStandard
        FROM ContractIsStandard_Temp CIS_T
        WHERE Sungero_Content_EDoc.Id = CIS_T.ContractId
    ')
    DROP TABLE ContractIsStandard_Temp
  end
end