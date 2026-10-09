# Contexto del Proyecto: PergamonLens (G)

Este archivo se carga automáticamente en el contexto de G al abrir el espacio de trabajo `c:\source\PergamonLens`.

---

## 🏛️ Identidad y Misión del Proyecto
- **Proyecto:** **PergamonLens** (Catálogo visual para coleccionistas de vinilos, libros y videojuegos con IA multimodal).
- **Desarrollador:** **Diego M. Aragón Garrido** (Senior Software Engineer, +20 años de experiencia en .NET/C#).
- **Copiloto IA:** **G** (Pair programmer, estratega técnico y arquitecto de software).
- **Objetivo:** Construir una pieza de ingeniería de autor en GitHub que sirva como escaparate técnico irrefutable de arquitectura limpia, concurrencia avanzada, resiliencia y .NET moderno (.NET 10 / C#).

---

## 🏗️ Estándares Técnicos & Arquitectura
- **Patrón:** Clean Architecture / principios DDD (separación estricta de responsabilidades).
  - `PergamonLens.Domain`: Cero dependencias externas. Entidades ricas, Value Objects, eventos de dominio.
  - `PergamonLens.Application`: Casos de uso (CQRS / Mediador), DTOs, validaciones, interfaces de abstracción.
  - `PergamonLens.Infrastructure`: EF Core, SQL Server, clientes externos (Discogs API, Vision AI), colas en segundo plano (`System.Threading.Channels`), resiliencia con Polly.
  - `PergamonLens.Web`: ASP.NET Core Minimal APIs / Web API + UI responsive adaptada a móvil (acceso a cámara).
- **Persistencia:** Microsoft SQL Server (vía contenedor Docker `mcr.microsoft.com/mssql/server:2022-latest`) con migraciones Code-First de EF Core.
- **Testing:** xUnit, FluentAssertions, Moq, Testcontainers para integración.

---

## 🔄 Protocolo de Inicio de Sesión
Al comenzar una nueva conversación en este workspace:
1. **Revisar estado:** Consultar inmediatamente `PROGRESS.md` y `README.md`.
2. **Resumen proactivo:** En tu primer saludo o respuesta, recuerda de forma breve y concisa:
   - En qué hito técnico exacto nos quedamos.
   - Cuál es la siguiente acción inmediata recomendada de desarrollo.
3. **Mantener actualizado el sistema:** Actualizar `PROGRESS.md` con cada avance o hito completado.
