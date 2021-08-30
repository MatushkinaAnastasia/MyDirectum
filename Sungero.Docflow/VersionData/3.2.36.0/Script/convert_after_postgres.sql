do $$
begin

DROP INDEX IF EXISTS public.idx_edoc_discriminator_documentdate_lifecyclestate_intapprstate;
DROP INDEX IF EXISTS public.idx_edoc_discriminator_documentdate_regstate_dockind_secureobje;
DROP INDEX IF EXISTS public.idx_assignment_task_discriminator_executionstate_iscompound_sta;

end$$;