if exists(select *
         from information_schema.tables
         where table_name = 'temp_companyresponsible')
  drop table temp_companyresponsible;

if exists(select *
          from information_schema.COLUMNS
          where TABLE_NAME = 'Sungero_Parties_Counterparty' and COLUMN_NAME = 'Responsible')
  begin
    execute ('
    create table temp_companyresponsible (
       CompanyId integer,
       ResponsibleId integer
     );
    insert into temp_companyresponsible
    select Id, Responsible
      from Sungero_Parties_Counterparty
      where Responsible is not null
  ')
  end