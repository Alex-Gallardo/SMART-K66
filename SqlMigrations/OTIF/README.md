# Alcance de datos OTIF

El dashboard sigue disponible para cualquier usuario autenticado. Sin el permiso
`Control.OTIF.VerTodos`, la API limita los pedidos por empresa y por el vendedor
SAP del encabezado (`ORDR.SlpCode`) que corresponde a sus registros de
`Usuario_Empresa`. Si no hay una asignación válida, no se consulta OTIF en HANA.

`Empresa_Id` de FAES (`20210705003`) aparece como **Escocesa** en el selector y
usa el esquema `SBOESCOCESA`. El `Codigo` de `Usuario_Empresa` se resuelve contra
`OSLP`: se prefiere el ID SAP anterior al guion, y un nombre sin ID solo se
acepta si identifica a un único vendedor. Los registros ambiguos no conceden
acceso. Los pedidos se filtran antes de recuperar las entregas; por ello, el
modo «Pedido» sigue calculándose con todas las líneas de cada pedido autorizado.

## Despliegue

1. Publicar juntos `OTIFController.cs`, `otif.sql`, la vista y el JavaScript
   actualizado. Confirmar que `App_Data/otif.sql` esté en el servidor.
2. Seleccionar explícitamente la base **SQL Server de la aplicación** y ejecutar
   `01_registrar_permiso_global.sql`. Es idempotente y no asigna el permiso a
   ningún rol.
3. Otorgar `Control.OTIF.VerTodos` solo a los roles que deban consultar las tres
   empresas y todos los vendedores. Sin esa asignación, todos permanecen
   restringidos por `Usuario_Empresa`.
4. Probar con usuarios sin registros, con varios vendedores en una empresa,
   con varias empresas y con el permiso global. Verificar un intento directo a
   `/OTIF/GetData` con una empresa no asignada (HTTP 403), la comparación de
   períodos y las exportaciones. Conciliar los totales con HANA en cada sociedad.

El script SQL registra la capacidad; la ejecución y la asignación de roles son
pasos operativos y no ocurren al publicar el código.
