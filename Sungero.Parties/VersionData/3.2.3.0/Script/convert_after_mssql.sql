if EXISTS(SELECT *
          FROM information_schema.tables
          WHERE TABLE_NAME = 'temp_companyresponsible')
  begin
    update c
      set Responsible = tmp.ResponsibleId
      from Sungero_Parties_Counterparty c
      join temp_companyresponsible tmp
        on tmp.CompanyId = c.Id
        
    drop table temp_companyresponsible;
  end