do $$
begin
if exists(select *
            from information_schema.tables
            where table_name = 'temp_companyresponsible')
  then
   drop table temp_companyresponsible;
  end if;

if exists(select *
          from information_schema.columns
          where table_name = 'sungero_parties_counterparty' and column_name = 'responsible')
  then
    create table temp_companyresponsible (
     CompanyId integer,
     ResponsibleId integer
    );
    insert into temp_companyresponsible
    select Id, Responsible
    from Sungero_Parties_Counterparty
    where Responsible is not null;
  end if;
end$$;