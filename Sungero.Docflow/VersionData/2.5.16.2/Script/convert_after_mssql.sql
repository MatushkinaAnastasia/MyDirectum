-- Конвертация полного типа прав у документов
update dbo.Sungero_System_AccessCtrlEnt
set OperationSet = '-50331721'
where OperationSet = '-16777289'

-- Конвертация старого полного типа прав у документов
declare @oldOperationSet int = '-16777281'
declare @newOperationSet int = '-50331721'

update Sungero_System_AccessCtrlEnt
set OperationSet = @newOperationSet
where OperationSet = @oldOperationSet

update Sungero_Core_AccessRightTyp
set OperationSet = @newOperationSet, GrantedMask = @newOperationSet
where OperationSet = @oldOperationSet

-- Создание нового полного типа прав для контрагентов
if (select top 1 1 from Sungero_Core_AccessRightTyp where lower(EntityTypeGuid) = '294767f1-009f-4fbd-80fc-f98c49ddc560'
                                                    and Sid = '6eb00eea-b585-43ce-8a0a-a294146e0825') is null
  begin
    declare @AccessRightTypTableName varchar(30) = 'Sungero_Core_AccessRightTyp'
    declare @NewId int
    exec Sungero_System_GetNewId @AccessRightTypTableName, @NewId output
    insert into [dbo].[Sungero_Core_AccessRightTyp] ([Id], [Name], [EntityTypeGuid], [OperationSet],
                                                     [GrantedMask], [AccessRightsTypeArea], [Description], [Sid],
                                                     [IsOverride], [Discriminator])
    values (@NewId, N'Полный доступ',	N'294767f1-009f-4fbd-80fc-f98c49ddc560',	-16777217,
            -16777217,	N'Both',	N'', N'6eb00eea-b585-43ce-8a0a-a294146e0825',	1,	N'34D8054F-43C0-4C6A-A1A6-7EE427D65DC8')
  end
else
  begin
    update Sungero_Core_AccessRightTyp
    set [Name] = N'Полный доступ',
        [OperationSet] = -16777217,
        [GrantedMask] = -16777217,
        [AccessRightsTypeArea] = N'Both',
        [Description] = N'',
        [IsOverride] = 1
    where lower(EntityTypeGuid) = '294767f1-009f-4fbd-80fc-f98c49ddc560' and Sid = '6eb00eea-b585-43ce-8a0a-a294146e0825'
  end

-- Конвертация полного типа прав у контрагента и его наследников
update dbo.Sungero_System_AccessCtrlEnt
set OperationSet = '-16777217'
where SecureObject in (select Id from dbo.Sungero_System_SecureObject
                       where EntityTypeGuid in ('294767f1-009f-4fbd-80fc-f98c49ddc560',
                                                '593e143c-616c-4d95-9457-fd916c4aa7f8',
                                                '78278dd7-f0d2-4e35-b543-13d0bd462cd6',
                                                '80c4e311-e95f-449b-984d-1fd540b8f0af',
                                                'f5509cdc-ac0c-4507-a4d3-61d7a0a9b6f6'))
and OperationSet = '-1'

update dbo.Sungero_Core_AccessRightEnt
set AccessRightsType = (select Id from dbo.Sungero_Core_AccessRightTyp 
                        where EntityTypeGuid = '294767f1-009f-4fbd-80fc-f98c49ddc560' and [Sid] = '6eb00eea-b585-43ce-8a0a-a294146e0825')
where AccessRights in (select Id from dbo.Sungero_Core_AccessRights
                       where EntityTypeGuid in ('294767f1-009f-4fbd-80fc-f98c49ddc560',
                                                '593e143c-616c-4d95-9457-fd916c4aa7f8',
                                                '78278dd7-f0d2-4e35-b543-13d0bd462cd6',
                                                '80c4e311-e95f-449b-984d-1fd540b8f0af',
                                                'f5509cdc-ac0c-4507-a4d3-61d7a0a9b6f6'))
and AccessRightsType in (select Id from dbo.Sungero_Core_AccessRightTyp
where EntityTypeGuid = '79AAA247-5F24-47A3-BF05-F0CD7AD30161' and [Sid] = '6eb00eea-b585-43ce-8a0a-a294146e0825')

-- Сбросить кэш для типа прав.
declare
  @ModifiedDate datetime,
  @TypeGuid varchar (36),
  @UpdateDate datetime
set @UpdateDate = getdate()

set @TypeGuid = '34d8054f-43c0-4c6a-a1a6-7ee427d65dc8'
set @ModifiedDate = 
  (select top 1 LastModified 
   from [dbo].[Sungero_System_EntityModifyInfo] 
   where EntityTypeGuid  = @TypeGuid)

if @ModifiedDate is null
  insert into [dbo].[Sungero_System_EntityModifyInfo] (EntityTypeGuid, LastModified) 
  values (@TypeGuid, @UpdateDate)
else
  update [dbo].[Sungero_System_EntityModifyInfo]
  set LastModified = @UpdateDate
  where EntityTypeGuid  = @TypeGuid
