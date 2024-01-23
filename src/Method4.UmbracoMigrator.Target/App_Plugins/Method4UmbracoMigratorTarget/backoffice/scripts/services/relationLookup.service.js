(function () {
    function relationLookupService($http, umbRequestHelper) {
        const service = this;
        var serviceRoot = "/umbraco/backoffice/Api/MigratorTargetApi";

        //// Functions ////
        service.getSavedRelationsCount = () => {
            return umbRequestHelper.resourcePromise($http.get(serviceRoot + `/GetRelationsCount`));
        }

        service.getRelationByOldKey = (oldKey) => {
            return umbRequestHelper.resourcePromise($http.post(serviceRoot + `/GetRelationByOldKey?oldKey=${oldKey}`));
        }
        service.getRelationByNewKey = (newKey) => {
            return umbRequestHelper.resourcePromise($http.post(serviceRoot + `/GetRelationByNewKey?newKey=${newKey}`));
        }

        service.getRelationByOldId = (oldId) => {
            return umbRequestHelper.resourcePromise($http.post(serviceRoot + `/GetRelationByOldId?oldId=${oldId}`));
        }
        service.getRelationByNewId = (newId) => {
            return umbRequestHelper.resourcePromise($http.post(serviceRoot + `/GetRelationByNewId?newId=${newId}`));
        }

        service.deleteAllRelations = () => {
            return umbRequestHelper.resourcePromise($http.delete(serviceRoot + `/DeleteAllRelations`));
        }
    }

    angular.module("migratorTarget").service("relationLookupService", relationLookupService);
})();