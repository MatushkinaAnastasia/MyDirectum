-- Перенос во временную таблицу сертификата из BoxBase
if exists(select *
          from information_schema.tables
          where TABLE_NAME = 'temp_CertificatesReceiptNotifications')
  drop table temp_CertificatesReceiptNotifications

if exists(select *
          from information_schema.COLUMNS
          where TABLE_NAME = 'Sungero_ExCore_BoxBase' and COLUMN_NAME = 'CertServiceDoc')
  begin
    execute ('
      create table temp_CertificatesReceiptNotifications (
        BoxId integer,
        CertificateId  integer
      )
      insert into temp_CertificatesReceiptNotifications
        select
          [Id],
          [CertServiceDoc]
        from [Sungero_ExCore_BoxBase]
        where [CertServiceDoc] is not null
  ')
  end