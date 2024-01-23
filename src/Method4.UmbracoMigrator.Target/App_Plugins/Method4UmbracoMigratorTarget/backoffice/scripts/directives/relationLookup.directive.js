(function () {
    function relationLookup() {
        return {
            restrict: "E",
            controller: ['$scope', 'relationLookupService', 'notificationsService', 'editorService', function relationLookupController($scope, relationLookupService, notificationsService, editorService) {
                const beginImportButton = document.querySelector("#begin-import-button");
                beginImportButton.addEventListener("importComplete", initialise);

                initialise();

                $scope.searchRelations = () => {
                    if ($scope.relationLookup.oldKeyLookup !== null && $scope.relationLookup.oldKeyLookup !== "") {
                        $scope.lookupRelation("key", true, $scope.relationLookup.oldKeyLookup);
                    }
                    else if ($scope.relationLookup.newKeyLookup !== null && $scope.relationLookup.newKeyLookup !== "") {
                        $scope.lookupRelation("key", false, $scope.relationLookup.newKeyLookup);
                    }
                    else if ($scope.relationLookup.oldIdLookup !== null && $scope.relationLookup.oldIdLookup !== "") {
                        $scope.lookupRelation("id", true, $scope.relationLookup.oldIdLookup);
                    }
                    else if ($scope.relationLookup.newIdLookup !== null && $scope.relationLookup.newIdLookup !== "") {
                        $scope.lookupRelation("id", false, $scope.relationLookup.newIdLookup);
                    }
                }

                $scope.lookupRelation = (type, isOld, value) => {
                    $scope.lookupRelationButtonState = 'busy';
                    $scope.relation = null;
                    $scope.relationNotFound = false;

                    if (value === null || value === undefined || value === "") {
                        console.error(type + " is empty");
                        notificationsService.error("😵 ", type + " is empty");
                        $scope.lookupRelationButtonState = 'error';
                        return;
                    }

                    if (type == "key") { // Key Lookup
                        var guidRegex = new RegExp('^[0-9a-f]{8}(-)?[0-9a-f]{4}(-)?[1-5][0-9a-f]{3}(-)?[89ab][0-9a-f]{3}(-)?[0-9a-f]{12}$', 'i');
                        if (guidRegex.test(value) == false) {
                            console.error("Not a valid GUID");
                            notificationsService.error("😵 ", "Not a valid GUID");
                            $scope.lookupRelationButtonState = 'error';
                            return;
                        }

                        if (isOld) {
                            relationLookupService.getRelationByOldKey(value)
                                .then(relationLookupResult)
                                .catch(relationLookupResultError);
                        }
                        else {
                            relationLookupService.getRelationByNewKey(value)
                                .then(relationLookupResult)
                                .catch(relationLookupResultError);
                        }
                    }
                    else if (type == "id") {// ID Lookup
                        if (isOld) {
                            relationLookupService.getRelationByOldId(value)
                                .then(relationLookupResult)
                                .catch(relationLookupResultError);
                        }
                        else {
                            relationLookupService.getRelationByNewId(value)
                                .then(relationLookupResult)
                                .catch(relationLookupResultError);
                        }
                    }
                }

                $scope.deleteAllRelations = () => {
                    $scope.deleteAllRelationsButtonState = 'busy';
                    relationLookupService.deleteAllRelations()
                        .then(function () {
                            notificationsService.success("🗑️ ", "All relations deleted!");
                            $scope.deleteAllRelationsButtonState = 'success';
                            initialise();
                        }).catch(function (error) {
                            console.error(error);
                            notificationsService.error("😵 ", errorMessage);
                            $scope.deleteAllRelationsButtonState = 'error';
                            initialise();
                        });
                }

                $scope.clearInputs = ($event, src) => {
                    if (src === "oldKey") {
                        $scope.relationLookup.newKeyLookup = null;
                        $scope.relationLookup.oldIdLookup = null;
                        $scope.relationLookup.newIdLookup = null;
                    }
                    else if (src === "newKey") {
                        $scope.relationLookup.oldKeyLookup = null;
                        $scope.relationLookup.oldIdLookup = null;
                        $scope.relationLookup.newIdLookup = null;
                    }
                    else if (src === "oldId") {
                        $scope.relationLookup.oldKeyLookup = null;
                        $scope.relationLookup.newKeyLookup = null;
                        $scope.relationLookup.newIdLookup = null;
                    }
                    else if (src === "newId") {
                        $scope.relationLookup.oldKeyLookup = null;
                        $scope.relationLookup.newKeyLookup = null;
                        $scope.relationLookup.oldIdLookup = null;
                    }

                    if ($event.keyCode == 13) {
                        $scope.searchRelations();
                    }
                }

                $scope.openContentEditor = ($event, nodeId) => {
                    var contentEditor = {
                        id: nodeId,
                        submit: function (model) {
                            editorService.close();
                        },
                        close: function () {
                            editorService.close();
                        }
                    };
                    editorService.contentEditor(contentEditor);
                };

                function initialise() {
                    $scope.relationCount = 0;
                    $scope.relation = null;
                    $scope.relationNotFound = false;
                    $scope.relationLookup = {
                        oldKeyLookup: null,
                        newKeyLookup: null,
                        oldIdLookup: null,
                        newIdLookup: null
                    }
                    getRelationCount();
                }

                function relationLookupResult(data) {
                    if (data !== null && data !== undefined) {
                        $scope.relation = data;
                        $scope.relationNotFound = false;
                    }
                    else {
                        $scope.relationNotFound = true;
                    }
                    $scope.lookupRelationButtonState = 'success';
                }
                function relationLookupResultError(error) {
                    console.error(error);
                    notificationsService.error("😵 ", "Failed to retrieve relation, check log for details");
                    $scope.lookupRelationButtonState = 'error';
                    $scope.relation = null;
                }


                function getRelationCount() {
                    relationLookupService.getSavedRelationsCount()
                        .then(function (data) {
                            if (data !== null && data !== undefined) {
                                $scope.relationCount = data;
                            }
                        }).catch(function (error) {
                            console.error(error);
                            notificationsService.error("😵 ", "Failed to retrieve relations count");
                        });
                }
            }],
            templateUrl: `/App_Plugins/Method4UmbracoMigratorTarget/backoffice/views/directives/relationLookup.html`
        }
    }

    angular.module("migratorTarget").directive("relationLookup", relationLookup);
})();