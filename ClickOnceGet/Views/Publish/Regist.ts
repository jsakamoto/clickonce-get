module Publish {
    var app = angular.module('Publish', []);

    app.controller('Regist', ($scope: any) => {
        $scope.greeting = { text: 'Hey!' };
        $scope.zipedPackage = { src: '' };
        $scope.wow = () => {
            alert('WOW!');
        };
        $scope.file_changed = (element) => {
            $scope.$apply(() => {
                $scope.zipedPackage.src = element.value;
            });
        };
    });
}