-- Удалить ссылки на отсутствующие в DirectumRX сущности.
declare @typeGuids cursor;
declare @typeGuid varchar(36);

-- Установить курсор по типам, на которые есть EEL.
set @typeGuids = cursor scroll dynamic
for
select distinct EntityType
from Sungero_Commons_ExtEntityLinks;

open @typeGuids;

fetch next from @typeGuids into @typeGuid;
while @@FETCH_STATUS = 0
begin
  
	-- Договоры и допники.
	if (@typeGuid = '265f2c57-6a8a-4a15-833b-ca00e8047fa5' or
	    @typeGuid = 'f37c7e63-b134-4446-9b5b-f8811f6c9666')
	begin
	  delete
		from Sungero_Commons_ExtEntityLinks
		where LOWER(EntityType) = @typeGuid
		  and (select count(*)
		       from Sungero_Content_EDoc doc
					 where doc.Id = EntityId) = 0
	end

	-- Организации, персоны и банки.
	if (@typeGuid = '593e143c-616c-4d95-9457-fd916c4aa7f8' or
	    @typeGuid = 'f5509cdc-ac0c-4507-a4d3-61d7a0a9b6f6' or
			@typeGuid = '80c4e311-e95f-449b-984d-1fd540b8f0af')
	begin
	  delete
		from Sungero_Commons_ExtEntityLinks
		where LOWER(EntityType) = @typeGuid
		  and (select count(*)
		       from Sungero_Parties_Counterparty con
					 where con.Id = EntityId) = 0
	end

	-- Контакты.
	if (@typeGuid = 'c8daaef9-a679-4a29-ac01-b93c1637c72e')
	begin
	  delete
		from Sungero_Commons_ExtEntityLinks
		where LOWER(EntityType) = @typeGuid
		  and (select count(*)
		       from Sungero_Parties_Contact con
					 where con.Id = EntityId) = 0
	end

	-- Валюты.
	if (@typeGuid = 'ffc2629f-dc30-4106-a3ce-c402ae7d32b9')
	begin
	  delete
		from Sungero_Commons_ExtEntityLinks
		where LOWER(EntityType) = @typeGuid
		  and (select count(*)
		       from Sungero_Commons_Currency cur
					 where cur.Id = EntityId) = 0
	end

  fetch next from @typeGuids into @typeGuid
end;

close @typeGuids;
deallocate @typeGuids;