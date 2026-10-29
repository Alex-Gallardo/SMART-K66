# Permiso de Gerencia Comercial

Ejecutar `01_permiso_menu.sql` en la base `POS-SmartK66_DEV` al desplegar el cambio. El script es repetible y registra `Control.GerenciaComercial.Ver` junto con la entrada principal del menú.

El permiso queda **sin asignar a roles**, según lo solicitado. Hasta que se conceda desde la administración de roles, ningún usuario podrá ver la entrada ni consultar la vista o sus endpoints. La última consulta del script permite comprobar las asignaciones existentes.
