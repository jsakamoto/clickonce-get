module ClickOnceGet {
    var app = angular.module('Home', ['ngResource', 'ngAnimate']);

    interface IScope extends ng.IScope {
        apps: any[];
        remove: any;
    }

    app.controller('MyApps', ($scope: IScope, $resource: ng.resource.IResourceService) => {
        var api = $resource('/api/myapps/:id');

        api.query().$promise.then((apps: any[]) => {
            $scope.apps = apps;
        });

        $scope.remove = (index: number) => {
            var appToDelete = $scope.apps[index];
            if (confirm('Delete "' + appToDelete.Name + '" - Are you sure?') == false) return;
            api.remove({ id: appToDelete.Name }).$promise
                .then(() => { $scope.apps.splice(index, 1); })
                .catch(() => { alert('Oops... something wrong.'); });
        };
    });
} 