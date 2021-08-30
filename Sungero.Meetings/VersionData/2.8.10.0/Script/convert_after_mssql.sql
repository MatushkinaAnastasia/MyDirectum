if not exists (select * from information_schema.COLUMNS 
               where table_name = 'Sungero_Content_EDoc' 
               and COLUMN_NAME = 'MinutesMeeting_Meeting_Sungero')
begin
  alter table Sungero_Content_EDoc
  add MinutesMeeting_Meeting_Sungero integer

  CREATE NONCLUSTERED INDEX [IXNF4A800A4FE7D95F1] ON [dbo].[Sungero_Content_EDoc]
  (
    [MinutesMeeting_Meeting_Sungero] ASC
  )WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]

  ALTER TABLE [dbo].[Sungero_Content_EDoc]  WITH CHECK ADD  CONSTRAINT [FKNC44C7AF3EC5215D8] FOREIGN KEY([MinutesMeeting_Meeting_Sungero])
  REFERENCES [dbo].[Sungero_Meeting_Meeting] ([Id])

  ALTER TABLE [dbo].[Sungero_Content_EDoc] CHECK CONSTRAINT [FKNC44C7AF3EC5215D8]
end
