(function () {
    "use strict";

    function dashboardController($controller, $scope, $timeout, notificationsService,
        migratorService) {
        const vm = this;
        const errorMessage = "Uh oh, something went wrong! Check the log for more details"

        // setup default properties
        //vm.loader = document.querySelector("#migrator-loader");
        vm.settings = {
            DontPublishAfterImport: false,
            OverwriteExistingValues: true,
            DisableAutoMapping: false,
            CleanImport: false,
            ChosenSnapshotName: null
        };
        initialise();

        //// Public Functions ////
        vm.toggleSetting = (settingName) => {
            vm.settings[settingName] = !vm.settings[settingName];
        }

        vm.selectSnapshot = (snapshotName) => {
            if (vm.settings.ChosenSnapshotName === snapshotName) {
                vm.settings.ChosenSnapshotName = null;
            }
            else {
                vm.settings.ChosenSnapshotName = snapshotName;
            }
        }

        vm.beginImport = () => {
            const beginImportButton = document.querySelector("#begin-import-button");
            vm.beginMigrationButtonState = 'busy';
            //vm.loader.hidden = false;

            if (vm.settings.ChosenSnapshotName === null) {
                notificationsService.warning("😨 ", "No migration snapshot chosen");
                vm.beginMigrationButtonState = 'error';
                return;
            }

            migratorService.beginMigrationImport(vm.settings)
                .then(function (data) {
                    console.log("Migration complete!")
                    notificationsService.success("😍 ", "Migration complete!");
                    //vm.loader.hidden = true;
                    vm.beginMigrationButtonState = 'success';
                    if (beginImportButton) {
                        beginImportButton.dispatchEvent(new Event("importComplete"));
                    }
                    initialise();
                }).catch(function (error) {
                    console.error(error);
                    notificationsService.error("😵 ", errorMessage);
                    //vm.loader.hidden = true;
                    vm.beginMigrationButtonState = 'error';
                    if (beginImportButton) {
                        beginImportButton.dispatchEvent(new Event("importComplete"));
                    }
                    initialise();
                });
        }

        vm.deleteAllSnapshots = () => {
            vm.deleteAllSnapshotsButtonState = 'busy';
            migratorService.deleteAllMigrationSnapshots()
                .then(function (data) {
                    notificationsService.success("🗑️ ", "All migration snapshots deleted!");
                    vm.deleteAllSnapshotsButtonState = 'success';
                    initialise();
                }).catch(function (error) {
                    console.error(error);
                    notificationsService.error("😵 ", errorMessage);
                    vm.deleteAllSnapshotsButtonState = 'error';
                    initialise();
                });
        }

        //// Private Functions ////
        function initialise() {
            vm.fileToUpload = null;
            vm.settings.ChosenSnapshotName = null;
            vm.uploadedSnapshots = [];

            getUploadedSnapshots();
        }

        function getUploadedSnapshots() {
            migratorService.getMigrationSnapshots()
                .then(function (data) {
                    if (data !== null && data !== undefined) {
                        for (var i in data) {
                            data[i].CreateDate = new Date(data[i].CreateDate);
                            data[i].SizeBytes = formatBytes(data[i].SizeBytes, 2);
                        }
                    }
                    vm.uploadedSnapshots = data;
                }).catch(function (error) {
                    console.error(error);
                    notificationsService.error("😵 ", "Failed to retrieve uploaded migration snapshots");
                });
        }

        /// formatBytes source: https://gist.github.com/zentala/1e6f72438796d74531803cc3833c039c
        function formatBytes(bytes, decimals) {
            if (bytes == 0) return '0 Bytes';
            var k = 1024,
                dm = decimals || 2,
                sizes = ['Bytes', 'KB', 'MB', 'GB', 'TB', 'PB', 'EB', 'ZB', 'YB'],
                i = Math.floor(Math.log(bytes) / Math.log(k));
            return parseFloat((bytes / Math.pow(k, i)).toFixed(dm)) + ' ' + sizes[i];
        }
    }

    angular.module("migratorTarget").controller("migratorTarget.dashboardController", dashboardController);
})();