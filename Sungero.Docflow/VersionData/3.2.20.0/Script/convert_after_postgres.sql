do $$
begin
  if exists(select *
              from information_schema.columns
              where table_name = 'Sungero_Content_EDoc' and column_name = 'DocumentDate_Docflow_Sungero')
  then
    update Sungero_Content_EDoc
      set DocumentDate_Docflow_Sungero = coalesce(RegDate_Docflow_Sungero, Created)
      where DocumentDate_Docflow_Sungero is null
        and Discriminator <> 'a523a263-bc00-40f9-810d-f582bae2205d';
      
    update Sungero_Content_EDoc
      set DocumentDate_Docflow_Sungero = coalesce(AccDate_Docflow_Sungero, Created)
      where DocumentDate_Docflow_Sungero is null
        and Discriminator = 'a523a263-bc00-40f9-810d-f582bae2205d';
  end if;
end$$;