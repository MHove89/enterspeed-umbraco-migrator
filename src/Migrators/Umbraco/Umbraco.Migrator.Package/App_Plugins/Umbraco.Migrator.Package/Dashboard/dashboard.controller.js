function dashboardMigratorController(notificationsService, migratorResource) {
    let vm = this;
    vm.title = "title";

    vm.importDocumentTypes = importDocumentTypes;
    vm.importData = importData;

    function importDocumentTypes() {
        migratorResource.importDocumentTypes().then(function (result) {
            notificationsService.success("Document types imported");
        }, function (error) {     
            notificationsService.error("Something went wrong when importing  document types: " + error);
        });  
    }  

    function importData() {
        migratorResource.importData().then(function (result) {
            notificationsService.success("Data imported");
        }, function (error) {
            notificationsService.error("Something went wrong when importing data: " + error);
        });
    }
}

angular.module("umbraco").controller("DashboardMigratorController", dashboardMigratorController);