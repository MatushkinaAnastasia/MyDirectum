  update sungero_docflow_register
  set numsection = 'LeadingDocument'
  where id in 
   (select r.id 
		from sungero_docflow_register as r
		join sungero_docflow_numSections as s
		on r.id = s.register 
		where r.numsection is NULL 
		and s.section = 'Document');

  update sungero_docflow_register
  set numperiod = 'Year'
  where id in 
   (select r.id 
		from sungero_docflow_register as r
		join sungero_docflow_numSections as s
		on r.id = s.register 
		where r.numperiod is NULL 
		and s.section = 'Year');

  update sungero_docflow_register
  set numperiod = 'Quarter'
  where id in 
   (select r.id 
		from sungero_docflow_register as r
		join sungero_docflow_numSections as s
		on r.id = s.register 
		where r.numperiod is NULL 
		and s.section = 'Quarter');	

  update sungero_docflow_register
  set numperiod = 'Month'
  where id in 
   (select r.id 
		from sungero_docflow_register as r
		join sungero_docflow_numSections as s
		on r.id = s.register 
		where r.numperiod is NULL 
		and s.section = 'Month');

  update sungero_docflow_register
  set numsection = 'NoSection'
  where numsection is NULL;
  
  update sungero_docflow_register
  set numperiod = 'Continuous'
  where numperiod is NULL