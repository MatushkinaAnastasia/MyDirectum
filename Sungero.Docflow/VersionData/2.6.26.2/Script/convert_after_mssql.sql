UPDATE Sungero_Docflow_PersonSetting
set ShowNotApprove = 0
where ShowNotApprove is null

-- Сброс кэша справочника Персональные настройки.
declare @newdate datetime
set @newdate = getdate()

UPDATE Sungero_System_EntityModifyInfo
SET LastModified = @newdate
WHERE EntityTypeGuid = 'edabdc0e-13d1-45e0-82a8-e60a5ea5bf68'