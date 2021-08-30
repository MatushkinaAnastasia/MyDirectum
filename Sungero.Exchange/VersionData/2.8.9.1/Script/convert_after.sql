update sungero_core_job 
set Description = case when Name = 'Преобразование формализованных документов' or Name = 'Преобразование документов электронного обмена'  
                         then 'Преобразование документов электронного обмена в PDF с наложением штампа электронной подписи'
                         else 'Convert electronic exchange documents to PDF with adding an electronic signature stamp'
                  end
where JobId = '65FA7815-170D-489C-B613-C2C2366161A4'
  and Description in ('Преобразование формализованных документов из XML в PDF с наложением штампа электронной подписи',
                      'Convert formalized documents from XML to PDF with adding an electronic signature stamp');