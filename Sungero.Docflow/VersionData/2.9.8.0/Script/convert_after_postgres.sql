-- Сброс кэша справочника Журналы регистрации.
UPDATE Sungero_System_EntityModifyInfo
SET LastModified = now()
WHERE EntityTypeGuid = 'd7800dd5-a9d2-41e9-bbc4-a39292ac1eeb';