-- ============================================================================
-- Cambios manuales: Núcleo del servicio (propina, observaciones, estados, snapshot)
-- Tabla: RegistroServicio  (PostgreSQL)
--
-- NOTA: No se usa migración EF porque el ModelSnapshot está desincronizado
-- con la base de datos real (schema drift). Estas sentencias agregan SOLO las
-- columnas nuevas que el código C# espera. Son idempotentes (IF NOT EXISTS).
--
-- Ejecutar una sola vez contra la BD (dev primero, luego producción).
-- ============================================================================

-- Estado: rechazado + motivo (Pendiente / Aprobado / Rechazado)
ALTER TABLE "RegistroServicio" ADD COLUMN IF NOT EXISTS "Rechazado"    boolean         NOT NULL DEFAULT false;
ALTER TABLE "RegistroServicio" ADD COLUMN IF NOT EXISTS "MotivoRechazo" varchar(256);

-- Propina (100% del trabajador)
ALTER TABLE "RegistroServicio" ADD COLUMN IF NOT EXISTS "Propina"       numeric(18,2)   NOT NULL DEFAULT 0;

-- Observaciones del trabajador
ALTER TABLE "RegistroServicio" ADD COLUMN IF NOT EXISTS "Observaciones" varchar(256);

-- Snapshot histórico inmutable (capturado al registrar el servicio)
ALTER TABLE "RegistroServicio" ADD COLUMN IF NOT EXISTS "ValorServicio"        numeric(18,2);
ALTER TABLE "RegistroServicio" ADD COLUMN IF NOT EXISTS "PorcentajeTrabajador" numeric(5,2);
ALTER TABLE "RegistroServicio" ADD COLUMN IF NOT EXISTS "Comision"             numeric(18,2);

-- Trazabilidad de propinas: estado entregada/pendiente (evita pagar doble)
ALTER TABLE "RegistroServicio" ADD COLUMN IF NOT EXISTS "PropinaPagada"      boolean NOT NULL DEFAULT false;
ALTER TABLE "RegistroServicio" ADD COLUMN IF NOT EXISTS "FechaPropinaPagada" timestamp with time zone;
