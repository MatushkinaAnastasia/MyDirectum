do $$
begin
if exists(select *
          from information_schema.tables
          where table_name = 'temp_companyresponsible')
  then
    update Sungero_Parties_Counterparty c1
    set Responsible = tmp.ResponsibleId
    from Sungero_Parties_Counterparty c
    join temp_companyresponsible tmp
      on tmp.CompanyId = c.Id
     where c.Id = c1.Id;
    
    drop table temp_companyresponsible;
  end if;
end$$;