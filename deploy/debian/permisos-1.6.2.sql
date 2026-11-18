-- Run as zuni_migrador after the idempotent migrations, in the same transaction.
REVOKE ALL ON TABLE public."PinesProfesionales", public."DobleFactor" FROM PUBLIC;
REVOKE DELETE, TRUNCATE, TRIGGER, REFERENCES ON TABLE public."PinesProfesionales", public."DobleFactor" FROM zuni_app;
GRANT SELECT, INSERT, UPDATE ON TABLE public."PinesProfesionales", public."DobleFactor" TO zuni_app;
