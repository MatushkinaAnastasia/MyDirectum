-- Виды документов

declare @documentkind varchar(36) = '14A59623-89A2-4EA8-B6E9-2AD4365F358C'
declare @systemname varchar(20) = 'Initialize'
declare @externalEntityId varchar(36)
declare @externalKindName varchar(500)
declare @externalKindNameEn varchar(500)
declare @externalTypeGuid varchar(36)
declare @entityId int
declare @newId int

declare @kinds table(externalId varchar(36), kindName varchar(500), kindNameEn varchar(500), typeGuid varchar(36))
insert into @kinds
select
'3981cdd1-a279-4a51-85d5-58db391603c2','Простой документ','Simple document','09584896-81e2-4c83-8f6c-70eb8321e1d0'
union select
'dd53874e-4420-4c80-be36-0711c7af4716','Заявление','Application','09584896-81e2-4c83-8f6c-70eb8321e1d0'
union select
'8cb5b6b3-755f-48f3-b5b4-2dfde6a1fc60','Служебная записка','Memo','95af409b-83fe-4697-a805-5a86ceec33f5'
union select
'75d45529-60ae-4d95-9c8f-b1016b766253','Протокол совещания','Minutes','bb4780ff-b2c3-4044-a390-e9e110791bf6'
union select
'2734ab0b-fd71-4fd2-820e-c25042488547','Приложение к документу','Addendum to document','58b9ed35-9c84-46cd-aa79-9b5ef5a82f5d'
union select
'4e02b0c3-d448-44e8-ae61-208a79a44205','Входящий документ эл. обмена', 'Incoming Exchange Document', 'cf8357c3-8266-490d-b75e-0bd3e46b1ae8'
union select
'fc5b2f85-548d-4de0-b1e9-66c873932111','Техническое задание','Terms of reference','56df80b3-a795-4378-ace5-c20a2b1fb6d9'
union select
'1b1f18b1-f42e-4939-b6ed-14d555d5faaa','Устав проекта','Project charter','56df80b3-a795-4378-ace5-c20a2b1fb6d9'
union select
'd0859d72-15c7-4ca7-b81e-611a5df1f112','Отчет по проекту','Project report','56df80b3-a795-4378-ace5-c20a2b1fb6d9'
union select
'f5869f19-67d3-47c2-9024-c60a96f2685b','План-график выполнения работ','Activity schedule','56df80b3-a795-4378-ace5-c20a2b1fb6d9'
union select
'479b7a76-3f38-434c-897e-733198c4f260','Проектное решение','Project design','56df80b3-a795-4378-ace5-c20a2b1fb6d9'
union select
'205d0822-eeab-4eb9-813d-738eceefe303','Аналитическая записка','Analytical note','56df80b3-a795-4378-ace5-c20a2b1fb6d9'
union select
'0002c3cb-43e1-4a01-a4fe-35abc8994d66','Входящее письмо','Incoming letter','8dd00491-8fd0-4a7a-9cf3-8b6dc2e6455d'
union select
'352ec449-e344-48ee-ad32-d0b2babdc56e','Исходящее письмо','Outgoing letter','d1d2a452-7732-4ba8-b199-0a4dc78898ac'
union select
'8f529647-3f37-484a-b83a-a793b69d013e','Приказ','Order','9570e517-7ab7-4f23-a959-3652715efad3'
union select
'2a42d335-4a84-4019-ab54-d0ab8344d232','Договор','Contract','f37c7e63-b134-4446-9b5b-f8811f6c9666'
union select
'a5b2c424-0f31-4809-b160-acc0c4583574','Дополнительное соглашение','Supplemental agreement','265f2c57-6a8a-4a15-833b-ca00e8047fa5'
union select
'c1f11157-4e42-4ce0-9423-ae05567b162e','Акт об оказании услуг (выполнении работ)','Contract statement','f2f5774d-5ca3-4725-b31d-ac618f6b8850'
union select
'558baab7-784d-42f8-bcb7-ba8c0e8821a3','Входящий счет на оплату', 'Incoming invoice', 'a523a263-bc00-40f9-810d-f582bae2205d'

DECLARE cur CURSOR FOR 
SELECT *
FROM @kinds

open cur
fetch next from cur into @externalEntityId, @externalKindName, @externalKindNameEn, @externalTypeGuid
while @@FETCH_STATUS = 0 
begin  
	if not exists (select 1 from dbo.Sungero_Core_ExternalLink
				   where EntityTypeGuid = @documentkind
				   and ExternalSystemId = @systemname
				   and ExternalEntityId = @externalEntityId)
	begin
	  exec Sungero_System_GetNewId 'Sungero_Core_ExternalLink', @newId output, 1

	  set @entityId =  (select top 1 Id from dbo.Sungero_Docflow_DocumentKind
						where (Name = @externalKindName or Name = @externalKindNameEn)
						and DocumentType = (select Id from dbo.Sungero_Docflow_DocumentType
											where DocTypeGuid = @externalTypeGuid))
	  if @entityId is not null
	  begin
		  insert into dbo.Sungero_Core_ExternalLink
		  values(@newId, @documentkind, @entityId, null, @externalEntityId, @systemname, null, 0)
	  end
	end
 
  fetch next from cur into @externalEntityId, @externalKindName, @externalKindNameEn, @externalTypeGuid
end
close cur
deallocate cur

-- Журналы

declare @documentregister varchar(36) = 'd7800dd5-a9d2-41e9-bbc4-a39292ac1eeb'
declare @externalRegisterName varchar(500)
declare @externalRegisterNameEn varchar(500)

declare @registers table(externalId varchar(36), name varchar(500), nameEn varchar(500))
insert into @registers
select
'88dd573b-522c-415b-8965-215845305acf', 'Протоколы совещаний', 'Minutes'
union select
'425355b4-00cc-4417-8ee2-15de460e034d', 'Служебные записки', 'Memos'
union select
'8a583acf-fae5-4d92-a54b-4da73a81e46c', 'Дополнительные соглашения', 'Supplemental agreements'
union select
'd9b3a138-0507-4cf3-b3b2-a7b231666838', 'Акты по договорам', 'Contract statements'

DECLARE cur CURSOR FOR 
SELECT *
FROM @registers

open cur
fetch next from cur into @externalEntityId, @externalRegisterName, @externalRegisterNameEn
while @@FETCH_STATUS = 0 
begin  
	if not exists (select 1 from dbo.Sungero_Core_ExternalLink
				   where EntityTypeGuid = @documentregister
				   and ExternalSystemId = @systemname
				   and ExternalEntityId = @externalEntityId)
	begin
	  exec Sungero_System_GetNewId 'Sungero_Core_ExternalLink', @newId output, 1

	  set @entityId = (select top 1 Id from dbo.Sungero_Docflow_Register
					   where Name = @externalRegisterName
					      or Name = @externalRegisterNameEn)
	  if @entityId is not null
	  begin
		  insert into dbo.Sungero_Core_ExternalLink
		  values(@newId, @documentregister, @entityId, null, @externalEntityId, @systemname, null, 0)
	  end
	end
 
  fetch next from cur into @externalEntityId, @externalRegisterName, @externalRegisterNameEn
end
close cur
deallocate cur