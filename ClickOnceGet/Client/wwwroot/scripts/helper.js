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
        })(Helper = Client.Helper || (Client.Helper = {}));
    })(Client = ClickOnceGet.Client || (ClickOnceGet.Client = {}));
})(ClickOnceGet || (ClickOnceGet = {}));
