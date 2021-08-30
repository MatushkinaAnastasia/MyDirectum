  declare @newId int
  declare @deliveryMethod int
  declare @recordsCount int
  declare @russianLocale bit

  -- Сброс кэша справочника "Условия"
  UPDATE Sungero_System_EntityModifyInfo
  SET LastModified = getdate()
  WHERE EntityTypeGuid IN (
                            '325a0811-c109-483d-bf57-3246eade5644',
							'3f6f31c3-16e2-457e-9f88-48b254a500d2',
							'0523387b-a689-41e5-bed3-95892df6922c'
                           )  

  -- Если нет русского типа документа "простой документ", то считаем это английским разворачиванием
  SET @russianLocale =  
    CASE WHEN EXISTS (SELECT 1 FROM Sungero_Docflow_DocumentType WHERE Name = 'Простой документ') THEN 1 ELSE 0 END

  -- Получаем id способа доставки
  SELECT @deliveryMethod = id 
  FROM Sungero_Docflow_DeliveryMethod 
  WHERE name = 
   CASE WHEN @russianLocale = 'True' THEN 'Сервис эл. обмена' ELSE 'Exchange service' END

  -- Подсчет количества записей
  SELECT @recordsCount = COUNT(*)
  FROM Sungero_Docflow_Condition
  WHERE ConditionType = 'DeliveryByExch'

  -- Выделение id в таблице Sungero_Docflow_CondBaseDelive		  
  EXEC Sungero_System_GetNewId 'Sungero_Docflow_CondBaseDelive', @newId output, @recordsCount

  
  -- Заполняем коллекцию способом доставки 'Сервис эл. обмена'
  INSERT INTO Sungero_Docflow_CondBaseDelive
  SELECT
          (ROW_Number() OVER(ORDER BY Sungero_Docflow_Condition.Id)) + @newId - 1,
		  '64123DB7-D49F-4DA6-AE43-3A3310CA335C',
		  Id,
		  @deliveryMethod
  FROM Sungero_Docflow_Condition
  WHERE ConditionType = 'DeliveryByExch';

  -- Обновляем Условия
  UPDATE Sungero_Docflow_Condition
  SET 
    Name = CASE WHEN @russianLocale = 'True' THEN 'Способ доставки - Сервис эл. обмена?' ELSE 'Is delivery method Exchange service?' END,
	  NegationName = CASE WHEN @russianLocale = 'True' THEN 'Способом доставки не является Сервис эл. обмена?' ELSE 'Is delivery method other than Exchange service?' END,
	  ConditionType = 'DeliveryMethod'
  WHERE ConditionType = 'DeliveryByExch'

  -- Сброс кэша справочника Роли согласования.
  UPDATE Sungero_System_EntityModifyInfo
  SET LastModified = getdate()
  WHERE EntityTypeGuid = '3445f357-1435-4444-9f24-a56a752fc471'

  -- Переименование роли согласования
  UPDATE Sungero_Docflow_ApprovalRole
  SET Name = 'Direct manager of initiator'
  WHERE Type = 'InitManager'
    AND Name = 'Manager of initiator'
    
  -- Переименование роли согласования
  UPDATE Sungero_Docflow_ApprovalRole
  SET Name = 'Непосредственный руководитель инициатора'
  WHERE Type = 'InitManager'
    AND Name = 'Руководитель инициатора'