update sungero_content_edoc
set name = replace(name, 'c', 'с')
where discriminator in ('d1d2a452-7732-4ba8-b199-0a4dc78898ac', '49d0c5e7-7069-44d2-8eb6-6e3098fc8b10')
  and (name like '%Иcх%'
  or name like '%Cвед%');


update sungero_docflow_documentkind
set shortname = replace(shortname, 'c', 'с')
where shortname like '%Иcх%'
   or shortname like '%Cведения%';