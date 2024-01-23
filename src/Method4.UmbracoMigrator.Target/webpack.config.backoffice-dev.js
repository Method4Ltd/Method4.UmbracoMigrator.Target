const path = require("path");
const CopyPlugin = require("copy-webpack-plugin");
const fs = require("fs");
const WebpackShellPluginNext = require("webpack-shell-plugin-next");


// This webpack script is used for copying the App_Plugins folder over to the sample site during development.
// This script is not used during a production build, as the build targets copies the folder over.
// 'npm run build:dev' or 'npm run watch:dev'
module.exports = {
    mode: "development",
    devtool: false,
    plugins: [
        new CopyPlugin({
            patterns: [
                {
                    from: "./App_Plugins/Method4UmbracoMigratorTarget/",
                    to: path.join(__dirname, "../Method4.UmbracoMigrator.Target.SampleSite/App_Plugins/Method4UmbracoMigratorTarget/"),
                    toType: "dir",
                    noErrorOnMissing: true
                }
            ]
        }),
        // This is needed to stop webpack from generating an empty main.js file https://stackoverflow.com/a/69576202
        new WebpackShellPluginNext({
            onBuildEnd: {
                scripts: [
                    () => {
                        fs.unlinkSync("dist/main.js");
                    }
                ]
            }
        })
    ]
};