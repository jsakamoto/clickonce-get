"use strict";
var ClickOnceGet;
(function (ClickOnceGet) {
    var Client;
    (function (Client) {
        var Helper;
        (function (Helper) {
            function startClickOnce(url) {
                var anchorElement = document.createElement('a');
                anchorElement.href = url;
                anchorElement.click();
                anchorElement.remove();
            }
            Helper.startClickOnce = startClickOnce;
            function getCookie(key) {
                var entry = document.cookie
                    .split(';')
                    .map(function (keyvalue) { return keyvalue.trim().split('='); })
                    .filter(function (keyvalue) { return decodeURIComponent(keyvalue[0]) === key; })
                    .pop();
                if (typeof (entry) === 'undefined')
                    return '';
                return decodeURIComponent(entry[1]);
            }
            Helper.getCookie = getCookie;
            function autofocus() {
                setTimeout(function () {
                    var element = document.querySelector('input[autofocus]');
                    if (element !== null) {
                        element.focus();
                        element.select();
                    }
                }, 100);
            }
            Helper.autofocus = autofocus;
        })(Helper = Client.Helper || (Client.Helper = {}));
    })(Client = ClickOnceGet.Client || (ClickOnceGet.Client = {}));
})(ClickOnceGet || (ClickOnceGet = {}));
