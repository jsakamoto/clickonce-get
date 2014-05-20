var ClickOnceGet;
(function (ClickOnceGet) {
    var app = angular.module('Home', ['ngResource', 'ngAnimate']);

    app.controller('MyApps', function ($scope, $resource) {
        var api = $resource('/api/myapps/:id');

        api.query().$promise.then(function (apps) {
            $scope.apps = apps;
        });

        $scope.remove = function (index) {
            var appToDelete = $scope.apps[index];
            if (confirm('Delete "' + appToDelete.Name + '" - Are you sure?') == false)
                return;
            api.remove({ id: appToDelete.Name }).$promise.then(function () {
                $scope.apps.splice(index, 1);
            }).catch(function () {
                alert('Oops... something wrong.');
            });
        };
    });
})(ClickOnceGet || (ClickOnceGet = {}));
//# sourceMappingURL=MyApps.js.map
