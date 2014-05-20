module Home {
    var app = angular.module('Home', []);

    app.controller('Index', ($scope: any) => {
        $scope.greeting = { text: 'Hello' };
    });
}
