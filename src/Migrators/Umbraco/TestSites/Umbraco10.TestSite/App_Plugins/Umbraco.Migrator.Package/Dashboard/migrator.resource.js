angular.module('umbraco.resources')
    .factory('migratorResource', function ($http) {
        return {
            importDocumentTypes() {
                return $http.post("/umbraco/backoffice/api/Migrator/ImportDocumentTypes");
            },
            importData() {
                return $http.post("/umbraco/backoffice/api/Migrator/ImportData");
            },
        }
    })