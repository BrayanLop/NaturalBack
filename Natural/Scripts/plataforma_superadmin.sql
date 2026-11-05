-- =====================================================================================
-- Natural · Alta del rol de plataforma (00) y del PRIMER superadmin
-- Ejecutar UNA vez en la BD de Natural (Supabase > SQL Editor). Es idempotente.
-- Después de esto el superadmin se administra desde la propia aplicación.
-- Reemplazar los valores marcados con <<...>> antes de ejecutar.
-- =====================================================================================

BEGIN;

-- 1) Rol de plataforma
INSERT INTO "Rol" ("Codigo", "Nombre", "Estado")
VALUES ('00', 'Plataforma', true)
ON CONFLICT ("Codigo") DO NOTHING;

-- 2) Persona del superadmin (sin empresa: "EmpresaId" NULL)
WITH nueva_persona AS (
    INSERT INTO "Persona" ("Nombre", "Apellido", "Cedula", "Residencia", "Email", "Edad", "Celular", "FechaNacimiento", "EmpresaId")
    SELECT '<<Nombre>>', '<<Apellido>>', '<<Cedula>>', NULL, '<<correo@dominio.com>>', 0, '<<3000000000>>', now(), NULL
    WHERE NOT EXISTS (SELECT 1 FROM "Usuario" WHERE "NombreUsuario" = '<<usuario_superadmin>>')
    RETURNING "Id"
),
-- 3) Usuario. La clave se guarda como el cliente la envía: SHA-256 de la contraseña en base64
--    (44 caracteres). Si en el API Seguridad:MigrarHashContrasenas = true, se convierte sola en el primer login.
nuevo_usuario AS (
    INSERT INTO "Usuario" ("NombreUsuario", "ClaveHash", "Estado", "PersonaId")
    SELECT '<<usuario_superadmin>>',
           encode(sha256(convert_to('<<contraseña_en_texto>>', 'UTF8')), 'base64'),
           true,
           "Id"
    FROM nueva_persona
    RETURNING "Id", "PersonaId"
),
rol_persona AS (
    INSERT INTO "PersonaRol" ("PersonaId", "RolCodigo")
    SELECT "PersonaId", '00' FROM nuevo_usuario
    RETURNING 1
)
INSERT INTO "UsuarioRol" ("UsuarioId", "RolCodigo")
SELECT "Id", '00' FROM nuevo_usuario;

COMMIT;

-- Verificación
SELECT u."Id", u."NombreUsuario", u."Estado", ur."RolCodigo", p."EmpresaId"
FROM "Usuario" u
JOIN "UsuarioRol" ur ON ur."UsuarioId" = u."Id"
JOIN "Persona" p ON p."Id" = u."PersonaId"
WHERE ur."RolCodigo" = '00';

-- =====================================================================================
-- Chequeo previo recomendado ANTES de desplegar esta versión: el login ahora rechaza
-- usuarios con "Estado" = false. Si aparecen filas aquí, esos usuarios quedarían sin acceso:
--   SELECT "Id", "NombreUsuario", "Estado" FROM "Usuario" WHERE "Estado" IS DISTINCT FROM true;
-- Y empresas desactivadas (Estado = false bloquea a toda la empresa):
--   SELECT "Id", "Nombre", "Estado" FROM "Empresa" WHERE "Estado" = false;
-- =====================================================================================
