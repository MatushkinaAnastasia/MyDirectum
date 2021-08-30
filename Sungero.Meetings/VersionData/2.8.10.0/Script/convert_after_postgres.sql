DO $$
BEGIN
  IF NOT EXISTS(SELECT 1 
                FROM information_schema.Columns 
                WHERE table_name = 'sungero_content_edoc' 
                  and column_name = 'minutesmeeting_meeting_sungero')
  THEN
    alter table sungero_content_edoc add minutesmeeting_meeting_sungero integer;

create index if not exists ixnf4a800a4fe7d95f1
	on sungero_content_edoc (minutesmeeting_meeting_sungero)
;

  alter table sungero_content_edoc
	add constraint fknc44c7af3ec5215d8
		foreign key (minutesmeeting_meeting_sungero) references sungero_meeting_meeting
;

  END IF;
END$$