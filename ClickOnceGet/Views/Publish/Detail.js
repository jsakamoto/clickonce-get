var Publish;
(function (Publish) {
    var app = angular.module('Publish', ['ngResource']);
    var DetailController = (function () {
        function DetailController($resource) {
            this.api = $resource('/api/myapps/:id');
        }
        DetailController.prototype.remove = function (appToDelete) {
            if (confirm('Delete "' + (appToDelete.Title || appToDelete.Name) + '" - Are you sure?') == false)
                return;
            var fromOfDetail = window.sessionStorage.getItem('fromOfDetail');
            var goBackUrl = fromOfDetail == 'myApps' ? '/home/MyApps' : '/';
            this.api.remove({ id: appToDelete.Name })
                .$promise
                .then(function () { window.location.href = goBackUrl; })
                .catch(function () { alert('Oops... something wrong.'); });
        };
        return DetailController;
    })();
    Publish.DetailController = DetailController;
    app.controller('DetailController', DetailController);
})(Publish || (Publish = {}));
//# sourceMappingURL=Detail.js.map