-- Run after Railway-PostgreSQL-Bridge.sql, against the same database.
-- These ASP.NET Identity hashes correspond to the demo passwords documented in
-- AkarangaBank-Script-DB-PostgreSQL.sql. Change them after signing in.
BEGIN;

DO $passwords$
DECLARE
    updated_count integer;
BEGIN
    WITH credentials(email, role_name, password_hash) AS (
        VALUES
            ('ADMIN_BJM@AKARANGA.BI', 'admin', 'AQAAAAIAAYagAAAAEOL0eZaYjvu6jraUlexb6CXPF7Csi6coQDxZU+aGGn6ztoVgQStIUyWO3iut1DVbdw=='),
            ('GESTIONNAIRE_BJM@AKARANGA.BI', 'gestionnaire', 'AQAAAAIAAYagAAAAEPzloZ6WcI2w2spqBvv1GNKdbSUOwyTicoKRqY2Emo/1zJDPY3PwBpaL6G5HfgWBDw=='),
            ('AUDITEUR_BJM@AKARANGA.BI', 'auditeur', 'AQAAAAIAAYagAAAAEJnnhEjIgZR+lxB+Wek4u7x7O8YJ03M9pEKbJ+R/7JIY0tU43N1O0Nk8FbpDaxkn2g=='),
            ('GUICHETIER_GIT@AKARANGA.BI', 'guichetier', 'AQAAAAIAAYagAAAAEHkgLX+RRxvnC3eH8TrpPR5gbGmHowg1/CIIQkR9EdvgueG/bkvC2ulwm+Y+8FZ/XQ=='),
            ('GUICHETIER_NGO@AKARANGA.BI', 'guichetier', 'AQAAAAIAAYagAAAAEBSN6pQWTw9eLJu1SZ7bFQE69ReahPjhUHGF+ri9w06BdiX9vCANKSUdtXIH2kKiaA==')
    )
    UPDATE public."Utilisateurs" AS account
    SET "PasswordHash" = credentials.password_hash,
        "EstActif" = TRUE,
        "SecurityStamp" = md5(random()::text),
        "ConcurrencyStamp" = md5(random()::text),
        "AccessFailedCount" = 0,
        "LockoutEnd" = NULL
    FROM credentials
    WHERE account."NormalizedEmail" = credentials.email
      AND account."Role" = credentials.role_name;

    GET DIAGNOSTICS updated_count = ROW_COUNT;
    IF updated_count <> 5 THEN
        RAISE EXCEPTION 'Expected five imported demo users, found %. No passwords were changed.', updated_count;
    END IF;
END
$passwords$;

COMMIT;

SELECT "Email", "Role", "EstActif", "PasswordHash" IS NOT NULL AS "HasPassword"
FROM public."Utilisateurs"
WHERE "NormalizedEmail" IN (
    'ADMIN_BJM@AKARANGA.BI',
    'GESTIONNAIRE_BJM@AKARANGA.BI',
    'AUDITEUR_BJM@AKARANGA.BI',
    'GUICHETIER_GIT@AKARANGA.BI',
    'GUICHETIER_NGO@AKARANGA.BI'
)
ORDER BY "Id";
