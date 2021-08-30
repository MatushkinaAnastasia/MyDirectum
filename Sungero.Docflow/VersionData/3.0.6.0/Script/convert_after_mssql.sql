  UPDATE Sungero_Core_RelationType
  SET UseSource = '1', UseTarget = '0'
  WHERE Name = 'Response'
  
  -- Сброс кэша справочника "Типы связей"
  UPDATE Sungero_System_EntityModifyInfo
  SET LastModified = getdate()
  WHERE EntityTypeGuid = 'b48f5d28-2036-4529-b215-a7b531eed778'