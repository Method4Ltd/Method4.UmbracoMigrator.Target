(function () {
    function migratorService($http, umbRequestHelper) {
        const service = this;
        var serviceRoot = "/umbraco/backoffice/Api/MigratorTargetApi";

        //// Functions ////
        service.getMigrationSnapshots = () => {
            return umbRequestHelper.resourcePromise($http.get(serviceRoot + `/GetMigrationSnapshots`));
        }
        service.beginMigrationImport = (settings) => {
            return umbRequestHelper.resourcePromise($http.post(serviceRoot + `/BeginMigrationImport`, settings));
        }
        service.deleteAllMigrationSnapshots = () => {
            return umbRequestHelper.resourcePromise($http.delete(serviceRoot + `/DeleteAllMigrationSnapshots`));
        }
    }

    angular.module("migratorTarget").service("migratorService", migratorService);
})();