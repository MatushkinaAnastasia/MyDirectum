if exists (select
           *
           from information_schema.columns
           where table_name = 'Sungero_ExCore_BoxBase'
             and column_name = 'ExchangeServic') 
and
exists (select * from information_schema.tables where table_name = 'ExchangeServiceLinks')

begin
  execute('update box
           set
               [ExchangeServic] = links.ExchangeSystem
           from 
             dbo.ExchangeServiceLinks links
           join [dbo].[Sungero_ExCore_BoxBase] box
             on box.Id = links.Id
          ')
end

if exists (select * from information_schema.tables where table_name = 'ExchangeServiceLinks')
  drop table ExchangeServiceLinks