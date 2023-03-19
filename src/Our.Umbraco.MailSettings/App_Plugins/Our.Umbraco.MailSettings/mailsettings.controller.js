angular.module("umbraco").controller("MailSettings.Controller", function ($scope, userService, mailSettingsService, authResource, notificationsService) {
    var vm = this;

    vm.securityOptions = [
        { id: 0, value: "None" },
        { id: 1, value: "Auto" },
        { id: 2, value: "SSLonconnect" },
        { id: 3, value: "StartTLS" },
        { id: 4, value: "StartTLSwhenavailable" }
    ];

    vm.loading = false;
    vm.savingState = "";
    vm.testState = "";
    vm.currentSettings = {};

    vm.smtpProperties = [
        {
            alias: "from",
            label: "From",
            description: "Specifies the default address emails will be sent from, this setting may be overridden some place",
            hideLabel: false,
            view: "textbox"
        },
        {
            alias: "host",
            label: "Host",
            description: "Address of the SMTP host used to send the email from.",
            hideLabel: false,
            view: "textbox"
        }, {
            alias: "port",
            label: "Port",
            description: "The port of the SMTP host, port 25 is a common port for SMTP.",
            hideLabel: false,
            view: "integer"
        }, {
            alias: "username",
            label: "Username",
            description: "The username used to authenticate with the specified SMTP server, when sending an email.",
            hideLabel: false,
            view: "textbox"
        }, {
            alias: "password",
            description: "The password used to authenticate with the specified SMTP server, when sending an email.",
            label: "Password",
            hideLabel: false,
            view: "textbox"
        }, {
            alias: "secureSocketOptions",
            label: "Security",
            description: "Allows you to specify what security should be used for the connection sending the email.",
            hideLabel: false,
            view: "radiobuttons",
            config: {
                items: vm.securityOptions
            }
        }
    ];

    vm.testProperties = [
        {
            alias: "from",
            label: "From",
            description: "Specifie the sender, will use default if not set",
            hideLabel: false,
            view: "textbox"
        },
        {
            alias: "to",
            label: "To",
            description: "The receiver of the testmail (1 address only)",
            hideLabel: false,
            view: "textbox"
        },
        {
            alias: "subject",
            label: "Subject",
            hideLabel: false,
            view: "textbox"
        }
    ];

    vm.securityOption = ["None", "Auto", "SslOnConnect", "StartTls", "StartTlsWhenAvailable"]

    vm.loadSettings = () => {
        vm.loading = true;

        mailSettingsService.getSettings().then((data) => {
            vm.currentSettings = data.data;
            for (var s in vm.currentSettings) {
                for (var p in vm.smtpProperties) {
                    if (vm.smtpProperties[p].alias === s)
                        vm.smtpProperties[p].value = vm.currentSettings[s];
                }
            }
            vm.loading = false;
        }, () => {
            vm.loading = false;
        });
        
    }

    vm.saveSettings = (saveToCurrent) => {

        vm.savingState = "busy";

        var settings = {};
        for (var p in vm.smtpProperties) {
            settings[vm.smtpProperties[p].alias] = vm.smtpProperties[p].value;
        }

        var updateSettings = { ...vm.currentSettings, ...settings };
        updateSettings.SaveToCurrent = saveToCurrent;

        mailSettingsService.saveSettings(updateSettings).then(() => {
            vm.savingState = "success";
            vm.loadSettings();
        }, () => {
            vm.savingState = "error";
        });
    }

    vm.sendTest = () => {
        vm.testState = "busy";

        var from = vm.testProperties.find(x => x.alias == "from")?.value ?? null;
        var to = vm.testProperties.find(x => x.alias == "to")?.value ?? null;
        var subject = vm.testProperties.find(x => x.alias == "from")?.value ?? null;

        mailSettingsService.sendTest(to, subject, from).then(() => {
            vm.testState = "success";
        }, (error) => {
            vm.testState = "error";
            notificationsService.error(error.data.ExceptionMessage, error.data.StackTrace)
        });
    }

    function init() {
        vm.loadSettings();

        authResource.getCurrentUser().then(response => {
            vm.testProperties.find(x => x.alias == "to").value = response.email;
        });
    }

    init();
});