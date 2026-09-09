/* =============================================================================
   BORRADORES NC — VALIDACION FINAL ESTRICTA DE PRODUCCION (SOLO LECTURA)
   Destino exclusivo: POS-SmartK66_DEV

   Ejecutar después de 01, 02 y 03. No modifica objetos ni datos. Si alguna
   condición del contrato no se cumple, finaliza con THROW y RESULTADO=REVISAR.
   ============================================================================= */

USE [POS-SmartK66_DEV];
GO

SET NOCOUNT ON;
SET LOCK_TIMEOUT 5000;
SET TRANSACTION ISOLATION LEVEL READ UNCOMMITTED;
GO

IF DB_NAME() <> N'POS-SmartK66_DEV'
BEGIN
    THROW 51500,
          'SEGURIDAD: esta validacion solo esta autorizada para POS-SmartK66_DEV.',
          1;
END;
GO

DECLARE @Validaciones TABLE
(
    ORDEN     int            NOT NULL PRIMARY KEY,
    VALIDACION nvarchar(200) NOT NULL,
    ESPERADO  bigint         NOT NULL,
    REAL      bigint         NOT NULL
);

INSERT INTO @Validaciones (ORDEN, VALIDACION, ESPERADO, REAL)
SELECT 10, N'Objetos principales', 5, COUNT_BIG(*)
FROM sys.objects O
JOIN sys.schemas S ON S.schema_id = O.schema_id
WHERE S.name = N'dbo'
  AND
  (
      (O.name IN (N'BORR_NC_SERIES', N'BORR_NC_ENC', N'BORR_NC_DET',
                  N'BORR_NC_ADJUNTO') AND O.type = 'U')
      OR (O.name = N'VW_BORR_NC_ACUMULADO' AND O.type = 'V')
  );

IF OBJECT_ID(N'dbo.BORR_NC_SERIES', N'U') IS NOT NULL
BEGIN
    INSERT INTO @Validaciones (ORDEN, VALIDACION, ESPERADO, REAL)
    SELECT 20, N'Series activas y configuradas', 3, COUNT_BIG(*)
    FROM dbo.BORR_NC_SERIES
    WHERE ACTIVO = 1
      AND ((EMPRESA = N'BOLIK' AND SERIE = N'BWB-')
        OR (EMPRESA = N'FAES'  AND SERIE = N'BWF-')
        OR (EMPRESA = N'GRACO' AND SERIE = N'BWG-'));
END
ELSE
BEGIN
    INSERT INTO @Validaciones VALUES
        (20, N'Series activas y configuradas', 3, 0);
END;

INSERT INTO @Validaciones (ORDEN, VALIDACION, ESPERADO, REAL)
SELECT 30, N'Permisos BorradorNC', 5, COUNT_BIG(*)
FROM dbo.Permiso
WHERE Nombre IN
(
    N'Control.BorradorNC.Ver',
    N'Control.BorradorNC.Guardar',
    N'Control.BorradorNC.Autorizar',
    N'Control.BorradorNC.Anular',
    N'Control.BorradorNC.VerTodos'
);

INSERT INTO @Validaciones (ORDEN, VALIDACION, ESPERADO, REAL)
SELECT 40, N'Entradas de menu BorradorNc', 2, COUNT_BIG(*)
FROM dbo.Menu
WHERE Controller = N'BorradorNc'
  AND ((Action = N'Index'
        AND PermisoId = N'Control.BorradorNC.Ver'
        AND IsActive = 1)
    OR (Action = N'Autorizaciones'
        AND PermisoId = N'Control.BorradorNC.Autorizar'
        AND IsActive = 1));

