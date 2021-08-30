if exists (select * from information_schema.tables where table_name = 'tmpLeadDocument')
begin
  execute('update docs
           set
               [LeadDocument_Docflow_Sungero] = tmp.LeadDocument
           from 
             dbo.tmpLeadDocument tmp
           inner join [dbo].[Sungero_Content_EDoc] docs
             on docs.Id = tmp.Id
          ')
end

if exists (select * from information_schema.tables where table_name = 'tmpLeadDocument')
  drop table tmpLeadDocument