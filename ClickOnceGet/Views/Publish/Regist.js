var Publish;
(function (Publish) {
    var app = angular.module('Publish', []);

    app.controller('Regist', function ($scope) {
        $scope.greeting = { text: 'Hey!' };
        $scope.zipedPackage = { src: '' };
        $scope.wow = function () {
            alert('WOW!');
        };
        $scope.file_changed = function (element) {
            $scope.$apply(function () {
                $scope.zipedPackage.src = element.value;
            });
        };
    });
})(Publish || (Publish = {}));
//# sourceMappingURL=Regist.js.map
