# TourSmart RD — Proyecto Integrador II

## Objetivo
Transformar AgenciaToursAPI en una plataforma web de gestión y reservas turísticas con un portal público, panel administrativo y recomendaciones contextuales según presupuesto, preferencias, disponibilidad y condiciones climáticas.

## Punto de partida (auditoría estática, 2026-11-05)
- Backend ASP.NET Core 8, EF Core, SQL Server, JWT y Swagger.
- Controladores para usuarios, tours, reservas, clientes, guías, países, destinos, categorías, transporte y métodos de pago.
- No hay frontend ni pruebas automatizadas visibles en la rama master.
- **Seguridad urgente:** existe un valor Jwt:Key dentro de appsettings.json versionado. Rotar cualquier clave que haya sido usada y mover a Secret Manager o variables de entorno; considerar el historial Git. No copiar valores secretos a reportes.
- Falta autorización por roles/propietario de recursos; revisar registro público y operaciones de usuario.
- No hay aforo/cupos máximo por tour ni comprobación de concurrencia al reservar.
- No hay ciclo explícito de estados de reserva o confirmación/cancelación ni integración de pagos reales.
- Revisar impuestos y aclarar si Precio incluye o excluye ITBIS antes de facturar.
- Auditar fechas/zonas horarias y validaciones.

## Arquitectura recomendada
- Mantener AgenciaToursAPI como backend REST.
- Crear frontend ASP.NET Core MVC en proyecto separado dentro de la solución, con Bootstrap.
- Comunicación segura desde frontend con API, roles y sesión apropiada; no exponer credenciales.
- SQL Server + migraciones EF Core.
- Pruebas automáticas para seguridad y reglas de negocio.

## Fases y criterios de aceptación
### Fase 0 — Resguardo y base
- Trabajar en feature/toursmart-integrador-ii; no alterar master directamente.
- Documentar arquitectura y reproducir compilación/migración en entorno de pruebas.
- Rotar claves expuestas antes de despliegue y eliminar secretos del código de la versión nueva.

### Fase 1 — Seguridad y reservas
- Roles Administrador, Operador y Cliente; restringir endpoints correctamente.
- Tours con capacidad y cupos, disponibilidad segura ante solicitudes concurrentes.
- Reservas Pendiente/Confirmada/Cancelada; políticas de cambio y cancelación.
- Pruebas para accesos no autorizados, sobreventa, fechas y estados.

### Fase 2 — Interfaz
- Landing/catálogo público con fotos, filtros y detalles.
- Inicio de sesión y mis reservas.
- Panel administrativo: tours, clientes, reservas, guías, destinos y transportes.
- Diseño responsivo, validación y feedback claros.

### Fase 3 — Diferenciación
- Motor explicable de recomendaciones por presupuesto, interés, duración y cupos.
- Clima a través de proveedor meteorológico, advertencias no deterministas y alternativas.
- Mostrar fuentes/fecha de los datos de clima; fallback cuando el proveedor no responde.
- No afirmar exclusividad frente a agencias existentes.

### Fase 4 — Cierre
- Reportes, comprobantes, métricas, datos de prueba y demo end-to-end.
- Documentación académica, casos de uso, ERD, arquitectura y pruebas.

## Definición de MVP demostrable
Un usuario explora tours, recibe una recomendación, comprueba cupo y reserva; el administrador gestiona cupos y confirma la reserva desde una interfaz web; todo persistido en SQL Server y validado por pruebas.

## Estado
Plan inicial. Sin cambios funcionales ni ejecución de tests por el momento.