;WITH AsignacionesEsperadas AS
(
    SELECT N'Administrador' AS ROL, N'Control.BorradorNC.Ver' AS PERMISO
    UNION ALL SELECT N'Administrador',    N'Control.BorradorNC.Guardar'
    UNION ALL SELECT N'Administrador',    N'Control.BorradorNC.Autorizar'
    UNION ALL SELECT N'Administrador',    N'Control.BorradorNC.Anular'
    UNION ALL SELECT N'Administrador',    N'Control.BorradorNC.VerTodos'
    UNION ALL SELECT N'AdministradorK66', N'Control.BorradorNC.Ver'
    UNION ALL SELECT N'AdministradorK66', N'Control.BorradorNC.Guardar'
    UNION ALL SELECT N'AdministradorK66', N'Control.BorradorNC.Autorizar'
    UNION ALL SELECT N'AdministradorK66', N'Control.BorradorNC.Anular'
    UNION ALL SELECT N'AdministradorK66', N'Control.BorradorNC.VerTodos'
    UNION ALL SELECT N'Telemarketing',    N'Control.BorradorNC.Ver'
    UNION ALL SELECT N'Telemarketing',    N'Control.BorradorNC.Guardar'
    UNION ALL SELECT N'VendedorK66',      N'Control.BorradorNC.Ver'
    UNION ALL SELECT N'VendedorK66',      N'Control.BorradorNC.Guardar'
)
INSERT INTO @Validaciones (ORDEN, VALIDACION, ESPERADO, REAL)
SELECT 50, N'Asignaciones objetivo por rol', 14, COUNT_BIG(*)
FROM AsignacionesEsperadas E
JOIN dbo.Rol R
  ON R.Nombre = E.ROL
JOIN dbo.Rol_Permiso RP
  ON RP.Rol_Id = R.Rol_Id
 AND RP.Permiso_Id = E.PERMISO;

INSERT INTO @Validaciones (ORDEN, VALIDACION, ESPERADO, REAL)
SELECT 60, N'Restricciones deshabilitadas o no confiables', 0,
       (SELECT COUNT_BIG(*)
        FROM sys.check_constraints C
        WHERE C.parent_object_id IN
              (OBJECT_ID(N'dbo.BORR_NC_SERIES'),
               OBJECT_ID(N'dbo.BORR_NC_ENC'),
               OBJECT_ID(N'dbo.BORR_NC_DET'),
               OBJECT_ID(N'dbo.BORR_NC_ADJUNTO'))
          AND (C.is_disabled = 1 OR C.is_not_trusted = 1))
       +
       (SELECT COUNT_BIG(*)
        FROM sys.foreign_keys F
        WHERE F.parent_object_id IN
              (OBJECT_ID(N'dbo.BORR_NC_DET'),
               OBJECT_ID(N'dbo.BORR_NC_ADJUNTO'))
          AND (F.is_disabled = 1 OR F.is_not_trusted = 1));

IF OBJECT_ID(N'dbo.BORR_NC_ADJUNTO', N'U') IS NOT NULL
   AND OBJECT_ID(N'dbo.BORR_NC_ENC', N'U') IS NOT NULL
BEGIN
    INSERT INTO @Validaciones (ORDEN, VALIDACION, ESPERADO, REAL)
    SELECT 70, N'Adjuntos huerfanos', 0, COUNT_BIG(*)
    FROM dbo.BORR_NC_ADJUNTO A
    LEFT JOIN dbo.BORR_NC_ENC E
      ON E.ID_EMPRESA = A.ID_EMPRESA
     AND E.ID_BORRADOR = A.ID_BORRADOR
    WHERE E.ID_BORRADOR IS NULL;
END
ELSE
BEGIN
    INSERT INTO @Validaciones VALUES (70, N'Adjuntos huerfanos', 0, -1);
END;

SELECT
    N'05V_RESULTADO' AS SECCION,
    VALIDACION,
    ESPERADO,
    REAL,
    CASE WHEN ESPERADO = REAL THEN N'OK' ELSE N'REVISAR' END AS RESULTADO
FROM @Validaciones
ORDER BY ORDEN;

IF EXISTS (SELECT 1 FROM @Validaciones WHERE ESPERADO <> REAL)
BEGIN
    THROW 51501,
          'La validacion final de Borradores NC encontro diferencias. Revise el resultado antes de publicar la aplicacion.',
          1;
END;

PRINT N'OK: despliegue de base de datos Borradores NC validado en POS-SmartK66_DEV.';
GO
