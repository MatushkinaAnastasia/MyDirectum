-- Обновление типа прав "Изменение" для контрагентов
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