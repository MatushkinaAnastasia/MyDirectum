declare @accDocRelationName varchar(50) = 'AccountingDocuments'
declare @financialDocRelationName varchar(50) = 'FinancialDocuments'

declare @accDocTargetDescriptionRU varchar(50) = 'Бухгалтерский документ'
declare @accDocTargetTitleRU varchar(50) = 'Бухгалтерские документы'

declare @financialDocTargetDescriptionRU varchar(50) = 'Финансовый документ'
declare @financialDocTargetTitleRU varchar(50) = 'Финансовые документы'

declare @accDocTargetDescriptionEN varchar(50) = 'Accounting document'
declare @accDocTargetTitleEN varchar(50) = 'Accounting documents'

declare @financialDocTargetDescriptionEN varchar(50) = 'Financial document'
declare @financialDocTargetTitleEN varchar(50) = 'Financial documents'

update Sungero_Core_RelationType
set TargetDescription = @financialDocTargetDescriptionRU
where TargetDescription = @accDocTargetDescriptionRU

update Sungero_Core_RelationType
set TargetTitle = @financialDocTargetTitleRU
where TargetTitle = @accDocTargetTitleRU

update Sungero_Core_RelationType
set TargetDescription = @financialDocTargetDescriptionEN
where TargetDescription = @accDocTargetDescriptionEN

update Sungero_Core_RelationType
set TargetTitle = @financialDocTargetTitleEN
where TargetTitle = @accDocTargetTitleEN

update Sungero_Core_RelationType
set Name = @financialDocRelationName
where Name = @accDocRelationName

-- Сброс кэша справочника Типы связей.
declare @newdate datetime
set @newdate = getdate()

UPDATE Sungero_System_EntityModifyInfo
SET LastModified = @newdate
WHERE EntityTypeGuid = 'B48F5D28-2036-4529-B215-A7B531EED778'