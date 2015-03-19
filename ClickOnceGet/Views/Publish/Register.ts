module Publish {
    var app = angular.module('Publish', []);

    app.controller('Register',($scope: any, $http: ng.IHttpService) => {
        $scope.zipedPackage = { src: '' };
        $scope.visibleHowToText = false;

        $scope.showHowToText = () => {
            $scope.visibleHowToText = true;
        };

        $scope.file_changed = (element) => {
            $scope.$apply(() => {
                $scope.zipedPackage.src = element.value;
            });
        };
    });
}