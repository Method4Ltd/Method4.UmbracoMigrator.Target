(function () {
    function signalrMigrationStatus() {
        return {
            restrict: "E",
            controller: ['$scope', function signalrMigrationStatusController($scope) {
                $scope.signalrHubConnected = false;

                var connection = new signalR.HubConnectionBuilder().withUrl("/migratorTarget/hub").build();

                connection.on("MigrationStatus", function (phaseNum, message, dateTime) {
                    var li = document.createElement("li");
                    document.getElementById("messagesList").appendChild(li);

                    var messageDate = new Date(dateTime);
                    var messageDateTime = `${messageDate.toLocaleDateString()} ${messageDate.toLocaleTimeString()}`;
                    // We can assign user-supplied strings to an element's textContent because it
                    // is not interpreted as markup. If you're assigning in any other way, you
                    // should be aware of possible script injection concerns.
                    switch (phaseNum) {
                        case -1:
                            li.textContent = `${messageDateTime} | >>>>> ${message} <<<<<`;
                            break;

                        case 0:
                            li.textContent = `${messageDateTime} | ${message}`;
                            break;

                        default:
                            li.textContent = `${messageDateTime} | Phase ${phaseNum} | ${message}`;
                    }
                });

                connection.start().then(function () {
                    $scope.$apply(function () {
                        $scope.signalrHubConnected = true;
                    });
                }).catch(function (err) {
                    return console.error(err.toString());
                });
            }],
            templateUrl: `/App_Plugins/Method4UmbracoMigratorTarget/backoffice/views/directives/signalr-status.html`
        }
    }

    angular.module("migratorTarget").directive("signalrMigrationStatus", signalrMigrationStatus);
})();