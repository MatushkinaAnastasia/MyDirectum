declare @projectKind varchar(36) = '01FD022D-F8C1-489D-818A-588B324FB29A'
declare @systemname varchar(20) = 'Initialize'
declare @externalEntityId varchar(36)
declare @externalKindName varchar(500)
declare @externalKindNameEn varchar(500)
declare @entityId int
declare @newId int

declare @kinds table(externalId varchar(36), name varchar(500), nameEn varchar(500))
insert into @kinds
select
'38de9ebf-8733-41b9-88b5-8c1884075e2c', 'Инвестиционный проект', 'Investment project'
union select
'2c569f84-709f-40c8-a179-97d58037b6a8', 'Проект внедрения информационных технологий', 'Information technology implementation project'
union select
'f4bc2b22-7e28-4eed-b9a9-8a8dab275da4', 'Проект организационного развития', 'Organizational development project'
union select
'd3568b17-0fa1-4d29-8abb-046a8dde4796', 'Проект создания нового продукта', 'New product development project'
union select
'53fbc9cf-726c-45ae-bd77-8c9bfec2a0c0', 'Проект организации продажи (сделка)', 'Sales organization project'
union select
'04ff23f3-33a5-4eae-a6b7-6398a71d0866', 'Маркетинговый проект', 'Marketing project'

DECLARE cur CURSOR FOR 
SELECT *
FROM @kinds

open cur
fetch next from cur into @externalEntityId, @externalKindName, @externalKindNameEn
while @@FETCH_STATUS = 0 
begin  
	if not exists (select 1 from dbo.Sungero_Core_ExternalLink
				   where EntityTypeGuid = @projectKind
				   and ExternalSystemId = @systemname
				   and ExternalEntityId = @externalEntityId)
	begin
	  exec Sungero_System_GetNewId 'Sungero_Core_ExternalLink', @newId output, 1

	  set @entityId =  (select top 1 Id from dbo.Sungero_Project_ProjectKind
						where Name = @externalKindName
						   or Name = @externalKindNameEn)
	  if @entityId is not null
	  begin
		  insert into dbo.Sungero_Core_ExternalLink
		  values(@newId, @projectKind, @entityId, null, @externalEntityId, @systemname, null, 0)
	  end
	end
 
  fetch next from cur into @externalEntityId, @externalKindName, @externalKindNameEn
end
close cur
deallocate cur