const path = require("path");
const CopyPlugin = require("copy-webpack-plugin");
const fs = require("fs");
const WebpackShellPluginNext = require("webpack-shell-plugin-next");

// This webpack script is used to compile the SignalR .js file needed for our App_Plugins.
// This usually only needs to be ran after updating SignalR to a new version.
// 'npm run build:signalr'
module.exports = {
    mode: "development",
    devtool: false,
    plugins: [
        new CopyPlugin({
            patterns: [
                {
                    from: "./node_modules/@microsoft/signalr/dist/browser/signalr.js",
                    to: path.join(__dirname, "/App_Plugins/Method4UmbracoMigratorTarget/backoffice/scripts/libraries/"),
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