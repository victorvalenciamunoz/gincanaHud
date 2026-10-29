-- Fase 12, paso 5a: las tablas de Supabase se crearon sin migraciones y sus datos eran de prueba.
-- Se borran las de GincanaHud para que las migraciones las creen de cero. keepalive_ping no se toca.
DROP TABLE IF EXISTS
    "Captures",
    "ActivityPois",
    "ActivityParticipants",
    "Pois",
    "AdminUsers",
    "Activities",
    "Users",
    "Organizations",
    "__EFMigrationsHistory"
    CASCADE;
