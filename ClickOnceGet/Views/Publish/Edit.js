$('*[ng-model]').toArray().map(function (elem) {
    $(elem).attr('ng-init', $(elem).attr('name') + '= \'' + $(elem).val() + '\'');
});
var Publish;
(function (Publish) {
    var app = angular.module('Publish', []);
    app.filter('charcounter', function () { return function (text, max) {
        return (max - (text || '').length).toString();
    }; });
    var EditController = (function () {
        function EditController($scope) {
            $scope.greeting = { text: 'Hello' };
        }
        return EditController;
    })();
    Publish.EditController = EditController;
    app.controller('EditController', EditController);
})(Publish || (Publish = {}));
//# sourceMappingURL=Edit.js.map