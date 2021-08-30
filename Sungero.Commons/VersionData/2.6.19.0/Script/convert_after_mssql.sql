-- Сконвертировать Core_ExternalLink на Commons_ExtEntityLinks
if exists (select 1 from INFORMATION_SCHEMA.TABLES where TABLE_NAME = 'Sungero_Commons_ExtEntityLinks')
   and exists (select 1 from INFORMATION_SCHEMA.TABLES where TABLE_NAME = 'Sungero_Core_ExternalLink')
begin
  declare @entityTypeGuid nvarchar(250)
  declare @entityId int
  declare @extEntityTypeId nvarchar(250)
  declare @extEntityId nvarchar(250)
  declare @extSystemId nvarchar(250)
  declare @isDeleted bit
  declare @newId int
  declare @lastIntegrationDate datetime

  -- Получить LastIntegrationDate в виде datetime, если она есть.
  if exists (select Data from Sungero_System_Setting where Name = 'LastIntegrationDate')
  begin
    set @lastIntegrationDate = (select CONVERT(datetime, SUBSTRING(Data, 2, 23), 126) from Sungero_System_Setting where Name = 'LastIntegrationDate')
  end

  -- Объявить курсор для неперенесенных ExternalLinks.
  declare el_cursor cursor for
    select [EntityTypeGuid]
          ,[EntityId]
          ,[ExternalEntityTypeId]
          ,[ExternalEntityId]
          ,[ExternalSystemId]
          ,[IsDeleted]
    from Sungero_Core_ExternalLink coreExtLinks
    where ExternalSystemId <> 'Initialize'
      and not exists (select 1
                      from Sungero_Commons_ExtEntityLinks commonsExtLinks
                      where coreExtLinks.EntityTypeGuid = commonsExtLinks.EntityType
                        and coreExtLinks.EntityId = commonsExtLinks.EntityId
                        and coreExtLinks.ExternalEntityTypeId = commonsExtLinks.ExtEntityType
                        and coreExtLinks.ExternalEntityId = commonsExtLinks.ExtEntityId
                        and coreExtLinks.ExternalSystemId = commonsExtLinks.ExtSystemId)

  open el_cursor
  fetch next from el_cursor into @entityTypeGuid, @entityId, @extEntityTypeId, @extEntityId, @extSystemId, @isDeleted

  while @@FETCH_STATUS = 0
  begin
    
    -- Пропустить ссылку, если она указывает на сущность не синхронизируемого типа
    if (UPPER(@entityTypeGuid) not in ('80C4E311-E95F-449B-984D-1FD540B8F0AF',  --Bank
                                       '78278DD7-F0D2-4E35-B543-13D0BD462CD6',  --CompanyBase
                                       '593E143C-616C-4D95-9457-FD916C4AA7F8',  --Company
                                       'C8DAAEF9-A679-4A29-AC01-B93C1637C72E',  --Contact
                                       'F37C7E63-B134-4446-9B5B-F8811F6C9666',  --Contract
                                       'FFC2629F-DC30-4106-A3CE-C402AE7D32B9',  --Currency
                                       'F5509CDC-AC0C-4507-A4D3-61D7A0A9B6F6',  --Person
                                       '265F2C57-6A8A-4A15-833B-CA00E8047FA5')) --SupAgreement
    begin
      fetch next from el_cursor into @entityTypeGuid, @entityId, @extEntityTypeId, @extEntityId, @extSystemId, @isDeleted
      continue
    end

    -- Проверить есть ли сущность в RX
    -- Контрагенты (CompanyBase, Company, Bank, Person)
    if (UPPER(@entityTypeGuid) in ('80C4E311-E95F-449B-984D-1FD540B8F0AF',   --Bank
                                   'F5509CDC-AC0C-4507-A4D3-61D7A0A9B6F6',   --Person
                                   '78278DD7-F0D2-4E35-B543-13D0BD462CD6',   --CompanyBase
                                   '593E143C-616C-4D95-9457-FD916C4AA7F8'))  --Company
    begin
      -- Пропустить ссылку, если такого контрагента не существует в RX
      if not exists (select 1
                     from Sungero_Parties_Counterparty
                     where UPPER(@entityTypeGuid) = UPPER(Discriminator)
                       and @entityId = Id)
        begin
          fetch next from el_cursor into @entityTypeGuid, @entityId, @extEntityTypeId, @extEntityId, @extSystemId, @isDeleted
          continue
        end
    end
    -- Контакты
    if (UPPER(@entityTypeGuid) = 'C8DAAEF9-A679-4A29-AC01-B93C1637C72E') --Contact
    begin
      -- Пропустить ссылку, если такой контакт не существует в RX
      if not exists (select 1
                     from Sungero_Parties_Contact
                     where UPPER(@entityTypeGuid) = UPPER(Discriminator)
                       and @entityId = Id)
        begin
          fetch next from el_cursor into @entityTypeGuid, @entityId, @extEntityTypeId, @extEntityId, @extSystemId, @isDeleted
          continue
        end
    end
    -- Договорные документы
    if (UPPER(@entityTypeGuid) in ('F37C7E63-B134-4446-9B5B-F8811F6C9666',  --Contract
                                   '265F2C57-6A8A-4A15-833B-CA00E8047FA5')) --SupAgreement
    begin
      -- Пропустить ссылку, если такого документа не существует в RX
      if not exists (select 1
                     from Sungero_Content_EDoc
                     where UPPER(@entityTypeGuid) = UPPER(Discriminator)
                       and @entityId = Id)
        begin
          fetch next from el_cursor into @entityTypeGuid, @entityId, @extEntityTypeId, @extEntityId, @extSystemId, @isDeleted
          continue
        end
    end
    -- Валюты
    if (UPPER(@entityTypeGuid) = 'FFC2629F-DC30-4106-A3CE-C402AE7D32B9')  --Currency
    begin
      -- Пропустить ссылку, если такой валюты не существует в RX
      if not exists (select 1
                     from Sungero_Commons_Currency
                     where UPPER(@entityTypeGuid) = UPPER(Discriminator)
                       and @entityId = Id)
        begin
          fetch next from el_cursor into @entityTypeGuid, @entityId, @extEntityTypeId, @extEntityId, @extSystemId, @isDeleted
          continue
        end
    end

    -- Получить следующий Id
    exec Sungero_System_GetNewId 'Sungero_Commons_ExtEntityLinks', @newId output, 1

    -- Установить значение IsDeleted = false, если оно не задано.
    if (@isDeleted is null)
      set @isDeleted = 'FALSE'
    
    -- Перенести записть ExternalLink в ExternalEntityLink
    -- Если LastIntegrationDate отсутствует - в записи останется null.
    insert into Sungero_Commons_ExtEntityLinks
    values(@newId, '4346363e-39b9-40eb-9c12-64f0cf48d87f', null, 'Active', @entityTypeGuid, @entityId, @extEntityTypeId, @extEntityId, @extSystemId, @lastIntegrationDate, @isDeleted, '')

    fetch next from el_cursor into @entityTypeGuid, @entityId, @extEntityTypeId, @extEntityId, @extSystemId, @isDeleted
  end

  close el_cursor
  deallocate el_cursor
end