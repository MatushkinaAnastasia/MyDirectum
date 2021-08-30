  update dbo.Sungero_Docflow_Register
  set NumSection = 'LeadingDocument'
  where Id in 
   (select r.Id 
		from dbo.Sungero_Docflow_Register as r
		join dbo.Sungero_Docflow_NumSections as s
		on r.Id = s.Register 
		where r.NumSection is NULL 
		and s.Section = 'Document')
		
  update dbo.Sungero_Docflow_Register
  set NumPeriod = 'Year'
  where Id in 
   (select r.Id 
		from dbo.Sungero_Docflow_Register as r
		join dbo.Sungero_Docflow_NumSections as s
		on r.Id = s.Register 
		where r.NumPeriod is NULL 
		and s.Section = 'Year')
		
  update dbo.Sungero_Docflow_Register
  set NumPeriod = 'Quarter'
  where Id in 
   (select r.Id 
		from dbo.Sungero_Docflow_Register as r
		join dbo.Sungero_Docflow_NumSections as s
		on r.Id = s.Register 
		where r.NumPeriod is NULL 
		and s.Section = 'Quarter')
		
  update dbo.Sungero_Docflow_Register
  set NumPeriod = 'Month'
  where Id in 
   (select r.Id 
		from dbo.Sungero_Docflow_Register as r
		join dbo.Sungero_Docflow_NumSections as s
		on r.Id = s.Register 
		where r.NumPeriod is NULL 
		and s.Section = 'Month')

  update dbo.Sungero_Docflow_Register
  set NumSection = 'NoSection'
  where NumSection is NULL

  update dbo.Sungero_Docflow_Register
  set NumPeriod = 'Continuous'
  where NumPeriod is NULL