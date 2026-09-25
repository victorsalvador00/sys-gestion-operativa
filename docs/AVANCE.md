# Avance del SGO (backend y frontend)

> Bitácora de trabajo para retomar entre sesiones. Última actualización: 2026-09-25.
> Fuente de verdad de reglas: `docs/specs/dominio.md`; tareas: `docs/specs/backend.md` §12.

## Estado por tarea

| Fase | Tarea | Estado | Commit |
|---|---|---|---|
| 1 | B-01 Solución, logging, ProblemDetails, health, OpenAPI, Docker | ✅ | `0c4ac5d` |
| 1 | B-02 DbContext, auditoría, timestamps, folios, semilla | ✅ | `fc263ae` |
| 1 | B-03 Login JWT, refresh con rotación, permisos, alcance de ubicación | ✅ | `192bc2f` |
| 1 | B-04 Usuarios, roles, permisos, bitácora (+ ubicaciones GET/PUT) | ✅ | `59e7a8f` |
| 1 | B-05 Unidades, categorías, artículos, mín/máx, CSV de artículos (+ POST ubicaciones) | ✅ | `64b0928` |
| 1 | B-06 Motor de inventario (FEFO, costo promedio, RN-02) | ✅ | `4465c3f` |
| 1 | B-07 Existencias, kardex, alertas, ajustes, CSV de existencias iniciales | ✅ | `7e0c71e` |
| 1 | B-08 Conteo físico y consumo de sucursal | ✅ | `659c74e` |
| 1 | B-09 Traspasos directos (despacho, tránsito, recepción con discrepancias) | ✅ | `64bdfcb` |
| 2 | B-10 Recetas versionadas y explosión teórica | ✅ | `ae9210a` |
| 2 | B-11 Órdenes de producción (completar transaccional) | ✅ | `e12626f` |
| 3 | B-12 Proveedores y artículos de proveedor | ✅ | `3671b24` |
| 3 | B-13 Requisiciones y conversión a OC | ✅ | `150b46d` |
| 3 | B-14 Órdenes de compra, aprobación por umbral, recepción parcial, cierre | ✅ | `dad493a` |
| 3 | B-15 Pedidos de sucursal, sugerido mín/máx, aprobación → traspasos, RN-24 | ✅ | `fa25631` |
| 3 | B-16 Tablero y `/settings` | ✅ | `3f19163` |
| 3 | B-17 Endurecimiento (índices, EXPLAIN, bundle de migraciones, compose prod) | ✅ | (ver `git log`) |

**Backend completo (B-01..B-17).**

### Frontend (`docs/specs/frontend.md` §11) — desde F-03 se trabaja directo en `main`

| Fase | Tarea | Estado | Commit |
|---|---|---|---|
| 1 | F-01 Proyecto Angular 22, Angular Material, es-MX, ESLint/Prettier, proxy, carpetas, Dockerfile | ✅ | (ver `git log`) |
| 1 | F-02 api:types, interceptores, AuthService, login, guardas, `*hasPermission`, ubicación activa | ✅ | (ver `git log`) |
| 1 | F-03 Layout y componentes compartidos | ✅ | (ver `git log`) |
| 1 | F-04 Administración | ⏳ | |
| 1 | F-05 Catálogos | ⏳ | |
| 1 | F-06 Inventario | ⏳ | |
| 1 | F-07 Conteo físico y consumo (móvil) | ⏳ | |
| 1 | F-08 Traspasos directos | ⏳ | |

Pruebas al cierre de B-17: **460 en verde** (322 unitarias, 138 de integración, incluida la de rendimiento), sin warnings.
Migraciones (en orden): `InitialCreate`, `AddRefreshTokens`, `AddRoleSystemKey`, `AddCatalog`, `AddInventory`,
`AddAdjustments`, `AddCountsAndConsumptions`, `AddTransfers`, `AddRecipes`, `AddProductionOrders`, `AddSuppliers`, `AddRequisitionsAndPurchaseOrders`, `AddGoodsReceipts`, `AddBranchOrders`, `TuneKardexIndexes`.

## Decisiones acordadas con el cliente

**Decisiones abiertas del dominio (§7)** — ver estado en `dominio.md`:
- ✅ Salidas en sucursal: consumo diario (solo sucursales) + conteo físico.
- ✅ Costeo: promedio ponderado por ubicación.
- ✅ Traspasos especiales permitidos con `logistics.transfers.special` (Administrador, Almacén, Gerente de operaciones).
- ✅ Lotes: por artículo (`TracksLots`).
- ✅ Umbral de OC: **configurable** en `AppSetting` (`purchasing.po_approval_threshold`), comparado contra el
  **subtotal sin IVA**; default `0` (toda OC requiere aprobación) hasta configurarlo en `/settings` (B-16).
