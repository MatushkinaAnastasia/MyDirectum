-- Заполенение свойства Number в гриде адресатов исходящего письма 
UPDATE Sungero_Docflow_OutAddressees T1
SET Number = T2.rownum
FROM 
  (SELECT ROW_NUMBER() OVER(PARTITION BY Edoc ORDER BY Id) AS rownum, id
    FROM Sungero_Docflow_OutAddressees
  ) T2
WHERE T1.id = T2.id AND Number IS NULL;

-- Заполенение свойства Number в справочнике Список рассылки 
UPDATE sungero_docflow_distribaddress T1
SET Number = T2.rownum
FROM 
  (SELECT ROW_NUMBER() OVER(PARTITION BY DistribList ORDER BY Id) AS rownum, id
    FROM sungero_docflow_distribaddress
  ) T2
WHERE T1.id = T2.id AND Number IS NULL;

-- Сброс кэша справочника Роли согласования.
UPDATE Sungero_System_EntityModifyInfo
SET LastModified = now()
WHERE EntityTypeGuid = '3445f357-1435-4444-9f24-a56a752fc471';

-- Переименование роли согласования
UPDATE Sungero_Docflow_ApprovalRole
SET Name = 'Непосредственный руководитель инициатора'
WHERE Type = 'InitManager'
  AND Name = 'Руководитель инициатора';