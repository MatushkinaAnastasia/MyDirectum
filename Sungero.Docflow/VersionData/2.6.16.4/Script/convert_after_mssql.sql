declare @correctionRelationName varchar(50) = 'Correction'

declare @correctionOldTargetDescription varchar(50) = 'Корректирует'
declare @correctionOldTargetTitle varchar(50) = 'Корректирует'

declare @correctionNewTargetDescription varchar(50) = 'Корректировочные документы'
declare @correctionNewTargetTitle varchar(50) = 'Корректировочные документы'

update Sungero_Core_RelationType
set TargetTitle = @correctionNewTargetTitle
where Name = @correctionRelationName and TargetTitle = @correctionOldTargetTitle

update Sungero_Core_RelationType
set TargetDescription = @correctionNewTargetDescription
where Name = @correctionRelationName and TargetDescription = @correctionOldTargetDescription

-- Сброс кэша справочника Типы связей.
declare @newdate datetime
set @newdate = getdate()

UPDATE Sungero_System_EntityModifyInfo
SET LastModified = @newdate
WHERE EntityTypeGuid = 'B48F5D28-2036-4529-B215-A7B531EED778'