- ✅ Días de alerta de caducidad: default 3, editable en `/settings` (B-16).

**Decisiones de implementación aprobadas:**
- Identity (usuarios/roles) se creó en B-02; los roles de sistema se identifican por `system_key` (se pueden renombrar).
- El rol **Administrador** siempre conserva todos los permisos. Anti-escalamiento: nadie otorga/quita permisos o ubicaciones que no tiene, ni administra usuarios con más acceso.
- El rol **Consulta** no ve la bitácora (`*.view` excluye `security.audit.view`).
- Restablecer contraseña: el admin captura una temporal; **sin** cambio forzado al siguiente login.
- Nombres de ubicaciones editables; en el frontend esa pantalla va en el **módulo de Usuarios y permisos**.
- CSV de artículos: encabezados en español (`sku,nombre,tipo,categoria,unidad_base,unidad_compra,factor_compra,maneja_lotes,vida_util_dias,almacenamiento,iva`); categoría inexistente = error (las categorías tienen su pantalla de alta/edición/desactivación); SKU existente se actualiza.
- Motivos de ajuste: Corrección (±, `Adjustment`); Merma/Caducado/Dañado (solo salidas, `Waste`); Uso interno (solo salida, `Adjustment`).
- CSV de existencias iniciales: `ubicacion,sku,cantidad,costo_unitario,lote,caducidad`; un ajuste por ubicación; se rechaza si el artículo ya tiene movimientos ahí.
- Lotes vencidos: se rechazan en despacho, producción y consumo; se permiten en ajustes, mermas y conteos.
- Conteo físico: líneas sin contar al cerrar = error; diferencias contra snapshot (los movimientos durante el conteo se respetan).
- Recetas: explosión de **un nivel**; editar una receta usada crea versión N+1 (`recipeVersion` en JSON; `version` es concurrencia).
- Producción: merma (RN-13) contra el teórico de lo **producido**; consumo real por defecto = ese teórico.
- Proveedores (B-12): RFC con formato SAT (12 moral / 13 física, fecha válida), en mayúsculas y **único**, salvo los
  genéricos `XAXX010101000` y `XEXX010101000` (varios proveedores extranjeros comparten el RFC genérico).
- Artículos de proveedor: baja lógica con `IsActive` (sin DELETE); un artículo por proveedor; solo artículos activos
  (de cualquier tipo); precio por unidad de compra (o unidad base si el artículo no tiene) sin IVA, ≥ 0.
- **Un solo proveedor preferido por artículo** (índice único parcial): marcar uno desmarca el anterior. Una fila
  inactiva nunca es preferida; desactivar un proveedor quita su preferencia en todos sus artículos, y sus artículos
  no se editan hasta reactivarlo. B-13 usará el preferido como proveedor sugerido.
- Requisiciones (B-13): solo **Fábrica y Comisariato** (`Location.CanPurchase`); `NeededBy` no puede ser pasada;
  cantidades en unidad de compra. Línea sin proveedor → toma el **preferido**; un proveedor indicado debe estar activo
  y tener el artículo activo en su catálogo. **Enviar exige proveedor en todas las líneas.** Aprobar y rechazar
  requieren `purchasing.po.approve`; rechazar pide motivo. Cancelar: en Draft, Submitted o Approved.
- Conversión (RN-34): OC agrupadas por **proveedor + ubicación de entrega**; **una línea de OC por línea de
  requisición** (sin sumar, conserva `RequisitionLineId`); precio = `SupplierItem.Price`, IVA = el del artículo;
  fecha esperada = la `NeededBy` más próxima; OC en `Draft`. Todo o nada; proveedor o artículo inactivo lo bloquea.
- La OC en B-13 es mínima (entidad, líneas, totales a 2 decimales redondeados por línea, `GET /purchase-orders`).
  Editar, enviar, aprobar/rechazar, cancelar, cerrar y recibir llegan en B-14.
- OC (B-14): estado nuevo **`Rejected`, final** (desde PendingApproval, con motivo). Cancelar solo en Draft,
  PendingApproval o Approved sin nada recibido; **cerrar solo desde PartiallyReceived**. Solo artículos del
  **catálogo activo del proveedor**; precio vacío = precio de catálogo (editable). Proveedor fijo al editar; las líneas
  que conservan `lineId` mantienen su liga con la requisición. Entrega solo en Fábrica/Comisariato.
- Recepción (B-14): factura opcional; una línea de OC puede repetirse (varios lotes); lote obligatorio si el artículo
  maneja lotes; sin caducidad → la del lote existente o hoy + vida útil; lotes vencidos se rechazan. Costo base =
  precio / factor (sin IVA). Tolerancia de sobre-recepción desde `AppSetting` (default 0 %). Se bloquea la OC
  (`poVersion` + lock de fila): recepciones simultáneas → una 201 y otra 409.
