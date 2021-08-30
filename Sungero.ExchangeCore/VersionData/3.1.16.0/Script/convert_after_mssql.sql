-- Восстановление сертификата в абонентском ящике НОР
if EXISTS(SELECT *
          FROM information_schema.tables
          WHERE TABLE_NAME = 'temp_CertificatesReceiptNotifications')
  begin
    execute ('
      update Sungero_ExCore_BoxBase
      set ServiceDocCert = temp.CertificateId
      from temp_CertificatesReceiptNotifications temp
      where Id = temp.BoxId
    ')
    DROP TABLE temp_CertificatesReceiptNotifications
  end