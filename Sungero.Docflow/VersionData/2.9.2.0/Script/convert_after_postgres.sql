DO $$ 

declare newId int;
declare recordsCount int;

BEGIN
  -- Установить новое свойство в False 
  UPDATE Sungero_Content_EDoc
  SET OutIsManyAddr_Docflow_Sungero = 'FALSE'
  WHERE Discriminator = 'D1D2A452-7732-4BA8-B199-0A4DC78898AC' AND OutIsManyAddr_Docflow_Sungero IS NULL;
  
  -- Подсчет количества записей
  SELECT COUNT(*) into recordsCount
  FROM Sungero_Content_EDoc
  LEFT JOIN Sungero_Docflow_OutAddressees 
  ON Sungero_Content_EDoc.Id = Sungero_Docflow_OutAddressees.Edoc
  WHERE Sungero_Content_EDoc.Discriminator = 'D1D2A452-7732-4BA8-B199-0A4DC78898AC' AND
         Sungero_Docflow_OutAddressees.EDoc IS NULL;
  
  -- Выделение id в таблице Sungero_Docflow_Addressees
  newId := (select Sungero_System_GetNewId('Sungero_Docflow_OutAddressees', recordsCount));

  -- Заполнения таблицы Адресаты
  INSERT INTO Sungero_Docflow_OutAddressees
  SELECT (ROW_Number() OVER(ORDER BY Sungero_Content_EDoc.Id)) + newId - 1 AS Id,
         'CB1F24E1-84E2-47E0-B08E-59C651168B56' AS Discriminator,
		  Sungero_Content_EDoc.Id AS EDoc, 
		  OutCorr_Docflow_Sungero AS Correspondent, 
		  OutAddressee_Docflow_Sungero AS Addressee, 
		  DeliveryMethod_Docflow_Sungero AS DeliveryMethod
  FROM Sungero_Content_EDoc
  LEFT JOIN Sungero_Docflow_OutAddressees 
  ON Sungero_Content_EDoc.Id = Sungero_Docflow_OutAddressees.Edoc
  WHERE Sungero_Content_EDoc.Discriminator = 'D1D2A452-7732-4BA8-B199-0A4DC78898AC' AND
        Sungero_Docflow_OutAddressees.EDoc IS NULL;

END$$;

  update sungero_content_edoc
  set respempl_docflow_sungero = null
   where discriminator = 'a523a263-bc00-40f9-810d-f582bae2205d'
     and respempl_docflow_sungero is not null      