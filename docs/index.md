# Rene.Utils.Db

Rene.Utils.Db provides generic MediatR command handlers for Entity Framework Core.
Register the entity/view-model pairs, choose a mapper, and send commands through MediatR.

## Start here

- [Getting started](guides/getting-started.md): install the packages and configure a working application.
- [Commands](guides/commands.md): create, read, update, delete and filtered queries.
- [Specifications](guides/specifications.md): filtering, ordering, includes and composition.
- [Mapping](guides/mapping.md): manual mappings, AutoMapper, Mapster and custom integrations.
- [Pagination](guides/pagination.md): zero-based pages and result metadata.
- [API reference](api/index.md): public types and members generated from the source code.

## Packages

| Package | Purpose |
|---------|---------|
| `Rene.Utils.Db.Abstractions` | Commands, specifications, mapping and unit-of-work contracts |
| `Rene.Utils.Db` | Generic handlers, dependency injection, specification combinators and manual mapping |
| `Rene.Utils.Db.AutoMapper` | Optional AutoMapper integration |
| `Rene.Utils.Db.Mapster` | Optional Mapster integration |

The implementation targets .NET 10. The abstractions target .NET Standard 2.1.
The core library does not require either AutoMapper or Mapster.

## Source and documentation

The [source repository](https://github.com/renepd/DotNet.Rene.Utils.Db) contains the
library, executable samples and tests. These pages describe the checked-in API,
not work pending on other branches. The API reference is regenerated with the site
so it follows the source used for that build.
