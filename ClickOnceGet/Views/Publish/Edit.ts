$('*[ng-model]').toArray().map(elem => {
    $(elem).attr('ng-init', $(elem).attr('name') + '= \'' + $(elem).val() + '\'');
});


module Publish {
    var app = angular.module('Publish', []);

    app.filter('charcounter',() => (text: string, max: number) =>
        (max - (text || '').length).toString());

    export class EditController {
        constructor($scope: any) {
            $scope.greeting = { text: 'Hello' };
        }
    }

    app.controller('EditController', EditController);
}