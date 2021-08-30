do $$

declare bodyConverterQueueItemDiscriminator uuid = '2d30e2aa-1d0b-45f0-8e4d-00318b3a5cfd';
declare status varchar(6) = 'Active';
declare retriesCount int = 0;
declare processStatus varchar(12) = 'NotProcessed';
declare newId int;
declare newQueueItemsCount int;

begin

if exists(select * from information_schema.tables where table_name = 'sungero_excore_queueitem')
then
    if exists(select * from information_schema.tables where table_name = 'tempTable')
    then
        drop table tempTable;
    end if;

create table tempTable
(
	id serial,
	Document int,
	VersionId int,
	ExchangeState varchar(15)
);

insert into tempTable (Document, VersionId, ExchangeState)
select 
	versions.EDoc,
	versions.Id,
	infos.ExchangeState
from Sungero_ExCh_ExchDocInfo as infos
join Sungero_Content_EDocVersion as versions
	on infos.Document = versions.EDoc
	and infos.VersionId = versions.Id
where versions.PublicBody_Id is null and infos.SenderSignId is not null;

-- Подсчет количества новых элементов очереди
select COUNT(1) into newQueueItemsCount from tempTable;

-- Выделение id в таблице Sungero_ExCore_QueueItem
newId := (select Sungero_System_GetNewId('Sungero_ExCore_QueueItem', newQueueItemsCount));

insert into Sungero_ExCore_QueueItem
(
	Id,
	Discriminator,
	Status,
	Retries,
	Document,
	VersionId,
	ExchangeState,
	ProcessStatus
)
select
	newId + t.Id,
	bodyConverterQueueItemDiscriminator,
	status,
	retriesCount,
	t.Document,
	t.VersionId,
	t.ExchangeState,
	processStatus
from tempTable as t;

drop table tempTable;

end if;
end$$;