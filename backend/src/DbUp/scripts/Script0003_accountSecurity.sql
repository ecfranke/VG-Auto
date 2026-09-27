-- Account security: forced password change and login lockout
ALTER TABLE public.user ADD COLUMN IF NOT EXISTS must_change_password boolean NOT NULL DEFAULT false;
ALTER TABLE public.user ADD COLUMN IF NOT EXISTS failed_login_count integer NOT NULL DEFAULT 0;
ALTER TABLE public.user ADD COLUMN IF NOT EXISTS locked_until timestamp with time zone NULL;

-- The well known default admin password ("carcare") must be replaced on next login.
UPDATE public.user SET must_change_password = true
 WHERE password = '$2a$11$zsTS62pGn5Cfca4CgqRJxebx45je/3nJj.puxIArFwtAjHew67m6i';