- Pedidos de sucursal (B-15): solo sucursales piden, a Fábrica o Comisariato; cualquier artículo activo; fecha
  requerida no pasada; cantidades en unidad base. Aprobar crea **un** traspaso Draft ligado (`branch_order_id`, ahora con
  FK) con lo aprobado (0..solicitado; todo en 0 → rechazar). El origen puede editar ese traspaso sin cambiar destino,
  sin artículos ajenos y sin pasar de lo aprobado. Cancelar ese traspaso **cancela el pedido**. RN-24 cuenta lo
  **despachado** (faltante en tránsito queda en el traspaso) → `Fulfilled`; si se mandó menos → `PartiallyFulfilled`
  (final). Sugerido: proyectado = existencia + en tránsito + pedidos enviados/aprobados sin despachar; si ≤ mín →
  máx − proyectado. Visibilidad: sucursal u origen en alcance; aprobar/rechazar exige alcance sobre el origen.
- `/settings` (B-16): lista tipada (clave, etiqueta, tipo, límites, decimales, versión) de los 3 parámetros; PUT
  parcial con versión por parámetro (409). Límites: umbral OC 0–99,999,999 (2 dec.), tolerancia 0–100 % (2 dec.),
  días de caducidad 0–365 enteros. Sin migración.
- `/dashboard` (B-16): solo autenticado (spec); cada bloque es `null` sin su permiso (`inventory.view`,
  `logistics.view`, `logistics.orders.approve`, `logistics.orders.create`, `purchasing.po.approve`, `production.view`);
  gráfica de bajo mínimo por ubicación solo con `locations.all`. Sin `locationId` suma todas las ubicaciones del alcance.
- Despliegue (B-17): imágenes construidas **en el Droplet** (sin registro); `deploy/docker-compose.yml` con `api`, `web`
  (Caddy: HTTPS automático, SPA desde `SPA_DIST`, proxy `/api` y `/health`, página de mantenimiento 503 sin build) y
  `migrate` (bundle de EF, target `migrator` del Dockerfile, perfil `tools`). Procedimiento en `deploy/README.md`.
- Rendimiento (B-17): prueba `Category=Performance` dentro de `dotnet test`; resultados y EXPLAIN en `docs/rendimiento.md`.
  Saldo corrido del kardex por SUM sobre índice cubriente (antes ventana sobre todo el historial).

**Frontend (F-01):**
- **Angular Material en lugar de PrimeNG** (2026-09-25): PrimeNG 22 y `@primeuix/themes` 3 cambiaron a la licencia
  "PrimeUI" (Community solo para <10 empleados/<1 MDD con clave anual; si no, de pago). PrimeNG 21.1.10 sigue MIT pero
  es la última versión libre y ata a Angular 21. Se eligió Angular Material/CDK 22 (MIT) + Chart.js para el tablero.
  Spec y CLAUDE.md actualizados.
- Angular 22.2 requiere Node ≥ 24.15 (se actualizó Node LTS a 24.19 en esta máquina).
- Proxy de desarrollo: `SGO_API_URL` o, por defecto, `http://localhost:8090`.
- Fuentes (Roboto) e íconos (Material Symbols) empaquetados localmente: funciona sin internet.
- `frontend/Dockerfile` (node:24-alpine → caddy:2-alpine con `/srv`) probado; el compose de producción **no se cambió**
  (sigue montando `frontend/dist/sgo/browser`). Decidir al contratar DigitalOcean si `web` se construye del Dockerfile.

**Frontend (F-02):**
- **Backend `fix(api)`:** MVC y el serializador de OpenAPI (`ConfigureHttpJsonOptions`) comparten opciones JSON:
  números estrictos (antes 215 campos salían `number | string`) y enums como texto (antes el OpenAPI los documentaba como
  enteros aunque la API manda texto). La API ya no acepta números entre comillas (400). Prueba `JsonOptionsTests`.
- `openapi-typescript` 7.13 declara peer `typescript@^5`; se usa `overrides` en package.json para TS 6 (genera bien).
  `schema.d.ts` se versiona. .NET 10 agrega `null` a la unión de cada enum: usar `ApiEnum<'X'>` (quita el null).
- Menú y rutas de todas las secciones con su permiso real; las que no existen muestran "Esta sección estará disponible
  pronto" (`placeholderRoute`). Cada F-xx reemplaza su placeholder.
- `/perfil` (cambiar contraseña) incluido. Tras cambiarla el backend cierra las sesiones → login con aviso.
- La traducción del paginador (`SpanishPaginatorIntl`) **no** se provee global (sumaba ~150 kB al arranque): la debe
  proveer `app-data-table` en F-03. Shell y login se cargan en diferido; carga inicial ≈ 121 kB comprimidos.
- 409 (`concurrency`, `insufficient_stock`): el interceptor los reenvía; los diálogos se hacen en F-03.

