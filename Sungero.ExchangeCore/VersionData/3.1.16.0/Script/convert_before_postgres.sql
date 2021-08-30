-- Перенос во временную таблицу сертификата из BoxBase
do $$
begin
if exists(select *
          from information_schema.tables
          where table_name = 'temp_certificatesreceiptnotifications')
then
  drop table temp_certificatesreceiptnotifications;
end if;

if exists(select *
          from information_schema.columns
          where table_name = 'sungero_excore_boxbase' and column_name = 'certservicedoc')
then
  create table temp_certificatesreceiptnotifications (
    BoxId integer,
    CertificateId  integer
  );
  insert into temp_certificatesreceiptnotifications
    select
      Id,
      CertServiceDoc
    from Sungero_ExCore_BoxBase
    where CertServiceDoc is not null;
end if;
end $$;