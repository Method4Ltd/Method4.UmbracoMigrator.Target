<img src="./images/UmbracoMigratorTarget_Logo.png" alt="Method4.UmbracoMigrator.Target Logo" title="Method4.UmbracoMigrator.Target Logo" height="130" align="right">

# Method4.UmbracoMigrator.Target - Contributing

There is a list of outstanding features in /ROADMAP, that will be actioned as and when we need them.
Feel free to submit a PR if you need them sooner.

---

## Getting Started
After you fork and clone down the project, you can run the sample site to test it locally.

This project has been developed using `Node v21.x`.

### Copying the package to the sample site
There is a webpack script in the `/Method4.UmbracoMigrator.Target/src/Method4.UmbracoMigrator.Target/` which will copy the `App_Plugins` folder to the sample site.

```
npm run build:dev
```

And the watch script will copy the folder when it detects that a file has changed.
```
npm run watch:dev
```

Additionally, there is a SignalR script that will build the signalr.js file, this should only need to be run after installing the npm packages, or after updating signalr.
```
npm run build:signalr
```

### Initial setup
On the initial setup, you will need to:
1. Install NPM packages
2. Build the SignalR script
3. Copy the `App_Plugins` folder to the sample site