**Frontend (F-03):**
- **Backend `fix(api)`:** `DefaultSuccessResponseConvention` documenta el `200` con el tipo real en toda acción sin
  respuesta exitosa declarada (el 401/403 a nivel de clase impedía que .NET lo infiriera): 88 operaciones pasaron a
  tener tipo de respuesta; solo quedan 3 con `204` (correcto). Prueba `OpenApiResponseTests`.
- Componentes en `shared/components`: `page-header` (migas automáticas por `BreadcrumbsService`), `data-table`
  (servidor: `ListQuery` → `toHttpParams`; plantillas `appCell`/`appCardDef`; provee el paginador en español),
  `status-tag`, `item-picker` (valor = `ItemListItemDto`), `location-picker` (valor = id), `qty-input`,
  `lines-editor` (`appLineColumn` + `[appLineColumnOf]` para tipar la línea; validadores `minLinesValidator`,
  `uniqueLinesValidator`), `confirm-summary` (`ConfirmService`), `shortages-dialog` y `concurrency-dialog`
  (`ConflictHandler.handle(error)`). Pipes `qty`, `mxn`, `statusLabel` (etiquetas de todos los enums, tipadas contra el
  OpenAPI: un enum nuevo rompe la compilación).
- Controles propios (CVA) muestran el error del control del padre con `outerControlErrorState` (MatInput/MatSelect no
  recalculan su error sin `NgControl` propio).
- Fechas `dd/MM/yyyy`: `EsMxDateAdapter` provisto en el shell (`provideAppDates`).
- Menú lateral colapsable (se recuerda en localStorage). Demo en `/demo/componentes` (menú Administración, permiso
  `settings.manage`, visible también en producción).
- `MatSnackBar` se carga al mostrar el primer aviso: carga inicial ≈ 98 kB comprimidos.
- `app-audit-panel` pasa a F-04 (junto con la bitácora).

## Pendientes y notas técnicas

- Dos recepciones simultáneas de **OC distintas** que crean el mismo lote nuevo del mismo artículo → una falla por el
  índice único (500; muy poco probable). Las de la misma OC ya se serializan (409).
- **B-12:** dos usuarios que marcan al mismo tiempo proveedores preferidos distintos para el mismo artículo → uno
  falla por el índice único parcial (500; muy poco probable). Revisar si se vuelve un problema.
- Vigilar el `COUNT(*)` del kardex si una ubicación llega a millones de movimientos (ver `docs/rendimiento.md`).
- Kardex devuelve `userId`, no el nombre del usuario (agregar si la pantalla lo pide).
- Ajustes y consumos no se cancelan (no hay endpoint en el spec); se corrige con otro ajuste.
- `docs/specs/dominio.md` §6 ahora tiene 29 permisos (se agregó `logistics.transfers.special`).
- **Frontend:** la fuente de Material Symbols pesa ~4 MB (se descarga una vez y queda en caché). Si pesa en celulares
  de sucursal, en F-16 generar un subconjunto con solo los íconos usados.
- **Frontend:** el datepicker (formato dd/MM/yyyy) necesita un `DateAdapter` es-MX; se resuelve en F-03.
- **Backend:** los errores de *model binding* (JSON mal formado o tipo incorrecto) salen con mensajes técnicos en inglés
  (`"The request field is required."`). Solo ocurren con peticiones mal armadas, no con el frontend; traducir si molesta.

## Cómo retomar

```bash
# Docker Desktop debe estar abierto.
# En esta máquina un proceso Java ajeno ocupa puertos entre 8080 y 8082 (varía), por eso API_PORT=8090.
# Producción: ver deploy/README.md. Por ahora (sin DigitalOcean) el sistema corre solo local, con respaldos
# automáticos en deploy/backups (servicio `backup` del compose de desarrollo; ver "Uso local" en deploy/README.md).
API_PORT=8090 docker compose -f deploy/docker-compose.dev.yml up -d   # Postgres + API con hot reload
curl http://localhost:8090/health/ready                             # 200 = listo
# OpenAPI/Scalar: http://localhost:8090/scalar/v1

cd backend
dotnet build
dotnet test          # usa Testcontainers (Docker)

cd ../frontend       # requiere Node >= 24.15
npm ci
npm start            # http://localhost:4200, proxy a la API en :8090
npm test && npm run lint
```

- Credenciales locales (admin, JWT, Postgres) en `deploy/.env` (no se versiona; copia de `deploy/.env.example`).
- La BD de desarrollo tiene datos de prueba: usuario "Demo Sucursal 3", categoría "Secos", artículos HAR-001 y AZU-001.
- Flujo por tarea (CLAUDE.md): leer tarea → proponer plan y esperar OK → implementar → pruebas en verde → resumen → commit `feat(...): ... [B-xx/F-xx]`.
