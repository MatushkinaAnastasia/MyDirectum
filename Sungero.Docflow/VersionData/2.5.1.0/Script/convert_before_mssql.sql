if exists(select * from INFORMATION_SCHEMA.TABLES where TABLE_NAME='Sungero_Docflow_AFAApprovers')
begin
  exec('
  delete from Sungero_Docflow_AFAApprovers
where 
  Assignment in 
    (select id
    from 
      Sungero_WF_Assignment
    where 
      Discriminator = ''593ff79e-38b4-4903-b20e-9e08cfea6307'') 
  and 
  Approver in
    (select id 
     from 
       Sungero_Core_Recipient
     where 
       Discriminator <> ''b7905516-2be5-4931-961c-cb38d5677565'')
  ')
end

