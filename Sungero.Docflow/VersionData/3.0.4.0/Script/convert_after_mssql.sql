-- Заполенение свойства Number в гриде адресатов исходящего письма 
UPDATE Sungero_Docflow_OutAddressees 
SET Number = T2.rownum
FROM Sungero_Docflow_OutAddressees T1 INNER JOIN 
  (SELECT ROW_NUMBER() OVER(PARTITION BY Edoc ORDER BY Id) AS rownum, id
     FROM Sungero_Docflow_OutAddressees
	 WHERE Number IS NULL
  ) T2
ON T1.id = T2.id

-- Заполенение свойства Number в справочнике Список рассылки
UPDATE Sungero_Docflow_DistribAddress 
SET Number = T2.rownum
FROM Sungero_Docflow_DistribAddress T1 INNER JOIN 
  (SELECT ROW_NUMBER() OVER(PARTITION BY DistribList ORDER BY Id) AS rownum, id
     FROM Sungero_Docflow_DistribAddress
	 WHERE Number IS NULL
  ) T2
ON T1.id = T2.id

-- Сброс кэша справочника Роли согласования.
UPDATE Sungero_System_EntityModifyInfo
SET LastModified = getdate()
WHERE EntityTypeGuid = '3445f357-1435-4444-9f24-a56a752fc471'

-- Переименование роли согласования
UPDATE Sungero_Docflow_ApprovalRole
SET Name = 'Непосредственный руководитель инициатора'
WHERE Type = 'InitManager'
  AND Name = 'Руководитель инициатора'