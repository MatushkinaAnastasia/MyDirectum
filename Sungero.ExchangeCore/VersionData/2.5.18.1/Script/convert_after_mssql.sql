declare @KAresponsibleGuid varchar(36) = 'C719C823-C4BD-4434-A34B-D7E83E524414'
declare @ExServiceUsersGuid varchar(36) = '5AFA06FB-3B66-4216-8681-56ACDEAC7FC1'
declare @BUBoxResponsibleGuid varchar(36) = 'AB0DC99D-02B3-4863-8760-D0E9562D7EEE'

declare @KAresponsibleId int
declare @ExServiceUsersId int
declare @BUBoxResponsibleId int

-- Поиск роли "Ответственные за сервисы обмена"
if (select 1 from Sungero_Core_Recipient where [Sid] = @BUBoxResponsibleGuid) is not null
begin
	set @BUBoxResponsibleId = (select id from Sungero_Core_Recipient where [Sid] = @BUBoxResponsibleGuid)

	-- Поиск роли "Пользователи с правами на работу через сервис обмена"
	if (select 1 from Sungero_Core_Recipient where [Sid] = @ExServiceUsersGuid) is not null
	begin
		set @ExServiceUsersId = (select id from Sungero_Core_Recipient where [Sid] = @ExServiceUsersGuid)

		-- Удаление из участников
		delete from Sungero_Core_RecipientLink where [Recipient] = @ExServiceUsersId and [Member] = @BUBoxResponsibleId

		-- Перенос пользователей в новую роль
		update Sungero_Core_RecipientLink 
		set [Recipient] = @ExServiceUsersId
		where [Recipient] = @BUBoxResponsibleId		

		-- Поиск роли "Ответственные за контрагентов"
		if (select 1 from Sungero_Core_Recipient where [Sid] = @KAresponsibleGuid) is not null
		begin
			set @KAresponsibleId = (select id from Sungero_Core_Recipient where [Sid] = @KAresponsibleGuid)

			-- Удаление из участников
			delete from Sungero_Core_RecipientLink where [Recipient] = @KAresponsibleId and [Member] = @BUBoxResponsibleId
		end
	end
  -- Удаление роли "Ответственные за сервисы обмена"
  delete from Sungero_Core_Recipient
  where id = @BUBoxResponsibleId
end