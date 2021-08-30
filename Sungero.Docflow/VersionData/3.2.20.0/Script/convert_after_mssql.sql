if exists(select *
          from information_schema.COLUMNS
          where TABLE_NAME = 'Sungero_Content_EDoc' and COLUMN_NAME = 'DocumentDate_Docflow_Sungero')
  begin
    execute ('
    update Sungero_Content_EDoc
      set DocumentDate_Docflow_Sungero = ISNULL(RegDate_Docflow_Sungero, Created)
      where DocumentDate_Docflow_Sungero is null
        and Discriminator <> ''a523a263-bc00-40f9-810d-f582bae2205d''
      
    update Sungero_Content_EDoc
      set DocumentDate_Docflow_Sungero = ISNULL(AccDate_Docflow_Sungero, Created)
      where DocumentDate_Docflow_Sungero is null
        and Discriminator = ''a523a263-bc00-40f9-810d-f582bae2205d''
      ')
  end