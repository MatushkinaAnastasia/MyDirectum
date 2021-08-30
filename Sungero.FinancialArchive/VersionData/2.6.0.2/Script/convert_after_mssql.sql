-- Восстановить удалённые ГСД колонки
if not exists (select * from information_schema.COLUMNS 
               where table_name = 'Sungero_Content_EDoc' 
               and COLUMN_NAME = 'Counterparty_Docflow_Sungero')
begin
  alter table Sungero_Content_EDoc
  add Counterparty_Docflow_Sungero integer
end
if not exists (select * from information_schema.COLUMNS 
               where table_name = 'Sungero_Content_EDoc' 
               and COLUMN_NAME = 'TotalAmount_Docflow_Sungero')
begin
  alter table Sungero_Content_EDoc
  add TotalAmount_Docflow_Sungero float
end
if not exists (select * from information_schema.COLUMNS 
               where table_name = 'Sungero_Content_EDoc' 
               and COLUMN_NAME = 'ContrCurrency_Docflow_Sungero')
begin
  alter table Sungero_Content_EDoc
  add ContrCurrency_Docflow_Sungero integer
end
if not exists (select * from information_schema.COLUMNS 
               where table_name = 'Sungero_Content_EDoc' 
               and COLUMN_NAME = 'PartySignatory_Docflow_Sungero')
begin
  alter table Sungero_Content_EDoc
  add PartySignatory_Docflow_Sungero integer
end
if not exists (select * from information_schema.COLUMNS 
               where table_name = 'Sungero_Content_EDoc' 
               and COLUMN_NAME = 'Contact_Contrac_Sungero')
begin
  alter table Sungero_Content_EDoc
  add Contact_Contrac_Sungero integer
end
if not exists (select * from information_schema.COLUMNS 
               where table_name = 'Sungero_Content_EDoc' 
               and COLUMN_NAME = 'RespEmpl_Contrac_Sungero')
begin
  alter table Sungero_Content_EDoc
  add RespEmpl_Contrac_Sungero integer
end
if not exists (select * from information_schema.COLUMNS 
               where table_name = 'Sungero_Content_EDoc' 
               and COLUMN_NAME = 'ValidFrom_Contrac_Sungero')
begin
  alter table Sungero_Content_EDoc
  add ValidFrom_Contrac_Sungero datetime
end
if not exists (select * from information_schema.COLUMNS 
               where table_name = 'Sungero_Content_EDoc' 
               and COLUMN_NAME = 'ValidTill_Contrac_Sungero')
begin
  alter table Sungero_Content_EDoc
  add ValidTill_Contrac_Sungero datetime
end

