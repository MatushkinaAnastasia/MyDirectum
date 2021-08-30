if exists (select * from information_schema.tables where table_name = 'ExchangeServiceLinks')
  drop table ExchangeServiceLinks

if exists (select
           *
           from information_schema.columns
           where table_name = 'Sungero_ExCore_BoxBase'
             and column_name = 'ExchangeSystem')
begin
  execute('create table ExchangeServiceLinks(Id integer, ExchangeSystem integer);
           insert into ExchangeServiceLinks
             select
               [Id],
               [ExchangeSystem]
             from
               [dbo].[Sungero_ExCore_BoxBase]
          ')
end
