DO $$
BEGIN

  IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'fkn1f17a7003322f438') 
  THEN
    ALTER table sungero_content_edoc
	    ADD constraint fkn1f17a7003322f438
		  foreign key (counterparty_docflow_sungero) references sungero_parties_counterparty;
  END IF;
  
  IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'fkn1bf1f61573e727d6')
  THEN
    ALTER table sungero_content_edoc
	    ADD constraint fkn1bf1f61573e727d6
		  foreign key (contrcurrency_docflow_sungero) references sungero_commons_currency;
  END IF;  
  
  IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'fkn8a3cd98e586767ec')
  THEN
    ALTER table sungero_content_edoc
	    ADD constraint fkn8a3cd98e586767ec
		  foreign key (partysignatory_docflow_sungero) references sungero_parties_contact;
  END IF;
  
  IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'fkn0e850395701fe7b1')
  THEN
    ALTER table sungero_content_edoc
	    ADD constraint fkn0e850395701fe7b1
		  foreign key (contact_contrac_sungero) references sungero_parties_contact;
  END IF;
  
  IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'fkn605b25ab0c41765a')
  THEN
    ALTER table sungero_content_edoc
	    ADD constraint fkn605b25ab0c41765a
		  foreign key (respempl_contrac_sungero) references sungero_core_recipient;
  END IF;

END$$