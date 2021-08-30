-- Восстановление сертификата в абонентском ящике НОР
do $$
begin
if exists(select *
          from information_schema.tables
          where table_name = 'temp_certificatesreceiptnotifications')
  then
      update Sungero_ExCore_BoxBase
      set ServiceDocCert = temp.CertificateId
      from temp_certificatesreceiptnotifications temp
      where Id = temp.BoxId;
      
    drop table temp_CertificatesReceiptNotifications;
  end if;
end$$;