-- Восстановить свойства в актах
if exists (select * from information_schema.tables where table_name = 'ContractStatementProperties')
begin 
exec('
  update r 
  set r.LeadDocument_FinArch_Sungero = t.LeadDocument,
    r.Counterparty_Docflow_Sungero = t.Counterparty,
    r.TotalAmount_Docflow_Sungero = t.TotalAmount,
    r.ContrCurrency_Docflow_Sungero = t.ContrCurrency,
    r.PartySignatory_Docflow_Sungero = t.PartySignatory,
    r.Contact_Contrac_Sungero = t.Contact,
    r.RespEmpl_Contrac_Sungero = t.RespEmpl,
    r.ValidFrom_Contrac_Sungero = t.ValidFrom,
    r.ValidTill_Contrac_Sungero = t.ValidTill
  from 
    Sungero_Content_EDoc r
  join ContractStatementProperties t 
    on r.Id = t.Id
    ')
    
  drop table ContractStatementProperties
 end

exec('
update Sungero_Content_EDoc
set AccCParty_Docflow_Sungero = Counterparty_Docflow_Sungero,
    AccTotalAmount_Docflow_Sungero = TotalAmount_Docflow_Sungero,
    Currency_Docflow_Sungero = ContrCurrency_Docflow_Sungero,
    PartySignatory_FinArch_Sungero = PartySignatory_Docflow_Sungero,
    Contact_Docflow_Sungero = Contact_Contrac_Sungero,
    RespEmpl_Docflow_Sungero = RespEmpl_Contrac_Sungero
where Discriminator = ''f2f5774d-5ca3-4725-b31d-ac618f6b8850''
')
-- Восстановить индекс
CREATE NONCLUSTERED INDEX idx_ElectronicDocument_LifeCycleState_Discriminator_RespEmpl ON Sungero_Content_EDoc
(
[LifeCycleState_Docflow_Sungero] ASC,
[Discriminator] ASC,
[RespEmpl_Contrac_Sungero] ASC
)
INCLUDE ( [SecureObject],
[IntApprState_Docflow_Sungero],
[ExtApprState_Docflow_Sungero])
WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF,
SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF,
ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]

-- Восстановить индексы и fk платформы (шо с именами, кэп?)
-- Counterparty_Docflow_Sungero
if not exists (SELECT * FROM sys.indexes 
               WHERE name='IXN27F62FB17FB390EF' AND object_id = OBJECT_ID('Sungero_Content_EDoc'))
begin
  CREATE NONCLUSTERED INDEX [IXN27F62FB17FB390EF] ON [Sungero_Content_EDoc]
  (
    [Counterparty_Docflow_Sungero] ASC
  )WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
end

if not exists (SELECT * FROM sys.foreign_keys 
               WHERE object_id = OBJECT_ID(N'FKN1F17A7003322F438') AND parent_object_id = OBJECT_ID(N'Sungero_Content_EDoc'))
begin
  ALTER TABLE [dbo].[Sungero_Content_EDoc]  WITH CHECK ADD  CONSTRAINT [FKN1F17A7003322F438] FOREIGN KEY([Counterparty_Docflow_Sungero])
  REFERENCES [dbo].[Sungero_Parties_Counterparty] ([Id])

  ALTER TABLE [dbo].[Sungero_Content_EDoc] CHECK CONSTRAINT [FKN1F17A7003322F438]
end

-- ContrCurrency_Docflow_Sungero
if not exists (SELECT * FROM sys.indexes 
               WHERE name='IXNF89F40C1A0FB954C' AND object_id = OBJECT_ID('Sungero_Content_EDoc'))
begin
  CREATE NONCLUSTERED INDEX [IXNF89F40C1A0FB954C] ON [dbo].[Sungero_Content_EDoc]
  (
    [ContrCurrency_Docflow_Sungero] ASC
  )WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
end

if not exists (SELECT * FROM sys.foreign_keys 
               WHERE object_id = OBJECT_ID(N'FKN1BF1F61573E727D6') AND parent_object_id = OBJECT_ID(N'Sungero_Content_EDoc'))
begin
  ALTER TABLE [dbo].[Sungero_Content_EDoc]  WITH CHECK ADD  CONSTRAINT [FKN1BF1F61573E727D6] FOREIGN KEY([ContrCurrency_Docflow_Sungero])
  REFERENCES [dbo].[Sungero_Commons_Currency] ([Id])

  ALTER TABLE [dbo].[Sungero_Content_EDoc] CHECK CONSTRAINT [FKN1BF1F61573E727D6]
end

-- PartySignatory_Docflow_Sungero
if not exists (SELECT * FROM sys.indexes 
               WHERE name='IXNBEE4FC70C2D1090A' AND object_id = OBJECT_ID('Sungero_Content_EDoc'))
begin
  CREATE NONCLUSTERED INDEX [IXNBEE4FC70C2D1090A] ON [dbo].[Sungero_Content_EDoc]
  (
    [PartySignatory_Docflow_Sungero] ASC
  )WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
end

if not exists (SELECT * FROM sys.foreign_keys 
               WHERE object_id = OBJECT_ID(N'FKN8A3CD98E586767EC') AND parent_object_id = OBJECT_ID(N'Sungero_Content_EDoc'))
begin
  ALTER TABLE [dbo].[Sungero_Content_EDoc]  WITH CHECK ADD  CONSTRAINT [FKN8A3CD98E586767EC] FOREIGN KEY([PartySignatory_Docflow_Sungero])
  REFERENCES [dbo].[Sungero_Parties_Contact] ([Id])

  ALTER TABLE [dbo].[Sungero_Content_EDoc] CHECK CONSTRAINT [FKN8A3CD98E586767EC]
end

-- Contact_Contrac_Sungero
if not exists (SELECT * FROM sys.indexes 
               WHERE name='IXN22EDEA54EF93DCE6' AND object_id = OBJECT_ID('Sungero_Content_EDoc'))
begin
  CREATE NONCLUSTERED INDEX [IXN22EDEA54EF93DCE6] ON [dbo].[Sungero_Content_EDoc]
  (
    [Contact_Contrac_Sungero] ASC
  )WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
end

if not exists (SELECT * FROM sys.foreign_keys 
               WHERE object_id = OBJECT_ID(N'FKN0E850395701FE7B1') AND parent_object_id = OBJECT_ID(N'Sungero_Content_EDoc'))
begin
  ALTER TABLE [dbo].[Sungero_Content_EDoc]  WITH CHECK ADD  CONSTRAINT [FKN0E850395701FE7B1] FOREIGN KEY([Contact_Contrac_Sungero])
  REFERENCES [dbo].[Sungero_Parties_Contact] ([Id])

  ALTER TABLE [dbo].[Sungero_Content_EDoc] CHECK CONSTRAINT [FKN0E850395701FE7B1]
end

-- RespEmpl_Contrac_Sungero
if not exists (SELECT * FROM sys.indexes 
               WHERE name='IXN22EDEA54EF93DCE6' AND object_id = OBJECT_ID('Sungero_Content_EDoc'))
begin
  CREATE NONCLUSTERED INDEX [IXN0668F037528318E1] ON [dbo].[Sungero_Content_EDoc]
  (
    [RespEmpl_Contrac_Sungero] ASC
  )WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
end

if not exists (SELECT * FROM sys.foreign_keys 
               WHERE object_id = OBJECT_ID(N'FKN605B25AB0C41765A') AND parent_object_id = OBJECT_ID(N'Sungero_Content_EDoc'))
begin
  ALTER TABLE [dbo].[Sungero_Content_EDoc]  WITH CHECK ADD  CONSTRAINT [FKN605B25AB0C41765A] FOREIGN KEY([RespEmpl_Contrac_Sungero])
  REFERENCES [dbo].[Sungero_Core_Recipient] ([Id])

  ALTER TABLE [dbo].[Sungero_Content_EDoc] CHECK CONSTRAINT [FKN605B25AB0C41765A]
end
