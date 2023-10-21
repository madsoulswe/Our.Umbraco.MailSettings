(function () {
    'use strict';

    function mailSettingsService($http) {

        var apiRoot = Umbraco.Sys.ServerVariables["OurUmbracoMailSettings"]["api"];

        return {
            getSettings: getSettings,
            saveSettings: saveSettings,
            sendTest: sendTest
        }


        function getSettings() {
            return $http({
                method: "GET",
                url: apiRoot + "getsettings"
            });
        }

        function saveSettings(settings) {
            return $http.put(apiRoot + "updatesettings", settings);
        }

        function sendTest(receiver, subject, sender) {
            return $http.post(apiRoot + "SendTest", {
                Sender: sender,
                Receiver: receiver,
                Subject: subject
            });
        }
    };

    angular.module('umbraco').factory('mailSettingsService', mailSettingsService);
})();