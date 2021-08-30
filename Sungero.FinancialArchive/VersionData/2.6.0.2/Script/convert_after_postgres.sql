DO $$
BEGIN
  IF NOT EXISTS(SELECT 1 
                FROM information_schema.Columns 
                WHERE table_name = 'sungero_content_edoc' 
                  and column_name = 'counterparty_docflow_sungero')
  THEN
    alter table Sungero_Content_EDoc add Counterparty_Docflow_Sungero integer;
  END IF;

  IF NOT EXISTS(SELECT 1 
                FROM information_schema.Columns 
                WHERE table_name = 'sungero_content_edoc' 
                  and column_name = 'totalamount_docflow_sungero')
  THEN
    alter table Sungero_Content_EDoc add TotalAmount_Docflow_Sungero double precision;
  END IF;

  IF NOT EXISTS(SELECT 1 
                FROM information_schema.Columns 
                WHERE table_name = 'sungero_content_edoc' 
                  and column_name = 'contrcurrency_docflow_sungero')
  THEN
    alter table Sungero_Content_EDoc add ContrCurrency_Docflow_Sungero integer;
  END IF;

  IF NOT EXISTS(SELECT 1 
                FROM information_schema.Columns 
                WHERE table_name = 'sungero_content_edoc' 
                  and column_name = 'partysignatory_docflow_sungero')
  THEN
    alter table Sungero_Content_EDoc add PartySignatory_Docflow_Sungero integer;
  END IF;

  IF NOT EXISTS(SELECT 1 
                FROM information_schema.Columns 
                WHERE table_name = 'sungero_content_edoc' 
                  and column_name = 'contact_contrac_sungero')
  THEN
    alter table Sungero_Content_EDoc add Contact_Contrac_Sungero integer;
  END IF;

  IF NOT EXISTS(SELECT 1 
                FROM information_schema.Columns 
                WHERE table_name = 'sungero_content_edoc' 
                  and column_name = 'respempl_contrac_sungero')
  THEN
    alter table Sungero_Content_EDoc add RespEmpl_Contrac_Sungero integer;
  END IF;

  IF NOT EXISTS(SELECT 1 
                FROM information_schema.Columns 
                WHERE table_name = 'sungero_content_edoc' 
                  and column_name = 'validfrom_contrac_sungero')
  THEN
    alter table Sungero_Content_EDoc add ValidFrom_Contrac_Sungero timestamp without time zone;
  END IF;

  IF NOT EXISTS(SELECT 1 
                FROM information_schema.Columns 
                WHERE table_name = 'sungero_content_edoc' 
                  and column_name = 'validtill_contrac_sungero')
  THEN
    alter table Sungero_Content_EDoc add ValidTill_Contrac_Sungero timestamp without time zone;
  END IF;
END$$