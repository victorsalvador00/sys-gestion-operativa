# Avance del backend SGO

> Bitácora de trabajo para retomar entre sesiones. Última actualización: 2026-09-24.
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
| 3 | B-12 Proveedores y artículos de proveedor | ⏭️ siguiente | |
| 3 | B-13 Requisiciones y conversión a OC | pendiente | |
| 3 | B-14 Órdenes de compra, aprobación por umbral, recepción parcial, cierre | pendiente — requiere umbral de OC | |
| 3 | B-15 Pedidos de sucursal, sugerido mín/máx, aprobación → traspasos, RN-24 | pendiente | |
| 3 | B-16 Tablero y `/settings` | pendiente | |
| 3 | B-17 Endurecimiento (índices, EXPLAIN, bundle de migraciones, compose prod) | pendiente | |

Pruebas al cierre de B-11: **300 en verde** (191 unitarias, 109 de integración), sin warnings.
Migraciones (en orden): `InitialCreate`, `AddRefreshTokens`, `AddRoleSystemKey`, `AddCatalog`, `AddInventory`,
`AddAdjustments`, `AddCountsAndConsumptions`, `AddTransfers`, `AddRecipes`, `AddProductionOrders`.

## Decisiones acordadas con el cliente

**Decisiones abiertas del dominio (§7)** — ver estado en `dominio.md`:
- ✅ Salidas en sucursal: consumo diario (solo sucursales) + conteo físico.
- ✅ Costeo: promedio ponderado por ubicación.
- ✅ Traspasos especiales permitidos con `logistics.transfers.special` (Administrador, Almacén, Gerente de operaciones).
- ✅ Lotes: por artículo (`TracksLots`).
- ⏳ **Umbral de OC (MXN): pendiente.** Provisional `0` → toda OC requiere aprobación. **Preguntar antes de B-14.**
- ⏳ Días de alerta de caducidad: sin confirmar; default 3 (editable).

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

## Pendientes y notas técnicas

- **B-15:** ligar traspasos con pedido (columna `branch_order_id` ya existe) y RN-24.
- **B-14:** dos recepciones simultáneas que crean el mismo número de lote nuevo → una falla por índice único (poco probable; revisar ahí).
- Kardex devuelve `userId`, no el nombre del usuario (agregar si la pantalla lo pide).
- Ajustes y consumos no se cancelan (no hay endpoint en el spec); se corrige con otro ajuste.
- `docs/specs/dominio.md` §6 ahora tiene 29 permisos (se agregó `logistics.transfers.special`).

## Cómo retomar

```bash
# Docker Desktop debe estar abierto.
# En esta máquina el puerto 8080 lo ocupa otro proceso (Java), por eso API_PORT=8081.
API_PORT=8081 docker compose -f deploy/docker-compose.dev.yml up -d   # Postgres + API con hot reload
curl http://localhost:8081/health/ready                             # 200 = listo
# OpenAPI/Scalar: http://localhost:8081/scalar/v1

cd backend
dotnet build
dotnet test          # usa Testcontainers (Docker)
```

- Credenciales locales (admin, JWT, Postgres) en `deploy/.env` (no se versiona; copia de `deploy/.env.example`).
- La BD de desarrollo tiene datos de prueba: usuario "Demo Sucursal 3", categoría "Secos", artículos HAR-001 y AZU-001.
- Flujo por tarea (CLAUDE.md): leer tarea → proponer plan y esperar OK → implementar → pruebas en verde → resumen → commit `feat(...): ... [B-xx]`.
