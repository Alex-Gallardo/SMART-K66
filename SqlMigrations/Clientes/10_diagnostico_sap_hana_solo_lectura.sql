-- SAP HANA, SOLO LECTURA. Ejecute en HANA Studio / Database Explorer, no en SSMS.
-- Reemplace <SCHEMA> por el schema de una empresa y <CARD_CODE> por un cliente
-- que tenga contactos y direcciones. Puede ocultar nombres, NIT, correo y teléfono
-- al compartir resultados; conserve los nombres de columnas y la presencia de valores.

SELECT "TABLE_NAME", "COLUMN_NAME", "DATA_TYPE_NAME"
FROM "SYS"."TABLE_COLUMNS"
WHERE "SCHEMA_NAME" = '<SCHEMA>'
  AND "TABLE_NAME" IN ('OCRD', 'OCPR', 'CRD1', 'OCRG', 'OCTG')
  AND ("COLUMN_NAME" IN (
       'CardCode', 'CardName', 'CardFName', 'CardType', 'LicTradNum', 'Address',
       'MailAddres', 'E_Mail', 'Phone1', 'Cellular', 'CntctPrsn', 'GroupCode',
       'GroupNum', 'Currency', 'CmpPrivate', 'Name', 'Position', 'Profession',
       'Tel1', 'Cellolar', 'E_MailL', 'Active', 'Street', 'StreetNo', 'Block',
       'City', 'County', 'State', 'Country', 'ZipCode', 'AdresType', 'LineNum',
       'PymntGroup', 'ExtraDays', 'ExtraMonth', 'PayDuMonth', 'GroupName')
       OR SUBSTRING("COLUMN_NAME", 1, 2) = 'U_')
ORDER BY "TABLE_NAME", "COLUMN_NAME";

SELECT C."CardCode", C."CardName", C."CardFName", C."CardType",
       C."LicTradNum", C."Address", C."MailAddres", C."E_Mail",
       C."Phone1", C."Cellular", C."CntctPrsn", C."GroupCode",
       C."GroupNum", C."Currency", C."CmpPrivate",
       G."GroupName", P."PymntGroup", P."ExtraDays",
       P."ExtraMonth", P."PayDuMonth"
FROM "<SCHEMA>"."OCRD" C
LEFT JOIN "<SCHEMA>"."OCRG" G ON G."GroupCode" = C."GroupCode"
LEFT JOIN "<SCHEMA>"."OCTG" P ON P."GroupNum" = C."GroupNum"
WHERE C."CardCode" = '<CARD_CODE>' AND C."CardType" = 'C';

SELECT "CntctCode", "CardCode", "Name", "Position", "Profession",
       "Tel1", "Cellolar", "E_MailL", "Active"
FROM "<SCHEMA>"."OCPR"
WHERE "CardCode" = '<CARD_CODE>'
ORDER BY "CntctCode";

SELECT "LineNum", "CardCode", "Address", "AdresType", "Street",
       "StreetNo", "Block", "City", "County", "State", "Country",
       "ZipCode"
FROM "<SCHEMA>"."CRD1"
WHERE "CardCode" = '<CARD_CODE>'
ORDER BY "AdresType", "LineNum";

-- Consulta opcional: ejecute solo si los U_* figuran en TABLE_COLUMNS para el schema elegido.
SELECT "CardCode", "CardFName", "U_NOMBRESECUNDARIO", "U_NIT",
       "U_METODOPAGO", "U_TIPOCLIENTE", "U_REGION", "U_PAIS",
       "U_CREDITO2", "U_CREDITO3", "U_CREDITOTEMPORADA"
FROM "<SCHEMA>"."OCRD"
WHERE "CardCode" = '<CARD_CODE>' AND "CardType" = 'C';
