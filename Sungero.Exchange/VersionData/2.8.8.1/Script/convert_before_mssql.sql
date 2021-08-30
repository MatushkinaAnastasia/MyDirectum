if exists(select 1 from INFORMATION_SCHEMA.TABLES where table_name = 'Sungero_ExCore_QueueItem')

begin

declare @tempTable table
(
	Id int identity(0,1),
	Document int,
	VersionId int,
	ExchangeState varchar(15)
)

insert into @tempTable
select 
	versions.EDoc Document,
	versions.Id VersionId,
	infos.ExchangeState
from Sungero_ExCh_ExchDocInfo as infos
join Sungero_Content_EDocVersion as versions
	on infos.Document = versions.EDoc
	and infos.VersionId = versions.Id
where versions.PublicBody_Id is null and infos.SenderSignId is not null

declare @bodyConverterQueueItemDiscriminator varchar(36) = '2d30e2aa-1d0b-45f0-8e4d-00318b3a5cfd'
declare @status varchar(6) = 'Active'
declare @retriesCount int = 0
declare @processStatus varchar(12) = 'NotProcessed'
declare @newId int
declare @newQueueItemsCount int

-- Подсчет количества новых элементов очереди
select @newQueueItemsCount = COUNT(1) from @tempTable

-- Выделение id в таблице Sungero_ExCore_QueueItem
exec Sungero_System_GetNewId 'Sungero_ExCore_QueueItem', @newId output, @newQueueItemsCount

insert into Sungero_ExCore_QueueItem
(
	[Id],
	[Discriminator],
	[Status],
	[Retries],
	[Document],
	[VersionId],
	[ExchangeState],
	[ProcessStatus]
)
select
	@newId + t.Id,
	@bodyConverterQueueItemDiscriminator,
	@status,
	@retriesCount,
	t.Document,
	t.VersionId,
	t.ExchangeState,
	@processStatus
from @tempTable as t

end