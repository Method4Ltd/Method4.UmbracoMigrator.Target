<img src="./images/UmbracoMigratorTarget_Logo.png" alt="Method4.UmbracoMigrator.Target Logo" title="Method4.UmbracoMigrator.Target Logo" height="130" align="right">

# Method4.UmbracoMigrator.Target - Roadmap

This is a list of outstanding features, that will be actioned as and when we need them.
Feel free to submit a PR if you need them sooner.

---

- Default Mappers
    - Support Language Variants in the default mappers
        - If you need to support variant properties, you will need to implement a custom mapper for now.
- Import - Phase 4
    - Add logic to setup scheduling on nodes that had schedules
- Umbraco 14
    - Add support for the new backoffice
- Backoffice (APP_Plugins)
    - Convert into an RCL (Razor Class Library)
- General
    - Move migration to a background thread to prevent the UI from timing out during a large import.
    - Add some logic to disable the import button whilst an import is in progress.