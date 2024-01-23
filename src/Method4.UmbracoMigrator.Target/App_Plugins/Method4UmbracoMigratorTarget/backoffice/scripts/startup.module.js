(function () {
    "use strict";
    const umbracoApp = angular.module("umbraco");
    angular.module("migratorTarget", []);
    umbracoApp.requires.push("migratorTarget");
})();