declare @responsibleCPRole varchar(36) = 'C719C823-C4BD-4434-A34B-D7E83E524414'
declare @exchangeUsersRole varchar(36) = '5AFA06FB-3B66-4216-8681-56ACDEAC7FC1'

declare @fullAccessCP int
set @fullAccessCP = (select top 1 Id from Sungero_Core_AccessRightTyp where lower(EntityTypeGuid) = '294767f1-009f-4fbd-80fc-f98c49ddc560'
                                                    and Sid = '6eb00eea-b585-43ce-8a0a-a294146e0825')

-- Конвертация полного типа прав у контрагента и его наследников
update dbo.Sungero_System_AccessCtrlEnt
set OperationSet = '-1'
where SecureObject in (select Id from dbo.Sungero_System_SecureObject
                       where EntityTypeGuid in ('294767f1-009f-4fbd-80fc-f98c49ddc560',
                                                '593e143c-616c-4d95-9457-fd916c4aa7f8',
                                                '78278dd7-f0d2-4e35-b543-13d0bd462cd6',
                                                '80c4e311-e95f-449b-984d-1fd540b8f0af',
                                                'f5509cdc-ac0c-4507-a4d3-61d7a0a9b6f6'))
and OperationSet = '-16777217'

update dbo.Sungero_Core_AccessRightEnt
set AccessRightsType = (select Id from dbo.Sungero_Core_AccessRightTyp 
                        where EntityTypeGuid = '79AAA247-5F24-47A3-BF05-F0CD7AD30161' and [Sid] = '6eb00eea-b585-43ce-8a0a-a294146e0825')
where AccessRights in (select Id from dbo.Sungero_Core_AccessRights
                       where EntityTypeGuid in ('294767f1-009f-4fbd-80fc-f98c49ddc560',
                                                '593e143c-616c-4d95-9457-fd916c4aa7f8',
                                                '78278dd7-f0d2-4e35-b543-13d0bd462cd6',
                                                '80c4e311-e95f-449b-984d-1fd540b8f0af',
                                                'f5509cdc-ac0c-4507-a4d3-61d7a0a9b6f6'))
and AccessRightsType in (select Id from dbo.Sungero_Core_AccessRightTyp
where EntityTypeGuid = '294767f1-009f-4fbd-80fc-f98c49ddc560' and [Sid] = '6eb00eea-b585-43ce-8a0a-a294146e0825')

delete from Sungero_Core_AccessRightTyp
where Id = @fullAccessCP

-- Удалить пользователей с правами на работу через сервис обмена из ответственных за контрагентов
if (select top 1 1 from Sungero_Core_Recipient where Sid = @exchangeUsersRole) is not null
begin
	declare @exchangeUserId int
	set @exchangeUserId = (select Id from Sungero_Core_Recipient where Sid = @exchangeUsersRole)

	if (select top 1 1 from Sungero_Core_Recipient where Sid = @responsibleCPRole) is not null
	begin
		declare @resposibleRoleId int
		set @resposibleRoleId = (select Id from Sungero_Core_Recipient where Sid = @responsibleCPRole)

		delete from Sungero_Core_RecipientLink
		where Recipient = @resposibleRoleId and Member = @exchangeUserId
	end
end

-- Конвертация типа прав "Установление обмена"
update Sungero_Core_AccessRightTyp
set OperationSet = '16777221', GrantedMask = '16777221'
where EntityTypeGuid = '294767f1-009f-4fbd-80fc-f98c49ddc560' and Sid = '6ccad865-aa9c-4afd-a08b-836363545aaf'

update Sungero_System_AccessCtrlEnt
set OperationSet = '16777221'
where SecureObject in (select Id from dbo.Sungero_System_SecureObject
                       where EntityTypeGuid in ('294767f1-009f-4fbd-80fc-f98c49ddc560',
                                                '593e143c-616c-4d95-9457-fd916c4aa7f8',
                                                '78278dd7-f0d2-4e35-b543-13d0bd462cd6',
                                                '80c4e311-e95f-449b-984d-1fd540b8f0af',
                                                'f5509cdc-ac0c-4507-a4d3-61d7a0a9b6f6'))
and OperationSet = '16777216'

-- Создание нового типа прав "Изменение" для контрагентов
if (select top 1 1 from Sungero_Core_AccessRightTyp where lower(EntityTypeGuid) = '294767f1-009f-4fbd-80fc-f98c49ddc560'
                                                    and Sid = '179af257-a60f-44b8-97b5-1d5bbd06716b') is null
  begin
    declare @AccessRightTypTableName varchar(30) = 'Sungero_Core_AccessRightTyp'
    declare @NewId int
    exec Sungero_System_GetNewId @AccessRightTypTableName, @NewId output
    insert into [dbo].[Sungero_Core_AccessRightTyp] ([Id], [Name], [EntityTypeGuid], [OperationSet],
                                                     [GrantedMask], [AccessRightsTypeArea], [Description], [Sid],
                                                     [IsOverride], [Discriminator])
    values (@NewId, N'Изменение',	N'294767f1-009f-4fbd-80fc-f98c49ddc560',	33554599,
            33554599,	N'Both',	N'', N'179af257-a60f-44b8-97b5-1d5bbd06716b',	1,	N'34D8054F-43C0-4C6A-A1A6-7EE427D65DC8')
  end
else
  begin
    update Sungero_Core_AccessRightTyp
    set [Name] = N'Изменение',
        [OperationSet] = 33554599,
        [GrantedMask] = 33554599,
        [AccessRightsTypeArea] = N'Both',
        [Description] = N'',
        [IsOverride] = 1
    where lower(EntityTypeGuid) = '294767f1-009f-4fbd-80fc-f98c49ddc560' and Sid = '179af257-a60f-44b8-97b5-1d5bbd06716b'
  end

-- Конвертация типа прав "Изменение" у контрагента и его наследников
update dbo.Sungero_System_AccessCtrlEnt
set OperationSet = '33554599'
where SecureObject in (select Id from dbo.Sungero_System_SecureObject
                       where EntityTypeGuid in ('294767f1-009f-4fbd-80fc-f98c49ddc560',
                                                '593e143c-616c-4d95-9457-fd916c4aa7f8',
                                                '78278dd7-f0d2-4e35-b543-13d0bd462cd6',
                                                '80c4e311-e95f-449b-984d-1fd540b8f0af',
                                                'f5509cdc-ac0c-4507-a4d3-61d7a0a9b6f6'))
and OperationSet = '167'

update dbo.Sungero_Core_AccessRightEnt
set AccessRightsType = (select Id from dbo.Sungero_Core_AccessRightTyp 
                        where EntityTypeGuid = '294767f1-009f-4fbd-80fc-f98c49ddc560' and [Sid] = '179af257-a60f-44b8-97b5-1d5bbd06716b')
where AccessRights in (select Id from dbo.Sungero_Core_AccessRights
                       where EntityTypeGuid in ('294767f1-009f-4fbd-80fc-f98c49ddc560',
                                                '593e143c-616c-4d95-9457-fd916c4aa7f8',
                                                '78278dd7-f0d2-4e35-b543-13d0bd462cd6',
                                                '80c4e311-e95f-449b-984d-1fd540b8f0af',
                                                'f5509cdc-ac0c-4507-a4d3-61d7a0a9b6f6'))
and AccessRightsType in (select Id from dbo.Sungero_Core_AccessRightTyp
where EntityTypeGuid = '04581D26-0780-4CFD-B3CD-C2CAFC5798B0' and [Sid] = '179af257-a60f-44b8-97b5-1d5bbd06716b')

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