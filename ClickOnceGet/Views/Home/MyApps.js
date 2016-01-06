var ClickOnceGet;
(function (ClickOnceGet) {
    var app = angular.module('Home', ['ngResource', 'ngAnimate']);
    app.controller('MyApps', function ($scope, $resource) {
        var api = $resource('/api/myapps/:id');
        api.query().$promise.then(function (apps) {
            $scope.apps = apps;
        });
        $scope.remove = function (appToDelete) {
            if (confirm('Delete "' + (appToDelete.Title || appToDelete.Name) + '" - Are you sure?') == false)
                return;
            var index = $scope.apps.indexOf(appToDelete);
            api.remove({ id: appToDelete.Name }).$promise
                .then(function () { $scope.apps.splice(index, 1); })
                .catch(function () { alert('Oops... something wrong.'); });
        };
    });
})(ClickOnceGet || (ClickOnceGet = {}));
//# sourceMappingURL=MyApps.js.map