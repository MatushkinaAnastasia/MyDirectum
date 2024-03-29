﻿do $$
-- Заполняем валютой по умолчанию
DECLARE CURRENCY INT;
DECLARE NUMBER CITEXT := 'б/н';
BEGIN
CURRENCY := (select Id from Sungero_Commons_Currency where IsDefault = TRUE limit 1);
if (CURRENCY is null)
  THEN
  CURRENCY := (select Id from Sungero_Commons_Currency where AlphaCode = 'RUB' limit 1);
END IF;
-- Если нет русского типа документа "простой документ", то считаем это английским разворачиванием
if not exists (select 1 from Sungero_Docflow_DocumentType where Name = 'Простой документ' limit 1)
  THEN
    NUMBER := '-';
END IF;

update Sungero_Content_EDoc
set Currency_Docflow_Sungero = CURRENCY
where Discriminator = 'a523a263-bc00-40f9-810d-f582bae2205d'
      and Sungero_Content_EDoc.Currency_Docflow_Sungero is null;

-- Заполняем сумму по умолчанию
UPDATE Sungero_Content_EDoc
set AccTotalAmount_Docflow_Sungero = 0
where Discriminator = 'a523a263-bc00-40f9-810d-f582bae2205d'
      and Sungero_Content_EDoc.AccTotalAmount_Docflow_Sungero is null;

-- Заполняем дату по умолчанию
UPDATE Sungero_Content_EDoc
set AccDate_Docflow_Sungero = Created
where Discriminator = 'a523a263-bc00-40f9-810d-f582bae2205d'
      and Sungero_Content_EDoc.AccDate_Docflow_Sungero is null;

-- Заполняем номер по умолчанию
update Sungero_Content_EDoc
set AccNumber_Docflow_Sungero = NUMBER
where Discriminator = 'a523a263-bc00-40f9-810d-f582bae2205d'
      and Sungero_Content_EDoc.AccNumber_Docflow_Sungero is null;
end$$;