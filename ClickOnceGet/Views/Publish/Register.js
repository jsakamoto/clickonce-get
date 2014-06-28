var Publish;
(function (Publish) {
    var app = angular.module('Publish', []);

    app.controller('Register', function ($scope) {
        $scope.greeting = { text: 'Hey!' };
        $scope.zipedPackage = { src: '' };
        $scope.visibleHowToText = false;
        $scope.showHowToText = function () {
            $scope.visibleHowToText = true;
        };
        $scope.file_changed = function (element) {
            $scope.$apply(function () {
                $scope.zipedPackage.src = element.value;
            });
        };
    });
})(Publish || (Publish = {}));
//# sourceMappingURL=Register.js.map
